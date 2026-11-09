// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations;
using Ante.Invitations.Accepting;
using Microsoft.Extensions.Options;

namespace Ante.Onboarding.Provisioning.for_HostProvisioningView.given;

public abstract class an_operation : Specification
{
    protected static readonly InvitationId _operationId = InvitationId.New();
    protected IEventStore _eventStore = null!;
    protected IReadModels _readModels = null!;
    protected ISignedInIdentity _signedInIdentity = null!;
    protected HostOutcomesMode _mode = HostOutcomesMode.DurableRequired;
    protected HostProvisioningResult? _recorded;
    protected HostProvisioningView _result = null!;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _readModels = Substitute.For<IReadModels>();
        _eventStore.ReadModels.Returns(_readModels);
        _readModels.GetInstanceById<HostProvisioningResult>(Arg.Any<ReadModelKey>(), Arg.Any<ReadModelSessionId?>())
            .Returns(_ => Task.FromResult(_recorded!));
        _signedInIdentity = Substitute.For<ISignedInIdentity>();
        _signedInIdentity.IsVerifiedRecoveryOwnerOf(_operationId, _eventStore).Returns(true);
    }

    protected async Task Ask() =>
        _result = await HostProvisioningView.ForOperation(_operationId, _signedInIdentity, Options.Create(new HostOutcomesConfig { Mode = _mode }), _eventStore);
}
#endif
