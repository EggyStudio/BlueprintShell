using BlueprintShell.Shell;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

namespace BlueprintShell.Shared;

/// <summary>
/// Layout component that drives the shell chrome (header, nav, dark-mode).
/// Subscribes to <see cref="ShellRegistry.Changed"/> and re-renders whenever the
/// descriptor is hot-reloaded.
/// </summary>
public partial class ShellLayout : LayoutComponentBase, IDisposable
{
    [Inject] private ShellRegistry Registry { get; set; } = null!;
    [Inject] private BlueprintShellOptions Options { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private IShellAuthContext Auth { get; set; } = null!;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    [Inject] private IServiceProvider Services { get; set; } = null!;

    private ShellDescriptor Descriptor => Registry.Current;
    private bool _isDark = true; // default: dark mode on

    protected override void OnInitialized()
    {
        Registry.Changed += OnShellChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try { _isDark = await JS.InvokeAsync<bool>("isDarkModeEnabled"); }
            catch { /* JS interop not yet ready - leave default */ }
            StateHasChanged();
        }
    }

    private async Task ToggleDarkMode()
    {
        await JS.InvokeVoidAsync("toggleDarkMode");
        _isDark = !_isDark;
    }

    private ShellChromeMode ResolveChrome()
    {
        var ctx = HttpContextAccessor.HttpContext;
        if (ctx is not null && Options.ChromeForServices is not null)
        {
            try { return Options.ChromeForServices(ctx, Services); }
            catch { /* fall through */ }
        }

        if (ctx is not null && Options.ChromeFor is not null)
        {
            try { return Options.ChromeFor(ctx); }
            catch { /* fall through */ }
        }

        return Options.ChromeMode;
    }

    private void OnShellChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => Registry.Changed -= OnShellChanged;
}
