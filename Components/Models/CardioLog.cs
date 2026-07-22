using FitnessApp.Models.Enums;

namespace FitnessApp.Models;

public class CardioLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? CardioActivityId { get; set; }
    public DateOnly Date { get; set; }
    public string ActivityName { get; set; } = "";
    public CardioIntensity Intensity { get; set; }
    public int ActiveTimeSec { get; set; }
    public int RestTimeSec { get; set; }
    public int Laps { get; set; }
    public string? LapSplitsCsv { get; set; }
    public decimal? DistanceKm { get; set; }
    public decimal CaloriesBurned { get; set; }
    public string? Notes { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
