using BlueprintShell.Shell;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BlueprintShell.Hubs;

/// <summary>
/// Defines the server-side operations clients can invoke over SignalR.
/// </summary>
public interface IShellHub
{
    /// <summary>Marks an item as selected in the shell.</summary>
    Task SelectItem(string itemId);

    /// <summary>Clears the current selection.</summary>
    Task DeselectItem();

    /// <summary>
    /// Queues a property change. The host application drains
    /// <see cref="ShellState.PendingPropertyEdits"/> each frame.
    /// </summary>
    Task UpdateProperty(string targetId, string scope, string name, string jsonValue);

    /// <summary>
    /// Queues a named command with an optional JSON argument payload.
    /// The host drains <see cref="ShellState.PendingCommands"/> each frame.
    /// </summary>
    Task ExecuteCommand(string commandId, string? jsonArgs = null);

    /// <summary>Returns the current shell descriptor version number.</summary>
    Task<int> GetShellVersion();
}

/// <summary>
/// Defines the client-side callbacks the server can push to connected browsers.
/// </summary>
public interface IShellClient
{
    /// <summary>Notifies the client that an item was selected.</summary>
    Task OnItemSelected(string itemId);

    /// <summary>Notifies the client that the selection was cleared.</summary>
    Task OnItemDeselected();

    /// <summary>Notifies the client that the shell descriptor changed.</summary>
    Task OnShellChanged(int version);
}

/// <summary>
/// SignalR hub that bridges the Blazor UI with the host application.
/// Clients invoke methods on <see cref="IShellHub"/>; the server pushes
/// events back via <see cref="IShellClient"/>.
/// </summary>
public sealed class ShellHub : Hub<IShellClient>, IShellHub
{
    private readonly ShellState _state;
    private readonly ShellRegistry _registry;

    public ShellHub(ShellState state, ShellRegistry registry)
    {
        _state = state;
        _registry = registry;
    }

    public Task SelectItem(string itemId)
    {
        _state.SelectedItemId = itemId;
        return Clients.Others.OnItemSelected(itemId);
    }

    public Task DeselectItem()
    {
        _state.SelectedItemId = null;
        return Clients.Others.OnItemDeselected();
    }

    public Task UpdateProperty(string targetId, string scope, string name, string jsonValue)
    {
        _state.PendingPropertyEdits.Enqueue(new PropertyEdit(targetId, scope, name, jsonValue));
        return Task.CompletedTask;
    }

    public Task ExecuteCommand(string commandId, string? jsonArgs = null)
    {
        _state.PendingCommands.Enqueue(new ShellCommand(commandId, jsonArgs));
        return Task.CompletedTask;
    }

    public Task<int> GetShellVersion() =>
        Task.FromResult(_registry.Version);
}

/// <summary>
/// Shared state readable by the host application each frame.
/// Thread-safe: queues are lock-free; <see cref="SelectedItemId"/> is
/// written only from hub calls (single SignalR dispatch thread per connection).
/// </summary>
public sealed class ShellState
{
    public string? SelectedItemId { get; set; }

    public ConcurrentQueue<PropertyEdit> PendingPropertyEdits { get; } = new();

    public ConcurrentQueue<ShellCommand> PendingCommands { get; } = new();
}

/// <summary>A property change queued by the UI for the host to apply.</summary>
/// <param name="TargetId">Identifier of the object whose property should change.</param>
/// <param name="Scope">Logical grouping (e.g. component type, subsystem name).</param>
/// <param name="Name">Property name within the scope.</param>
/// <param name="JsonValue">New value, JSON-encoded.</param>
public sealed record PropertyEdit(string TargetId, string Scope, string Name, string JsonValue);

/// <summary>A named command queued by the UI for the host to execute.</summary>
/// <param name="CommandId">Unique command identifier.</param>
/// <param name="JsonArgs">Optional JSON-encoded arguments.</param>
public sealed record ShellCommand(string CommandId, string? JsonArgs);
