using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;
using FitnessApp.Models.Enums;

namespace FitnessApp.Services;

public class DirectMessageService
{
    private readonly AppDbContext _db;

    public DirectMessageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserSearchResult>> SearchUsersAsync(int meId, string? query, int limit = 8)
    {
        query = query?.Trim() ?? "";
        if (query.Length == 0) return new();

        var users = await _db.Users
            .Where(u => u.Id != meId && u.Username.Contains(query))
            .OrderBy(u => u.Username)
            .Take(limit)
            .ToListAsync();

        if (users.Count == 0) return new();

        var ids = users.Select(u => u.Id).ToList();
        var settings = await _db.ProfileSettings.Where(p => ids.Contains(p.UserId)).ToListAsync();

        return users
            .Select(u => new UserSearchResult(
                u.Id,
                u.Username,
                u.AvatarData,
                PresenceStatus.Normalize(u.Status),
                BuildPublicProfile(u, settings.FirstOrDefault(p => p.UserId == u.Id))))
            .ToList();
    }

    public async Task<PublicProfileDto?> GetPublicProfileAsync(int targetId)
    {
        var user = await _db.Users.FindAsync(targetId);
        if (user is null) return null;
        var settings = await _db.ProfileSettings.FirstOrDefaultAsync(p => p.UserId == targetId);
        return BuildPublicProfile(user, settings);
    }

    private static PublicProfileDto BuildPublicProfile(User user, ProfileSettings? settings)
    {
        bool Pub(string? visibility) => visibility == "public";

        var displayName = settings is not null && Pub(settings.NameVisibility) && !string.IsNullOrWhiteSpace(user.FullName)
            ? user.FullName
            : null;

        var bio = settings is not null && Pub(settings.BioVisibility) && !string.IsNullOrWhiteSpace(user.Bio)
            ? user.Bio
            : null;

        var stats = new List<ProfileStat>();
        if (settings is not null)
        {
            if (Pub(settings.LevelVisibility) && user.FitnessLevel.HasValue)
                stats.Add(new ProfileStat("Fitness level", user.FitnessLevel.Value.ToString()));
            if (Pub(settings.GoalVisibility) && user.PrimaryGoal.HasValue)
                stats.Add(new ProfileStat("Primary goal", FormatGoal(user.PrimaryGoal.Value)));
            if (Pub(settings.TrainingDaysVisibility) && user.TrainingDaysPerWeek.HasValue)
                stats.Add(new ProfileStat("Training", $"{user.TrainingDaysPerWeek} days / week"));
            if (Pub(settings.WeightVisibility) && user.WeightKg.HasValue)
                stats.Add(new ProfileStat("Weight", $"{user.WeightKg.Value:0.#} kg"));
            if (Pub(settings.HeightVisibility) && user.HeightCm.HasValue)
                stats.Add(new ProfileStat("Height", $"{user.HeightCm.Value:0.#} cm"));
            if (Pub(settings.AgeVisibility) && user.DateOfBirth.HasValue)
            {
                var age = AgeFrom(user.DateOfBirth.Value);
                if (age > 0) stats.Add(new ProfileStat("Age", $"{age} years"));
            }
            if (Pub(settings.MemberSinceVisibility))
                stats.Add(new ProfileStat("Member since", user.CreatedAt.ToLocalTime().ToString("MMM yyyy")));
        }

        return new PublicProfileDto(
            user.Id,
            user.Username,
            user.AvatarData,
            PresenceStatus.Normalize(user.Status),
            displayName,
            bio,
            stats);
    }

    public async Task<List<ConversationDto>> GetConversationsAsync(int meId)
    {
        var conversations = await _db.Conversations
            .Where(c => c.User1Id == meId || c.User2Id == meId)
            .ToListAsync();

        if (conversations.Count == 0) return new();

        var conversationIds = conversations.Select(c => c.Id).ToList();
        var otherIds = conversations.Select(c => c.User1Id == meId ? c.User2Id : c.User1Id).Distinct().ToList();
        var others = await _db.Users.Where(u => otherIds.Contains(u.Id)).ToListAsync();

        var metas = await _db.DirectMessages
            .Where(m => conversationIds.Contains(m.ConversationId))
            .Select(m => new MessageMeta(m.Id, m.ConversationId, m.SenderId, m.Content, m.IsImage, m.AttachmentName, m.IsRead, m.CreatedAt))
            .ToListAsync();

        var lastByConversation = metas
            .GroupBy(m => m.ConversationId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.CreatedAt).First());

        var unreadByConversation = metas
            .Where(m => m.SenderId != meId && !m.IsRead)
            .GroupBy(m => m.ConversationId)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<ConversationDto>();
        foreach (var conversation in conversations)
        {
            var otherId = conversation.User1Id == meId ? conversation.User2Id : conversation.User1Id;
            var other = others.FirstOrDefault(u => u.Id == otherId);
            if (other is null) continue;

            lastByConversation.TryGetValue(conversation.Id, out var last);
            unreadByConversation.TryGetValue(conversation.Id, out var unread);

            result.Add(new ConversationDto(
                conversation.Id,
                otherId,
                other.Username,
                other.AvatarData,
                PresenceStatus.Normalize(other.Status),
                last is null ? null : PreviewOf(last.Content, last.IsImage, last.AttachmentName),
                conversation.LastMessageAt,
                unread));
        }

