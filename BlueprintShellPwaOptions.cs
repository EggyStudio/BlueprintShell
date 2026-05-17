namespace BlueprintShell;

/// <summary>Options for <see cref="BlueprintShellExtensions.MapBlueprintShellPwa"/>.</summary>
public sealed class BlueprintShellPwaOptions
{
    /// <summary>Public path the manifest will be served at. Defaults to <c>/manifest.webmanifest</c>.</summary>
    public string ManifestPath { get; set; } = "/manifest.webmanifest";

    /// <summary>Public path the service worker will be served at. Defaults to <c>/sw.js</c>.</summary>
    public string ServiceWorkerPath { get; set; } = "/sw.js";

    /// <summary>PWA theme color (matches the system chrome).</summary>
    public string ThemeColor { get; set; } = "#0a0a0a";

    /// <summary>PWA background color (splash screen).</summary>
    public string BackgroundColor { get; set; } = "#ffffff";

    /// <summary>Short app name shown on the home screen.</summary>
    public string ShortName { get; set; } = "Shell";

    /// <summary>Full app name shown in the install prompt. Falls back to <see cref="ShortName"/> when null.</summary>
    public string? Name { get; set; }

    /// <summary>Display mode (<c>standalone</c>, <c>fullscreen</c>, <c>minimal-ui</c>, <c>browser</c>).</summary>
    public string DisplayMode { get; set; } = "standalone";

    /// <summary>Start URL the PWA opens on launch.</summary>
    public string StartUrl { get; set; } = "/";

    /// <summary>Cache the shell's own static CSS/icons/fonts in the service worker.</summary>
    public bool CacheStaticAssets { get; set; } = true;

    /// <summary>Additional URLs to precache in the service worker.</summary>
    public IList<string> ExtraCacheUrls { get; } = new List<string>();

    /// <summary>Optional list of manifest <c>icons</c> entries; defaults to a single 512×512 PNG if empty.</summary>
    public IList<PwaIcon> Icons { get; } = new List<PwaIcon>();
}

/// <summary>A PWA manifest icon entry.</summary>
/// <param name="Src">Icon URL.</param>
/// <param name="Sizes">Sizes string, e.g. <c>"192x192"</c>.</param>
/// <param name="Type">MIME type, e.g. <c>"image/png"</c>.</param>
/// <param name="Purpose">Optional purpose hint (<c>"any"</c>, <c>"maskable"</c>, <c>"monochrome"</c>).</param>
public sealed record PwaIcon(string Src, string Sizes, string Type = "image/png", string? Purpose = null);
