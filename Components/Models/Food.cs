namespace FitnessApp.Models;

public class Food
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal CaloriesPer100g { get; set; }
    public decimal? ProteinPer100g { get; set; }
    public decimal? CarbsPer100g { get; set; }
    public decimal? FatPer100g { get; set; }

    public ICollection<FoodPreparation> Preparations { get; set; } = new List<FoodPreparation>();
    public ICollection<FoodLog> FoodLogs { get; set; } = new List<FoodLog>();
}

public class FoodPreparation
{
    public int Id { get; set; }
    public int FoodId { get; set; }
    public Food Food { get; set; } = null!;
    public string Method { get; set; } = "";
    public decimal? CalorieModifier { get; set; }
}

public class FoodLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int FoodId { get; set; }
    public Food Food { get; set; } = null!;
    public int? PreparationId { get; set; }
    public FoodPreparation? Preparation { get; set; }
    public DateOnly Date { get; set; }
    public decimal QuantityGrams { get; set; }
    public decimal CaloriesConsumed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
