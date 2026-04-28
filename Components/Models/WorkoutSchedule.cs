using FitnessApp.Models.Enums;

namespace FitnessApp.Models;

public class WorkoutSchedule
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;
    public DateOnly ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? LiveSessionJson { get; set; }
}

public class LiveSessionState
{
    public List<LiveExerciseState> Exercises { get; set; } = new();
    public int CurrentExerciseIndex { get; set; }
    public int ExpandedExerciseIndex { get; set; } = -1;
}

public class LiveExerciseState
{
    public int ExerciseId { get; set; }
    public int OriginalSets { get; set; }
    public int OriginalReps { get; set; }
    public bool IsSkipped { get; set; }
    public List<LiveSetState> Sets { get; set; } = new();
}

public class LiveSetState
{
    public int Reps { get; set; }
    public decimal? WeightKg { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsSkipped { get; set; }
}
