using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;
using FitnessApp.Models.Enums;

namespace FitnessApp.Services;

public class ProgressService
{
    private readonly AppDbContext _db;

    public ProgressService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetTotalWorkoutsAsync(int userId)
    {
        return await _db.WorkoutLogs.CountAsync(wl => wl.UserId == userId);
    }

    public async Task<int> GetStreakAsync(int userId)
    {
        var dates = await _db.WorkoutLogs
            .Where(wl => wl.UserId == userId)
            .Select(wl => wl.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync();

        if (dates.Count == 0) return 0;

        var today = DateOnly.FromDateTime(DateTime.Now);
        if (dates[0] != today && dates[0] != today.AddDays(-1)) return 0;

        int streak = 1;
        for (int i = 1; i < dates.Count; i++)
        {
            if (dates[i] == dates[i - 1].AddDays(-1)) streak++;
            else break;
        }
        return streak;
    }

    public async Task<decimal> GetWeeklyVolumeAsync(int userId)
    {
        var weekStart = DateOnly.FromDateTime(DateTime.Now).AddDays(-(int)DateTime.Now.DayOfWeek);
        return await _db.ExerciseSetLogs
            .Where(s => s.ExerciseLog.WorkoutLog.UserId == userId
                && s.ExerciseLog.WorkoutLog.Date >= weekStart
                && s.IsCompleted && s.WeightKg.HasValue && s.RepsCompleted.HasValue)
            .SumAsync(s => s.WeightKg!.Value * s.RepsCompleted!.Value);
    }

    public async Task<int> GetAvgSessionMinutesAsync(int userId)
    {
        var monthStart = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
        var logs = await _db.WorkoutLogs
            .Where(wl => wl.UserId == userId && wl.Date >= monthStart && wl.CompletedAt != null)
            .Select(wl => new { wl.StartedAt, wl.CompletedAt })
            .ToListAsync();

        if (logs.Count == 0) return 0;
        return (int)logs.Average(l => (l.CompletedAt!.Value - l.StartedAt).TotalMinutes);
    }

    public async Task<List<MuscleCoverageItem>> GetMuscleCoverageAsync(int userId)
    {
        var weekStart = DateOnly.FromDateTime(DateTime.Now).AddDays(-(int)DateTime.Now.DayOfWeek);
        var muscleNames = await _db.ExerciseSetLogs
            .Where(s => s.ExerciseLog.WorkoutLog.UserId == userId
                && s.ExerciseLog.WorkoutLog.Date >= weekStart
                && s.IsCompleted)
            .SelectMany(s => s.ExerciseLog.Exercise.ExerciseMuscleGroups
                .Where(mg => mg.IsPrimary && mg.MuscleGroup != null && mg.MuscleGroup.Name != "")
                .Select(mg => mg.MuscleGroup.Name))
            .ToListAsync();

        var targets = new Dictionary<string, int>
        {
            ["Chest"] = 12, ["Back"] = 12, ["Shoulders"] = 9, ["Biceps"] = 9,
            ["Triceps"] = 9, ["Abs"] = 9, ["Quadriceps"] = 12, ["Hamstrings"] = 9,
            ["Glutes"] = 9, ["Calves"] = 6, ["Traps"] = 6, ["Forearms"] = 6
        };

        var sets = muscleNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name)
            .ToDictionary(group => group.Key, group => group.Count());

        return targets.Select(t =>
        {
            sets.TryGetValue(t.Key, out int actual);
            var status = actual == 0 ? "missing" : actual < t.Value * 0.7 ? "low" : actual > t.Value * 1.2 ? "excess" : "good";
            return new MuscleCoverageItem { Name = t.Key, Sets = actual, Target = t.Value, Status = status };
        }).ToList();
    }

    public async Task<List<WeeklyCalorieItem>> GetWeeklyCaloriesAsync(int userId, int weeks = 5)
    {
        var result = new List<WeeklyCalorieItem>();
        var today = DateOnly.FromDateTime(DateTime.Now);

        for (int i = weeks - 1; i >= 0; i--)
        {
            var weekEnd = today.AddDays(-i * 7);
            var weekStart = weekEnd.AddDays(-6);
            var logs = await _db.WorkoutLogs
                .Where(wl => wl.UserId == userId && wl.Date >= weekStart && wl.Date <= weekEnd)
                .Select(wl => wl.TotalCaloriesBurned)
                .ToListAsync();
            var kcal = logs.Sum();
            result.Add(new WeeklyCalorieItem { Label = weekStart.ToString("MMM d"), Kcal = (int)kcal, HasLogs = logs.Count > 0 });
        }
        return result;
    }

