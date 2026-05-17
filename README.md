# BlueprintShell

![logo](https://raw.githubusercontent.com/EggyStudio/BlueprintShell/main/.github/assets/logo.png)

**BlueprintShell** is an embeddable editor shell built on [BlazorBlueprint](https://blazorblueprint.com). Add it to any .NET application and get a fully themed, dockable editor UI - panels, toolbars, dark-mode, hot-reload - served on a configurable port from your own process.

> Packages are published to [GitHub Packages](https://github.com/EggyStudio/BlueprintShell/packages).

---

## Installation

Add the GitHub Packages feed to your `NuGet.config` (or `~/.nuget/NuGet/NuGet.Config`):

```xml
<packageSources>
  <add key="EggyStudio" value="https://nuget.pkg.github.com/EggyStudio/index.json" />
</packageSources>
```

Then add the package:

```bash
dotnet add package BlueprintShell
```

---

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

---

## Configuration - `BlueprintShellOptions`

| Property | Type | Default | Description |
|---|---|---|---|
| `Url` | `string` | `"http://localhost:5000"` | Kestrel listen URL. Accepts any format supported by `UseUrls`, e.g. `"http://*:5100"` or `"https://localhost:5001;http://localhost:5000"`. Only used in standalone mode. |
| `AppTitle` | `string` | `"Blueprint Shell"` | Title shown in the shell header and browser tab. |
| `LaunchBrowser` | `bool` | `false` | Open the system browser pointing at `Url` once the server is ready. |
| `ChromeMode` | `ShellChromeMode` | `Full` | Default chrome level: `Full` (dock + header), `Minimal` (header only), `Hidden` (nothing - just shell services around `@Body`). |
| `ChromeFor` | `Func<HttpContext, ShellChromeMode>?` | `null` | Per-request override; evaluated before `ChromeMode`. |
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

---

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
| `Route` | When set, a nav-link to this panel appears in the shell header. |
| `TabGroup` | Groups this panel as a tab inside another panel. |
| `TabOrder` | Sort order within a tab group. |
| `InitialSize` | Fraction (0–1) of the parent dock area. Default `0.25`. |
| `Closeable` | Whether the user can close the panel. Default `true`. |
| `Visible` | Whether the panel starts visible. Default `true`. |
| `RequiresRole` | Filters the panel out for requests where `IShellAuthContext.Roles` doesn't contain the value (`"*"` means any authenticated user). |

---

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

---

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

---

## Reader pages

Reader pages are routed components that bypass the dock layout - useful for full-bleed
content (articles, marketing pages) hosted alongside an editor.

```razor
@* Article.razor *@
@attribute [ReaderPage("article", "/wiki/{Identifier}",
    Chrome = ShellChromeMode.Hidden)]

@code {
    [Parameter] public string? Identifier { get; set; }
}
```

The shell resolves the route via a catch-all router and forwards segments as
`[Parameter]` properties on the component (same convention as `@page`).

## Auth-aware chrome

Implement `IShellAuthContext` and the shell will filter `[EditorPanel(..., RequiresRole = "...")]`
panels and `[ReaderPage(..., RequiresRole = "...")]` pages automatically. Use `"*"` to require
any authenticated user.

```csharp
public sealed class MyAuthContext : IShellAuthContext
{
    public bool IsAuthenticated => /* ... */;
    public string? UserId => /* ... */;
    public IReadOnlySet<string> Roles => /* ... */;
}

builder.Services.AddScoped<IShellAuthContext, MyAuthContext>();
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

The active preset is rendered as `<style id="bp-active-theme">` and replaced when
`SetActiveTheme` is called.

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

Serves a manifest at `/manifest.webmanifest` and a service worker at `/sw.js`,
precaching the shell's own CSS and any URLs you add via `ExtraCacheUrls`.

## Diagnostics

Set `o.EnableDiagnostics = true` and `GET /_shell/diagnostics` returns a JSON dump
of panels, reader pages, themes, and loaded assemblies - useful when something
doesn't render and you want to know whether it was discovered.

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

---

## License

[MPL 2.0](LICENSE)
