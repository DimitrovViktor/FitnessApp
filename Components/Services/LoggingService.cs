using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;
using FitnessApp.Models.Enums;

namespace FitnessApp.Services;

public class LoggingService
{
    private readonly AppDbContext _db;

    public LoggingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<WorkoutLog> SaveWorkoutLogAsync(int userId, int? workoutId, DateTime startedAt, DateTime completedAt, decimal caloriesBurned, List<ExerciseLogEntry> exercises)
    {
        var log = new WorkoutLog
        {
            UserId = userId,
            WorkoutId = workoutId,
            Date = DateOnly.FromDateTime(startedAt),
            StartedAt = startedAt,
            CompletedAt = completedAt,
            TotalCaloriesBurned = caloriesBurned
        };

        _db.WorkoutLogs.Add(log);
        await _db.SaveChangesAsync();

        for (int i = 0; i < exercises.Count; i++)
        {
            var entry = exercises[i];
            var exLog = new ExerciseLog
            {
                WorkoutLogId = log.Id,
                ExerciseId = entry.ExerciseId,
                Status = entry.Status,
                CaloriesBurned = entry.CaloriesBurned,
                SortOrder = i
            };

            _db.ExerciseLogs.Add(exLog);
            await _db.SaveChangesAsync();

            for (int s = 0; s < entry.Sets.Count; s++)
            {
                var setEntry = entry.Sets[s];
                _db.ExerciseSetLogs.Add(new ExerciseSetLog
                {
                    ExerciseLogId = exLog.Id,
                    SetNumber = s + 1,
                    RepsCompleted = setEntry.RepsCompleted,
                    WeightKg = setEntry.WeightKg,
                    IsCompleted = setEntry.IsCompleted
                });
            }
            await _db.SaveChangesAsync();
        }

        await UpdateDailyCaloriesAsync(userId, log.Date);
        return log;
    }

