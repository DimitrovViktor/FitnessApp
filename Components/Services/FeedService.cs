using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;

namespace FitnessApp.Services;

public class FeedService
{
    private readonly AppDbContext _db;
    private readonly DirectMessageService _dm;
    private readonly ActivityShareService _activities;
    private readonly PresenceTracker _presence;

    public FeedService(AppDbContext db, DirectMessageService dm, ActivityShareService activities, PresenceTracker presence)
    {
        _db = db;
        _dm = dm;
        _activities = activities;
        _presence = presence;
    }

    public async Task<PostDto?> CreatePostAsync(int meId, string? content, string? imageData, int? sharedWorkoutId, int? sharedProgramId)
    {
        content = content?.Trim();
        if (string.IsNullOrEmpty(content) && string.IsNullOrEmpty(imageData) && sharedWorkoutId is null && sharedProgramId is null) return null;

        var post = new Post
        {
            AuthorId = meId,
            Content = string.IsNullOrEmpty(content) ? null : content,
            ImageData = string.IsNullOrEmpty(imageData) ? null : imageData,
            SharedWorkoutId = sharedWorkoutId,
            SharedProgramId = sharedProgramId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        return await BuildPostDtoAsync(meId, post.Id);
    }

    public async Task<PostDto?> EditPostAsync(int meId, int postId, string? content)
    {
        content = content?.Trim();
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post is null || post.AuthorId != meId) return null;
        if (string.IsNullOrEmpty(content) && string.IsNullOrEmpty(post.ImageData)) return null;

        post.Content = string.IsNullOrEmpty(content) ? null : content;
        post.IsEdited = true;
        post.EditedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await BuildPostDtoAsync(meId, post.Id);
    }

