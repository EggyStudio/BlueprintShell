using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace BlueprintShell;

/// <summary>Visual chrome level rendered around routed content.</summary>
public enum ShellChromeMode
{
    /// <summary>Full shell: header, nav, dock zones, panel headers.</summary>
    Full,

    /// <summary>Minimal chrome: header bar only, no dock splitters or panel headers.</summary>
    Minimal,

    /// <summary>No chrome at all: only cascading services around the routed content.</summary>
    Hidden,
}

/// <summary>How to render dock zones below the mobile breakpoint.</summary>
public enum MobileBehavior
{
    /// <summary>Collapse all dock zones into a single vertical scrolling stack.</summary>
    Stacked,

    /// <summary>Move side dock zones into a slide-out drawer.</summary>
    Drawer,

    /// <summary>Hide non-center dock zones entirely.</summary>
    Hidden,
}

/// <summary>
/// Configuration options for the BlueprintShell server.
/// Pass to <see cref="ShellServerHost.StartAsync"/> or to
/// <see cref="BlueprintShellExtensions.AddBlueprintShell"/> when embedding in an existing app.
/// </summary>
public sealed class BlueprintShellOptions
{
    /// <summary>
    /// Kestrel listen URL.  Supports any format accepted by <c>UseUrls</c>, e.g.:
    /// <list type="bullet">
    ///   <item><c>"http://localhost:5000"</c> (default)</item>
    ///   <item><c>"http://*:5100"</c> - all interfaces on port 5100</item>
    ///   <item><c>"https://localhost:5001;http://localhost:5000"</c> - dual binding</item>
    /// </list>
    /// Only used by <see cref="ShellServerHost.StartAsync"/>; ignored in embedded mode.
    /// </summary>
    public string Url { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Title shown in the shell header and the browser tab.
    /// </summary>
    public string AppTitle { get; set; } = "Blueprint Shell";

    /// <summary>
    /// Open the system browser pointing at <see cref="Url"/> once the server is ready.
    /// Default: <see langword="false"/>.
    /// </summary>
    public bool LaunchBrowser { get; set; } = false;

    /// <summary>
    /// Default chrome level for routes that do not match <see cref="ChromeFor"/>.
    /// Defaults to <see cref="ShellChromeMode.Full"/>.
    /// </summary>
    public ShellChromeMode ChromeMode { get; set; } = ShellChromeMode.Full;

    /// <summary>
    /// Optional per-request chrome selector. Evaluated by the shell layout to override
    /// <see cref="ChromeMode"/> on a route-by-route basis (e.g. <c>Hidden</c> for reader,
    /// <c>Full</c> under <c>/edit/*</c>).
    /// </summary>
    public Func<HttpContext, ShellChromeMode>? ChromeFor { get; set; }

    /// <summary>
    /// DI-aware variant of <see cref="ChromeFor"/>. Receives the scoped <see cref="IServiceProvider"/>
    /// so the callback can resolve services such as <see cref="Shell.IShellAuthContext"/>.
    /// When set, this is evaluated <em>before</em> <see cref="ChromeFor"/>.
    /// </summary>
    public Func<HttpContext, IServiceProvider, ShellChromeMode>? ChromeForServices { get; set; }

    /// <summary>
    /// Viewport width in CSS pixels below which the dock collapses according to
    /// <see cref="MobileBehavior"/>. Defaults to <c>768</c>.
    /// </summary>
    public int MobileBreakpointPx { get; set; } = 768;

    /// <summary>
    /// How the dock layout adapts below <see cref="MobileBreakpointPx"/>.
    /// Defaults to <see cref="MobileBehavior.Stacked"/>.
    /// </summary>
    public MobileBehavior MobileBehavior { get; set; } = MobileBehavior.Stacked;

    /// <summary>
    /// Base path for the shell's static assets. When set, the shell rewrites its CSS imports
    /// to load from this prefix instead of <c>/_content/BlueprintShell</c>.
    /// </summary>
    public string? StaticAssetsBasePath { get; set; }

    /// <summary>
    /// Assemblies scanned for <see cref="Shell.ShellAttribute"/>, <see cref="Shell.PanelAttribute"/>,
    /// <see cref="Shell.ReaderPageAttribute"/>, and <see cref="Shell.GeneratedShellRegistrationAttribute"/>.
    /// When empty (default), every loaded assembly in the current <see cref="AppDomain"/> is scanned.
    /// </summary>
    public IList<Assembly> ScanAssemblies { get; } = new List<Assembly>();

    /// <summary>
    /// Exposes <c>GET /_shell/diagnostics</c> with a JSON dump of registered panels, sources,
    /// active theme, and loaded assemblies. Defaults to <see langword="false"/>.
    /// Recommended: enable only when <c>IHostEnvironment.IsDevelopment()</c>.
    /// </summary>
    public bool EnableDiagnostics { get; set; } = false;

    /// <summary>
    /// SignalR hub path for the shell. Defaults to <c>/shell-hub</c>.
    /// Customise to avoid collisions with other SignalR endpoints.
    /// </summary>
    public string SignalRHubPath { get; set; } = "/shell-hub";

    /// <summary>
    /// Name of the theme preset selected on first render. Falls back to the built-in
    /// <c>"default"</c> preset when null.
    /// </summary>
    public string? ActiveTheme { get; set; }
}
