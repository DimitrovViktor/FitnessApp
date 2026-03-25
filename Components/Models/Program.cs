namespace FitnessApp.Models;

public class Program
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int DurationWeeks { get; set; }
    public int DaysPerWeek { get; set; }
    public string? TargetLevel { get; set; }
    public string? TargetGoal { get; set; }
    public bool IsPreBuilt { get; set; }
    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<Workout> Workouts { get; set; } = new List<Workout>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
