// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Ante.Invitations.Accepting.for_InvitationAttestationVerifier.given;

/// <summary>
/// A completion whose actor is a Cratis Identity account: the provider is Identity, the issuer is Identity's issuer
/// and the subject is the account's GUID in its canonical "D" form.
/// </summary>
public class an_identity_account_assertion : a_signed_assertion
{
    protected const string IdentityIssuer = "https://identity.example.com";
    protected const string AccountSubject = "3f2504e0-4f89-41d3-9a0c-0305e82c3301";

    void Establish()
    {
        _configuration.Attestation.Providers.Add(new InvitationAttestationProvider { Key = "identity", Issuer = IdentityIssuer, AcceptableAssurances = ["mfa"] });
        Complete();
        _claims["provider_key"] = "identity";
        _claims["provider_issuer"] = IdentityIssuer;
        _claims["provider_subject"] = AccountSubject;
    }
}
#endif
