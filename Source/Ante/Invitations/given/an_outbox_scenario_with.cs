// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Serialization;

namespace Ante.Invitations.given;

/// <summary>
/// An in-process scenario whose event sequence is the outbox, with only the constraint <typeparamref name="TConstraint"/>.
/// </summary>
/// <remarks>
/// Ante's constraints apply to the event log, so an append to the outbox is not validated against them: the outbox
/// carries copies of facts the event log has already validated.
/// </remarks>
/// <typeparam name="TConstraint">The constraint to define.</typeparam>
public class an_outbox_scenario_with<TConstraint> : Specification, IDisposable
    where TConstraint : IConstraint, new()
{
    protected EventScenario Scenario = null!;

    void Establish() => Scenario = new EventScenario(EventSequenceId.Outbox, "test-event-store", "default", new ConstraintDefinitions());

    public void Dispose() => Scenario.Dispose();

    sealed class ConstraintDefinitions : ICanProvideConstraints
    {
        public IImmutableList<IConstraintDefinition> Provide()
        {
            var builder = new ConstraintBuilder(Defaults.Instance.EventTypes, new CamelCaseNamingPolicy());
            new TConstraint().Define(builder);
            return builder.Build();
        }
    }
}
#endif
