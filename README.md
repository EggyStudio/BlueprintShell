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
