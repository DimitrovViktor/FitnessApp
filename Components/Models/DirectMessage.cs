namespace FitnessApp.Models;

public class DirectMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public string? Content { get; set; }
    public string? AttachmentData { get; set; }
    public string? AttachmentName { get; set; }
    public string? AttachmentType { get; set; }
    public bool IsImage { get; set; }
    public long AttachmentSize { get; set; }
    public int? SharedWorkoutId { get; set; }
    public int? SharedProgramId { get; set; }
    public bool IsRead { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
