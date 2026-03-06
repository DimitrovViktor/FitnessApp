using FitnessApp.Models.Enums;

namespace FitnessApp.Models;

public class WorkoutLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? WorkoutId { get; set; }
    public Workout? Workout { get; set; }
    public DateOnly Date { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal TotalCaloriesBurned { get; set; }

    public ICollection<ExerciseLog> ExerciseLogs { get; set; } = new List<ExerciseLog>();
}

public class ExerciseLog
{
    public int Id { get; set; }
    public int WorkoutLogId { get; set; }
    public WorkoutLog WorkoutLog { get; set; } = null!;
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public LogStatus Status { get; set; }
    public decimal CaloriesBurned { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }

    public ICollection<ExerciseSetLog> SetLogs { get; set; } = new List<ExerciseSetLog>();
}

public class ExerciseSetLog
{
    public int Id { get; set; }
    public int ExerciseLogId { get; set; }
    public ExerciseLog ExerciseLog { get; set; } = null!;
    public int SetNumber { get; set; }
    public int? RepsCompleted { get; set; }
    public decimal? WeightKg { get; set; }
    public int? DurationSec { get; set; }
    public bool IsCompleted { get; set; }
}
