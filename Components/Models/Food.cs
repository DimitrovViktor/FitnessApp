namespace FitnessApp.Models;

public class Food
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal CaloriesPer100g { get; set; }
    public decimal? ProteinPer100g { get; set; }
    public decimal? CarbsPer100g { get; set; }
    public decimal? FatPer100g { get; set; }
    public string DietCategory { get; set; } = "maintenance";
    public string FoodGroup { get; set; } = "Other";
    public string ServingUnit { get; set; } = "serving";
    public decimal ServingGrams { get; set; } = 100;
    public bool IsCustom { get; set; }
    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public ICollection<FoodPreparation> Preparations { get; set; } = new List<FoodPreparation>();
    public ICollection<FoodLog> FoodLogs { get; set; } = new List<FoodLog>();
    public ICollection<DietPlanFood> DietPlanFoods { get; set; } = new List<DietPlanFood>();
    public ICollection<DietSchedule> DietSchedules { get; set; } = new List<DietSchedule>();
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
    public int? DietScheduleId { get; set; }
    public DietSchedule? DietSchedule { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? MealTime { get; set; }
    public string MealName { get; set; } = "Meal";
    public decimal QuantityGrams { get; set; }
    public decimal CaloriesConsumed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DietPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string DietCategory { get; set; } = "maintenance";
    public string? TargetLevel { get; set; }
    public string? TargetGoal { get; set; }
    public int DurationWeeks { get; set; } = 4;
    public int MealsPerDay { get; set; } = 3;
    public decimal? DailyCaloriesTarget { get; set; }
    public decimal? DailyProteinTarget { get; set; }
    public bool IsPreBuilt { get; set; }
    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<DietPlanFood> Foods { get; set; } = new List<DietPlanFood>();
    public ICollection<DietSchedule> DietSchedules { get; set; } = new List<DietSchedule>();
}

public class DietPlanFood
{
    public int Id { get; set; }
    public int DietPlanId { get; set; }
    public DietPlan DietPlan { get; set; } = null!;
    public int FoodId { get; set; }
    public Food Food { get; set; } = null!;
    public int DayNumber { get; set; } = 1;
    public string MealName { get; set; } = "Meal";
    public decimal QuantityGrams { get; set; } = 100;
    public int SortOrder { get; set; }
}

public class DietSchedule
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int FoodId { get; set; }
    public Food Food { get; set; } = null!;
    public int? DietPlanId { get; set; }
    public DietPlan? DietPlan { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    public string MealName { get; set; } = "Meal";
    public decimal QuantityGrams { get; set; } = 100;
    public string Status { get; set; } = "planned";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
