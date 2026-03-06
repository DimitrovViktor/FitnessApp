namespace FitnessApp.Models;

public class MuscleGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? BodyRegion { get; set; }

    public ICollection<ExerciseMuscleGroup> ExerciseMuscleGroups { get; set; } = new List<ExerciseMuscleGroup>();
}
