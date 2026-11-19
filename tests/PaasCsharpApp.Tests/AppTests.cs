using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace PaasCsharpApp.Tests;

public sealed class AppTests : IDisposable
{
    private readonly List<SentryEvent> _captured = [];
    private readonly TestApp _factory;

    public AppTests()
    {
        _factory = new TestApp("Первая <публикация>", _captured);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task HomeShowsHeadingAndEscapedMessage()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Equal(
            "<h1>Привет от Хекслета!</h1><p>Первая &lt;публикация&gt;</p>\n",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HomeFallsBackToDefaultMessage()
    {
        using var factory = new TestApp(string.Empty, []);
        using var client = factory.CreateClient();

        var body = await client.GetStringAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal("<h1>Привет от Хекслета!</h1><p>Hello, PaaS!</p>\n", body);
    }

    [Fact]
    public async Task ErrorReturns500SendsEventToSentryAndServerKeepsWorking()
    {
        using var client = _factory.CreateClient();

        using var error = await client.GetAsync(new Uri("/error", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);
        Assert.Equal("text/plain; charset=utf-8", error.Content.Headers.ContentType?.ToString());
        Assert.Equal("Oops! Something went wrong!\n", await error.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        SentryEvent captured;
        lock (_captured)
        {
            captured = Assert.Single(_captured);
        }

        Assert.Equal("Oops! Something went wrong!", captured.Exception?.Message);

        using var home = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
    }

    [Fact]
    public async Task UnknownPathReturns404()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/missing", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("text/plain; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Equal("not found\n", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RequestIsLogged()
    {
        using var client = _factory.CreateClient();
        var original = Console.Out;
        using var captured = new StringWriter();
        Console.SetOut(captured);
        try
        {
            using var response = await client.GetAsync(new Uri("/missing", UriKind.Relative), TestContext.Current.CancellationToken);
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.Contains("GET /missing 404", captured.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ListensOnAllInterfaces()
    {
        var urls = _factory.Services.GetRequiredService<IConfiguration>()[WebHostDefaults.ServerUrlsKey];

        Assert.Equal("http://0.0.0.0:3000", urls);
    }
}
