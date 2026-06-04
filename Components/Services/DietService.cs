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

    public async Task<List<Food>> GetAllAccessibleFoodsAsync(int userId)
    {
        return await _db.Foods
            .Where(f => f.CreatedByUserId == null || f.CreatedByUserId == userId)
            .OrderBy(f => f.DietCategory)
            .ThenBy(f => f.FoodGroup)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<Food?> GetFoodAsync(int userId, int foodId)
    {
        return await GetAccessibleFoodAsync(userId, foodId);
    }

    public async Task<List<FoodLog>> GetFoodLogsAsync(int userId, DateOnly date)
    {
        return await _db.FoodLogs
            .Include(fl => fl.Food)
            .Where(fl => fl.UserId == userId && fl.Date == date)
            .OrderBy(fl => fl.MealTime)
            .ThenBy(fl => fl.CreatedAt)
            .ThenBy(fl => fl.Id)
            .ToListAsync();
    }

    public async Task<List<FoodLog>> GetFoodLogsForRangeAsync(int userId, DateOnly start, DateOnly end)
    {
        return await _db.FoodLogs
            .Include(fl => fl.Food)
            .Where(fl => fl.UserId == userId && fl.Date >= start && fl.Date <= end)
            .OrderBy(fl => fl.Date)
            .ThenBy(fl => fl.MealTime)
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
            FoodGroup = NormalizeFoodGroup(data.FoodGroup),
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
        return await AddFoodLogAsync(userId, foodId, date, null, "Meal", amount, mode, null);
    }

    public async Task<bool> AddFoodLogAsync(int userId, int foodId, DateOnly date, TimeOnly? time, string mealName, decimal amount, string mode, int? dietScheduleId = null)
    {
        var food = await GetAccessibleFoodAsync(userId, foodId);
        if (food is null) return false;

        var grams = ToGrams(food, amount, mode);
        if (grams <= 0) return false;

        _db.FoodLogs.Add(new FoodLog
        {
            UserId = userId,
            FoodId = food.Id,
            DietScheduleId = dietScheduleId,
            Date = date,
            MealTime = time,
            MealName = CleanMealName(mealName),
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

    public async Task<bool> UpdateFoodLogAsync(int userId, int logId, decimal grams, TimeOnly? time, string mealName)
    {
        var log = await _db.FoodLogs
            .Include(fl => fl.Food)
            .FirstOrDefaultAsync(fl => fl.Id == logId && fl.UserId == userId);

        if (log is null) return false;

        grams = Clamp(grams, 1, 100000);
        log.QuantityGrams = grams;
        log.MealTime = time;
        log.MealName = CleanMealName(mealName);
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

    public async Task<List<DietPlan>> GetDietPlansAsync(int userId, string category)
    {
        category = NormalizeCategory(category);
        return await _db.DietPlans
            .Include(dp => dp.Foods).ThenInclude(dpf => dpf.Food)
            .Where(dp => dp.DietCategory == category && (dp.IsPreBuilt || dp.CreatedByUserId == userId))
            .OrderBy(dp => dp.IsPreBuilt ? 1 : 0)
            .ThenBy(dp => dp.Name)
            .ToListAsync();
    }

    public async Task<List<DietPlan>> GetAllAccessibleDietPlansAsync(int userId)
    {
        return await _db.DietPlans
            .Include(dp => dp.Foods).ThenInclude(dpf => dpf.Food)
            .Where(dp => dp.IsPreBuilt || dp.CreatedByUserId == userId)
            .OrderBy(dp => dp.DietCategory)
            .ThenBy(dp => dp.Name)
            .ToListAsync();
    }

    public async Task<DietPlan?> GetDietPlanAsync(int userId, int planId)
    {
        return await _db.DietPlans
            .Include(dp => dp.Foods).ThenInclude(dpf => dpf.Food)
            .FirstOrDefaultAsync(dp => dp.Id == planId && (dp.IsPreBuilt || dp.CreatedByUserId == userId));
    }

    public async Task<DietPlan?> CopyDietPlanAsync(int userId, int planId)
    {
        var source = await GetDietPlanAsync(userId, planId);
        if (source is null) return null;

        var copy = new DietPlan
        {
            Name = source.Name.StartsWith("My ", StringComparison.OrdinalIgnoreCase) ? source.Name : $"My {source.Name}",
            Description = source.Description,
            DietCategory = source.DietCategory,
            TargetLevel = source.TargetLevel,
            TargetGoal = source.TargetGoal,
            DurationWeeks = source.DurationWeeks,
            MealsPerDay = source.MealsPerDay,
            DailyCaloriesTarget = source.DailyCaloriesTarget,
            DailyProteinTarget = source.DailyProteinTarget,
            IsPreBuilt = false,
            CreatedByUserId = userId,
            Notes = source.Notes,
            Tags = source.Tags
        };

        _db.DietPlans.Add(copy);
        await _db.SaveChangesAsync();

        foreach (var item in source.Foods.OrderBy(f => f.DayNumber).ThenBy(f => f.SortOrder))
        {
            _db.DietPlanFoods.Add(new DietPlanFood
            {
                DietPlanId = copy.Id,
                FoodId = item.FoodId,
                DayNumber = item.DayNumber,
                MealName = item.MealName,
                QuantityGrams = item.QuantityGrams,
                SortOrder = item.SortOrder
            });
        }

        await _db.SaveChangesAsync();
        return copy;
    }

    public async Task<DietPlan?> CreateDietPlanAsync(int userId, DietPlanFormData data)
    {
        var name = data.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return null;

        var plan = new DietPlan
        {
            Name = name,
            Description = data.Description.Trim(),
            DietCategory = NormalizeCategory(data.DietCategory),
            TargetLevel = CleanOptional(data.TargetLevel),
            TargetGoal = CleanOptional(data.TargetGoal),
            DurationWeeks = (int)Clamp(data.DurationWeeks, 1, 52),
            MealsPerDay = (int)Clamp(data.MealsPerDay, 1, 8),
            DailyCaloriesTarget = data.DailyCaloriesTarget > 0 ? data.DailyCaloriesTarget : null,
            DailyProteinTarget = data.DailyProteinTarget > 0 ? data.DailyProteinTarget : null,
            IsPreBuilt = false,
            CreatedByUserId = userId,
            Tags = JsonList(NormalizeCategory(data.DietCategory), data.TargetGoal, data.TargetLevel),
            Notes = data.Notes.Trim()
        };

        _db.DietPlans.Add(plan);
        await _db.SaveChangesAsync();

        foreach (var item in data.Items.Where(i => i.FoodId > 0 && i.QuantityGrams > 0).OrderBy(i => i.DayNumber).ThenBy(i => i.SortOrder))
        {
            var accessible = await GetAccessibleFoodAsync(userId, item.FoodId);
            if (accessible is null) continue;

            _db.DietPlanFoods.Add(new DietPlanFood
            {
                DietPlanId = plan.Id,
                FoodId = item.FoodId,
                DayNumber = Math.Max(1, item.DayNumber),
                MealName = CleanMealName(item.MealName),
                QuantityGrams = Clamp(item.QuantityGrams, 1, 5000),
                SortOrder = item.SortOrder
            });
        }

        await _db.SaveChangesAsync();
        return await GetDietPlanAsync(userId, plan.Id);
    }


    public async Task<DietPlan?> CopyDietPlanDayAsync(int userId, int planId, int dayNumber)
    {
        var source = await GetDietPlanAsync(userId, planId);
        if (source is null) return null;

        var items = source.Foods
            .Where(f => f.DayNumber == Math.Max(1, dayNumber))
            .OrderBy(f => f.SortOrder)
            .ToList();

        if (items.Count == 0) return null;

        var copy = new DietPlan
        {
            Name = $"My {source.Name} Day {Math.Max(1, dayNumber)}",
            Description = $"Saved single-day version of {source.Name}.",
            DietCategory = source.DietCategory,
            TargetLevel = source.TargetLevel,
            TargetGoal = source.TargetGoal,
            DurationWeeks = 1,
            MealsPerDay = Math.Max(1, items.Select(i => i.MealName).Distinct(StringComparer.OrdinalIgnoreCase).Count()),
            DailyCaloriesTarget = Round(items.Sum(i => i.Food.CaloriesPer100g * i.QuantityGrams / 100)),
            DailyProteinTarget = Round(items.Sum(i => (i.Food.ProteinPer100g ?? 0) * i.QuantityGrams / 100)),
            IsPreBuilt = false,
            CreatedByUserId = userId,
            Notes = source.Notes,
            Tags = JsonList(source.DietCategory, source.TargetGoal, source.TargetLevel, "Day")
        };

        _db.DietPlans.Add(copy);
        await _db.SaveChangesAsync();

        var sort = 0;
        foreach (var item in items)
        {
            _db.DietPlanFoods.Add(new DietPlanFood
            {
                DietPlanId = copy.Id,
                FoodId = item.FoodId,
                DayNumber = 1,
                MealName = item.MealName,
                QuantityGrams = item.QuantityGrams,
                SortOrder = sort++
            });
        }

        await _db.SaveChangesAsync();
        return await GetDietPlanAsync(userId, copy.Id);
    }

    public async Task<bool> AddFoodToDietPlanAsync(int userId, int planId, int foodId, int dayNumber, string mealName, decimal grams)
    {
        var plan = await _db.DietPlans
            .Include(dp => dp.Foods)
            .FirstOrDefaultAsync(dp => dp.Id == planId && dp.CreatedByUserId == userId && !dp.IsPreBuilt);

        if (plan is null) return false;

        var food = await GetAccessibleFoodAsync(userId, foodId);
        if (food is null) return false;

        grams = Clamp(grams, 1, 100000);
        dayNumber = Math.Max(1, dayNumber);
        var sortOrder = plan.Foods.Count == 0 ? 0 : plan.Foods.Max(f => f.SortOrder) + 1;

        _db.DietPlanFoods.Add(new DietPlanFood
        {
            DietPlanId = plan.Id,
            FoodId = food.Id,
            DayNumber = dayNumber,
            MealName = CleanMealName(mealName),
            QuantityGrams = grams,
            SortOrder = sortOrder
        });

        plan.MealsPerDay = Math.Max(plan.MealsPerDay, plan.Foods.Select(f => f.MealName).Distinct(StringComparer.OrdinalIgnoreCase).Count() + 1);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateDietPlanFoodAsync(int userId, int itemId, int dayNumber, string mealName, decimal grams)
    {
        var item = await _db.DietPlanFoods
            .Include(i => i.DietPlan)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.DietPlan.CreatedByUserId == userId && !i.DietPlan.IsPreBuilt);

        if (item is null) return false;

        item.DayNumber = Math.Max(1, dayNumber);
        item.MealName = CleanMealName(mealName);
        item.QuantityGrams = Clamp(grams, 1, 100000);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDietPlanFoodAsync(int userId, int itemId)
    {
        var item = await _db.DietPlanFoods
            .Include(i => i.DietPlan)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.DietPlan.CreatedByUserId == userId && !i.DietPlan.IsPreBuilt);

        if (item is null) return false;

        _db.DietPlanFoods.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDietPlanAsync(int userId, int planId)
    {
        var plan = await _db.DietPlans.FirstOrDefaultAsync(dp => dp.Id == planId && dp.CreatedByUserId == userId && !dp.IsPreBuilt);
        if (plan is null) return false;

        _db.DietPlans.Remove(plan);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<DietSchedule>> GetDietSchedulesForDateAsync(int userId, DateOnly date)
    {
        return await _db.DietSchedules
            .Include(ds => ds.Food)
            .Include(ds => ds.DietPlan)
            .Where(ds => ds.UserId == userId && ds.ScheduledDate == date)
            .OrderBy(ds => ds.ScheduledTime)
            .ThenBy(ds => ds.MealName)
            .ThenBy(ds => ds.Id)
            .ToListAsync();
    }

    public async Task<List<DietSchedule>> GetDietSchedulesForRangeAsync(int userId, DateOnly start, DateOnly end)
    {
        return await _db.DietSchedules
            .Include(ds => ds.Food)
            .Include(ds => ds.DietPlan)
            .Where(ds => ds.UserId == userId && ds.ScheduledDate >= start && ds.ScheduledDate <= end)
            .OrderBy(ds => ds.ScheduledDate)
            .ThenBy(ds => ds.ScheduledTime)
            .ThenBy(ds => ds.MealName)
            .ToListAsync();
    }

    public async Task<List<DietSchedule>> GetDietSchedulesForMonthAsync(int userId, int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return await GetDietSchedulesForRangeAsync(userId, start, end);
    }

    public async Task<DietSchedule?> ScheduleFoodIfMissingAsync(int userId, int foodId, DateOnly date, TimeOnly? time, string mealName, decimal grams, int? dietPlanId = null)
    {
        var food = await GetAccessibleFoodAsync(userId, foodId);
        if (food is null) return null;

        grams = Clamp(grams, 1, 5000);
        mealName = CleanMealName(mealName);

        var existing = await _db.DietSchedules.FirstOrDefaultAsync(ds => ds.UserId == userId
            && ds.FoodId == foodId
            && ds.ScheduledDate == date
            && ds.ScheduledTime == time
            && ds.MealName == mealName
            && ds.Status != "skipped");

        if (existing is not null) return existing;

        var schedule = new DietSchedule
        {
            UserId = userId,
            FoodId = foodId,
            DietPlanId = dietPlanId,
            ScheduledDate = date,
            ScheduledTime = time,
            MealName = mealName,
            QuantityGrams = grams,
            Status = "planned"
        };

        _db.DietSchedules.Add(schedule);
        await _db.SaveChangesAsync();
        return schedule;
    }

    public async Task<int> ScheduleDietPlanAsync(int userId, int planId, DateOnly startDate, TimeOnly? time, IReadOnlySet<int> days)
    {
        var plan = await GetDietPlanAsync(userId, planId);
        if (plan is null || plan.Foods.Count == 0 || days.Count == 0) return 0;

        var end = startDate.AddDays(plan.DurationWeeks * 7 - 1);
        var scheduled = 0;
        var planDays = plan.Foods.Select(f => f.DayNumber).DefaultIfEmpty(1).Max();
        var activeDay = 1;

        for (var date = startDate; date <= end; date = date.AddDays(1))
        {
            if (!days.Contains((int)date.DayOfWeek)) continue;

            var dayItems = plan.Foods.Where(f => f.DayNumber == activeDay).OrderBy(f => f.SortOrder).ToList();
            if (dayItems.Count == 0)
            {
                dayItems = plan.Foods.OrderBy(f => f.SortOrder).Take(plan.MealsPerDay).ToList();
            }

            var mealIndex = 0;
            foreach (var item in dayItems)
            {
                var mealTime = AddMealOffset(time, mealIndex);
                var created = await ScheduleFoodIfMissingAsync(userId, item.FoodId, date, mealTime, item.MealName, item.QuantityGrams, plan.Id);
                if (created is not null) scheduled++;
                mealIndex++;
            }

            activeDay = activeDay >= planDays ? 1 : activeDay + 1;
        }

        return scheduled;
    }

    public async Task<bool> CompleteDietScheduleAsync(int userId, int scheduleId)
    {
        var schedule = await _db.DietSchedules
            .Include(ds => ds.Food)
            .FirstOrDefaultAsync(ds => ds.Id == scheduleId && ds.UserId == userId);

        if (schedule is null) return false;

        schedule.Status = "completed";
        await _db.SaveChangesAsync();
        await AddFoodLogAsync(userId, schedule.FoodId, schedule.ScheduledDate, schedule.ScheduledTime, schedule.MealName, schedule.QuantityGrams, "grams", schedule.Id);
        return true;
    }

    public async Task<bool> SkipDietScheduleAsync(int userId, int scheduleId)
    {
        var schedule = await _db.DietSchedules.FirstOrDefaultAsync(ds => ds.Id == scheduleId && ds.UserId == userId);
        if (schedule is null) return false;
        schedule.Status = "skipped";
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDietScheduleAsync(int userId, int scheduleId)
    {
        var schedule = await _db.DietSchedules.FirstOrDefaultAsync(ds => ds.Id == scheduleId && ds.UserId == userId);
        if (schedule is null) return false;
        _db.DietSchedules.Remove(schedule);
        await _db.SaveChangesAsync();
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

    private static TimeOnly? AddMealOffset(TimeOnly? start, int index)
    {
        if (!start.HasValue) return null;
        return start.Value.AddHours(Math.Min(index, 6) * 3);
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

    private static string CleanMealName(string? mealName)
    {
        var value = mealName?.Trim();
        return string.IsNullOrWhiteSpace(value) ? "Meal" : value.Length > 80 ? value[..80] : value;
    }

    private static string? CleanOptional(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
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
            "weight loss" => "loss",
            "gain" => "gain",
            "weight gain" => "gain",
            "maintenance" => "maintenance",
            "maintain" => "maintenance",
            _ => "maintenance"
        };
    }

    public static string NormalizeFoodGroup(string? group)
    {
        var value = group?.Trim();
        return string.IsNullOrWhiteSpace(value) ? "Other" : value.Length > 64 ? value[..64] : value;
    }

    private static string JsonList(params string?[] items)
    {
        return System.Text.Json.JsonSerializer.Serialize(items.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i!.Trim()).ToList());
    }
}

public class CustomFoodFormData
{
    public string Name { get; set; } = "";
    public string DietCategory { get; set; } = "maintenance";
    public string FoodGroup { get; set; } = "Other";
    public string ServingUnit { get; set; } = "serving";
    public decimal ServingGrams { get; set; } = 100;
    public decimal CaloriesPerServing { get; set; } = 100;
    public decimal? ProteinPerServing { get; set; }
    public decimal? CarbsPerServing { get; set; }
    public decimal? FatPerServing { get; set; }
}

public class DietPlanFormData
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string DietCategory { get; set; } = "maintenance";
    public string TargetLevel { get; set; } = "Beginner";
    public string TargetGoal { get; set; } = "General Health";
    public int DurationWeeks { get; set; } = 4;
    public int MealsPerDay { get; set; } = 3;
    public decimal? DailyCaloriesTarget { get; set; }
    public decimal? DailyProteinTarget { get; set; }
    public string Notes { get; set; } = "";
    public List<DietPlanFoodFormData> Items { get; set; } = new();
}

public class DietPlanFoodFormData
{
    public int FoodId { get; set; }
    public int DayNumber { get; set; } = 1;
    public string MealName { get; set; } = "Meal";
    public decimal QuantityGrams { get; set; } = 100;
    public int SortOrder { get; set; }
}
