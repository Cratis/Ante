// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Ante.Outbox;
using Ante.Resources;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Types;

namespace Ante.Invitations.UserSetup;

/// <summary>
/// Represents the current acceptance status for user setup onboarding.
/// </summary>
public enum UserSetupAcceptanceStatus
{
    /// <summary>
    /// Acceptance has not been recorded yet - there is nothing to resume, so a client may safely
    /// (re)submit.
    /// </summary>
    Pending,

    /// <summary>
    /// Acceptance has been recorded to Ante's own event log but has not yet fully reached the outbox. A
    /// client observing this must keep waiting rather than resubmitting: resubmitting would collide with
    /// the one-use invitation constraint.
    /// </summary>
    Recorded,

    /// <summary>
    /// Acceptance - and any required legal fact - has fully reached the outbox. This is the only state
    /// safe to hand off to the host.
    /// </summary>
    Accepted,
}

/// <summary>
/// The names of the constraints guarding user setup.
/// </summary>
public static class UserSetupConstraintNames
{
    /// <summary>
    /// The constraint keeping a join-tenant invitation acceptable only once.
    /// </summary>
    public const string OneUseInvitation = "OneUseJoinTenantInvitation";
}

/// <summary>
/// Validator for the <see cref="AcceptInvitation"/> command.
/// </summary>
public class AcceptInvitationValidator : CommandValidator<AcceptInvitation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptInvitationValidator"/> class.
    /// </summary>
    /// <param name="legalDocumentSource">The legal document source the invited user has to accept, when the host has configured one.</param>
    /// <param name="signedInIdentity">The identity the user is signed in with for this request.</param>
    public AcceptInvitationValidator(ILegalDocumentSource legalDocumentSource, ISignedInIdentity signedInIdentity)
    {
        // The invitation id travels in the open - a URL, a token, an invite link - so knowing it must
        // never be enough to act on it. Only a caller who has verifiably exchanged this exact invitation
        // may accept it; the same message the pending-invitation check below uses, so a mismatched owner
        // is indistinguishable from an invitation that is no longer pending.
        RuleFor(c => c.InvitationId)
            .Must(invitationId => signedInIdentity.IsVerifiedOwnerOf(invitationId))
            .WithMessage(_ => Messages.Get("AcceptNotPending"));

        RuleFor(c => (string)c.FirstName).MustBeARequiredName("First name");
        RuleFor(c => (string)c.LastName).MustBeARequiredName("Last name");

        // Compares against null and casts rather than coalescing against MiddleName.NotSet, so the
        // selector never reads a static member of the concept type itself - a shape ARC0013 cannot tell
        // apart from dereferencing a possibly-null concept (false positive: Cratis/Arc#2658). The lambda
        // also can't yield a member name FluentValidation can infer, so OverridePropertyName pins the
        // failure to the field the wizard actually renders instead of leaving it unattributed.
        RuleFor(c => c.MiddleName == null ? string.Empty : (string)c.MiddleName)
            .MustBeAValidName("Middle name")
            .OverridePropertyName(nameof(AcceptInvitation.MiddleName));

        LegalTermsRules.Apply(this, legalDocumentSource, c => c.AcceptedLegalTerms, c => c.AcceptedLegalVersion);
    }
}

/// <summary>
/// Enforces that a join-tenant invitation can be accepted at most once.
/// </summary>
/// <remarks>
/// <see cref="AcceptInvitation"/> treats an already-accepted invitation as no longer pending by reading
/// <see cref="PendingInvitationToJoin"/>, which is removed once accepted - but that is a read-model
/// check and races: two concurrent submits of the same invitation can both observe it as still pending
/// before either append lands. This constraint is the atomic backstop, enforced by the kernel at append
/// time - only one <see cref="InvitationToJoinTenantAccepted"/> can ever be appended per invitation
/// (its event source). It applies to the event log only: the outbox holds one forwarded copy per delivery, so it
/// needs no index of its own.
/// </remarks>
public class OneUseJoinTenantInvitationConstraint : IConstraint
{
    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) => builder
        .ForEventLog()
        .Unique<InvitationToJoinTenantAccepted>(
            "This invitation has already been accepted.",
            UserSetupConstraintNames.OneUseInvitation);
}

