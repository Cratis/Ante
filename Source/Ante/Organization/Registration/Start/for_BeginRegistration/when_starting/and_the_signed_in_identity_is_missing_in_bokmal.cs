// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using Ante.Invitations;
using Cratis.Arc.Validation;
using Microsoft.AspNetCore.Http;

namespace Ante.Organization.Registration.Start.for_BeginRegistration.when_starting;

public class and_the_signed_in_identity_is_missing_in_bokmal : Specification
{
    Result<ValidationResult, IEnumerable<object>> _result = null!;

    async Task Because()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nb-NO");
            _result = await new BeginRegistration(InvitationId.NotSet).Handle(
                Substitute.For<IHttpContextAccessor>(),
                Substitute.For<Ante.IdentityProviders.IIdentityProviderResolver>(),
                Substitute.For<IEventStore>(),
                Microsoft.Extensions.Options.Options.Create(new AnteOptions()),
                TimeProvider.System);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact] void should_reject_the_start() => _result.TryGetResult(out _).ShouldBeTrue();
    [Fact] void should_explain_the_rejection_in_bokmal()
    {
        _result.TryGetResult(out var rejection);
        Assert.Equal("Du må være logget inn med en identitet og en identitetsleverandør for å registrere en organisasjon.", rejection.Message);
    }
}
#endif
