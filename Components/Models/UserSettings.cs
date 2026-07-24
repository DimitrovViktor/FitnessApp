namespace FitnessApp.Models;

public class UserSettings
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Theme { get; set; } = "night";
    public string WeightUnit { get; set; } = "kg";
    public string DistanceUnit { get; set; } = "km";
    public string EnergyUnit { get; set; } = "kcal";
    public string CalendarStart { get; set; } = "monday";
    public int RestTimerDefault { get; set; } = 60;
    public bool AutoStartRestTimer { get; set; } = true;
    public bool ShowWeightInSets { get; set; } = true;
    public bool ConfirmBeforeSkip { get; set; } = true;
    public bool WorkoutReminders { get; set; } = true;
    public bool SocialNotifications { get; set; } = true;
    public string SocialVisibility { get; set; } = "public";
}
