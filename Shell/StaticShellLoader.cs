using System.Reflection;

namespace BlueprintShell.Shell;

/// <summary>
/// Discovers compile-time-registered editor shells, panels, and reader pages by reflecting
/// across loaded assemblies (or an explicit list supplied via <see cref="BlueprintShellOptions.ScanAssemblies"/>),
/// and feeds them into the <see cref="ShellRegistry"/>.
/// </summary>
/// <remarks>
/// <para>Two passes run:</para>
/// <list type="number">
///   <item><description>Static methods marked with <see cref="GeneratedShellRegistrationAttribute"/> are invoked
///         (these are emitted by the source generator).</description></item>
///   <item><description>Types marked with <see cref="EditorPanelAttribute"/> / <see cref="ReaderPageAttribute"/>
///         are collected into a single <see cref="ShellSourceIds.Static"/> source.</description></item>
/// </list>
/// </remarks>
public static class StaticShellLoader
{
    /// <summary>Loads from every assembly in <see cref="AppDomain.CurrentDomain"/>.</summary>
    public static int LoadInto(ShellRegistry registry) => LoadInto(registry, scanAssemblies: null);

    /// <summary>
    /// Loads from an explicit assembly list. When <paramref name="scanAssemblies"/> is null or empty
    /// the current AppDomain is scanned instead.
    /// </summary>
    public static int LoadInto(ShellRegistry registry, IReadOnlyCollection<Assembly>? scanAssemblies)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var assemblies = scanAssemblies is { Count: > 0 }
            ? scanAssemblies
            : AppDomain.CurrentDomain.GetAssemblies();

        int invoked = InvokeGeneratedRegistrations(registry, assemblies);
        var (panels, readers) = CollectAttributedComponents(assemblies);

        if (panels.Count > 0 || readers.Count > 0)
        {
            registry.RegisterSource(ShellSourceIds.Static + ":discovered", new ShellSource
            {
                PanelComponents = panels,
                ReaderPages = readers,
                Precedence = 0,
            });
        }

        return invoked;
    }

    private static int InvokeGeneratedRegistrations(ShellRegistry registry, IEnumerable<Assembly> assemblies)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        var attrType = typeof(GeneratedShellRegistrationAttribute);
        var registryType = typeof(ShellRegistry);

        int invoked = 0;
        foreach (var asm in assemblies)
        {
            if (asm.IsDynamic) continue;

            foreach (var t in SafeGetTypes(asm))
            {
                MethodInfo[] methods;
                try { methods = t.GetMethods(flags); }
                catch { continue; }

                foreach (var m in methods)
                {
                    if (m.GetCustomAttributes(attrType, inherit: false).Length == 0) continue;
                    var ps = m.GetParameters();
                    if (ps.Length != 1 || ps[0].ParameterType != registryType) continue;
                    try
                    {
                        m.Invoke(null, new object[] { registry });
                        invoked++;
                    }
                    catch
                    {
                        // Swallow; one bad registration must not prevent others from running.
                    }
                }
            }
        }

        return invoked;
    }

    private static (List<(EditorPanelAttribute, Type)> panels, List<(ReaderPageAttribute, Type)> readers)
        CollectAttributedComponents(IEnumerable<Assembly> assemblies)
    {
        var panels = new List<(EditorPanelAttribute, Type)>();
        var readers = new List<(ReaderPageAttribute, Type)>();

        foreach (var asm in assemblies)
        {
            if (asm.IsDynamic) continue;
            foreach (var t in SafeGetTypes(asm))
            {
                foreach (var attr in t.GetCustomAttributes(typeof(EditorPanelAttribute), inherit: false))
                    panels.Add(((EditorPanelAttribute)attr, t));

                foreach (var attr in t.GetCustomAttributes(typeof(ReaderPageAttribute), inherit: false))
                    readers.Add(((ReaderPageAttribute)attr, t));
            }
        }

        return (panels, readers);
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
        catch { return Array.Empty<Type>(); }
    }
}
