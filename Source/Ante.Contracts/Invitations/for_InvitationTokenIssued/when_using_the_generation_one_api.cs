// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Ante.Contracts.Invitations.for_InvitationTokenIssued;

public class when_using_the_generation_one_api : Specification
{
    InvitationTokenIssued _published = null!;
    InvitationFlowType _flow;
    string _token = null!;
    InvitationTokenIssued _named = null!;
    InvitationFlowType _namedFlow;
    string _namedToken = null!;

    // This is the source shape shipped in Cratis.Ante.Contracts 0.9.0; the
    // public CLR signatures must remain available for already compiled hosts.
#pragma warning disable CS0618
    void Because()
    {
        _published = new InvitationTokenIssued(InvitationFlowType.JoinTenant, "jwt");
        (_flow, _token) = _published;
        _named = new InvitationTokenIssued(FlowType: InvitationFlowType.CreateTenant, Token: "named-jwt");
        _named.Deconstruct(FlowType: out _namedFlow, Token: out _namedToken);
    }
#pragma warning restore CS0618

    [Fact] void should_keep_the_original_constructor_and_deconstructor() => Assert.Equal((InvitationFlowType.JoinTenant, "jwt"), (_flow, _token));
    [Fact] void should_keep_the_original_named_constructor_arguments() => Assert.Equal((InvitationFlowType.CreateTenant, "named-jwt"), (_named.FlowType, _named.Token));
    [Fact] void should_keep_the_original_named_deconstruct_arguments() => Assert.Equal((InvitationFlowType.CreateTenant, "named-jwt"), (_namedFlow, _namedToken));
    [Fact] void should_treat_the_unknown_expiry_as_unusable() => Assert.Equal(DateTimeOffset.UnixEpoch, _published.ExpiresAt);
    [Fact] void should_keep_the_original_binary_signatures()
    {
        Assert.NotNull(typeof(InvitationTokenIssued).GetConstructor([typeof(InvitationFlowType), typeof(string)]));
        Assert.NotNull(typeof(InvitationTokenIssued).GetMethod(nameof(InvitationTokenIssued.Deconstruct), [typeof(InvitationFlowType).MakeByRefType(), typeof(string).MakeByRefType()]));
    }
}
#endif