    public async Task<List<WorkoutLog>> GetWorkoutLogsForDateAsync(int userId, DateOnly date)
    {
        return await _db.WorkoutLogs
            .Include(wl => wl.Workout)
            .Include(wl => wl.ExerciseLogs).ThenInclude(el => el.Exercise)
                .ThenInclude(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Include(wl => wl.ExerciseLogs).ThenInclude(el => el.SetLogs)
            .Where(wl => wl.UserId == userId && wl.Date == date)
            .OrderBy(wl => wl.StartedAt)
            .ToListAsync();
    }

    public async Task<List<WorkoutLog>> GetWorkoutLogsForRangeAsync(int userId, DateOnly start, DateOnly end)
    {
        return await _db.WorkoutLogs
            .Include(wl => wl.Workout)
            .Include(wl => wl.ExerciseLogs).ThenInclude(el => el.Exercise)
            .Include(wl => wl.ExerciseLogs).ThenInclude(el => el.SetLogs)
            .Where(wl => wl.UserId == userId && wl.Date >= start && wl.Date <= end)
            .OrderBy(wl => wl.Date).ThenBy(wl => wl.StartedAt)
            .ToListAsync();
    }

    public async Task<DailyLog?> GetDailyLogAsync(int userId, DateOnly date)
    {
        return await _db.DailyLogs.FirstOrDefaultAsync(dl => dl.UserId == userId && dl.Date == date);
    }

    public async Task<DailyLog> SaveDailyLogAsync(int userId, DateOnly date, string? notes, int? energyLevel, int? moodRating)
    {
        var existing = await _db.DailyLogs.FirstOrDefaultAsync(dl => dl.UserId == userId && dl.Date == date);

        if (existing is not null)
        {
            existing.Notes = notes;
            existing.EnergyLevel = energyLevel;
            existing.MoodRating = moodRating;
            await _db.SaveChangesAsync();
            return existing;
        }

        var workoutCals = await _db.WorkoutLogs
            .Where(wl => wl.UserId == userId && wl.Date == date)
            .SumAsync(wl => wl.TotalCaloriesBurned);

        var log = new DailyLog
        {
            UserId = userId,
            Date = date,
            Notes = notes,
            EnergyLevel = energyLevel,
            MoodRating = moodRating,
            TotalCaloriesBurned = workoutCals
        };

        _db.DailyLogs.Add(log);
        await _db.SaveChangesAsync();
        return log;
    }

    public async Task<List<DaySummary>> GetWeekSummaryAsync(int userId, DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(6);
        var logs = await GetWorkoutLogsForRangeAsync(userId, weekStart, weekEnd);
        var dailyLogs = await _db.DailyLogs
            .Where(dl => dl.UserId == userId && dl.Date >= weekStart && dl.Date <= weekEnd)
            .ToListAsync();

        var result = new List<DaySummary>();
        for (int i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            var dayLogs = logs.Where(l => l.Date == date).ToList();
            var daily = dailyLogs.FirstOrDefault(dl => dl.Date == date);

            result.Add(new DaySummary
            {
                Date = date,
                WorkoutCount = dayLogs.Count,
                TotalSets = dayLogs.Sum(l => l.ExerciseLogs.Sum(el => el.SetLogs.Count(s => s.IsCompleted))),
                TotalExercises = dayLogs.Sum(l => l.ExerciseLogs.Count(el => el.Status != LogStatus.Skipped)),
                CaloriesBurned = dayLogs.Sum(l => l.TotalCaloriesBurned),
                Notes = daily?.Notes,
                EnergyLevel = daily?.EnergyLevel,
                MoodRating = daily?.MoodRating
            });
        }

        return result;
    }

    public async Task<List<PersonalRecord>> GetRecentPersonalRecordsAsync(int userId, int count = 5)
    {
        return await _db.PersonalRecords
            .Include(pr => pr.Exercise)
            .Where(pr => pr.UserId == userId)
            .OrderByDescending(pr => pr.Date)
            .Take(count)
            .ToListAsync();
    }

    public async Task CheckAndSavePersonalRecordsAsync(int userId, List<ExerciseLogEntry> exercises)
    {
        foreach (var entry in exercises.Where(e => e.Status != LogStatus.Skipped))
        {
            foreach (var set in entry.Sets.Where(s => s.IsCompleted && s.WeightKg.HasValue && s.RepsCompleted.HasValue))
            {
                var existing = await _db.PersonalRecords
                    .FirstOrDefaultAsync(pr => pr.UserId == userId && pr.ExerciseId == entry.ExerciseId && pr.Reps == set.RepsCompleted!.Value);

                if (existing is null || set.WeightKg!.Value > existing.WeightKg)
                {
                    if (existing is not null)
                    {
                        existing.WeightKg = set.WeightKg!.Value;
                        existing.Date = DateOnly.FromDateTime(DateTime.Now);
                    }
                    else
                    {
                        _db.PersonalRecords.Add(new PersonalRecord
                        {
                            UserId = userId,
                            ExerciseId = entry.ExerciseId,
                            WeightKg = set.WeightKg!.Value,
                            Reps = set.RepsCompleted!.Value,
                            Date = DateOnly.FromDateTime(DateTime.Now)
                        });
                    }
                    await _db.SaveChangesAsync();
                }
            }
        }
    }

    public async Task<TrackingSummary> GetTrackingSummaryAsync(int userId, DateOnly date)
    {
        var logs = await GetWorkoutLogsForDateAsync(userId, date);
        var dailyLog = await GetDailyLogAsync(userId, date);

        var schedules = await _db.WorkoutSchedules
            .Include(ws => ws.Workout)
            .Where(ws => ws.UserId == userId && ws.ScheduledDate == date)
            .ToListAsync();

        return new TrackingSummary
        {
            WorkoutLogs = logs,
            DailyLog = dailyLog,
            Schedules = schedules,
            TotalWorkouts = logs.Count,
            TotalExercises = logs.Sum(l => l.ExerciseLogs.Count(el => el.Status != LogStatus.Skipped)),
            TotalSets = logs.Sum(l => l.ExerciseLogs.Sum(el => el.SetLogs.Count(s => s.IsCompleted))),
            TotalCalories = logs.Sum(l => l.TotalCaloriesBurned),
            SkippedExercises = logs.Sum(l => l.ExerciseLogs.Count(el => el.Status == LogStatus.Skipped)),
            ModifiedExercises = logs.Sum(l => l.ExerciseLogs.Count(el => el.Status == LogStatus.Modified))
        };
    }

    private async Task UpdateDailyCaloriesAsync(int userId, DateOnly date)
    {
        var totalCals = await _db.WorkoutLogs
            .Where(wl => wl.UserId == userId && wl.Date == date)
            .SumAsync(wl => wl.TotalCaloriesBurned);

        var daily = await _db.DailyLogs.FirstOrDefaultAsync(dl => dl.UserId == userId && dl.Date == date);
        if (daily is not null)
        {
            daily.TotalCaloriesBurned = totalCals;
            await _db.SaveChangesAsync();
        }
    }
}

public class ExerciseLogEntry
{
    public int ExerciseId { get; set; }
    public LogStatus Status { get; set; }
    public decimal CaloriesBurned { get; set; }
    public List<SetLogEntry> Sets { get; set; } = new();
}

public class SetLogEntry
{
    public int? RepsCompleted { get; set; }
    public decimal? WeightKg { get; set; }
    public bool IsCompleted { get; set; }
}

public class DaySummary
{
    public DateOnly Date { get; set; }
    public int WorkoutCount { get; set; }
    public int TotalSets { get; set; }
    public int TotalExercises { get; set; }
    public decimal CaloriesBurned { get; set; }
    public string? Notes { get; set; }
    public int? EnergyLevel { get; set; }
    public int? MoodRating { get; set; }
}

public class TrackingSummary
{
    public List<WorkoutLog> WorkoutLogs { get; set; } = new();
    public DailyLog? DailyLog { get; set; }
    public List<WorkoutSchedule> Schedules { get; set; } = new();
    public int TotalWorkouts { get; set; }
    public int TotalExercises { get; set; }
    public int TotalSets { get; set; }
    public decimal TotalCalories { get; set; }
    public int SkippedExercises { get; set; }
    public int ModifiedExercises { get; set; }
}
