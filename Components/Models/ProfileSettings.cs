namespace FitnessApp.Models;

public class ProfileSettings
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string NameVisibility { get; set; } = "notset";
    public string BioVisibility { get; set; } = "notset";
    public string LevelVisibility { get; set; } = "notset";
    public string GoalVisibility { get; set; } = "notset";
    public string TrainingDaysVisibility { get; set; } = "notset";
    public string WeightVisibility { get; set; } = "notset";
    public string HeightVisibility { get; set; } = "notset";
    public string AgeVisibility { get; set; } = "notset";
    public string MemberSinceVisibility { get; set; } = "notset";
    public string WorkoutsVisibility { get; set; } = "notset";
}
