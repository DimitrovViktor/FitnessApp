using FitnessApp.Models.Enums;

namespace FitnessApp.Models;

public class Exercise
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? ExerciseType { get; set; }
    public int DifficultyRating { get; set; }
    public ExerciseLevel Level { get; set; }
    public MovementType MovementType { get; set; }
    public decimal MetValue { get; set; }
    public decimal? RepTimeSec { get; set; }

    public ICollection<ExerciseMuscleGroup> ExerciseMuscleGroups { get; set; } = new List<ExerciseMuscleGroup>();
    public ICollection<ExerciseEquipment> ExerciseEquipment { get; set; } = new List<ExerciseEquipment>();
    public ICollection<ExerciseMedia> Media { get; set; } = new List<ExerciseMedia>();
    public ICollection<ExerciseAlternative> Alternatives { get; set; } = new List<ExerciseAlternative>();
    public ICollection<ExerciseAlternative> AlternativeOf { get; set; } = new List<ExerciseAlternative>();
    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
}
