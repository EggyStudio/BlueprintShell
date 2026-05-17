namespace BlueprintShell.Shell;

/// <summary>
/// Per-request auth context consumed by the shell when deciding chrome, panel visibility,
/// and reader-page eligibility. Consumers register their own implementation via
/// <c>services.AddScoped&lt;IShellAuthContext, MyAuthContext&gt;()</c>.
/// </summary>
/// <remarks>
/// The shell does not impose any auth scheme - it only reads <see cref="IsAuthenticated"/>
/// and <see cref="Roles"/>. Reader-pages and editor panels marked with a <c>RequiresRole</c>
/// are filtered out for requests where the role is not present.
/// </remarks>
public interface IShellAuthContext
{
    /// <summary>Whether the current request belongs to an authenticated user.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Stable user identifier, or null when anonymous.</summary>
    string? UserId { get; }

    /// <summary>Roles granted to the current user (case-sensitive).</summary>
    IReadOnlySet<string> Roles { get; }
}

/// <summary>
/// Default anonymous implementation registered by the shell when consumers do not provide one.
/// Reports <see cref="IsAuthenticated"/> = <see langword="false"/> and an empty role set.
/// </summary>
internal sealed class AnonymousShellAuthContext : IShellAuthContext
{
    public bool IsAuthenticated => false;
    public string? UserId => null;
    public IReadOnlySet<string> Roles { get; } = new HashSet<string>(StringComparer.Ordinal);
}

/// <summary>Static helpers for honouring <c>RequiresRole</c> on panels and reader pages.</summary>
public static class ShellAuthGate
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="required"/> is null/empty,
    /// when it equals <c>"*"</c> and the user is authenticated, or when
    /// <see cref="IShellAuthContext.Roles"/> contains it.
    /// </summary>
    public static bool Allow(IShellAuthContext? ctx, string? required)
    {
        if (string.IsNullOrEmpty(required)) return true;
        if (ctx is null) return false;
        if (required == "*") return ctx.IsAuthenticated;
        return ctx.Roles.Contains(required);
    }
}
