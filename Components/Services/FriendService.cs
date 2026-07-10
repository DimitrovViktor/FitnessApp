using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;

namespace FitnessApp.Services;

public class FriendService
{
    private readonly AppDbContext _db;

    public FriendService(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> AreFriendsAsync(int a, int b)
    {
        if (a == b) return Task.FromResult(false);
        return _db.Friendships.AnyAsync(f => f.Status == FriendshipStatus.Accepted &&
            ((f.RequesterId == a && f.AddresseeId == b) || (f.RequesterId == b && f.AddresseeId == a)));
    }

    public async Task<HashSet<int>> GetFriendIdsAsync(int meId)
    {
        var rows = await _db.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted && (f.RequesterId == meId || f.AddresseeId == meId))
            .Select(f => f.RequesterId == meId ? f.AddresseeId : f.RequesterId)
            .ToListAsync();
        return rows.ToHashSet();
    }

    public async Task<FriendshipState> GetStateAsync(int meId, int otherId)
    {
        if (meId == otherId) return FriendshipState.Self;

        var friendship = await _db.Friendships.FirstOrDefaultAsync(f =>
            (f.RequesterId == meId && f.AddresseeId == otherId) ||
            (f.RequesterId == otherId && f.AddresseeId == meId));

        if (friendship is null) return FriendshipState.None;
        if (friendship.Status == FriendshipStatus.Accepted) return FriendshipState.Friends;
        return friendship.RequesterId == meId ? FriendshipState.Outgoing : FriendshipState.Incoming;
    }

    public async Task<FriendshipState> SendRequestAsync(int meId, int otherId)
    {
        if (meId == otherId) return FriendshipState.Self;
        if (await _db.Users.FindAsync(otherId) is null) return FriendshipState.None;

        var existing = await _db.Friendships.FirstOrDefaultAsync(f =>
            (f.RequesterId == meId && f.AddresseeId == otherId) ||
            (f.RequesterId == otherId && f.AddresseeId == meId));

        if (existing is not null)
        {
            if (existing.Status == FriendshipStatus.Accepted) return FriendshipState.Friends;
            if (existing.RequesterId == meId) return FriendshipState.Outgoing;
            existing.Status = FriendshipStatus.Accepted;
            existing.RespondedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return FriendshipState.Friends;
        }

        _db.Friendships.Add(new Friendship
        {
            RequesterId = meId,
            AddresseeId = otherId,
            Status = FriendshipStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return FriendshipState.Outgoing;
    }

    public async Task<FriendshipState> AcceptRequestAsync(int meId, int otherId)
    {
        var request = await _db.Friendships.FirstOrDefaultAsync(f =>
            f.RequesterId == otherId && f.AddresseeId == meId && f.Status == FriendshipStatus.Pending);

        if (request is null) return await GetStateAsync(meId, otherId);

        request.Status = FriendshipStatus.Accepted;
        request.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return FriendshipState.Friends;
    }

    public async Task<FriendshipState> RemoveAsync(int meId, int otherId)
    {
        var rows = await _db.Friendships.Where(f =>
            (f.RequesterId == meId && f.AddresseeId == otherId) ||
            (f.RequesterId == otherId && f.AddresseeId == meId)).ToListAsync();

        if (rows.Count > 0)
        {
            _db.Friendships.RemoveRange(rows);
            await _db.SaveChangesAsync();
        }
        return FriendshipState.None;
    }

    public async Task<Dictionary<int, FriendshipState>> GetStatesAsync(int meId, List<int> otherIds)
    {
        var result = new Dictionary<int, FriendshipState>();
        if (otherIds.Count == 0) return result;

        var rows = await _db.Friendships
            .Where(f => (f.RequesterId == meId && otherIds.Contains(f.AddresseeId)) ||
                        (f.AddresseeId == meId && otherIds.Contains(f.RequesterId)))
            .ToListAsync();

        foreach (var id in otherIds)
        {
            if (id == meId) { result[id] = FriendshipState.Self; continue; }
            var row = rows.FirstOrDefault(f =>
                (f.RequesterId == meId && f.AddresseeId == id) ||
                (f.AddresseeId == meId && f.RequesterId == id));

            if (row is null) result[id] = FriendshipState.None;
            else if (row.Status == FriendshipStatus.Accepted) result[id] = FriendshipState.Friends;
            else result[id] = row.RequesterId == meId ? FriendshipState.Outgoing : FriendshipState.Incoming;
        }
        return result;
    }

    public async Task<List<FriendUserDto>> GetFriendsAsync(int meId)
    {
        var friendIds = await _db.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted && (f.RequesterId == meId || f.AddresseeId == meId))
            .Select(f => f.RequesterId == meId ? f.AddresseeId : f.RequesterId)
            .ToListAsync();

        return await LoadUsersAsync(friendIds);
    }

    public async Task<List<FriendUserDto>> GetIncomingRequestsAsync(int meId)
    {
        var ids = await _db.Friendships
            .Where(f => f.AddresseeId == meId && f.Status == FriendshipStatus.Pending)
            .Select(f => f.RequesterId)
            .ToListAsync();

        return await LoadUsersAsync(ids);
    }

    public async Task<List<FriendUserDto>> GetOutgoingRequestsAsync(int meId)
    {
        var ids = await _db.Friendships
            .Where(f => f.RequesterId == meId && f.Status == FriendshipStatus.Pending)
            .Select(f => f.AddresseeId)
            .ToListAsync();

        return await LoadUsersAsync(ids);
    }

    private async Task<List<FriendUserDto>> LoadUsersAsync(List<int> ids)
    {
        if (ids.Count == 0) return new();

        var users = await _db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
        return users
            .OrderBy(u => u.Username)
            .Select(u => new FriendUserDto(u.Id, u.Username, u.AvatarData, PresenceStatus.Normalize(u.Status)))
            .ToList();
    }
}

public enum FriendshipState
{
    Self,
    None,
    Outgoing,
    Incoming,
    Friends
}

public record FriendUserDto(int Id, string Username, string? AvatarData, string Status);
