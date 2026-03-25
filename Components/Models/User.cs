using FitnessApp.Models.Enums;

namespace FitnessApp.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsAdmin { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public FitnessLevel? FitnessLevel { get; set; }
    public PrimaryGoal? PrimaryGoal { get; set; }
    public int? TrainingDaysPerWeek { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool OnboardingCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserEquipment> UserEquipment { get; set; } = new List<UserEquipment>();
    public ICollection<UserInjury> Injuries { get; set; } = new List<UserInjury>();
    public ICollection<Workout> Workouts { get; set; } = new List<Workout>();
    public ICollection<WorkoutLog> WorkoutLogs { get; set; } = new List<WorkoutLog>();
    public ICollection<BodyMeasurement> BodyMeasurements { get; set; } = new List<BodyMeasurement>();
    public ICollection<FoodLog> FoodLogs { get; set; } = new List<FoodLog>();
    public ICollection<CardioLog> CardioLogs { get; set; } = new List<CardioLog>();
    public ICollection<DailyLog> DailyLogs { get; set; } = new List<DailyLog>();
    public ICollection<PersonalRecord> PersonalRecords { get; set; } = new List<PersonalRecord>();
    public ICollection<UserGoal> Goals { get; set; } = new List<UserGoal>();
}
