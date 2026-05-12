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
        var data = await _db.ExerciseSetLogs
            .Where(s => s.ExerciseLog.WorkoutLog.UserId == userId
                && s.ExerciseLog.WorkoutLog.Date >= weekStart
                && s.IsCompleted)
            .Select(s => new { s.ExerciseLog.Exercise.ExerciseMuscleGroups })
            .ToListAsync();

        var targets = new Dictionary<string, int>
        {
            ["Chest"] = 12, ["Back"] = 12, ["Shoulders"] = 9, ["Biceps"] = 9,
            ["Triceps"] = 9, ["Abs"] = 9, ["Quadriceps"] = 12, ["Hamstrings"] = 9,
            ["Glutes"] = 9, ["Calves"] = 6, ["Traps"] = 6, ["Forearms"] = 6
        };

        var sets = new Dictionary<string, int>();
        foreach (var d in data)
            foreach (var mg in d.ExerciseMuscleGroups.Where(m => m.IsPrimary))
            {
                sets.TryGetValue(mg.MuscleGroup.Name, out int cur);
                sets[mg.MuscleGroup.Name] = cur + 1;
            }

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
            var kcal = await _db.WorkoutLogs
                .Where(wl => wl.UserId == userId && wl.Date >= weekStart && wl.Date <= weekEnd)
                .SumAsync(wl => wl.TotalCaloriesBurned);
            result.Add(new WeeklyCalorieItem { Label = weekStart.ToString("MMM d"), Kcal = (int)kcal });
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

        var grouped = recent.GroupBy(el => el.ExerciseId).Take(5);
        var suggestions = new List<OverloadSuggestion>();

        foreach (var group in grouped)
        {
            var latest = group.First();
            var completedSets = latest.SetLogs.Where(s => s.IsCompleted && s.WeightKg.HasValue).ToList();
            if (completedSets.Count == 0) continue;

            var maxWeight = completedSets.Max(s => s.WeightKg!.Value);
            var allRepsHit = completedSets.All(s => s.RepsCompleted >= latest.SetLogs.Max(x => x.RepsCompleted ?? 0));

            if (allRepsHit && maxWeight > 0)
            {
                suggestions.Add(new OverloadSuggestion
                {
                    ExerciseName = latest.Exercise.Name,
                    Suggestion = $"Add 2.5 kg — {maxWeight + 2.5m} kg",
                    Type = "weight",
                    Reason = $"Completed all {completedSets.Count} sets at {maxWeight} kg"
                });
            }
            else if (completedSets.Count >= 3)
            {
                var avgReps = (int)completedSets.Average(s => s.RepsCompleted ?? 0);
                suggestions.Add(new OverloadSuggestion
                {
                    ExerciseName = latest.Exercise.Name,
                    Suggestion = $"Add 1 rep — {completedSets.Count}×{avgReps + 1}",
                    Type = "reps",
                    Reason = $"Maintained all sets at {maxWeight} kg"
                });
            }
        }

        return suggestions.Take(3).ToList();
    }
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
