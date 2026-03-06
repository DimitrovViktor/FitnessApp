namespace FitnessApp.Models;

public class BodyMeasurement
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly Date { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? BodyFatPercent { get; set; }
    public decimal? ChestCm { get; set; }
    public decimal? WaistCm { get; set; }
    public decimal? HipsCm { get; set; }
    public decimal? BicepsCm { get; set; }
    public decimal? ThighsCm { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PersonalRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public decimal WeightKg { get; set; }
    public int Reps { get; set; }
    public DateOnly Date { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DailyLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly Date { get; set; }
    public string? Notes { get; set; }
    public int? EnergyLevel { get; set; }
    public int? MoodRating { get; set; }
    public decimal TotalCaloriesBurned { get; set; }
    public decimal TotalCaloriesConsumed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UserGoal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? CurrentValue { get; set; }
    public string? Unit { get; set; }
    public DateOnly? TargetDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
