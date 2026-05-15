using BlazorBlueprint.Components;
using BlueprintShell.Hubs;
using BlueprintShell.Shell;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BlueprintShell;

/// <summary>
/// Extension methods for embedding BlueprintShell inside an <em>existing</em> ASP.NET Core app
/// (as opposed to spawning a dedicated server via <see cref="EditorServerHost.StartAsync"/>).
/// </summary>
/// <example><code>
/// // Program.cs of the host app:
/// builder.Services.AddBlueprintShell(o =>
/// {
///     o.AppTitle = "My Engine Editor";
/// });
///
/// // …after builder.Build():
/// app.MapBlueprintShell();
///
/// // Then in the host's Routes.razor / layout, reference ShellLayout and the shell pages.
/// </code></example>
public static class BlueprintShellExtensions
{
    /// <summary>
    /// Registers all BlueprintShell services: <see cref="BlueprintShellOptions"/>,
    /// <see cref="ShellRegistry"/>, <see cref="ShellState"/>, BlazorBlueprint, and SignalR.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="configure">Optional delegate to customise <see cref="BlueprintShellOptions"/>.</param>
    public static IServiceCollection AddBlueprintShell(
        this IServiceCollection services,
        Action<BlueprintShellOptions>? configure = null)
    {
        var options = new BlueprintShellOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<ShellRegistry>();
        services.AddSingleton<ShellState>();
        services.AddBlazorBlueprintComponents();
        services.AddSignalR();

        return services;
    }

    /// <summary>
    /// Maps the BlueprintShell SignalR hub at <c>/shell-hub</c>.
    /// Call this on the <see cref="WebApplication"/> after <c>Build()</c>.
    /// </summary>
    public static WebApplication MapBlueprintShell(this WebApplication app)
    {
        app.MapHub<ShellHub>("/shell-hub");
        return app;
    }
}
