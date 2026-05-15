namespace BlueprintShell;

/// <summary>
/// Configuration options for the BlueprintShell editor server.
/// Pass to <see cref="EditorServerHost.StartAsync"/> or to
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
    /// Only used by <see cref="EditorServerHost.StartAsync"/>; ignored in embedded mode.
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
}