    public async Task<List<PersonalRecord>> GetPersonalRecordsAsync(int userId, int count = 6)
    {
        return await _db.PersonalRecords
            .Include(pr => pr.Exercise)
            .Where(pr => pr.UserId == userId)
            .OrderByDescending(pr => pr.Date)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<BodyMeasurement>> GetRecentMeasurementsAsync(int userId, int count = 2)
    {
        return await _db.BodyMeasurements
            .Where(bm => bm.UserId == userId)
            .OrderByDescending(bm => bm.Date)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<BodyMeasurement>> GetAllMeasurementsAsync(int userId)
    {
        return await _db.BodyMeasurements
            .Where(bm => bm.UserId == userId)
            .OrderByDescending(bm => bm.Date)
            .ToListAsync();
    }

    public async Task<List<BodyMeasurement>> GetMeasurementsForChartAsync(int userId)
    {
        return await _db.BodyMeasurements
            .Where(bm => bm.UserId == userId)
            .OrderBy(bm => bm.Date)
            .ToListAsync();
    }

    public async Task<List<PersonalRecord>> GetAllPersonalRecordsAsync(int userId)
    {
        return await _db.PersonalRecords
            .Include(pr => pr.Exercise)
            .Where(pr => pr.UserId == userId)
            .OrderBy(pr => pr.Date)
            .ToListAsync();
    }

    public async Task<bool> UpdateMeasurementAsync(int id, int userId, decimal? weight, decimal? bodyFat, decimal? chest, decimal? waist)
    {
        var m = await _db.BodyMeasurements.FirstOrDefaultAsync(bm => bm.Id == id && bm.UserId == userId);
        if (m is null) return false;
        m.WeightKg = weight;
        m.BodyFatPercent = bodyFat;
        m.ChestCm = chest;
        m.WaistCm = waist;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMeasurementAsync(int id, int userId)
    {
        var m = await _db.BodyMeasurements.FirstOrDefaultAsync(bm => bm.Id == id && bm.UserId == userId);
        if (m is null) return false;
        _db.BodyMeasurements.Remove(m);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task SaveMeasurementAsync(int userId, decimal? weight, decimal? bodyFat, decimal? chest, decimal? waist)
    {
        _db.BodyMeasurements.Add(new BodyMeasurement
        {
            UserId = userId,
            Date = DateOnly.FromDateTime(DateTime.Now),
            WeightKg = weight,
            BodyFatPercent = bodyFat,
            ChestCm = chest,
            WaistCm = waist
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<ConsistencyDay>> GetConsistencyAsync(int userId, int days = 30)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var start = today.AddDays(-(days - 1));
        var activeDates = await _db.WorkoutLogs
            .Where(wl => wl.UserId == userId && wl.Date >= start && wl.Date <= today)
            .Select(wl => wl.Date)
            .Distinct()
            .ToListAsync();

        var result = new List<ConsistencyDay>();
        for (int i = 0; i < days; i++)
        {
            var date = start.AddDays(i);
            result.Add(new ConsistencyDay { Date = date, IsActive = activeDates.Contains(date) });
        }
        return result;
    }

    public async Task<int> GetBestStreakAsync(int userId)
    {
        var dates = await _db.WorkoutLogs
            .Where(wl => wl.UserId == userId)
            .Select(wl => wl.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        if (dates.Count == 0) return 0;

        int best = 1, current = 1;
        for (int i = 1; i < dates.Count; i++)
        {
            if (dates[i] == dates[i - 1].AddDays(1)) { current++; if (current > best) best = current; }
            else current = 1;
        }
        return best;
    }

    public async Task<List<OverloadSuggestion>> GetOverloadSuggestionsAsync(int userId)
    {
        var recent = await _db.ExerciseLogs
            .Include(el => el.Exercise)
            .Include(el => el.SetLogs)
            .Where(el => el.WorkoutLog.UserId == userId && el.Status != LogStatus.Skipped)
            .OrderByDescending(el => el.WorkoutLog.Date)
            .Take(50)
            .ToListAsync();

        var settings = await _db.UserSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
        var weightUnit = settings?.WeightUnit ?? "kg";
        var grouped = recent.GroupBy(el => el.ExerciseId).Take(5);
        var suggestions = new List<OverloadSuggestion>();

        foreach (var group in grouped)
        {
            var latest = group.First();
            var completedSets = latest.SetLogs.Where(s => s.IsCompleted && s.WeightKg.HasValue).ToList();
            if (completedSets.Count == 0) continue;

            var maxWeight = completedSets.Max(s => s.WeightKg!.Value);
            var nextWeight = SettingsService.ToKg((SettingsService.FromKg(maxWeight, weightUnit) ?? maxWeight) + (weightUnit == "lbs" ? 5m : 2.5m), weightUnit) ?? maxWeight;
            var allRepsHit = completedSets.All(s => s.RepsCompleted >= latest.SetLogs.Max(x => x.RepsCompleted ?? 0));

            if (allRepsHit && maxWeight > 0)
            {
                suggestions.Add(new OverloadSuggestion
                {
                    ExerciseName = latest.Exercise?.Name ?? "Exercise",
                    Suggestion = $"Add {(weightUnit == "lbs" ? "5 lbs" : "2.5 kg")} — {SettingsService.FormatWeight(nextWeight, weightUnit)}",
                    Type = "weight",
                    Reason = $"Completed all {completedSets.Count} sets at {SettingsService.FormatWeight(maxWeight, weightUnit)}"
                });
            }
            else if (completedSets.Count >= 3)
            {
                var avgReps = (int)completedSets.Average(s => s.RepsCompleted ?? 0);
                suggestions.Add(new OverloadSuggestion
                {
                    ExerciseName = latest.Exercise?.Name ?? "Exercise",
                    Suggestion = $"Add 1 rep — {completedSets.Count}×{avgReps + 1}",
                    Type = "reps",
                    Reason = $"Maintained all sets at {SettingsService.FormatWeight(maxWeight, weightUnit)}"
                });
            }
        }

        return suggestions.Take(3).ToList();
    }

    public async Task<ProgressDietSummary> GetDietSummaryAsync(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var sevenStart = today.AddDays(-6);
        var thirtyStart = today.AddDays(-29);

        var todayLogs = await _db.FoodLogs
            .Include(fl => fl.Food)
            .Where(fl => fl.UserId == userId && fl.Date == today)
            .ToListAsync();

        var sevenDayLogs = await _db.FoodLogs
            .Include(fl => fl.Food)
            .Where(fl => fl.UserId == userId && fl.Date >= sevenStart && fl.Date <= today)
            .ToListAsync();

        var dietLoggedDays = await _db.FoodLogs
            .Where(fl => fl.UserId == userId && fl.Date >= thirtyStart && fl.Date <= today)
            .Select(fl => fl.Date)
            .Distinct()
            .CountAsync();

        var scheduledToday = await _db.DietSchedules
            .CountAsync(ds => ds.UserId == userId && ds.ScheduledDate == today && ds.Status != "deleted");

        return new ProgressDietSummary
        {
            CaloriesToday = Round(todayLogs.Sum(GetCalories)),
            ProteinToday = Round(todayLogs.Sum(GetProtein)),
            CarbsToday = Round(todayLogs.Sum(GetCarbs)),
            FatToday = Round(todayLogs.Sum(GetFat)),
            LoggedFoodsToday = todayLogs.Count,
            ScheduledMealsToday = scheduledToday,
            AverageCaloriesSevenDays = Round(sevenDayLogs.Sum(GetCalories) / 7m),
            DietLoggedDays = dietLoggedDays
        };
    }

    public async Task<List<ProgressNutritionDay>> GetWeeklyNutritionAsync(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var start = today.AddDays(-6);
        var logs = await _db.FoodLogs
            .Include(fl => fl.Food)
            .Where(fl => fl.UserId == userId && fl.Date >= start && fl.Date <= today)
            .ToListAsync();

        var result = new List<ProgressNutritionDay>();
        for (var i = 0; i < 7; i++)
        {
            var date = start.AddDays(i);
            var dayLogs = logs.Where(fl => fl.Date == date).ToList();
            result.Add(new ProgressNutritionDay
            {
                Date = date,
                Label = date.ToString("ddd"),
                Calories = Round(dayLogs.Sum(GetCalories)),
                Protein = Round(dayLogs.Sum(GetProtein)),
                Carbs = Round(dayLogs.Sum(GetCarbs)),
                Fat = Round(dayLogs.Sum(GetFat)),
                HasLogs = dayLogs.Count > 0
            });
        }

        return result;
    }

    public async Task<List<ConsistencyDay>> GetDietConsistencyAsync(int userId, int days = 30)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var start = today.AddDays(-(days - 1));
        var loggedDates = await _db.FoodLogs
            .Where(fl => fl.UserId == userId && fl.Date >= start && fl.Date <= today)
            .Select(fl => fl.Date)
            .Distinct()
            .ToListAsync();

        var result = new List<ConsistencyDay>();
        for (var i = 0; i < days; i++)
        {
            var date = start.AddDays(i);
            result.Add(new ConsistencyDay { Date = date, IsActive = loggedDates.Contains(date) });
        }

        return result;
    }

    public async Task<List<ProgressFoodLogItem>> GetRecentFoodLogsAsync(int userId, int count = 20)
    {
        var logs = await _db.FoodLogs
            .Include(fl => fl.Food)
            .Where(fl => fl.UserId == userId)
            .OrderByDescending(fl => fl.Date)
            .ThenByDescending(fl => fl.MealTime)
            .Take(count)
            .ToListAsync();

        return logs.Select(fl => new ProgressFoodLogItem
        {
            Date = fl.Date,
            FoodName = fl.Food?.Name ?? "Food removed",
            MealName = string.IsNullOrWhiteSpace(fl.MealName) ? "Meal" : fl.MealName,
            MealTime = fl.MealTime,
            Grams = fl.QuantityGrams,
            Calories = Round(fl.CaloriesConsumed),
            Protein = Round(GetProtein(fl)),
            Carbs = Round(GetCarbs(fl)),
            Fat = Round(GetFat(fl))
        }).ToList();
    }

    public async Task<List<ProgressDietScheduleItem>> GetUpcomingDietSchedulesAsync(int userId, int days = 7)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var end = today.AddDays(days);
        var schedules = await _db.DietSchedules
            .Include(ds => ds.Food)
            .Where(ds => ds.UserId == userId && ds.ScheduledDate >= today && ds.ScheduledDate <= end && ds.Status != "deleted")
            .OrderBy(ds => ds.ScheduledDate)
            .ThenBy(ds => ds.ScheduledTime)
            .Take(12)
            .ToListAsync();

        return schedules.Select(ds => new ProgressDietScheduleItem
        {
            FoodName = ds.Food?.Name ?? "Food removed",
            ScheduledDate = ds.ScheduledDate,
            ScheduledTime = ds.ScheduledTime,
            MealName = string.IsNullOrWhiteSpace(ds.MealName) ? "Meal" : ds.MealName,
            QuantityGrams = ds.QuantityGrams,
            Status = ds.Status
        }).ToList();
    }

    private static decimal GetCalories(FoodLog log) => log.CaloriesConsumed;

    private static decimal GetProtein(FoodLog log) => (((log.Food?.ProteinPer100g) ?? 0m) * log.QuantityGrams) / 100m;

    private static decimal GetCarbs(FoodLog log) => (((log.Food?.CarbsPer100g) ?? 0m) * log.QuantityGrams) / 100m;

    private static decimal GetFat(FoodLog log) => (((log.Food?.FatPer100g) ?? 0m) * log.QuantityGrams) / 100m;

    private static decimal Round(decimal value) => decimal.Round(value, 1, MidpointRounding.AwayFromZero);

}

public class MuscleCoverageItem
{
    public string Name { get; set; } = "";
    public int Sets { get; set; }
    public int Target { get; set; }
    public string Status { get; set; } = "missing";
}

public class WeeklyCalorieItem
{
    public string Label { get; set; } = "";
    public int Kcal { get; set; }
    public bool HasLogs { get; set; }
}

public class OverloadSuggestion
{
    public string ExerciseName { get; set; } = "";
    public string Suggestion { get; set; } = "";
    public string Type { get; set; } = "";
    public string Reason { get; set; } = "";
}

public class ConsistencyDay
{
    public DateOnly Date { get; set; }
    public bool IsActive { get; set; }
}


public class ProgressDietSummary
{
    public decimal CaloriesToday { get; set; }
    public decimal ProteinToday { get; set; }
    public decimal CarbsToday { get; set; }
    public decimal FatToday { get; set; }
    public int LoggedFoodsToday { get; set; }
    public int ScheduledMealsToday { get; set; }
    public decimal AverageCaloriesSevenDays { get; set; }
    public int DietLoggedDays { get; set; }
}

public class ProgressNutritionDay
{
    public DateOnly Date { get; set; }
    public string Label { get; set; } = "";
    public decimal Calories { get; set; }
    public decimal Protein { get; set; }
    public decimal Carbs { get; set; }
    public decimal Fat { get; set; }
    public bool HasLogs { get; set; }
}

public class ProgressFoodLogItem
{
    public DateOnly Date { get; set; }
    public string FoodName { get; set; } = "";
    public string MealName { get; set; } = "";
    public TimeOnly? MealTime { get; set; }
    public decimal Grams { get; set; }
    public decimal Calories { get; set; }
    public decimal Protein { get; set; }
    public decimal Carbs { get; set; }
    public decimal Fat { get; set; }
}

public class ProgressDietScheduleItem
{
    public string FoodName { get; set; } = "";
    public DateOnly ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    public string MealName { get; set; } = "";
    public decimal QuantityGrams { get; set; }
    public string Status { get; set; } = "planned";
}
