namespace BlueprintShell.Shell;

// -- Shell Discovery --

/// <summary>
/// Marks a class implementing <see cref="IEditorShellBuilder"/> for discovery
/// by the runtime script compiler. The builder's <c>Build()</c> method is called
/// to produce a <see cref="ShellDescriptor"/> tree that drives the editor UI.
/// </summary>
/// <seealso cref="IEditorShellBuilder"/>
/// <seealso cref="ShellDescriptor"/>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EditorShellAttribute : Attribute;

// -- Blazor Panel Discovery --

/// <summary>
/// Marks a native Blazor component (<c>.razor</c> file) as an editor panel.
/// The compiler discovers types annotated with this attribute and creates a
/// <see cref="PanelDescriptor"/> with <see cref="PanelDescriptor.ComponentType"/>
/// set to the component's <see cref="Type"/>, enabling <c>DynamicComponent</c> rendering.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute in a <c>.razor</c> file via the <c>@attribute</c> directive:
/// </para>
/// <code>
/// @attribute [EditorPanel("my-panel", "My Panel", DockZone.Right)]
/// </code>
/// <para>
/// The component is compiled at runtime alongside <c>.cs</c> scripts and benefits
/// from full Blazor features: <c>@onclick</c>, <c>@bind</c>, <c>@inject</c>,
/// component parameters, lifecycle methods, and CSS isolation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// @attribute [EditorPanel("inspector", "Inspector", DockZone.Right, Icon = "settings", Route = "/inspector")]
///
/// @inject ShellRegistry Registry
///
/// &lt;h3&gt;Inspector Panel&lt;/h3&gt;
/// &lt;p&gt;Selected entity: @EntityId&lt;/p&gt;
///
/// @code {
///     [Parameter] public int? EntityId { get; set; }
/// }
/// </code>
/// </example>
/// <seealso cref="EditorShellAttribute"/>
/// <seealso cref="PanelDescriptor"/>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EditorPanelAttribute : Attribute
{
    /// <summary>Unique panel identifier (used for tab grouping and persistence).</summary>
    public string Id { get; }

    /// <summary>Display title shown in the panel header / tab.</summary>
    public string Title { get; }

    /// <summary>Default dock zone for initial placement.</summary>
    public DockZone Zone { get; }

    /// <summary>Optional icon name displayed alongside the title (e.g. <c>"settings"</c>).</summary>
    public string? Icon { get; set; }

    /// <summary>Optional URL route for this panel (e.g. <c>"/inspector"</c>).</summary>
    public string? Route { get; set; }

    /// <summary>Optional tab group identifier. When set, the panel appears as a tab in the group.</summary>
    public string? TabGroup { get; set; }

    /// <summary>Sort order within a tab group. Lower values appear first.</summary>
    public int TabOrder { get; set; }

    /// <summary>Initial size as a fraction (0..1) of the parent dock area. Defaults to 0.25.</summary>
    public float InitialSize { get; set; } = 0.25f;

    /// <summary>Whether the panel can be closed by the user. Defaults to <see langword="true"/>.</summary>
    public bool Closeable { get; set; } = true;

    /// <summary>Whether the panel is visible on creation. Defaults to <see langword="true"/>.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Priority for ordering when multiple panels compete for the same position. Lower values win.</summary>
    public int Order { get; set; }

    /// <summary>
    /// When set, the panel is only included in the merged shell when
    /// <see cref="IShellAuthContext.Roles"/> contains this value (or the role is
    /// <c>"*"</c> meaning any authenticated user).
    /// </summary>
    public string? RequiresRole { get; set; }

    /// <summary>Creates a new <see cref="EditorPanelAttribute"/> with the specified panel metadata.</summary>
    /// <param name="id">Unique panel identifier.</param>
    /// <param name="title">Display title for the panel header / tab.</param>
    /// <param name="zone">Default dock zone for initial placement. Defaults to <see cref="DockZone.Center"/>.</param>
    public EditorPanelAttribute(string id, string title, DockZone zone = DockZone.Center)
    {
        Id = id;
        Title = title;
        Zone = zone;
    }
}

// -- Reader Page Discovery --

/// <summary>
/// Marks a Blazor component as a non-dockable routed page rendered inside the shell.
/// Reader pages bypass the dock layout entirely: no panel headers, no splitters, no close
/// button - they receive only the configured chrome (see <see cref="ShellChromeMode"/>)
/// plus access to shell services (theming, auth, registry).
/// </summary>
/// <remarks>
/// Apply via <c>@attribute [ReaderPage(...)]</c> in a <c>.razor</c> file. The route is
/// registered with the shell router and parameters are forwarded to the component the
/// same way an <c>@page</c> directive would.
/// </remarks>
/// <example>
/// <code>
/// @attribute [ReaderPage("article", "/wiki/{Identifier}")]
///
/// @code {
///     [Parameter] public string? Identifier { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ReaderPageAttribute : Attribute
{
    /// <summary>Unique reader-page identifier.</summary>
    public string Id { get; }

    /// <summary>Route template (e.g. <c>"/wiki/{Identifier}"</c>).</summary>
    public string Route { get; }

    /// <summary>Optional chrome override for this specific page. Falls back to <see cref="BlueprintShellOptions.ChromeMode"/> when null.</summary>
    public ShellChromeMode? Chrome { get; set; }

    /// <summary>Optional layout type. When null, the shell selects a layout from <see cref="Chrome"/>.</summary>
    public Type? Layout { get; set; }

    /// <summary>Optional role requirement; honoured by <see cref="IShellAuthContext"/>.</summary>
    public string? RequiresRole { get; set; }

    /// <summary>Display title used for &lt;title&gt; and breadcrumbs. Falls back to the component name.</summary>
    public string? Title { get; set; }

    /// <summary>Creates a new <see cref="ReaderPageAttribute"/>.</summary>
    /// <param name="id">Unique reader-page identifier (used in diagnostics).</param>
    /// <param name="route">Route template, may contain Blazor route parameters.</param>
    public ReaderPageAttribute(string id, string route)
    {
        Id = id;
        Route = route;
    }
}