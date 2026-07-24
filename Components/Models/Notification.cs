namespace FitnessApp.Models;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Kind { get; set; } = "system";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string DedupeKey { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
