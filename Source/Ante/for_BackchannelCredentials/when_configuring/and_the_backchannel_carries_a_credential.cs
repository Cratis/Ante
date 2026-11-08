// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Ante.Invitations.HostOutcome;
using Ante.Invitations.UserSetup;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Ante.for_BackchannelCredentials.when_configuring;

public class and_the_backchannel_carries_a_credential : Specification
{
    HttpMessageHandler _identity = null!;
    HttpMessageHandler _outcome = null!;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddHttpClient<IIdentityBackchannel, IdentityBackchannel>().WithoutRedirects();
        services.AddHttpClient<IHostOutcomeBackchannel, HostOutcomeBackchannel>().WithoutRedirects();
        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();
        _identity = PrimaryHandler(provider, monitor, nameof(IIdentityBackchannel));
        _outcome = PrimaryHandler(provider, monitor, nameof(IHostOutcomeBackchannel));
    }

    static HttpMessageHandler PrimaryHandler(IServiceProvider provider, IOptionsMonitor<HttpClientFactoryOptions> monitor, string name)
    {
        var builder = new CapturingBuilder(provider) { Name = name };
        foreach (var action in monitor.Get(name).HttpMessageHandlerBuilderActions)
        {
            action(builder);
        }

        return builder.PrimaryHandler;
    }

    sealed class CapturingBuilder(IServiceProvider services) : HttpMessageHandlerBuilder
    {
        public override string? Name { get; set; }

        public override HttpMessageHandler PrimaryHandler { get; set; } = null!;

        public override IList<DelegatingHandler> AdditionalHandlers { get; } = [];

        public override IServiceProvider Services => services;

        public override HttpMessageHandler Build() => PrimaryHandler;
    }

    [Fact] void should_not_follow_identity_redirects() => Assert.False(Assert.IsType<SocketsHttpHandler>(_identity).AllowAutoRedirect);
    [Fact] void should_not_follow_outcome_redirects() => Assert.False(Assert.IsType<SocketsHttpHandler>(_outcome).AllowAutoRedirect);
}
#endif