        return result.OrderByDescending(c => c.LastMessageAt).ToList();
    }

    public async Task<ConversationDto?> GetOrCreateConversationAsync(int meId, int otherId)
    {
        if (meId == otherId) return null;
        var other = await _db.Users.FindAsync(otherId);
        if (other is null) return null;

        var lowId = Math.Min(meId, otherId);
        var highId = Math.Max(meId, otherId);

        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.User1Id == lowId && c.User2Id == highId);

        if (conversation is null)
        {
            conversation = new Conversation
            {
                User1Id = lowId,
                User2Id = highId,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();
        }

        return new ConversationDto(
            conversation.Id,
            otherId,
            other.Username,
            other.AvatarData,
            PresenceStatus.Normalize(other.Status),
            null,
            conversation.LastMessageAt,
            0);
    }

    public async Task<bool> IsParticipantAsync(int meId, int conversationId)
    {
        return await _db.Conversations
            .AnyAsync(c => c.Id == conversationId && (c.User1Id == meId || c.User2Id == meId));
    }

    public async Task<List<ChatMessageDto>> GetMessagesAsync(int meId, int conversationId, int limit = 300)
    {
        if (!await IsParticipantAsync(meId, conversationId)) return new();

        var messages = await _db.DirectMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();

        messages.Reverse();
        return messages.Select(ToDto).ToList();
    }

    public async Task MarkReadAsync(int meId, int conversationId)
    {
        var unread = await _db.DirectMessages
            .Where(m => m.ConversationId == conversationId && m.SenderId != meId && !m.IsRead)
            .ToListAsync();

        if (unread.Count == 0) return;
        foreach (var message in unread) message.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task<ChatMessageDto?> SendMessageAsync(int meId, int conversationId, string? content, AttachmentInput? attachment)
    {
        if (!await IsParticipantAsync(meId, conversationId)) return null;

        content = content?.Trim();
        if (string.IsNullOrEmpty(content) && attachment is null) return null;

        var conversation = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId);
        if (conversation is null) return null;

        var message = new DirectMessage
        {
            ConversationId = conversationId,
            SenderId = meId,
            Content = string.IsNullOrEmpty(content) ? null : content,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        if (attachment is not null)
        {
            message.AttachmentData = attachment.Data;
            message.AttachmentName = attachment.Name;
            message.AttachmentType = attachment.Type;
            message.IsImage = attachment.IsImage;
            message.AttachmentSize = attachment.Size;
        }

        _db.DirectMessages.Add(message);
        conversation.LastMessageAt = message.CreatedAt;
        await _db.SaveChangesAsync();

        return ToDto(message);
    }

    private static string PreviewOf(string? content, bool isImage, string? attachmentName)
    {
        if (!string.IsNullOrWhiteSpace(content)) return content!;
        if (isImage) return "Photo";
        return string.IsNullOrWhiteSpace(attachmentName) ? "Attachment" : attachmentName!;
    }

    private static ChatMessageDto ToDto(DirectMessage m) => new(
        m.Id,
        m.ConversationId,
        m.SenderId,
        m.Content,
        m.AttachmentData,
        m.AttachmentName,
        m.AttachmentType,
        m.IsImage,
        m.AttachmentSize,
        m.CreatedAt);

    private static int AgeFrom(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dob.Year;
        if (dob > today.AddYears(-age)) age--;
        return age;
    }

    private static string FormatGoal(PrimaryGoal goal) => goal switch
    {
        PrimaryGoal.FatLoss => "Fat Loss",
        PrimaryGoal.GeneralHealth => "General Health",
        _ => goal.ToString()
    };

    private sealed record MessageMeta(int Id, int ConversationId, int SenderId, string? Content, bool IsImage, string? AttachmentName, bool IsRead, DateTime CreatedAt);
}

public record UserSearchResult(int Id, string Username, string? AvatarData, string Status, PublicProfileDto Profile);

public record PublicProfileDto(int Id, string Username, string? AvatarData, string Status, string? DisplayName, string? Bio, List<ProfileStat> Stats);

public record ProfileStat(string Label, string Value);

public record ConversationDto(int Id, int OtherUserId, string OtherUsername, string? OtherAvatarData, string OtherStatus, string? LastMessagePreview, DateTime LastMessageAt, int UnreadCount);

public record ChatMessageDto(int Id, int ConversationId, int SenderId, string? Content, string? AttachmentData, string? AttachmentName, string? AttachmentType, bool IsImage, long AttachmentSize, DateTime CreatedAt);

public class AttachmentInput
{
    public string Data { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsImage { get; set; }
    public long Size { get; set; }
}
