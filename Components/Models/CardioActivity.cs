using FitnessApp.Models.Enums;

namespace FitnessApp.Models;

public class CardioActivity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Name { get; set; } = "";
    public CardioIntensity Intensity { get; set; } = CardioIntensity.Moderate;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