    public async Task<bool> DeletePostAsync(int meId, int postId)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post is null || post.AuthorId != meId) return false;

        var commentIds = await _db.Comments.Where(c => c.PostId == postId).Select(c => c.Id).ToListAsync();
        var commentReactions = await _db.CommentReactions.Where(r => commentIds.Contains(r.CommentId)).ToListAsync();
        var comments = await _db.Comments.Where(c => c.PostId == postId).ToListAsync();
        var postReactions = await _db.PostReactions.Where(r => r.PostId == postId).ToListAsync();
        var postShares = await _db.PostShares.Where(s => s.PostId == postId).ToListAsync();

        _db.CommentReactions.RemoveRange(commentReactions);
        _db.Comments.RemoveRange(comments);
        _db.PostReactions.RemoveRange(postReactions);
        _db.PostShares.RemoveRange(postShares);
        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<PostDto>> GetFeedAsync(int meId, int limit = 50)
    {
        var posts = await _db.Posts
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync();

        var dtos = await BuildPostDtosAsync(meId, posts);
        return dtos.OrderByDescending(p => p.CreatedAt).ToList();
    }

    public async Task<ProfileFeedDto?> GetUserProfileAsync(int meId, int targetId, int limit = 50)
    {
        var user = await _db.Users.FindAsync(targetId);
        if (user is null) return null;

        var authoredIds = await _db.Posts.Where(p => p.AuthorId == targetId).Select(p => p.Id).ToListAsync();
        var sharedIds = await _db.PostShares.Where(s => s.UserId == targetId).Select(s => s.PostId).ToListAsync();
        var allIds = authoredIds.Concat(sharedIds).Distinct().ToList();

        var posts = await _db.Posts.Where(p => allIds.Contains(p.Id)).ToListAsync();
        var dtos = await BuildPostDtosAsync(meId, posts);
        dtos = dtos.OrderByDescending(p => p.CreatedAt).Take(limit).ToList();

        var preview = await _dm.GetProfilePreviewAsync(meId, targetId);
        var bio = preview?.Profile.Bio;
        var stats = preview?.Profile.Stats ?? new List<ProfileStat>();

        return new ProfileFeedDto(user.Id, user.Username, user.AvatarData, PresenceStatus.Effective(user.Status, _presence.IsOnline(user.Id)), bio, stats, dtos, sharedIds);
    }

    public async Task<PostShareDto?> ToggleShareAsync(int meId, int postId)
    {
        if (!await _db.Posts.AnyAsync(p => p.Id == postId)) return null;

        var existing = await _db.PostShares.FirstOrDefaultAsync(s => s.PostId == postId && s.UserId == meId);
        if (existing is null)
            _db.PostShares.Add(new PostShare { PostId = postId, UserId = meId, CreatedAt = DateTime.UtcNow });
        else
            _db.PostShares.Remove(existing);
        await _db.SaveChangesAsync();

        var shares = await _db.PostShares.Where(s => s.PostId == postId).Select(s => s.UserId).ToListAsync();
        return new PostShareDto(postId, shares.Count, shares.Contains(meId));
    }

    private async Task<List<PostDto>> BuildPostDtosAsync(int meId, List<Post> posts)
    {
        if (posts.Count == 0) return new();

        var postIds = posts.Select(p => p.Id).ToList();
        var authorIds = posts.Select(p => p.AuthorId).Distinct().ToList();
        var authors = await _db.Users.Where(u => authorIds.Contains(u.Id)).ToListAsync();

        var reactions = await _db.PostReactions
            .Where(r => postIds.Contains(r.PostId))
            .Select(r => new { r.PostId, r.UserId, r.IsLike })
            .ToListAsync();

        var commentPostIds = await _db.Comments
            .Where(c => postIds.Contains(c.PostId))
            .Select(c => c.PostId)
            .ToListAsync();

        var shares = await _db.PostShares
            .Where(s => postIds.Contains(s.PostId))
            .Select(s => new { s.PostId, s.UserId })
            .ToListAsync();

        var wIds = posts.Where(p => p.SharedWorkoutId is not null).Select(p => p.SharedWorkoutId!.Value).Distinct().ToList();
        var pIds = posts.Where(p => p.SharedProgramId is not null).Select(p => p.SharedProgramId!.Value).Distinct().ToList();
        var wMap = await _activities.GetWorkoutSummariesAsync(wIds);
        var pMap = await _activities.GetProgramSummariesAsync(pIds);

        var result = new List<PostDto>();
        foreach (var post in posts)
        {
            var author = authors.FirstOrDefault(u => u.Id == post.AuthorId);
            var postReactions = reactions.Where(r => r.PostId == post.Id).ToList();
            var postShares = shares.Where(s => s.PostId == post.Id).ToList();
            SharedActivityDto? activity = null;
            if (post.SharedWorkoutId is not null) wMap.TryGetValue(post.SharedWorkoutId.Value, out activity);
            else if (post.SharedProgramId is not null) pMap.TryGetValue(post.SharedProgramId.Value, out activity);

            result.Add(new PostDto(
                post.Id,
                post.AuthorId,
                author?.Username ?? "User",
                author?.AvatarData,
                PresenceStatus.Effective(author?.Status, author is not null && _presence.IsOnline(author.Id)),
                post.Content,
                post.ImageData,
                post.IsEdited,
                post.CreatedAt,
                postReactions.Count(r => r.IsLike),
                postReactions.Count(r => !r.IsLike),
                MyReactionValue(postReactions.Where(r => r.UserId == meId).Select(r => (bool?)r.IsLike).FirstOrDefault()),
                commentPostIds.Count(id => id == post.Id),
                postShares.Count,
                postShares.Any(s => s.UserId == meId),
                activity));
        }
        return result;
    }

    public async Task<PostReactionDto?> ReactPostAsync(int meId, int postId, bool isLike)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post is null) return null;

        var existing = await _db.PostReactions.FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == meId);
        if (existing is null)
        {
            _db.PostReactions.Add(new PostReaction { PostId = postId, UserId = meId, IsLike = isLike });
        }
        else if (existing.IsLike == isLike)
        {
            _db.PostReactions.Remove(existing);
        }
        else
        {
            existing.IsLike = isLike;
        }
        await _db.SaveChangesAsync();

        var reactions = await _db.PostReactions.Where(r => r.PostId == postId)
            .Select(r => new { r.UserId, r.IsLike }).ToListAsync();
        return new PostReactionDto(
            postId,
            reactions.Count(r => r.IsLike),
            reactions.Count(r => !r.IsLike),
            MyReactionValue(reactions.Where(r => r.UserId == meId).Select(r => (bool?)r.IsLike).FirstOrDefault()));
    }

    public async Task<List<CommentDto>> GetCommentsAsync(int meId, int postId)
    {
        var comments = await _db.Comments
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        if (comments.Count == 0) return new();

        var commentIds = comments.Select(c => c.Id).ToList();
        var authorIds = comments.Select(c => c.AuthorId).Distinct().ToList();
        var authors = await _db.Users.Where(u => authorIds.Contains(u.Id)).ToListAsync();

        var reactions = await _db.CommentReactions
            .Where(r => commentIds.Contains(r.CommentId))
            .Select(r => new { r.CommentId, r.UserId, r.IsLike })
            .ToListAsync();

        return comments.Select(c =>
        {
            var author = authors.FirstOrDefault(u => u.Id == c.AuthorId);
            var commentReactions = reactions.Where(r => r.CommentId == c.Id).ToList();
            return new CommentDto(
                c.Id,
                c.PostId,
                c.ParentCommentId,
                c.AuthorId,
                author?.Username ?? "User",
                author?.AvatarData,
                c.Content,
                c.IsEdited,
                c.CreatedAt,
                commentReactions.Count(r => r.IsLike),
                commentReactions.Count(r => !r.IsLike),
                MyReactionValue(commentReactions.Where(r => r.UserId == meId).Select(r => (bool?)r.IsLike).FirstOrDefault()));
        }).ToList();
    }

    public async Task<CommentDto?> AddCommentAsync(int meId, int postId, int? parentCommentId, string? content)
    {
        content = content?.Trim();
        if (string.IsNullOrEmpty(content)) return null;
        if (!await _db.Posts.AnyAsync(p => p.Id == postId)) return null;

        int? parentId = null;
        if (parentCommentId is not null)
        {
            var parent = await _db.Comments.FirstOrDefaultAsync(c => c.Id == parentCommentId && c.PostId == postId);
            if (parent is not null)
                parentId = parent.ParentCommentId ?? parent.Id;
        }

        var comment = new Comment
        {
            PostId = postId,
            ParentCommentId = parentId,
            AuthorId = meId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        var author = await _db.Users.FindAsync(meId);
        return new CommentDto(comment.Id, postId, parentId, meId, author?.Username ?? "User", author?.AvatarData,
            comment.Content, false, comment.CreatedAt, 0, 0, 0);
    }

    public async Task<CommentDto?> EditCommentAsync(int meId, int commentId, string? content)
    {
        content = content?.Trim();
        if (string.IsNullOrEmpty(content)) return null;

        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment is null || comment.AuthorId != meId) return null;

        comment.Content = content;
        comment.IsEdited = true;
        comment.EditedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var author = await _db.Users.FindAsync(meId);
        var reactions = await _db.CommentReactions.Where(r => r.CommentId == commentId)
            .Select(r => new { r.UserId, r.IsLike }).ToListAsync();
        return new CommentDto(comment.Id, comment.PostId, comment.ParentCommentId, meId, author?.Username ?? "User",
            author?.AvatarData, comment.Content, true, comment.CreatedAt,
            reactions.Count(r => r.IsLike), reactions.Count(r => !r.IsLike),
            MyReactionValue(reactions.Where(r => r.UserId == meId).Select(r => (bool?)r.IsLike).FirstOrDefault()));
    }

    public async Task<bool> DeleteCommentAsync(int meId, int commentId)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment is null || comment.AuthorId != meId) return false;

        var threadIds = await _db.Comments
            .Where(c => c.Id == commentId || c.ParentCommentId == commentId)
            .Select(c => c.Id)
            .ToListAsync();

        var reactions = await _db.CommentReactions.Where(r => threadIds.Contains(r.CommentId)).ToListAsync();
        var comments = await _db.Comments.Where(c => threadIds.Contains(c.Id)).ToListAsync();

        _db.CommentReactions.RemoveRange(reactions);
        _db.Comments.RemoveRange(comments);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<CommentReactionDto?> ReactCommentAsync(int meId, int commentId, bool isLike)
    {
        if (!await _db.Comments.AnyAsync(c => c.Id == commentId)) return null;

        var existing = await _db.CommentReactions.FirstOrDefaultAsync(r => r.CommentId == commentId && r.UserId == meId);
        if (existing is null)
        {
            _db.CommentReactions.Add(new CommentReaction { CommentId = commentId, UserId = meId, IsLike = isLike });
        }
        else if (existing.IsLike == isLike)
        {
            _db.CommentReactions.Remove(existing);
        }
        else
        {
            existing.IsLike = isLike;
        }
        await _db.SaveChangesAsync();

        var reactions = await _db.CommentReactions.Where(r => r.CommentId == commentId)
            .Select(r => new { r.UserId, r.IsLike }).ToListAsync();
        return new CommentReactionDto(
            commentId,
            reactions.Count(r => r.IsLike),
            reactions.Count(r => !r.IsLike),
            MyReactionValue(reactions.Where(r => r.UserId == meId).Select(r => (bool?)r.IsLike).FirstOrDefault()));
    }

    private async Task<PostDto?> BuildPostDtoAsync(int meId, int postId)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post is null) return null;
        var author = await _db.Users.FindAsync(post.AuthorId);
        var reactions = await _db.PostReactions.Where(r => r.PostId == postId)
            .Select(r => new { r.UserId, r.IsLike }).ToListAsync();
        var commentCount = await _db.Comments.CountAsync(c => c.PostId == postId);
        var shares = await _db.PostShares.Where(s => s.PostId == postId).Select(s => s.UserId).ToListAsync();

        var activity = await _activities.GetSummaryAsync(post.SharedWorkoutId, post.SharedProgramId);

        return new PostDto(
            post.Id,
            post.AuthorId,
            author?.Username ?? "User",
            author?.AvatarData,
            PresenceStatus.Effective(author?.Status, author is not null && _presence.IsOnline(author.Id)),
            post.Content,
            post.ImageData,
            post.IsEdited,
            post.CreatedAt,
            reactions.Count(r => r.IsLike),
            reactions.Count(r => !r.IsLike),
            MyReactionValue(reactions.Where(r => r.UserId == meId).Select(r => (bool?)r.IsLike).FirstOrDefault()),
            commentCount,
            shares.Count,
            shares.Contains(meId),
            activity);
    }

    private static int MyReactionValue(bool? isLike)
    {
        if (isLike is null) return 0;
        return isLike.Value ? 1 : -1;
    }
}

public record PostDto(int Id, int AuthorId, string AuthorUsername, string? AuthorAvatarData, string AuthorStatus, string? Content, string? ImageData, bool IsEdited, DateTime CreatedAt, int LikeCount, int DislikeCount, int MyReaction, int CommentCount, int ShareCount, bool SharedByMe, SharedActivityDto? SharedActivity);

public record PostShareDto(int PostId, int ShareCount, bool SharedByMe);

public record ProfileFeedDto(int UserId, string Username, string? AvatarData, string Status, string? Bio, List<ProfileStat> Stats, List<PostDto> Posts, List<int> SharedPostIds);

public record PostReactionDto(int PostId, int LikeCount, int DislikeCount, int MyReaction);

public record CommentDto(int Id, int PostId, int? ParentCommentId, int AuthorId, string AuthorUsername, string? AuthorAvatarData, string Content, bool IsEdited, DateTime CreatedAt, int LikeCount, int DislikeCount, int MyReaction);

public record CommentReactionDto(int CommentId, int LikeCount, int DislikeCount, int MyReaction);
