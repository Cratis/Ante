// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Ante.Contracts.Invitations.for_InvitationTokenIssued;

public class when_using_named_arguments : Specification
{
    static readonly DateTimeOffset _expiry = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    InvitationTokenIssued _published = null!;
    InvitationFlowType _flow;
    string _token = null!;
    DateTimeOffset _expiresAt;

    // Cratis.Ante.Contracts 0.9.0 was a positional record, so hosts may use its
    // PascalCase parameter names as named arguments.
    void Because()
    {
        _published = new InvitationTokenIssued(FlowType: InvitationFlowType.JoinTenant, Token: "jwt", ExpiresAt: _expiry);
        _published.Deconstruct(FlowType: out _flow, Token: out _token, ExpiresAt: out _expiresAt);
    }

    [Fact] void should_construct_from_the_original_parameter_names() => Assert.Equal((InvitationFlowType.JoinTenant, "jwt", _expiry), (_published.FlowType, _published.Token, _published.ExpiresAt));
    [Fact] void should_deconstruct_to_the_original_parameter_names() => Assert.Equal((InvitationFlowType.JoinTenant, "jwt", _expiry), (_flow, _token, _expiresAt));
}
#endif
