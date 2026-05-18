namespace BlueprintShell.Shell;

/// <summary>
/// A single contribution of shell content to the <see cref="ShellRegistry"/>,
/// keyed in the registry by an opaque source id (see <see cref="ShellSourceIds"/>).
/// </summary>
/// <remarks>
/// <para>
/// Multiple sources coexist in the registry simultaneously: typically one
/// <see cref="ShellSourceIds.Static"/> source supplied by the source generator
/// (compiled-in shells) and one <see cref="ShellSourceIds.Dynamic"/> source
/// supplied by the runtime Roslyn compiler (hot-reloaded shells). The registry
/// merges all sources into a single <see cref="ShellDescriptor"/> on every
/// mutation; on panel-id collisions the source with higher
/// <see cref="Precedence"/> wins.
/// </para>
/// </remarks>
/// <seealso cref="ShellRegistry"/>
public sealed class ShellSource
{
    /// <summary>Discovered <c>[Shell]</c> builders, sorted later by <see cref="IShellContribution.Order"/>.</summary>
    public IReadOnlyList<IShellContribution> Builders { get; init; } = Array.Empty<IShellContribution>();

    /// <summary>Discovered <c>[Panel]</c> Blazor component types with their attribute metadata.</summary>
    public IReadOnlyList<(PanelAttribute Attr, Type Type)> PanelComponents { get; init; }
        = Array.Empty<(PanelAttribute, Type)>();

    /// <summary>Discovered <c>[ReaderPage]</c> Blazor component types with their attribute metadata.</summary>
    public IReadOnlyList<(ReaderPageAttribute Attr, Type Type)> ReaderPages { get; init; }
        = Array.Empty<(ReaderPageAttribute, Type)>();

    /// <summary>Raw CSS snippets to inject into the shell page.</summary>
    public IReadOnlyList<string> CustomCss { get; init; } = Array.Empty<string>();

    /// <summary>Theme presets contributed by this source. Merged into <see cref="ShellDescriptor.Themes"/>.</summary>
    public IReadOnlyDictionary<string, ThemePreset> Themes { get; init; }
        = new Dictionary<string, ThemePreset>(StringComparer.Ordinal);

    /// <summary>
    /// Higher value wins on duplicate <see cref="PanelDescriptor.Id"/>. Convention:
    /// 0 = static (compiled-in), 100 = dynamic (hot-reloaded user scripts).
    /// </summary>
    public int Precedence { get; init; }
}

/// <summary>Well-known source identifiers used by the engine. Custom sources may use any other string.</summary>
public static class ShellSourceIds
{
    /// <summary>Source id used by the source-generator-driven static loader.</summary>
    public const string Static = "static";

    /// <summary>Source id used by the runtime Roslyn-based hot-reload compiler.</summary>
    public const string Dynamic = "dynamic";
}