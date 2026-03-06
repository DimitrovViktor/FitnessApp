using FitnessApp.Models.Enums;

namespace FitnessApp.Models;

public class ExerciseMuscleGroup
{
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public int MuscleGroupId { get; set; }
    public MuscleGroup MuscleGroup { get; set; } = null!;
    public bool IsPrimary { get; set; }
}

public class ExerciseEquipment
{
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
}

public class ExerciseAlternative
{
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public int AlternativeExerciseId { get; set; }
    public Exercise AlternativeExercise { get; set; } = null!;
}

public class ExerciseMedia
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public MediaType MediaType { get; set; }
    public string Url { get; set; } = "";
    public string? Title { get; set; }
    public int SortOrder { get; set; }
}
