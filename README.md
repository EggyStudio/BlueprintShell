<div align="center">
  <img src="https://raw.githubusercontent.com/EggyStudio/BlueprintShell/main/.github/assets/logo.png" alt="logo" />
</div>

<h1 align="center" style="text-align:center">BlueprintShell</h1>
<h4 align="center" style="text-align:center">
Embeddable editor shell built on BlazorBlueprint.<br>Dockable panels, reader pages, theming, auth-aware chrome, and PWA - all from a single NuGet.
</h4>
<p align="center" style="text-align:center">
Drop BlueprintShell into any .NET app and get a themed UI on a dedicated port, or mount it inside your existing ASP.NET Core pipeline.
<br><em>
Designed for engine editors, internal tools, and content sites that mix a public reader with an authenticated editor.
</em></p>

<p align="center" style="text-align:center">
  <img alt="Platform" src="https://img.shields.io/badge/Platform-.NET%2010-512BD4">
  <img alt="UI" src="https://img.shields.io/badge/UI-Blazor%20Server-5C2D91">
  <img alt="Components" src="https://img.shields.io/badge/Components-BlazorBlueprint-0EA5E9">
  <img alt="Realtime" src="https://img.shields.io/badge/Realtime-SignalR-FF6F61">
  <a href="LICENSE"><img alt="License: MPL 2.0" src="https://img.shields.io/badge/License-MPL%202.0-blue.svg"></a>
  <a href="https://github.com/EggyStudio/BlueprintShell"><img alt="GitHub" src="https://img.shields.io/badge/GitHub-EggyStudio%2FBlueprintShell-181717"></a>
</p>

## Quick Installation

```bash
dotnet add package BlueprintShell
```

<!-- TOC -->

