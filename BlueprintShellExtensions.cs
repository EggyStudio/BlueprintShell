using System.Reflection;
using System.Text;
using System.Text.Json;
using BlazorBlueprint.Components;
using BlueprintShell.Hubs;
using BlueprintShell.Shell;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace BlueprintShell;

/// <summary>
/// Extension methods for embedding BlueprintShell inside an <em>existing</em> ASP.NET Core app
/// (as opposed to spawning a dedicated server via <see cref="EditorServerHost.StartAsync"/>).
/// </summary>
/// <example><code>
/// // Program.cs of the host app:
/// builder.Services.AddBlueprintShell(o =>
/// {
///     o.AppTitle = "My Engine Editor";
///     o.ChromeFor = ctx => ctx.Request.Path.StartsWithSegments("/edit")
///         ? ShellChromeMode.Full
///         : ShellChromeMode.Hidden;
/// });
///
/// // …after builder.Build():
/// app.MapBlueprintShell();
/// app.MapBlueprintShellPwa(new() { ShortName = "Encyclopedia" });
/// </code></example>
public static class BlueprintShellExtensions
{
    /// <summary>
    /// Registers all BlueprintShell services: <see cref="BlueprintShellOptions"/>,
    /// <see cref="ShellRegistry"/>, <see cref="ShellState"/>, <see cref="IShellAuthContext"/> (default anonymous),
    /// <see cref="IHttpContextAccessor"/>, BlazorBlueprint, and SignalR.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="configure">Optional delegate to customise <see cref="BlueprintShellOptions"/>.</param>
    public static IServiceCollection AddBlueprintShell(
        this IServiceCollection services,
        Action<BlueprintShellOptions>? configure = null)
    {
        var options = new BlueprintShellOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<ShellRegistry>();
        services.AddSingleton<ShellState>();
        services.AddHttpContextAccessor();
        services.TryAddScoped<IShellAuthContext, AnonymousShellAuthContext>();
        services.AddBlazorBlueprintComponents();
        services.AddSignalR();

        // Discover [EditorPanel] / [ReaderPage] / generated registrations on startup.
        // Idempotent: StaticShellLoader's registered source id is stable per call, so a
        // duplicate invocation (e.g. from MapBlueprintShell) just overwrites itself.
        services.AddHostedService<StaticShellLoaderService>();

        return services;
    }

    /// <summary>
    /// Hosted service that runs <see cref="StaticShellLoader.LoadInto"/> exactly once at
    /// application start, honouring <see cref="BlueprintShellOptions.ScanAssemblies"/>.
    /// </summary>
    private sealed class StaticShellLoaderService : IHostedService
    {
        private readonly ShellRegistry _registry;
        private readonly BlueprintShellOptions _options;

        public StaticShellLoaderService(ShellRegistry registry, BlueprintShellOptions options)
        {
            _registry = registry;
            _options = options;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var assemblies = _options.ScanAssemblies.Count > 0
                ? _options.ScanAssemblies.ToArray()
                : null;
            StaticShellLoader.LoadInto(_registry, assemblies);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>
    /// Maps the BlueprintShell SignalR hub at <see cref="BlueprintShellOptions.SignalRHubPath"/>
    /// (default <c>/shell-hub</c>) and optionally exposes <c>/_shell/diagnostics</c>.
    /// Call this on the <see cref="WebApplication"/> after <c>Build()</c>.
    /// </summary>
    public static WebApplication MapBlueprintShell(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<BlueprintShellOptions>();
        app.MapHub<ShellHub>(options.SignalRHubPath);

        if (options.EnableDiagnostics)
            MapDiagnostics(app);

        return app;
    }

    /// <summary>
    /// Generates a PWA manifest and a small service worker on the fly and maps them at the
    /// configured paths. Adds a hint header so consumers can detect that the shell is serving
    /// its own PWA scaffolding.
    /// </summary>
    public static WebApplication MapBlueprintShellPwa(this WebApplication app, BlueprintShellPwaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var shellOptions = app.Services.GetRequiredService<BlueprintShellOptions>();
        var hubPath = shellOptions.SignalRHubPath;

        app.MapGet(options.ManifestPath, (HttpContext ctx) =>
        {
            ctx.Response.ContentType = "application/manifest+json";
            return Results.Content(BuildManifest(options), "application/manifest+json", Encoding.UTF8);
        });

        app.MapGet(options.ServiceWorkerPath, (HttpContext ctx) =>
        {
            ctx.Response.ContentType = "application/javascript";
            ctx.Response.Headers["Service-Worker-Allowed"] = "/";
            return Results.Content(BuildServiceWorker(options, hubPath), "application/javascript", Encoding.UTF8);
        });

        if (options.RenderRegistrationScript)
        {
            app.MapGet("/manifest.bootstrap.js", (HttpContext ctx) =>
            {
                ctx.Response.ContentType = "application/javascript";
                return Results.Content(BuildRegistrationScript(options), "application/javascript", Encoding.UTF8);
            });
        }

        return app;
    }

    private static readonly JsonSerializerOptions DiagnosticsJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
    };