/// <summary>
/// The identity the invited user signed in with, resolved and cleared for onboarding.
/// </summary>
/// <param name="Provider">The identity provider the user authenticated with.</param>
/// <param name="Subject">The compliance subject the events are appended under.</param>
public record AcceptingUserIdentity(IdentityProviderName Provider, Cratis.Chronicle.Subject Subject);

/// <summary>
/// Command for accepting an invitation to join an existing tenant.
/// </summary>
/// <param name="InvitationId">The invitation this user setup is for.</param>
/// <param name="FirstName">The first name of the user being onboarded.</param>
/// <param name="MiddleName">The middle name of the user being onboarded.</param>
/// <param name="LastName">The last name of the user being onboarded.</param>
/// <param name="AcceptedLegalTerms">Whether the user has accepted the terms and conditions and the privacy policy.</param>
/// <param name="AcceptedLegalVersion">The version of the legal document set that was presented and accepted.</param>
[Command]
public record AcceptInvitation(InvitationId InvitationId, FirstName FirstName, MiddleName? MiddleName, LastName LastName, bool AcceptedLegalTerms, LegalVersion AcceptedLegalVersion)
{
    /// <summary>
    /// Resolves the identity the user signed in with and clears it for onboarding into the organization.
    /// </summary>
    /// <param name="signedInIdentity">The identity the user is signed in with for this request.</param>
    /// <param name="pendingInvitation">The current state of the pending invitation, resolved from the Chronicle projection.</param>
    /// <param name="identityBackchannel">The backchannel used to ask the organization whether the identity is already taken.</param>
    /// <returns>
    /// A <see cref="Result{T0, T1}"/> containing either the resolved identity or a failed <see cref="ValidationResult"/>.
    /// </returns>
    public async Task<Result<AcceptingUserIdentity, ValidationResult>> Provide(
        ISignedInIdentity signedInIdentity,
        PendingInvitationToJoin? pendingInvitation,
        IIdentityBackchannel identityBackchannel)
    {
        if (pendingInvitation is null)
        {
            return ValidationResult.Error(Messages.Get("AcceptNotPending"));
        }

        var (identityProvider, complianceSubject) = signedInIdentity.Resolve(InvitationId, (Cratis.Chronicle.Subject)pendingInvitation.Subject);
        if (string.IsNullOrWhiteSpace(identityProvider.Value))
        {
            return ValidationResult.Error(Messages.Get("AcceptIdentityRequired"));
        }

        // The same person can hold several invitations to one organization - for instance one per email
        // address they were invited under. Onboarding a second one under a login that already belongs
        // to a user there would mint a duplicate user, so ask the organization before committing to the
        // flow.
        if (await identityBackchannel.IsSubjectAlreadyAssociatedWithAUser(pendingInvitation.TenantName, complianceSubject.ToString()))
        {
            return ValidationResult.Error(Messages.Get("LoginAlreadyAssociated"));
        }

        return new AcceptingUserIdentity(identityProvider, complianceSubject);
    }

