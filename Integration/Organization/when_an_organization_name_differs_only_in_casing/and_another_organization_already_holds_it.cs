// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Ante.Integration.given;
using Ante.Invitations.OrganizationSetup;
using Ante.Organization.Names;
using Microsoft.Extensions.DependencyInjection;

namespace Ante.Integration.Organization.when_an_organization_name_differs_only_in_casing;

/// <summary>
/// The name constraint ignores casing at the kernel: appending the same name in another casing to a different event
/// source is refused by the constraint itself, without any read-model check in front of it. Chronicle client
/// 19.21.x registered the constraint case-sensitive (Cratis/Chronicle#4368), so this appended successfully.
/// </summary>
[Collection(ChronicleCollection.Name)]
public class and_another_organization_already_holds_it : a_running_ante
{
    readonly string _organization = $"Casing-{Guid.NewGuid():N}"[..20];
    IAppendResult _first;
    IAppendResult _differentlyCased;
    IAppendResult _sameEventSourceAgain;

    async Task Because()
    {
        await using var scope = Ante.Services.CreateAsyncScope();
        var eventLog = scope.ServiceProvider.GetRequiredService<IEventStore>().EventLog;

        var holder = Guid.NewGuid().ToString("D");
        _first = await eventLog.Append(holder, new OrganizationNameReservationReceived(_organization));
        _differentlyCased = await eventLog.Append(Guid.NewGuid().ToString("D"), new OrganizationNameReservationReceived(_organization.ToUpperInvariant()));
        _sameEventSourceAgain = await eventLog.Append(holder, new OrganizationNameReservationReceived(_organization.ToUpperInvariant()));
    }

    [Fact] void should_let_the_first_holder_claim_the_name() => _first.IsSuccess.ShouldBeTrue();
    [Fact] void should_refuse_a_different_casing_from_another_organization() => _differentlyCased.IsSuccess.ShouldBeFalse();

    [Fact]
    void should_violate_the_unique_organization_name_constraint() =>
        _differentlyCased.ConstraintViolations.Select(_ => _.ConstraintName.Value).ShouldContainOnly(OrganizationSetupConstraintNames.UniqueOrganizationName);

    [Fact] void should_let_the_holder_restate_its_own_name_in_another_casing() => _sameEventSourceAgain.IsSuccess.ShouldBeTrue();
}