- [Quick Installation](#quick-installation)
- [Usage](#usage)
- [Configuration - `BlueprintShellOptions`](#configuration---blueprintshelloptions)
- [Writing panels](#writing-panels)
- [Writing shell builders](#writing-shell-builders)
- [Reader pages](#reader-pages)
- [Auth-aware chrome](#auth-aware-chrome)
- [DI-aware chrome resolution](#di-aware-chrome-resolution)
- [Header slots](#header-slots)
- [Theme presets](#theme-presets)
- [PWA](#pwa)
- [Diagnostics](#diagnostics)
- [Multiple shell sources](#multiple-shell-sources)
- [Dialogs / popovers (`BbPortalHost`)](#dialogs--popovers-bbportalhost)
- [CSS / theming](#css--theming)
- [Project Structure](#project-structure)
- [License](#license)

## Usage

### Standalone mode - dedicated port

Spin up a separate Kestrel server inside your existing process. The shell runs independently and doesn't interfere with your app's own HTTP stack.

```csharp
using BlueprintShell;

// Start the shell server (non-blocking).
var shell = await EditorServerHost.StartAsync(new BlueprintShellOptions
{
    Url      = "http://localhost:5100",
    AppTitle = "My Engine Editor",
});

// ... run your loop ...

// Shut down cleanly when done.
await shell.StopAsync();
```

Open `http://localhost:5100` in a browser to see the shell.

### Embedded mode - same ASP.NET Core app

If your host is already an ASP.NET Core app, register the shell services and map its hub into your existing pipeline:

```csharp
// Program.cs
builder.Services.AddBlueprintShell(o =>
{
    o.AppTitle = "My Editor";
});

// After builder.Build():
app.MapBlueprintShell();   // maps /shell-hub SignalR endpoint
```

Then add the shell's `App`, `Routes`, and `ShellLayout` into your Razor routing as needed.

## Configuration - `BlueprintShellOptions`

| Property | Type | Default | Description |
|---|---|---|---|
| `Url` | `string` | `"http://localhost:5000"` | Kestrel listen URL. Accepts any format supported by `UseUrls`, e.g. `"http://*:5100"` or `"https://localhost:5001;http://localhost:5000"`. Only used in standalone mode. |
| `AppTitle` | `string` | `"Blueprint Shell"` | Title shown in the shell header and browser tab. |
| `LaunchBrowser` | `bool` | `false` | Open the system browser pointing at `Url` once the server is ready. |
| `ChromeMode` | `ShellChromeMode` | `Full` | Default chrome level: `Full` (dock + header), `Minimal` (header only), `Hidden` (nothing - just shell services around `@Body`). |
| `ChromeFor` | `Func<HttpContext, ShellChromeMode>?` | `null` | Per-request override; evaluated before `ChromeMode`. |
| `ChromeForServices` | `Func<HttpContext, IServiceProvider, ShellChromeMode>?` | `null` | DI-aware variant of `ChromeFor`; evaluated **first** when set. |
| `MobileBreakpointPx` | `int` | `768` | Viewport width below which the dock collapses. |
| `MobileBehavior` | `MobileBehavior` | `Stacked` | `Stacked`, `Drawer`, or `Hidden` below the mobile breakpoint. |
| `StaticAssetsBasePath` | `string?` | `null` | Override the `/_content/BlueprintShell` prefix used by the shell's CSS imports. |
| `ScanAssemblies` | `IList<Assembly>` | _all loaded_ | Explicit assemblies to scan for `[EditorShell]`, `[EditorPanel]`, `[ReaderPage]`. Empty (default) scans the whole AppDomain. |
| `EnableDiagnostics` | `bool` | `false` | Expose `GET /_shell/diagnostics` (recommended only in Development). |
| `SignalRHubPath` | `string` | `"/shell-hub"` | Customise to avoid collisions with other SignalR endpoints. |
| `ActiveTheme` | `string?` | `null` | Name of the initial theme preset (see [Theme presets](#theme-presets)). |

### Per-request chrome (anonymous reader vs. logged-in editor)

```csharp
builder.Services.AddBlueprintShell(o =>
{
    o.AppTitle = "Encyclopedia";
    o.ChromeFor = ctx => ctx.Request.Path.StartsWithSegments("/edit")
        ? ShellChromeMode.Full
        : ShellChromeMode.Hidden;
});
```

## Writing panels

Decorate any Blazor component with `[EditorPanel]` to register it as a dockable panel. The attribute is discovered at startup by `StaticShellLoader` via reflection.

```razor
@* MyPanel.razor *@
@attribute [EditorPanel("my-panel", "My Panel", DockZone.Right,
    Icon    = "settings",
    Route   = "/my-panel",
    Visible = true)]

@inject ShellRegistry Registry

<div class="p-4">
    <h3 class="text-sm font-semibold">Hello from My Panel</h3>
</div>
```

| Parameter | Description |
|---|---|
| `id` | Unique panel key (used for tab-grouping and persistence). |
| `title` | Display name in the panel header / tab. |
| `zone` | Default dock position: `Top`, `Left`, `Right`, `Bottom`, `Center`, `Float`. |
| `Icon` | Optional Lucide icon name shown in the header. |
| `Route` | When set, a nav-link to this panel appears in the shell header **and** the catch-all router serves the panel at the given URL. Route parameters (e.g. `"/edit/article/{Identifier}"`) are forwarded to matching `[Parameter]` properties - same convention as `@page`. |
| `TabGroup` | Groups this panel as a tab inside another panel. |
| `TabOrder` | Sort order within a tab group. |
| `InitialSize` | Fraction (0-1) of the parent dock area. Default `0.25`. |
| `Closeable` | Whether the user can close the panel. Default `true`. |
| `Visible` | Whether the panel starts visible. Default `true`. |
| `RequiresRole` | Filters the panel out for requests where `IShellAuthContext.Roles` doesn't contain the value (`"*"` means any authenticated user). |

## Writing shell builders

For code-driven layout (no Razor), implement `IEditorShellBuilder` and decorate with `[EditorShell]`:

```csharp
using BlueprintShell.Shell;

[EditorShell]
public class MyShell : IEditorShellBuilder
{
    public int Order => 0;   // lower runs first

    public void Build(IShellBuilder shell)
    {
        shell.Panel("toolbar", "Toolbar", DockZone.Top, p => p
            .Content(c => c
                .Row(null, row => row
                    .Button(null, "Save",  onClick: OnSave,  icon: "save")
                    .Button(null, "Build", onClick: OnBuild, icon: "hammer", variant: "secondary")
                    .Spacer()
                    .DarkModeToggle())));
    }

    private static void OnSave()  { /* ... */ }
    private static void OnBuild() { /* ... */ }
}
```

Multiple builders coexist: `Order` controls execution order; higher-precedence sources (dynamic hot-reload) override lower ones on panel-id collisions.

## Reader pages

Reader pages are routed components that bypass the dock layout - useful for full-bleed content (articles, marketing pages) hosted alongside an editor.

```razor
@* Article.razor *@
@attribute [ReaderPage("article", "/wiki/{Identifier}",
    Chrome = ShellChromeMode.Hidden)]

@code {
    [Parameter] public string? Identifier { get; set; }
}
```

The shell resolves the route via a catch-all router and forwards segments as `[Parameter]` properties on the component (same convention as `@page`).

### Incremental adoption (`UseBlazorRouter = true`)

If you already have a catalog of `@page`-routed components and don't want to migrate them wholesale, set `UseBlazorRouter = true`. The shell records the entry in `ShellRegistry` (so it shows up in diagnostics and participates in role tracking) but the catch-all router skips it - your existing Blazor `Router` continues to handle the URL. `RequiresRole` becomes advisory in this mode: the shell can't gate a request it doesn't render, so enforce the role check inside the component or via your own routing middleware.

```razor
@page "/wiki/{Identifier}"
@attribute [ReaderPage("article", "/wiki/{Identifier}", UseBlazorRouter = true)]
```

## Auth-aware chrome

Implement `IShellAuthContext` and the shell will filter `[EditorPanel(..., RequiresRole = "...")]` panels and `[ReaderPage(..., RequiresRole = "...")]` pages automatically. Use `"*"` to require any authenticated user.

```csharp
public sealed class MyAuthContext : IShellAuthContext
{
    public bool IsAuthenticated => /* ... */;
    public string? UserId => /* ... */;
    public IReadOnlySet<string> Roles => /* ... */;
}

builder.Services.AddScoped<IShellAuthContext, MyAuthContext>();
```

## DI-aware chrome resolution

`ChromeFor` runs without access to scoped services. When you need to resolve `IShellAuthContext` (or anything else from DI) to pick chrome, use `ChromeForServices` instead - it receives the scoped `IServiceProvider` and is evaluated before `ChromeFor`.

```csharp
o.ChromeForServices = (ctx, sp) =>
{
    var auth = sp.GetRequiredService<IShellAuthContext>();
    return auth.IsAuthenticated ? ShellChromeMode.Full : ShellChromeMode.Hidden;
};
```

## Header slots

Drop your own content into the shell header (e.g. a global search box):

```csharp
registry.RegisterHeaderSlot(builder =>
{
    builder.OpenComponent<GlobalSearchBox>(0);
    builder.CloseComponent();
});
```

## Theme presets

```csharp
registry.RegisterTheme("reader", new ThemePreset
{
    Variables =
    {
        ["--background"] = "oklch(1 0 0)",
        ["--foreground"] = "oklch(0.15 0 0)",
        ["--font-sans"]  = "\"Source Serif Pro\", Georgia, serif",
    },
});

registry.SetActiveTheme("reader");
```

The active preset is rendered as `<style id="bp-active-theme">` and replaced when `SetActiveTheme` is called.

## PWA

```csharp
app.MapBlueprintShellPwa(new BlueprintShellPwaOptions
{
    ShortName        = "Encyclopedia",
    ThemeColor       = "#0a0a0a",
    BackgroundColor  = "#ffffff",
    CacheStaticAssets = true,
});
```

Serves a manifest at `/manifest.webmanifest` and a service worker at `/sw.js`, precaching the shell's own CSS and any URLs you add via `ExtraCacheUrls`.

The generated service worker skips `/_blazor`, `/_framework`, and the configured SignalR hub path by default (caching them breaks Blazor Server reconnects after deploy). Add more prefixes via `BlueprintShellPwaOptions.ExcludePathPrefixes`.

To register the service worker on the client, either drop the helper component into your layout / `App.razor`:

```razor
<BlueprintShellPwaBootstrap />
```

...or set `RenderRegistrationScript = false` and emit the `navigator.serviceWorker.register('/sw.js')` call yourself.

## Diagnostics

Set `o.EnableDiagnostics = true` and `GET /_shell/diagnostics` returns a JSON dump of panels, reader pages, themes, and loaded assemblies - useful when something doesn't render and you want to know whether it was discovered. All fields are camelCase.

## Multiple shell sources

`ShellRegistry` supports multiple named sources that are merged at runtime. This lets an engine contribute a static source at startup and a hot-reload compiler contribute a dynamic source later:

```csharp
var registry = new ShellRegistry();

// Static source - built-in panels:
registry.RegisterSource("static", new ShellSource
{
    Builders   = new[] { new MyShell() },
    Precedence = 0,
});

// Dynamic source - runtime-compiled panels override static ones on id collision:
registry.RegisterSource("dynamic", new ShellSource
{
    PanelComponents = discoveredPanels,
    Precedence      = 100,
});

var shell = await EditorServerHost.StartAsync(registry: registry);
```

## Dialogs / popovers (`BbPortalHost`)

BlazorBlueprint renders dialogs, popovers, tooltips, and toasts into a portal host - your layout must include `<BbPortalHost />` (from `BlazorBlueprint.Primitives.Services`, **not** `BlazorBlueprint.Primitives`) for those primitives to appear. All three of the shell's built-in layouts (`ShellLayout`, `MinimalLayout`, `ReaderLayout`, in `BlueprintShell.Shared.Layouts`) already include it; consumers writing their own layout need to add it themselves.

> The built-in layouts live in `BlueprintShell.Shared.Layouts` so importing `BlueprintShell.Shared` to pick up helpers like `BlueprintShellPwaBootstrap` doesn't pull in names that clash with consumer-defined `ReaderLayout` / `MinimalLayout` classes.

## CSS / theming

The shell uses [BlazorBlueprint's](https://blazorblueprint.com) OKLCH-based CSS variable system. Override variables in your own stylesheet or supply raw CSS snippets via the builder API:

```csharp
// Custom CSS injected into every page:
registry.RegisterSource("my-theme", new ShellSource
{
    CustomCss = new[]
    {
        ":root { --primary: oklch(0.55 0.2 260); }",
    },
});
```

## Project Structure

```
BlueprintShell/
├── BlueprintShell.csproj           # NuGet metadata + content packaging
├── BlueprintShellOptions.cs        # Host-level options (chrome, mobile, scan, diagnostics)
├── BlueprintShellPwaOptions.cs     # PWA manifest + service-worker options
├── BlueprintShellExtensions.cs     # AddBlueprintShell, MapBlueprintShell, MapBlueprintShellPwa
├── Program.cs                      # EditorServerHost - standalone Kestrel runner
├── App.razor                       # Root HTML document (theme, BB CSS, scripts)
├── Routes.razor                    # Router; selects layout per reader-page
├── _Imports.razor                  # In-project usings
├── Hubs/
│   └── ShellHub.cs                 # SignalR hub + ShellState
├── Pages/
│   └── Index.razor                 # Catch-all - resolves reader pages & panel routes
├── Shared/                         # Components in `BlueprintShell.Shared`
│   ├── Layouts/                    # Built-in layouts (namespace `BlueprintShell.Shared.Layouts`)
│   │   ├── ShellLayout.razor(.cs)  # Full chrome (dock + header + nav)
│   │   ├── MinimalLayout.razor     # Header-only chrome
│   │   └── ReaderLayout.razor      # No chrome - just shell services
│   ├── ShellDockLayout.razor       # Dock zones with mobile breakpoint behaviour
│   ├── ShellThemeStyle.razor       # Renders active ThemePreset as <style>
│   ├── BlueprintShellPwaBootstrap.razor # Emits the SW registration <script> tag
│   ├── DockPanel.razor             # Panel chrome (title bar, close button)
│   ├── DockPanelGroup.razor        # Tab-group container
│   └── ElementRenderer.razor       # Maps Element tree to BlazorBlueprint components
├── Shell/
│   ├── Attributes.cs               # [EditorShell], [EditorPanel], [ReaderPage]
│   ├── Descriptors.cs              # Panel, ReaderPage, Theme, Menubar descriptors
│   ├── Elements.cs                 # Element + IContentBuilder API
│   ├── ShellRegistry.cs            # Merge of sources, themes, header slots
│   ├── ShellSource.cs              # One contribution (builders, panels, readers, css, themes)
│   ├── StaticShellLoader.cs        # Reflection discovery (respects ScanAssemblies)
│   ├── GeneratedShellRegistrationAttribute.cs # Source-generator hook
│   ├── IShellAuthContext.cs        # Per-request auth + RequiresRole gate
│   ├── RouteMatcher.cs             # Template matcher for catch-all routes
│   ├── Builders/                   # ShellBuilder, PanelBuilder, ContentBuilder, CssBuilder
│   └── Helper/                     # Icon helpers, variant maps
├── wwwroot/
│   ├── css/app.css                 # Tailwind output
│   ├── styles/themes/default.css   # OKLCH variable defaults
│   └── images/
├── contentFiles/any/any/_Imports.razor # Drop-in usings shipped to consumers
├── LICENSE                         # MPL 2.0
└── README.md
```

## License

This project is licensed under the **Mozilla Public License 2.0** - see the [LICENSE](LICENSE) file for details.