    /// <summary>
    /// Handles the command by producing an <see cref="InvitationToJoinTenantAccepted"/> event and,
    /// when the host has a legal document source configured, the <see cref="LegalTermsAccepted"/>
    /// record of what the invited user agreed to.
    /// </summary>
    /// <param name="identity">The identity resolved for the accepting user.</param>
    /// <param name="pendingInvitation">The current state of the pending invitation, resolved from the Chronicle projection.</param>
    /// <param name="existingSetup">Durable organization setup evidence from a reused id predating the one-use marker.</param>
    /// <param name="legalDocumentSource">The legal document source to resolve authoritative acceptance evidence against.</param>
    /// <param name="acceptanceFence">The authoritative invitation revision used to fence revocation.</param>
    /// <param name="signedInIdentity">The actor whose live attested session owns this acceptance.</param>
    /// <param name="eventStore">The local event store for composing the atomic append.</param>
    /// <returns>The compliance subject and events fenced against revocation and legal activation.</returns>
    /// <remarks>
    /// Does not mark the invitation as accepted here - that would be a pre-append success signal, visible
    /// to a polling client before the event this method returns has even been appended, let alone
    /// forwarded to the outbox. <see cref="JoinTenantAcceptanceOutbox"/> marks it once the acceptance is
    /// verifiably durable in Ante's own outbox instead.
    /// </remarks>
    public async Task<Result<ValidationResult, (Cratis.Chronicle.Subject, EventsWithConcurrencyScopes)>> Handle(
        AcceptingUserIdentity identity,
        PendingInvitationToJoin? pendingInvitation,
        OrganizationSetupProgress? existingSetup,
        ILegalDocumentSource legalDocumentSource,
        IInvitationAcceptanceFence acceptanceFence,
        ISignedInIdentity signedInIdentity,
        IEventStore eventStore)
    {
        if (pendingInvitation is null || existingSetup is not null)
        {
            return ValidationResult.Error(Messages.Get("AcceptNotPending"));
        }

        // Resolved once, authoritatively, from the host's document source as it reads right now - not
        // trusted from the command payload - so the version recorded as evidence is always the version
        // this very check just confirmed was accepted.
        var legalResolution = await LegalAcceptanceEvidence.ResolveWithScope(
            legalDocumentSource,
            AcceptedLegalTerms,
            AcceptedLegalVersion,
            pendingInvitation.TenantName,
            identity.Provider,
            identity.Subject.ToString());
        if (!legalResolution.TryGetResult(out var legalEvidence))
        {
            legalResolution.TryGetError(out var legalError);
            return legalError;
        }

        if (!signedInIdentity.IsVerifiedOwnerOf(InvitationId))
        {
            return ValidationResult.Error(Messages.Get("AcceptNotPending"));
        }

        var owner = signedInIdentity.AttestedOwnerOf(InvitationId);
        if (signedInIdentity.IsAttestedExchange && owner is null)
        {
            return ValidationResult.Error(Messages.Get("AcceptNotPending"));
        }

        var events = new List<object>
        {
            new OnboardingAttemptClaimed(),
            new InvitationToJoinTenantAccepted(
                pendingInvitation.TenantName,
                identity.Provider,
                identity.Subject.ToString(),
                FirstName,
                MiddleName ?? Contracts.Invitations.MiddleName.NotSet,
                LastName,
                pendingInvitation.Email,
                pendingInvitation.Roles),
        };
        if (owner is not null)
        {
            events.Add(owner);
        }

        events.AddRange(legalEvidence.Events);

        var scope = await acceptanceFence.For(InvitationId, InvitationFlowType.JoinTenant);
        if (scope is null)
        {
            return ValidationResult.Error(Messages.Get("AcceptNotPending"));
        }

        return (identity.Subject, await LegalAcceptanceEvidence.ForAppend(eventStore, InvitationId, events, legalEvidence, scope));
    }
}

/// <summary>
/// Represents the current user setup acceptance status.
/// </summary>
/// <param name="InvitationId">The invitation identifier.</param>
/// <param name="Status">The acceptance status.</param>
[ReadModel]
public record UserSetupAcceptanceStatusView(InvitationId InvitationId, UserSetupAcceptanceStatus Status)
{
    /// <summary>
    /// Gets the user setup acceptance status for a specific invitation.
    /// </summary>
    /// <remarks>
    /// Durable evidence from the local record and the outbox is read first - from the authoritative event
    /// log and outbox when the read models lag them - so a re-entering user -
    /// new tab, restarted Ante, or a dropped connection reconnecting to a different replica - resumes
    /// into <see cref="UserSetupAcceptanceStatus.Recorded"/> or <see cref="UserSetupAcceptanceStatus.Accepted"/>
    /// precisely once durable evidence supports it, never from an in-memory flag alone. A client that
    /// only ever sees Pending has never actually recorded anything and may safely (re)submit; one that
    /// sees Recorded has already submitted and must keep waiting rather than resubmitting, even if the
    /// local browser tab restarted in between.
    /// </remarks>
    /// <param name="invitationId">The invitation identifier.</param>
    /// <param name="signedInIdentity">Verifier of invitation ownership.</param>
    /// <param name="subscriptions">The subscription tracker.</param>
    /// <param name="facts">Resolves publication progress from the read models and, when they lag, the authoritative log and outbox.</param>
    /// <param name="eventStore">The scoped store used to release the committed owner's identity.</param>
    /// <returns>An observable status stream for the invitation.</returns>
    public static ISubject<UserSetupAcceptanceStatusView> StatusForInvitation(
        InvitationId invitationId,
        ISignedInIdentity signedInIdentity,
        UserSetupStatusSubscriptions subscriptions,
        IJoinTenantPublicationFacts facts,
        IEventStore eventStore)
    {
        if (!signedInIdentity.IsVerifiedRecoveryOwnerOf(invitationId, eventStore))
        {
            return new BehaviorSubject<UserSetupAcceptanceStatusView>(new(invitationId, UserSetupAcceptanceStatus.Pending));
        }

        var progress = facts.Resolve(invitationId).GetAwaiter().GetResult();
        var status = subscriptions.GetStatus(
            invitationId,
            isRecorded: progress != PublicationProgress.None,
            isFullyPublished: progress == PublicationProgress.Published);
        if (!signedInIdentity.IsAttestedExchange)
        {
            return status;
        }

        var actor = signedInIdentity.CaptureRecoveryActor();
        return actor is null
            ? new BehaviorSubject<UserSetupAcceptanceStatusView>(new(invitationId, UserSetupAcceptanceStatus.Pending))
            : new InvitationStatusOwnerFilter<UserSetupAcceptanceStatusView>(
                status, invitationId, actor, eventStore, view => view.Status == UserSetupAcceptanceStatus.Pending);
    }
}

