using System.Net;
using Sentry;

const string heading = "Привет от Хекслета!";

var builder = WebApplication.CreateSlimBuilder(args);

// Пустое значение приравнивается к незаданному.
string Setting(string name, string fallback)
{
    var value = builder.Configuration[name];
    return string.IsNullOrEmpty(value) ? fallback : value;
}

var port = Setting("PORT", "3000");
var message = Setting("SERVER_MESSAGE", "Hello, PaaS!");

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
// Без SENTRY_DSN SDK ничего не отправляет.
builder.WebHost.UseSentry(options => options.Dsn = builder.Configuration["SENTRY_DSN"] ?? string.Empty);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

var app = builder.Build();

app.Use(async (context, next) =>
{
    await next(context).ConfigureAwait(false);
    Console.WriteLine($"{context.Request.Method} {context.Request.Path} {context.Response.StatusCode}");
});

app.MapGet("/", () => Results.Content(
    $"<h1>{heading}</h1><p>{WebUtility.HtmlEncode(message)}</p>\n",
    "text/html; charset=utf-8"));

app.MapGet("/error", (IHub hub) =>
{
    const string text = "Oops! Something went wrong!";
    try
    {
        throw new InvalidOperationException(text);
    }
    catch (InvalidOperationException error)
    {
        hub.CaptureException(error);
    }

    return Results.Text($"{text}\n", "text/plain; charset=utf-8", statusCode: 500);
});

app.MapFallback(() => Results.Text("not found\n", "text/plain; charset=utf-8", statusCode: 404));

app.Run();

// Нужен тестам: WebApplicationFactory<Program> запускает приложение целиком.
internal partial class Program;
