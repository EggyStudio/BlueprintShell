using BlazorBlueprint.Components;
using BlueprintShell.Hubs;
using BlueprintShell.Shell;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace BlueprintShell;

/// <summary>
/// Configures and starts the BlueprintShell Blazor application on its own Kestrel server.
/// Suitable for use from a game engine, desktop app, or any .NET host that wants an
/// embedded editor UI on a dedicated port.
/// </summary>
/// <example><code>
/// var shell = await EditorServerHost.StartAsync(new BlueprintShellOptions
/// {
///     Url      = "http://localhost:5100",
///     AppTitle = "My Editor",
/// });
/// // ... later:
/// await shell.StopAsync();
/// </code></example>
public static class EditorServerHost
{
    /// <summary>
    /// Builds and starts the shell Blazor Server.
    /// Returns the running <see cref="WebApplication"/> so the caller can stop it later.
    /// </summary>
    /// <param name="options">Shell configuration (URL, title, etc.). Defaults are used when null.</param>
    /// <param name="args">Command-line args forwarded to the web host builder.</param>
    /// <param name="registry">
    /// Optional externally-owned <see cref="ShellRegistry"/>. Pass the same instance from the
    /// engine host to share state. A new registry is created when null.
    /// </param>
    public static async Task<WebApplication> StartAsync(
        BlueprintShellOptions? options = null,
        string[]? args = null,
        ShellRegistry? registry = null)
    {
        options ??= new BlueprintShellOptions();

        // ApplicationName must point to this assembly so the static web-assets pipeline
        // (wwwroot, _content/, CSS) resolves correctly even when hosted in-process from
        // a different executable.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args           = args ?? [],
            ApplicationName = typeof(EditorServerHost).Assembly.GetName().Name!,
        });

        builder.WebHost.UseUrls(options.Url);

        // Serve the library's wwwroot/ content at the root path even in non-Development
        // environments (e.g. when embedded inside the engine process).
        builder.WebHost.UseStaticWebAssets();

        var shellRegistry = registry ?? new ShellRegistry();
        var staticCount   = StaticShellLoader.LoadInto(shellRegistry, options.ScanAssemblies.Count > 0 ? options.ScanAssemblies.ToArray() : null);
        Console.WriteLine($"[BlueprintShell] Static shell registrations: {staticCount}");

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(shellRegistry);
        builder.Services.AddSingleton<ShellState>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.TryAddScoped<IShellAuthContext, AnonymousShellAuthContext>();

        builder.Services
            .AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddBlazorBlueprintComponents();
        builder.Services.AddSignalR();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        app.MapHub<ShellHub>(options.SignalRHubPath);

        if (options.EnableDiagnostics)
        {
            app.MapGet("/_shell/diagnostics", (ShellRegistry r) => Results.Json(new
            {
                version = r.Version,
                activeTheme = r.ActiveTheme,
                themes = r.Current.Themes.Keys,
                panels = r.Current.Panels.Select(p => new { p.Id, p.Title, zone = p.DefaultZone.ToString(), p.Route, p.RequiresRole }),
                readerPages = r.Current.ReaderPages.Select(rp => new { rp.Id, rp.Route, rp.Title, rp.RequiresRole }),
            }));
        }

        await app.StartAsync();
        return app;
    }
}
