namespace FitnessApp.Models;

public class Equipment
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    public ICollection<UserEquipment> UserEquipment { get; set; } = new List<UserEquipment>();
    public ICollection<ExerciseEquipment> ExerciseEquipment { get; set; } = new List<ExerciseEquipment>();
}

public class UserEquipment
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
}
