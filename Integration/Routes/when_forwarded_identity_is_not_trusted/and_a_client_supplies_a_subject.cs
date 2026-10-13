// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Integration.given;
using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_forwarded_identity_is_not_trusted;

[Collection(ChronicleCollection.Name)]
public class and_a_client_supplies_a_subject : a_routed_ante
{
    readonly Guid _registration = Guid.NewGuid();
    Reply _me;
    Reply _begin;
    Reply _complete;
    Reply _fullPrincipal;

    protected override bool Proxied => false;

    protected override IReadOnlyDictionary<string, string?> DeploymentSettings => new Dictionary<string, string?>
    {
        ["Cratis:Arc:TrustForwardedIdentityHeaders"] = "false",
    };

    async Task Because()
    {
        // The fixture configures exactly one provider: it must not turn a raw subject header into an owner.
        var headers = new Dictionary<string, string> { ["x-ms-client-principal-id"] = "victim" };
        _me = await Send(HttpMethod.Get, "/.cratis/me", headers: headers);
        _begin = await Send(HttpMethod.Post, "/api/organization/registration/start", body: new { registrationId = _registration }, headers: headers);
        _complete = await Send(HttpMethod.Post, "/api/organization/registration", body: new { registrationId = _registration, organizationName = $"Forged{Suffix}", firstName = "Grace", lastName = "Hopper", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty }, headers: headers);
        _fullPrincipal = await Send(HttpMethod.Get, "/.cratis/me", "victim");
    }

    [Fact] public void should_reject_the_subject_header_as_identity() => _me.Status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] public void should_not_begin_a_registration_as_the_victim() => _begin.IsSuccess.ShouldBeFalse();
    [Fact] public void should_not_complete_a_registration_as_the_victim() => _complete.IsSuccess.ShouldBeFalse();
    [Fact] public void should_also_reject_a_complete_forwarded_principal() => _fullPrincipal.Status.ShouldEqual(HttpStatusCode.Unauthorized);
}
