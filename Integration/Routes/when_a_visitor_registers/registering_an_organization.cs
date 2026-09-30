// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Ante.Contracts.Organization;
using Ante.Integration.given;
using Ante.Integration.Routes.given;

namespace Ante.Integration.Routes.when_a_visitor_registers;

/// <summary>
/// Self-service registration needs a signed-in visitor at every step: an anonymous caller is refused, and a registration
/// id started by one visitor cannot be submitted or read by another. Only the visitor who started it can complete it and
/// see its status. Shared by every cell of the route matrix.
/// </summary>
public abstract class registering_an_organization : a_routed_ante
{
    const int Pending = 0;
    const int Accepted = 2;
    const string Start = "/api/organization/registration/start";
    const string Register = "/api/organization/registration";

    readonly Guid _registration = Guid.NewGuid();
    string _visitor;
    string _stranger;
    string _organization;
    Reply _startByAnonymous;
    Reply _startByVisitor;
    Reply _registerByAnonymous;
    Reply _registerByStranger;
    Reply _registerByVisitor;
    Reply _registerAgain;
    Reply _statusBeforeToVisitor;
    Reply _statusBeforeToStranger;
    Reply _statusToVisitor;
    Reply _statusToStranger;
    Reply _statusToAnonymous;
    OrganizationRegistrationCompleted _published;

    string Status => $"/api/invitations/organization-setup/status-for-registration?registrationId={_registration:D}";

    async Task Because()
    {
        _visitor = $"visitor-{Suffix}";
        _stranger = $"stranger-{Suffix}";
        _organization = $"Self{Suffix}";

        _startByAnonymous = await Send(HttpMethod.Post, Start, body: new { registrationId = _registration });
        _registerByAnonymous = await Send(HttpMethod.Post, Register, body: RegisterCommand());
        _startByVisitor = await Send(HttpMethod.Post, Start, _visitor, body: new { registrationId = _registration });
        _registerByStranger = await Send(HttpMethod.Post, Register, _stranger, body: RegisterCommand());
        _statusBeforeToVisitor = await Send(HttpMethod.Get, Status, _visitor);
        _statusBeforeToStranger = await Send(HttpMethod.Get, Status, _stranger);

        _registerByVisitor = await Send(HttpMethod.Post, Register, _visitor, body: RegisterCommand());
        _published = await Host.WaitForFromAnte<OrganizationRegistrationCompleted>(_registration.ToString());
        await Eventually.Until(async () => StatusOf((_statusToVisitor = await Send(HttpMethod.Get, Status, _visitor)).Json) == Accepted, what: "the owner's registration status");
        _statusToStranger = await Send(HttpMethod.Get, Status, _stranger);
        _statusToAnonymous = await Send(HttpMethod.Get, Status);
        _registerAgain = await Send(HttpMethod.Post, Register, _visitor, body: RegisterCommand());
    }

    [Fact] public void should_refuse_an_anonymous_visitor_starting_a_registration() => IsRefused(_startByAnonymous).ShouldBeTrue();
    [Fact] public void should_let_a_signed_in_visitor_start_a_registration() => IsAccepted(_startByVisitor).ShouldBeTrue();
    [Fact] public void should_refuse_an_anonymous_visitor_submitting_the_registration() => IsRefused(_registerByAnonymous).ShouldBeTrue();
    [Fact] public void should_refuse_another_visitor_submitting_the_registration_the_visitor_started() => IsRefused(_registerByStranger).ShouldBeTrue();
    [Fact] public void should_let_the_visitor_who_started_it_submit_the_registration() => IsAccepted(_registerByVisitor).ShouldBeTrue();
    [Fact] public void should_refuse_submitting_the_same_registration_twice() => IsRefused(_registerAgain).ShouldBeTrue();
    [Fact] public void should_publish_the_registration_under_the_visitors_subject() => _published.Subject.ShouldEqual(_visitor);
    [Fact] public void should_publish_the_registration_under_the_configured_provider() => _published.IdentityProvider.Value.ShouldEqual(AnteApplication.IdentityProvider);

    [Fact] public void should_report_pending_to_the_visitor_before_submitting() => StatusOf(_statusBeforeToVisitor.Json).ShouldEqual(Pending);
    [Fact] public void should_report_pending_to_another_visitor_before_submitting() => StatusOf(_statusBeforeToStranger.Json).ShouldEqual(Pending);
    [Fact] public void should_report_accepted_and_the_organization_to_the_visitor_afterwards() => _statusToVisitor.Data.GetProperty("organizationName").GetString().ShouldEqual(_organization);
    [Fact] public void should_report_pending_and_no_organization_to_another_visitor_afterwards() => StatusOf(_statusToStranger.Json).ShouldEqual(Pending);
    [Fact] public void should_not_tell_another_visitor_the_organization_name() => _statusToStranger.Data.GetProperty("organizationName").GetString().ShouldEqual(string.Empty);
    [Fact] public void should_report_pending_and_no_organization_to_an_anonymous_visitor_afterwards() => StatusOf(_statusToAnonymous.Json).ShouldEqual(Pending);
    [Fact] public void should_not_tell_an_anonymous_visitor_the_organization_name() => _statusToAnonymous.Data.GetProperty("organizationName").GetString().ShouldEqual(string.Empty);

    static bool IsRefused(Reply reply) => reply.Status == HttpStatusCode.BadRequest && !reply.IsSuccess;

    static bool IsAccepted(Reply reply) => reply.IsOk && reply.IsSuccess;

    object RegisterCommand() => new { registrationId = _registration, organizationName = _organization, firstName = "Grace", lastName = "Hopper", acceptedLegalTerms = false, acceptedLegalVersion = string.Empty };
}
