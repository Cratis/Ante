// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Contracts.Onboarding;
using Ante.Invitations;
using Ante.Invitations.Accepting;
using Ante.Invitations.HostOutcome;
using Ante.Invitations.OrganizationSetup;
using Ante.Invitations.UserSetup;
using Ante.Organization.Registration;
using Microsoft.Extensions.Options;

namespace Ante.Onboarding.Provisioning;

/// <summary>The durable host provisioning status shown on the completion screens.</summary>
public enum HostProvisioningStatus
{
    /// <summary>Durable outcomes are not required, or the caller may not see this operation.</summary>
    Unknown,

    /// <summary>No verified terminal outcome yet. A silent host stays pending; it never times out into a failure.</summary>
    Pending,

    /// <summary>The host verified that provisioning is durable.</summary>
    Succeeded,

    /// <summary>The host gave up provisioning.</summary>
    Failed,

    /// <summary>The person needs a new invitation.</summary>
    RequiresNewInvitation,
}

/// <summary>Records a verified host start of provisioning; it binds the attempt for the operation.</summary>
/// <param name="AttemptId">The host's attempt.</param>
/// <param name="SourceStore">The configured host store that signed the outcome.</param>
/// <param name="ProofId">The verified proof id.</param>
[EventType]
public record HostProvisioningStartRecorded(HostProvisioningAttemptId AttemptId, string SourceStore, string ProofId);

/// <summary>Records the verified terminal success of host provisioning.</summary>
/// <param name="AttemptId">The host's attempt.</param>
/// <param name="SourceStore">The configured host store that signed the outcome.</param>
/// <param name="ProofId">The verified proof id.</param>
[EventType]
public record HostProvisioningSuccessRecorded(HostProvisioningAttemptId AttemptId, string SourceStore, string ProofId);

/// <summary>Records the verified terminal failure of host provisioning.</summary>
/// <param name="AttemptId">The host's attempt.</param>
/// <param name="SourceStore">The configured host store that signed the outcome.</param>
/// <param name="ReasonCode">The generic reason code.</param>
/// <param name="ProofId">The verified proof id.</param>
[EventType]
public record HostProvisioningFailureRecorded(HostProvisioningAttemptId AttemptId, string SourceStore, HostProvisioningReasonCode ReasonCode, string ProofId);

/// <summary>Records the verified terminal outcome that the person needs a new invitation.</summary>
/// <param name="AttemptId">The host's attempt.</param>
/// <param name="SourceStore">The configured host store that signed the outcome.</param>
/// <param name="ReasonCode">The generic reason code.</param>
/// <param name="ProofId">The verified proof id.</param>
[EventType]
public record HostProvisioningNewInvitationRequirementRecorded(HostProvisioningAttemptId AttemptId, string SourceStore, HostProvisioningReasonCode ReasonCode, string ProofId);

/// <summary>
/// Records a verified terminal outcome that contradicts the first terminal outcome. The result is unchanged; only
/// the contradiction's kind is kept, never its reason code.
/// </summary>
/// <param name="AttemptId">The host's attempt.</param>
/// <param name="SourceStore">The configured host store that signed the outcome.</param>
/// <param name="Outcome">The contradicting outcome.</param>
/// <param name="ProofId">The verified proof id.</param>
[EventType]
public record HostProvisioningOutcomeQuarantined(HostProvisioningAttemptId AttemptId, string SourceStore, HostProvisioningOutcomeKind Outcome, string ProofId);

/// <summary>The durable host provisioning result of an accepted operation; the first terminal outcome wins.</summary>
/// <param name="Id">The invitation or registration id.</param>
/// <param name="Status">The durable status.</param>
/// <param name="ReasonCode">The generic reason code of a terminal failure, mapped by name; null otherwise.</param>
[ReadModel]
[EventLog]
[FromEvent<UserSetupCompleted>]
[FromEvent<OrganizationSetupCompleted>]
[FromEvent<OrganizationRegistrationRecorded>]
[FromEvent<HostProvisioningSuccessRecorded>]
[FromEvent<HostProvisioningFailureRecorded>]
[FromEvent<HostProvisioningNewInvitationRequirementRecorded>]
public record HostProvisioningResult(
    InvitationId Id,
    [SetValue<UserSetupCompleted>(HostProvisioningStatus.Pending)]
    [SetValue<OrganizationSetupCompleted>(HostProvisioningStatus.Pending)]
    [SetValue<OrganizationRegistrationRecorded>(HostProvisioningStatus.Pending)]
    [SetValue<HostProvisioningSuccessRecorded>(HostProvisioningStatus.Succeeded)]
    [SetValue<HostProvisioningFailureRecorded>(HostProvisioningStatus.Failed)]
    [SetValue<HostProvisioningNewInvitationRequirementRecorded>(HostProvisioningStatus.RequiresNewInvitation)] HostProvisioningStatus Status,
    HostProvisioningReasonCode? ReasonCode);

/// <summary>
/// The durable host provisioning status of one operation, answered only to its verified owner.
/// </summary>
/// <param name="OperationId">The invitation or registration id.</param>
/// <param name="IsRequired">Whether this deployment runs with Ante:HostOutcomes:Mode=DurableRequired.</param>
/// <param name="Status">The durable status.</param>
/// <param name="ReasonCode">The generic reason code of a terminal failure; empty otherwise.</param>
[ReadModel]
public record HostProvisioningView(InvitationId OperationId, bool IsRequired, HostProvisioningStatus Status, string ReasonCode)
{
    /// <summary>
    /// Gets the durable status of an operation. Without a recorded terminal outcome it is pending, however long ago
    /// the operation was accepted.
    /// </summary>
    /// <param name="operationId">The invitation or registration id.</param>
    /// <param name="signedInIdentity">The verified sign-in.</param>
    /// <param name="config">The host outcome configuration.</param>
    /// <param name="eventStore">Ante's local store.</param>
    /// <returns>The view.</returns>
    public static async Task<HostProvisioningView> ForOperation(
        InvitationId operationId,
        ISignedInIdentity signedInIdentity,
        IOptions<HostOutcomesConfig> config,
        IEventStore eventStore)
    {
        var isRequired = config.Value.Mode == HostOutcomesMode.DurableRequired;
        if (!isRequired || !await HostOutcomeView.IsOwner(operationId, signedInIdentity, eventStore))
        {
            return new(operationId, isRequired, HostProvisioningStatus.Unknown, string.Empty);
        }

        var result = await eventStore.ReadModels.GetInstanceById<HostProvisioningResult>(operationId.Value);
        return result is null
            ? new(operationId, true, HostProvisioningStatus.Pending, string.Empty)
            : new(operationId, true, result.Status, result.ReasonCode?.Value ?? string.Empty);
    }
}
