using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mahdi.App.E2E;

/// <summary>
/// Publishes the Blazor app once per test run and serves the static output, like Azure Static Web Apps
/// would (static files plus a fallback to index.html). The app is published with the "Test" environment
/// so the <c>?seed=</c> query parameter is honoured.
/// Set MAHDI_E2E_SITE to a published wwwroot folder to skip the publish step.
/// </summary>
[SetUpFixture]
public sealed class AppServer
{
    private static WebApplication? server;

    public static string BaseUrl { get; private set; } = string.Empty;

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        var site = Environment.GetEnvironmentVariable("MAHDI_E2E_SITE") is { Length: > 0 } existing
            ? existing
            : await PublishAsync();

        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        server = builder.Build();
        var files = new PhysicalFileProvider(site);
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".webmanifest"] = "application/manifest+json";
        server.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
        server.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = files,
            ContentTypeProvider = contentTypes,
            ServeUnknownFileTypes = true,
            DefaultContentType = "application/octet-stream",
        });
        server.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = files });

        await server.StartAsync();
        BaseUrl = server.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First()
            .TrimEnd('/') + "/";
        TestContext.Progress.WriteLine($"Serving {site} at {BaseUrl}");
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        if (server is not null)
        {
            await server.DisposeAsync();
        }
    }

    private static async Task<string> PublishAsync()
    {
        var root = FindRepositoryRoot();
        var output = Path.Combine(Path.GetTempPath(), "mahdi-e2e-site");
        var project = Path.Combine(root, "src", "Mahdi.App", "Mahdi.App.csproj");

        var start = new ProcessStartInfo("dotnet",
            ["publish", project, "-c", "Release", "-o", output, "-p:WasmApplicationEnvironmentName=Test", "--nologo"])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start dotnet publish.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet publish failed:\n{await stdout}\n{await stderr}");
        }

        return Path.Combine(output, "wwwroot");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DuneMahdiSolo.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root (DuneMahdiSolo.slnx) not found.");
    }
}