/// <summary>
/// Forwards <see cref="InvitationToJoinTenantAccepted"/> to the outbox so the host can subscribe.
/// </summary>
/// <remarks>
/// Pinned to the event log deliberately - see the remarks on <c language="csharp">LegalTermsAcceptanceOutbox</c>
/// for why an unattributed reactor handling a <c language="csharp">Cratis.Ante.Contracts</c> event is unsafe to route once a
/// deployment renames its store away from the compiled "Ante" literal.
/// </remarks>
/// <param name="eventStore">The event store.</param>
/// <param name="notifiers">Every registered <see cref="IPublicationStatusNotifier"/>, given a chance to accelerate a live status subscription once this fact is durably published.</param>
/// <param name="logger">The logger.</param>
[Reactor(eventSequence: EventSequenceId.LogId)]
public class JoinTenantAcceptanceOutbox(IEventStore eventStore, IInstancesOf<IPublicationStatusNotifier> notifiers, ILogger<JoinTenantAcceptanceOutbox> logger) : IReactor
{
    /// <summary>
    /// Forwards the join-tenant accepted event to the outbox.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    /// <param name="delivery">The delivery being handled; a redelivery of it does not publish the fact again.</param>
    public async Task On(InvitationToJoinTenantAccepted @event, EventContext context, ReactorDelivery delivery) =>
        await eventStore.PublishToOutbox(delivery, context, @event, notifiers, logger, announceRecorded: true);
}

/// <summary>
/// Tracks user setup acceptance status streams per invitation.
/// </summary>
public class UserSetupStatusSubscriptions : IDisposable
{
    static readonly TimeSpan _defaultEntryRetention = TimeSpan.FromMinutes(2);

    readonly ConcurrentDictionary<InvitationId, BehaviorSubject<UserSetupAcceptanceStatusView>> _subscriptions = new();
    readonly Timer _cleanupTimer;
    readonly ConcurrentDictionary<InvitationId, DateTimeOffset> _acceptedAt = new();
    readonly TimeSpan _entryRetention;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSetupStatusSubscriptions"/> class.
    /// </summary>
    /// <param name="cleanupInterval">Optional cleanup interval override.</param>
    /// <param name="entryRetention">Optional terminal entry retention override.</param>
    public UserSetupStatusSubscriptions(TimeSpan? cleanupInterval = null, TimeSpan? entryRetention = null)
    {
        _entryRetention = entryRetention ?? _defaultEntryRetention;

        var effectiveCleanupInterval = cleanupInterval ?? TimeSpan.FromSeconds(30);
        _cleanupTimer = new(_ => Cleanup(), null, effectiveCleanupInterval, effectiveCleanupInterval);
    }

