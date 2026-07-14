namespace FitnessApp.Models;

public class Post
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public string? Content { get; set; }
    public string? ImageData { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();
    public ICollection<PostShare> Shares { get; set; } = new List<PostShare>();
}

public class PostShare
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PostReaction
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public int UserId { get; set; }
    public bool IsLike { get; set; }
}
