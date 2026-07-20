using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace FitnessApp.Services;

public class ProfilePresenceState
{
    public event Action? OnChange;

    public void NotifyChanged() => OnChange?.Invoke();
}

public class PresenceTracker
{
    private readonly ConcurrentDictionary<int, int> _connections = new();

    public event Action? OnChange;

    public void Connect(int userId)
    {
        var count = _connections.AddOrUpdate(userId, 1, (_, c) => c + 1);
        if (count == 1) OnChange?.Invoke();
    }

    public void Disconnect(int userId)
    {
        if (!_connections.ContainsKey(userId)) return;
        var count = _connections.AddOrUpdate(userId, 0, (_, c) => c - 1);
        if (count <= 0)
        {
            _connections.TryRemove(userId, out _);
            OnChange?.Invoke();
        }
    }

    public bool IsOnline(int userId) => _connections.TryGetValue(userId, out var c) && c > 0;
}

public class PresenceCircuitHandler : CircuitHandler
{
    private readonly PresenceTracker _tracker;
    private readonly AuthenticationStateProvider _auth;
    private int? _userId;
    private bool _counted;

    public PresenceCircuitHandler(PresenceTracker tracker, AuthenticationStateProvider auth)
    {
        _tracker = tracker;
        _auth = auth;
    }

    public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _userId ??= await ResolveUserIdAsync();
        if (_userId is not null && !_counted)
        {
            _tracker.Connect(_userId.Value);
            _counted = true;
        }
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Release();
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Release();
        return Task.CompletedTask;
    }

    private void Release()
    {
        if (_userId is not null && _counted)
        {
            _tracker.Disconnect(_userId.Value);
            _counted = false;
        }
    }

    private async Task<int?> ResolveUserIdAsync()
    {
        var state = await _auth.GetAuthenticationStateAsync();
        var id = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var uid) ? uid : null;
    }
}

public static class PresenceStatus
{
    public const string Online = "online";
    public const string Away = "away";
    public const string Dnd = "dnd";
    public const string Invisible = "invisible";
    public const string Offline = "offline";

    public static readonly (string Key, string Label)[] Options = new[]
    {
        (Online, "Online"),
        (Away, "Away"),
        (Dnd, "Do Not Disturb"),
        (Invisible, "Invisible")
    };

    public static string Normalize(string? status) =>
        status is Online or Away or Dnd or Invisible ? status! : Online;

    public static string Effective(string? chosen, bool isOnline)
    {
        if (!isOnline) return Offline;
        var status = Normalize(chosen);
        return status == Invisible ? Offline : status;
    }

    public static string Label(string? status) => status switch
    {
        Away => "Away",
        Dnd => "Do Not Disturb",
        Invisible => "Invisible",
        Offline => "Offline",
        _ => "Online"
    };
}
