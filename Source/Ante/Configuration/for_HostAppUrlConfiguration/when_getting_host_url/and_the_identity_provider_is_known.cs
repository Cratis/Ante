// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.Accepting;
using Microsoft.Extensions.Options;

namespace Ante.Configuration.for_HostAppUrlConfiguration.when_getting_host_url;

public class and_the_identity_provider_is_known : Specification
{
    HostAppUrlConfiguration _result = null!;

    void Because()
    {
        var options = Options.Create(new AnteOptions { HostAppUrl = "https://{tenant}.example.com/" });
        var signedInIdentity = Substitute.For<ISignedInIdentity>();
        signedInIdentity.ResolveProvider().Returns((IdentityProviderName)"GitHub");

        _result = HostAppUrlConfiguration.HostUrl(options, signedInIdentity);
    }

    [Fact] void should_carry_the_configured_host_url() => Assert.Equal("https://{tenant}.example.com/", _result.HostAppUrl);
    [Fact] void should_build_a_provider_specific_sign_in_path() => Assert.Equal("/.cratis/login/github?returnUrl=%2F", _result.SignInPath);
}
#endif
