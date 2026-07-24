using FitnessApp.Data;
using FitnessApp.Models;
using FitnessApp.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Services;

public class NotificationService
{
    private readonly AppDbContext _db;
    private readonly NotificationBroker _broker;

    public NotificationService(AppDbContext db, NotificationBroker broker)
    {
        _db = db;
        _broker = broker;
    }

    public async Task<List<Notification>> GetAsync(int userId, int take = 50)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> UnreadCountAsync(int userId)
    {
        return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAllReadAsync(int userId)
    {
        var items = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        if (items.Count == 0) return;
        foreach (var n in items) n.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task MarkReadAsync(int notificationId)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId);
        if (n is null || n.IsRead) return;
        n.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task ClearAsync(int userId)
    {
        var items = await _db.Notifications.Where(n => n.UserId == userId).ToListAsync();
        if (items.Count == 0) return;
        _db.Notifications.RemoveRange(items);
        await _db.SaveChangesAsync();
    }

    public async Task<Notification?> AddAsync(int userId, string kind, string title, string body, string? link, string dedupeKey)
    {
        if (userId <= 0) return null;

        var exists = await _db.Notifications.AnyAsync(n => n.UserId == userId && n.DedupeKey == dedupeKey);
        if (exists) return null;

        var notification = new Notification
        {
            UserId = userId,
            Kind = kind,
            Title = title,
            Body = body,
            Link = link,
            DedupeKey = dedupeKey,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
        _broker.Publish(notification);
        return notification;
    }

    private async Task<bool> SocialEnabledAsync(int userId)
    {
        var s = await _db.UserSettings.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
        return s?.SocialNotifications ?? true;
    }

    private async Task<bool> RemindersEnabledAsync(int userId)
    {
        var s = await _db.UserSettings.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
        return s?.WorkoutReminders ?? true;
    }

    public async Task NotifyDirectMessageAsync(int recipientId, int senderId, string senderName, int messageId, string preview)
    {
        if (recipientId == senderId) return;
        if (!await SocialEnabledAsync(recipientId)) return;

        var body = string.IsNullOrWhiteSpace(preview)
            ? "Sent you a message"
            : (preview.Length > 90 ? preview[..90] + "…" : preview);

        await AddAsync(recipientId, "message", $"{senderName} messaged you", body, "/social", $"dm:{messageId}");
    }

    public async Task NotifyPostLikedAsync(int ownerId, int actorId, string actorName, int postId)
    {
        if (ownerId == actorId) return;
        if (!await SocialEnabledAsync(ownerId)) return;

        await AddAsync(ownerId, "like", $"{actorName} liked your post", "Your post received a new like", "/social", $"like:{postId}:{actorId}");
    }

    public async Task NotifyPostRepostedAsync(int ownerId, int actorId, string actorName, int postId)
    {
        if (ownerId == actorId) return;
        if (!await SocialEnabledAsync(ownerId)) return;

        await AddAsync(ownerId, "repost", $"{actorName} reposted your post", "Your post was shared to their followers", "/social", $"repost:{postId}:{actorId}");
    }

    public async Task SyncScheduleRemindersAsync(int userId)
    {
        if (userId <= 0) return;
        if (!await RemindersEnabledAsync(userId)) return;

        var now = DateTime.Now;
        var horizon = now.AddHours(24);
        var today = DateOnly.FromDateTime(now);
        var tomorrow = today.AddDays(1);

        var workouts = await _db.WorkoutSchedules
            .Include(s => s.Workout)
            .Where(s => s.UserId == userId
                        && s.Status == ScheduleStatus.Scheduled
                        && (s.ScheduledDate == today || s.ScheduledDate == tomorrow))
            .ToListAsync();

        foreach (var s in workouts)
        {
            var when = s.ScheduledDate.ToDateTime(s.ScheduledTime ?? new TimeOnly(23, 59));
            if (when <= now || when > horizon) continue;

            var key = $"sched:workout:{s.Id}:{when:yyyyMMddHH}";
            var timeText = s.ScheduledTime.HasValue ? when.ToString("ddd HH:mm") : when.ToString("ddd");
            await AddAsync(userId, "schedule", "Upcoming workout",
                $"{s.Workout?.Name ?? "Workout"} is scheduled for {timeText}", "/dashboard", key);
        }

        var meals = await _db.DietSchedules
            .Include(m => m.Food)
            .Where(m => m.UserId == userId
                        && m.Status == "planned"
                        && (m.ScheduledDate == today || m.ScheduledDate == tomorrow))
            .ToListAsync();

        foreach (var m in meals)
        {
            var when = m.ScheduledDate.ToDateTime(m.ScheduledTime ?? new TimeOnly(23, 59));
            if (when <= now || when > horizon) continue;

            var key = $"sched:meal:{m.Id}:{when:yyyyMMddHH}";
            var timeText = m.ScheduledTime.HasValue ? when.ToString("ddd HH:mm") : when.ToString("ddd");
            await AddAsync(userId, "schedule", "Upcoming meal",
                $"{m.MealName} is planned for {timeText}", "/diet", key);
        }
    }
}
