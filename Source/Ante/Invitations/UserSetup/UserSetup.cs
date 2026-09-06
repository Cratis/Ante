// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Ante.Contracts.Legal;
using Ante.Invitations.Accepting;
using Ante.Invitations.Receiving;
using Ante.Legal;
using Cratis.Arc.Validation;

namespace Ante.Invitations.UserSetup;

/// <summary>
/// Represents the current acceptance status for user setup onboarding.
/// </summary>
public enum UserSetupAcceptanceStatus
{
    /// <summary>
    /// The user acceptance confirmation has not been received yet.
    /// </summary>
    Pending,

    /// <summary>
    /// The user acceptance confirmation has been received.
    /// </summary>
    Accepted,

    /// <summary>
    /// The confirmation was not received before the timeout window elapsed.
    /// </summary>
    TimedOut
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
            .WithMessage("Invitation is no longer pending and cannot be used to accept the invitation.");

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
/// (its event source).
/// </remarks>
public class OneUseJoinTenantInvitationConstraint : IConstraint
{
    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) => builder
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
            return ValidationResult.Error("Invitation is no longer pending and cannot be used to accept the invitation.");
        }

        var (identityProvider, complianceSubject) = signedInIdentity.Resolve(InvitationId, (Cratis.Chronicle.Subject)pendingInvitation.Subject);

        // The same person can hold several invitations to one organization - for instance one per email
        // address they were invited under. Onboarding a second one under a login that already belongs
        // to a user there would mint a duplicate user, so ask the organization before committing to the
        // flow.
        if (await identityBackchannel.IsSubjectAlreadyAssociatedWithAUser(pendingInvitation.TenantName, complianceSubject.ToString()))
        {
            return ValidationResult.Error("This login is already associated with a user in the organization. Sign in with it instead of accepting this invitation.");
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
    /// <param name="subscriptions">The user setup status subscriptions.</param>
    /// <param name="legalDocumentSource">The legal document source to resolve authoritative acceptance evidence against.</param>
    /// <returns>The compliance subject the events are appended under, and the events to append.</returns>
    public async Task<Result<ValidationResult, (Cratis.Chronicle.Subject, IEnumerable<object>)>> Handle(
        AcceptingUserIdentity identity,
        PendingInvitationToJoin? pendingInvitation,
        UserSetupStatusSubscriptions subscriptions,
        ILegalDocumentSource legalDocumentSource)
    {
        if (pendingInvitation is null)
        {
            return ValidationResult.Error("Invitation is no longer pending and cannot be used to accept the invitation.");
        }

        // Resolved once, authoritatively, from the host's document source as it reads right now - not
        // trusted from the command payload - so the version recorded as evidence is always the version
        // this very check just confirmed was accepted.
        var legalResolution = await LegalAcceptanceEvidence.Resolve(
            legalDocumentSource,
            AcceptedLegalTerms,
            AcceptedLegalVersion,
            pendingInvitation.TenantName,
            identity.Provider,
            identity.Subject.ToString());
        if (!legalResolution.TryGetResult(out var legalEvents))
        {
            legalResolution.TryGetError(out var legalError);
            return legalError;
        }

        subscriptions.MarkAccepted(InvitationId);

        var events = new List<object>
        {
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
        events.AddRange(legalEvents);

        return (identity.Subject, events);
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
    /// <param name="invitationId">The invitation identifier.</param>
    /// <param name="subscriptions">The subscription tracker.</param>
    /// <returns>An observable status stream for the invitation.</returns>
    public static ISubject<UserSetupAcceptanceStatusView> StatusForInvitation(
        InvitationId invitationId,
        UserSetupStatusSubscriptions subscriptions) =>
        subscriptions.GetStatus(invitationId);
}

/// <summary>
/// Forwards <see cref="InvitationToJoinTenantAccepted"/> to the outbox so the host can subscribe.
/// </summary>
/// <param name="eventStore">The event store.</param>
[Reactor]
public class JoinTenantAcceptanceOutbox(IEventStore eventStore) : IReactor
{
    /// <summary>
    /// Forwards the join-tenant accepted event to the outbox.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task On(InvitationToJoinTenantAccepted @event, EventContext context) =>
        await eventStore.GetEventSequence(EventSequenceId.Outbox)
            .Append(context.EventSourceId, @event);
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
    /// Gets the status stream for an invitation.
    /// </summary>
    /// <param name="invitationId">The invitation identifier.</param>
    /// <returns>An observable status stream.</returns>
    public ISubject<UserSetupAcceptanceStatusView> GetStatus(InvitationId invitationId) =>
        _subscriptions.GetOrAdd(invitationId, static key => new(new(key, UserSetupAcceptanceStatus.Pending)));

    /// <summary>
    /// Marks an invitation as accepted. Ante's own job is done the moment the command handling that
    /// calls this returns - there is no external confirmation to wait for, so acceptance is immediate.
    /// </summary>
    /// <param name="invitationId">The invitation identifier.</param>
    public void MarkAccepted(InvitationId invitationId)
    {
        var subject = _subscriptions.GetOrAdd(invitationId, static key => new(new(key, UserSetupAcceptanceStatus.Pending)));
        _acceptedAt[invitationId] = DateTimeOffset.UtcNow;
        subject.OnNext(new(invitationId, UserSetupAcceptanceStatus.Accepted));
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
