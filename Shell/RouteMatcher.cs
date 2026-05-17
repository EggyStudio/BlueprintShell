namespace BlueprintShell.Shell;

/// <summary>
/// Tiny route-template matcher used by the shell's catch-all router to resolve
/// <see cref="ReaderPageDescriptor"/> and <see cref="PanelDescriptor"/> routes.
/// Supports literal segments and <c>{Name}</c> placeholders (including <c>{*Catch}</c>).
/// </summary>
public static class RouteMatcher
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="path"/> matches <paramref name="template"/>.
    /// Extracted parameters are written into <paramref name="values"/> (case-insensitive keys).
    /// </summary>
    public static bool Match(string template, string path, out Dictionary<string, string?> values)
    {
        values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var tSegs = Split(template);
        var pSegs = Split(path);

        int ti = 0, pi = 0;
        while (ti < tSegs.Length)
        {
            var t = tSegs[ti];
            if (t.StartsWith("{*", StringComparison.Ordinal) && t.EndsWith('}'))
            {
                var name = t.Substring(2, t.Length - 3);
                values[name] = string.Join('/', pSegs.Skip(pi));
                return true;
            }

            if (pi >= pSegs.Length) return false;

            if (t.StartsWith('{') && t.EndsWith('}'))
            {
                var name = t.Substring(1, t.Length - 2);
                // Strip type constraints like {Id:int}
                var colon = name.IndexOf(':');
                if (colon >= 0) name = name[..colon];
                values[name] = pSegs[pi];
            }
            else if (!string.Equals(t, pSegs[pi], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            ti++;
            pi++;
        }

        return pi == pSegs.Length;
    }

    private static string[] Split(string path) =>
        (path ?? string.Empty).Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
}
