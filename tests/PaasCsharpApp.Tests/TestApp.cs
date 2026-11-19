using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace PaasCsharpApp.Tests;

internal sealed class TestApp(string message, List<SentryEvent> captured) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("SERVER_MESSAGE", message);
        builder.UseSetting("SENTRY_DSN", "http://public@127.0.0.1/1");
        builder.ConfigureServices(services =>
            services.PostConfigure<SentryAspNetCoreOptions>(options =>
            {
                options.SetBeforeSend((sentryEvent, _) =>
                {
                    lock (captured)
                    {
                        captured.Add(sentryEvent);
                    }

                    return sentryEvent;
                });
                options.Transport = new NullTransport();
            }));
    }

    private sealed class NullTransport : ITransport
    {
        public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