    private static void MapDiagnostics(WebApplication app)
    {
        app.MapGet("/_shell/diagnostics", (ShellRegistry registry) =>
        {
            var snapshot = registry.Current;
            var payload = new
            {
                version = registry.Version,
                activeTheme = registry.ActiveTheme,
                themes = snapshot.Themes.Keys.ToArray(),
                panels = snapshot.Panels.Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    zone = p.DefaultZone.ToString(),
                    route = p.Route,
                    requiresRole = p.RequiresRole,
                    visible = p.Visible,
                    closeable = p.Closeable,
                    component = p.ComponentType?.FullName,
                    widget = string.IsNullOrEmpty(p.WidgetKey) ? null : p.WidgetKey,
                }),
                readerPages = snapshot.ReaderPages.Select(r => new
                {
                    id = r.Id,
                    route = r.Route,
                    title = r.Title,
                    requiresRole = r.RequiresRole,
                    chrome = r.Chrome?.ToString(),
                    useBlazorRouter = r.UseBlazorRouter,
                    component = r.ComponentType.FullName,
                }),
                assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .Select(a => a.GetName().Name)
                    .OrderBy(n => n)
                    .ToArray(),
            };

            return Results.Json(payload, DiagnosticsJsonOptions);
        });
    }

    private static string BuildManifest(BlueprintShellPwaOptions o)
    {
        var icons = o.Icons.Count > 0
            ? o.Icons.Select(i => new { src = i.Src, sizes = i.Sizes, type = i.Type, purpose = i.Purpose }).Cast<object>().ToArray()
            : new object[]
            {
                new { src = "/_content/BlueprintShell/icon-512.png", sizes = "512x512", type = "image/png", purpose = (string?)"any" },
            };

        var doc = new
        {
            name = o.Name ?? o.ShortName,
            short_name = o.ShortName,
            start_url = o.StartUrl,
            display = o.DisplayMode,
            theme_color = o.ThemeColor,
            background_color = o.BackgroundColor,
            icons,
        };

        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string BuildServiceWorker(BlueprintShellPwaOptions o, string hubPath)
    {
        var precache = new List<string> { o.StartUrl };
        if (o.CacheStaticAssets)
        {
            precache.AddRange(new[]
            {
                "/_content/BlueprintShell/css/app.css",
                "/_content/BlueprintShell/styles/themes/default.css",
                "/_content/BlazorBlueprint.Components/blazorblueprint.css",
            });
        }

        precache.AddRange(o.ExtraCacheUrls);

        var excludes = new HashSet<string>(o.ExcludePathPrefixes, StringComparer.Ordinal)
        {
            "/_blazor",
            "/_framework",
        };
        if (!string.IsNullOrEmpty(hubPath)) excludes.Add(hubPath);

        var precacheJson = JsonSerializer.Serialize(precache);
        var excludesJson = JsonSerializer.Serialize(excludes.ToArray());

        return $$"""
            const CACHE = 'blueprintshell-v1';
            const PRECACHE = {{precacheJson}};
            const EXCLUDE = {{excludesJson}};
            self.addEventListener('install', e => {
                e.waitUntil(caches.open(CACHE).then(c => c.addAll(PRECACHE)).then(() => self.skipWaiting()));
            });
            self.addEventListener('activate', e => {
                e.waitUntil(caches.keys().then(keys =>
                    Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k)))
                ).then(() => self.clients.claim()));
            });
            self.addEventListener('fetch', e => {
                if (e.request.method !== 'GET') return;
                const url = new URL(e.request.url);
                if (url.origin !== self.location.origin) return;
                for (const p of EXCLUDE) { if (url.pathname.startsWith(p)) return; }
                e.respondWith(
                    caches.match(e.request).then(hit =>
                        hit || fetch(e.request).then(res => {
                            const copy = res.clone();
                            if (res.ok) caches.open(CACHE).then(c => c.put(e.request, copy));
                            return res;
                        }).catch(() => caches.match(PRECACHE[0]))
                    )
                );
            });
            """;
    }

    private static string BuildRegistrationScript(BlueprintShellPwaOptions o)
    {
        var swPath = JsonSerializer.Serialize(o.ServiceWorkerPath);
        return $$"""
            if ('serviceWorker' in navigator) {
                window.addEventListener('load', () => {
                    navigator.serviceWorker.register({{swPath}}).catch(err =>
                        console.warn('[BlueprintShell] SW registration failed:', err));
                });
            }
            """;
    }
}
