namespace FitnessApp.Models;

public class Conversation
{
    public int Id { get; set; }
    public int User1Id { get; set; }
    public int User2Id { get; set; }
    public User User1 { get; set; } = null!;
    public User User2 { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public ICollection<DirectMessage> Messages { get; set; } = new List<DirectMessage>();
}
