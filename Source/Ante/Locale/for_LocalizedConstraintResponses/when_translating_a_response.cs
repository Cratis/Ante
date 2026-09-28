// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using System.Text.Json.Nodes;

namespace Ante.Locale.for_LocalizedConstraintResponses;

public class when_translating_a_response : Specification
{
    [Theory]
    [InlineData("UniqueOrganizationName", "En organisasjon med dette navnet finnes allerede.")]
    [InlineData("OneUseJoinTenantInvitation", "Invitasjonen er allerede godtatt.")]
    [InlineData("OneUseCreateTenantInvitation", "Invitasjonen er allerede godtatt.")]
    [InlineData("OneUseOnboardingAttempt", "Dette oppsettet er allerede sendt inn.")]
    public void should_localize_the_known_constraint_without_changing_its_identity(string name, string expected)
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nb-NO");
            var response = $"{{\"validationResults\":[{{\"reason\":\"constraintViolation\",\"reasonDetail\":\"{name}\",\"message\":\"Original\"}}]}}";
            var result = JsonNode.Parse(LocalizedConstraintResponses.Translate(response))!;
            Assert.Equal(expected, result["validationResults"]![0]!["message"]!.GetValue<string>());
            Assert.Equal(name, result["validationResults"]![0]!["reasonDetail"]!.GetValue<string>());
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public void should_localize_a_concurrency_violation_as_a_retryable_message()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nb-NO");
            const string response = "{\"validationResults\":[{\"reason\":\"concurrencyViolation\",\"message\":\"Expected sequence number 4 but was 5\"}]}";
            var result = JsonNode.Parse(LocalizedConstraintResponses.Translate(response))!;
            Assert.Equal("Noe ble endret mens du sendte inn, for eksempel vilkårene. Se over siden og send inn på nytt.", result["validationResults"]![0]!["message"]!.GetValue<string>());
            Assert.Equal("concurrencyViolation", result["validationResults"]![0]!["reason"]!.GetValue<string>());
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public void should_preserve_unknown_validation_errors()
    {
        const string response = "{\"validationResults\":[{\"reason\":\"rule\",\"reasonDetail\":\"UniqueOrganizationName\",\"message\":\"Original\"}]}";
        Assert.Equal(response, LocalizedConstraintResponses.Translate(response));
    }
}
#endif
