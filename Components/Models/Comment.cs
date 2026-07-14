namespace FitnessApp.Models;

public class Comment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public int? ParentCommentId { get; set; }
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public string Content { get; set; } = "";
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<CommentReaction> Reactions { get; set; } = new List<CommentReaction>();
}

public class CommentReaction
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public Comment Comment { get; set; } = null!;
    public int UserId { get; set; }
    public bool IsLike { get; set; }
}