    /// <summary>
    /// Gets the status stream for an invitation, seeded from durable evidence so a re-entering client
    /// never observes a stale value.
    /// </summary>
    /// <remarks>
    /// Seeding keeps re-entry idempotent: a fresh in-memory entry otherwise starts out
    /// <see cref="UserSetupAcceptanceStatus.Pending"/>, so without the durable check a user returning
    /// after a restart - or reconnecting to a different replica - would see a stale pending state even
    /// though acceptance was already recorded or fully published. An already-live subject is only ever
    /// moved forward (Pending -&gt; Recorded -&gt; Accepted), never backward, so a slow reconnect's
    /// durable read cannot regress a state another tab watching the same subject has already observed.
    /// </remarks>
    /// <param name="invitationId">The invitation identifier.</param>
    /// <param name="isRecorded">Whether durable evidence confirms acceptance has been recorded.</param>
    /// <param name="isFullyPublished">Whether durable evidence already confirms full publication.</param>
    /// <returns>An observable status stream.</returns>
    public ISubject<UserSetupAcceptanceStatusView> GetStatus(InvitationId invitationId, bool isRecorded = false, bool isFullyPublished = false)
    {
        var subject = GetOrAdd(invitationId);

        if (isFullyPublished)
        {
            MarkAccepted(invitationId);
        }
        else if (isRecorded)
        {
            // MarkRecorded never regresses an already-Accepted subject: a stale read of the recorded
            // collection racing behind a durable publication another caller already observed must not
            // un-accept a subject a different tab is watching right now.
            MarkRecorded(invitationId);
        }

        return subject;
    }

    /// <summary>
    /// Marks an invitation as recorded. Called once durable evidence confirms acceptance has committed to
    /// Ante's own event log, so a subscriber already waiting on this subject learns it must not resubmit,
    /// without needing to reconnect first.
    /// </summary>
    /// <remarks>
    /// Never moves a subject that is already <see cref="UserSetupAcceptanceStatus.Accepted"/> back, and does
    /// not emit again for one that is already <see cref="UserSetupAcceptanceStatus.Recorded"/>.
    /// </remarks>
    /// <param name="invitationId">The invitation identifier.</param>
    public void MarkRecorded(InvitationId invitationId) => MarkRecorded(GetOrAdd(invitationId), invitationId);

    /// <summary>
    /// Marks an invitation as recorded only when a subscription for it is already open on this replica. A
    /// subscription that opens later is seeded from durable facts, so creating an entry nobody watches
    /// would only leave one behind for every replayed acceptance.
    /// </summary>
    /// <param name="invitationId">The invitation identifier.</param>
    public void MarkRecordedIfWatched(InvitationId invitationId)
    {
        if (_subscriptions.TryGetValue(invitationId, out var subject))
        {
            MarkRecorded(subject, invitationId);
        }
    }

    /// <summary>
    /// Marks an invitation as accepted. Called only once durable evidence - both the local record and the
    /// outbox - confirms the acceptance (and any required legal fact) has actually been published, never
    /// from the command handling that accepted it: that would be a pre-append success signal, visible
    /// before the event even exists in the log, let alone the outbox.
    /// </summary>
    /// <param name="invitationId">The invitation identifier.</param>
    public void MarkAccepted(InvitationId invitationId)
    {
        var subject = GetOrAdd(invitationId);
        _acceptedAt[invitationId] = DateTimeOffset.UtcNow;
        lock (subject)
        {
            subject.OnNext(new(invitationId, UserSetupAcceptanceStatus.Accepted));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cleanupTimer.Dispose();
        foreach (var subscription in _subscriptions.Values)
        {
            subscription.OnCompleted();
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }

    static void MarkRecorded(BehaviorSubject<UserSetupAcceptanceStatusView> subject, InvitationId invitationId)
    {
        // Held per subject so a concurrent MarkAccepted cannot slip in between the check and the push and
        // then be overtaken by a Recorded that no longer applies.
        lock (subject)
        {
            // Already Recorded is a no-op too - a redelivered or replayed acceptance, or a re-seeding read,
            // has nothing new to tell a subscriber that already knows.
            if (subject.Value.Status != UserSetupAcceptanceStatus.Pending)
            {
                return;
            }

            subject.OnNext(new(invitationId, UserSetupAcceptanceStatus.Recorded));
        }
    }

    BehaviorSubject<UserSetupAcceptanceStatusView> GetOrAdd(InvitationId invitationId) =>
        _subscriptions.GetOrAdd(invitationId, static key => new(new(key, UserSetupAcceptanceStatus.Pending)));

    void Cleanup()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (invitationId, acceptedAt) in _acceptedAt)
        {
            if (now - acceptedAt < _entryRetention)
            {
                continue;
            }

            if (_subscriptions.TryRemove(invitationId, out var subject))
            {
                subject.OnCompleted();
                subject.Dispose();
            }

            _acceptedAt.TryRemove(invitationId, out _);
        }
    }
}
