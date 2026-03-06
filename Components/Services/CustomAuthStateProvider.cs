using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using FitnessApp.Data;

namespace FitnessApp.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _session;
    private readonly ProtectedLocalStorage _local;
    private readonly AppDbContext _db;
    private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));
    private bool _initialized;
    private int? _cachedUserId;

    private const string SessionKey = "userId";
    private const string LocalKey = "userId_persistent";

    public CustomAuthStateProvider(ProtectedSessionStorage session, ProtectedLocalStorage local, AppDbContext db)
    {
        _session = session;
        _local = local;
        _db = db;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_initialized)
        {
            try
            {
                var sessionResult = await _session.GetAsync<int>(SessionKey);
                if (sessionResult.Success)
                {
                    _cachedUserId = sessionResult.Value;
                }
                else
                {
                    var localResult = await _local.GetAsync<int>(LocalKey);
                    if (localResult.Success)
                    {
                        _cachedUserId = localResult.Value;
                        await _session.SetAsync(SessionKey, localResult.Value);
                    }
                }
                _initialized = true;
            }
            catch
            {
                return AnonymousState;
            }
        }

        if (_cachedUserId is null)
            return AnonymousState;

        var user = await _db.Users.FindAsync(_cachedUserId.Value);
        if (user is null)
        {
            _cachedUserId = null;
            return AnonymousState;
        }

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email)
        }, "custom");

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task LoginAsync(int userId, bool rememberMe)
    {
        await _session.SetAsync(SessionKey, userId);

        if (rememberMe)
            await _local.SetAsync(LocalKey, userId);

        _cachedUserId = userId;
        _initialized = true;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task LogoutAsync()
    {
        try { await _session.DeleteAsync(SessionKey); } catch { }
        try { await _local.DeleteAsync(LocalKey); } catch { }
        _cachedUserId = null;
        _initialized = false;
    }
}
