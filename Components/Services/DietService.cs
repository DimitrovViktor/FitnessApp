using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;

namespace FitnessApp.Services;

public class DietService
{
    private readonly AppDbContext _db;

    public DietService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Food>> GetFoodsAsync(int userId, string category)
    {
        category = NormalizeCategory(category);

        return await _db.Foods
            .Where(f => (f.CreatedByUserId == null || f.CreatedByUserId == userId) && f.DietCategory == category)
            .OrderBy(f => f.IsCustom ? 0 : 1)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<List<FoodLog>> GetFoodLogsAsync(int userId, DateOnly date)
    {
        return await _db.FoodLogs
            .Include(fl => fl.Food)
            .Where(fl => fl.UserId == userId && fl.Date == date)
            .OrderBy(fl => fl.CreatedAt)
            .ThenBy(fl => fl.Id)
            .ToListAsync();
    }

    public async Task<Food?> CreateCustomFoodAsync(int userId, CustomFoodFormData data)
    {
        var servingGrams = Clamp(data.ServingGrams, 1, 5000);
        var unit = string.IsNullOrWhiteSpace(data.ServingUnit) ? "serving" : data.ServingUnit.Trim();
        var name = data.Name.Trim();

        if (name.Length == 0) return null;

        var food = new Food
        {
            Name = name,
            DietCategory = NormalizeCategory(data.DietCategory),
            ServingUnit = unit,
            ServingGrams = servingGrams,
            CaloriesPer100g = Per100g(data.CaloriesPerServing, servingGrams),
            ProteinPer100g = Per100g(data.ProteinPerServing, servingGrams),
            CarbsPer100g = Per100g(data.CarbsPerServing, servingGrams),
            FatPer100g = Per100g(data.FatPerServing, servingGrams),
            IsCustom = true,
            CreatedByUserId = userId
        };

        _db.Foods.Add(food);
        await _db.SaveChangesAsync();
        return food;
    }

    public async Task<bool> AddFoodLogAsync(int userId, int foodId, DateOnly date, decimal amount, string mode)
    {
        var food = await GetAccessibleFoodAsync(userId, foodId);
        if (food is null) return false;

        var grams = ToGrams(food, amount, mode);
        if (grams <= 0) return false;

        _db.FoodLogs.Add(new FoodLog
        {
            UserId = userId,
            FoodId = food.Id,
            Date = date,
            QuantityGrams = grams,
            CaloriesConsumed = Round(food.CaloriesPer100g * grams / 100),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await SyncDailyCaloriesAsync(userId, date);
        return true;
    }

    public async Task<bool> UpdateFoodLogAsync(int userId, int logId, decimal amount, string mode)
    {
        var log = await _db.FoodLogs
            .Include(fl => fl.Food)
            .FirstOrDefaultAsync(fl => fl.Id == logId && fl.UserId == userId);

        if (log is null) return false;

        var grams = ToGrams(log.Food, amount, mode);
        if (grams <= 0) return false;

        log.QuantityGrams = grams;
        log.CaloriesConsumed = Round(log.Food.CaloriesPer100g * grams / 100);
        await _db.SaveChangesAsync();
        await SyncDailyCaloriesAsync(userId, log.Date);
        return true;
    }

    public async Task<bool> DeleteFoodLogAsync(int userId, int logId)
    {
        var log = await _db.FoodLogs.FirstOrDefaultAsync(fl => fl.Id == logId && fl.UserId == userId);
        if (log is null) return false;

        var date = log.Date;
        _db.FoodLogs.Remove(log);
        await _db.SaveChangesAsync();
        await SyncDailyCaloriesAsync(userId, date);
        return true;
    }

    private async Task SyncDailyCaloriesAsync(int userId, DateOnly date)
    {
        var total = await _db.FoodLogs
            .Where(fl => fl.UserId == userId && fl.Date == date)
            .SumAsync(fl => (decimal?)fl.CaloriesConsumed) ?? 0;

        var daily = await _db.DailyLogs.FirstOrDefaultAsync(dl => dl.UserId == userId && dl.Date == date);
        if (daily is null)
        {
            daily = new DailyLog
            {
                UserId = userId,
                Date = date
            };
            _db.DailyLogs.Add(daily);
        }

        daily.TotalCaloriesConsumed = Round(total);
        await _db.SaveChangesAsync();
    }

    private async Task<Food?> GetAccessibleFoodAsync(int userId, int foodId)
    {
        return await _db.Foods.FirstOrDefaultAsync(f => f.Id == foodId && (f.CreatedByUserId == null || f.CreatedByUserId == userId));
    }

    private static decimal ToGrams(Food food, decimal amount, string mode)
    {
        amount = Clamp(amount, 0.01m, 100000m);
        return mode == "grams" ? amount : amount * Math.Max(food.ServingGrams, 1);
    }

    private static decimal? Per100g(decimal? perServing, decimal servingGrams)
    {
        return perServing is null ? null : Round(perServing.Value * 100 / servingGrams);
    }

    private static decimal Per100g(decimal perServing, decimal servingGrams)
    {
        return Round(perServing * 100 / servingGrams);
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    public static string NormalizeCategory(string? category)
    {
        return category?.Trim().ToLowerInvariant() switch
        {
            "loss" => "loss",
            "gain" => "gain",
            "maintenance" => "maintenance",
            _ => "maintenance"
        };
    }
}

public class CustomFoodFormData
{
    public string Name { get; set; } = "";
    public string DietCategory { get; set; } = "maintenance";
    public string ServingUnit { get; set; } = "serving";
    public decimal ServingGrams { get; set; } = 100;
    public decimal CaloriesPerServing { get; set; } = 100;
    public decimal? ProteinPerServing { get; set; }
    public decimal? CarbsPerServing { get; set; }
    public decimal? FatPerServing { get; set; }
}
