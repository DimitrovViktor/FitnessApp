using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;
using FitnessApp.Models.Enums;

namespace FitnessApp.Services;

public class CardioService
{
    private readonly AppDbContext _db;

    public CardioService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CardioActivity>> GetActivitiesAsync(int userId) =>
        await _db.CardioActivities
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Name)
            .ToListAsync();

    public async Task<CardioActivity?> GetActivityAsync(int id, int userId) =>
        await _db.CardioActivities.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

    public async Task<CardioActivity> CreateActivityAsync(int userId, string name, CardioIntensity intensity)
    {
        var maxSort = await _db.CardioActivities.Where(a => a.UserId == userId).MaxAsync(a => (int?)a.SortOrder) ?? -1;
        var activity = new CardioActivity
        {
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(name) ? "Cardio" : name.Trim(),
            Intensity = intensity,
            SortOrder = maxSort + 1
        };
        _db.CardioActivities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public async Task<bool> UpdateActivityAsync(int id, int userId, string name, CardioIntensity intensity)
    {
        var activity = await GetActivityAsync(id, userId);
        if (activity is null) return false;
        activity.Name = string.IsNullOrWhiteSpace(name) ? "Cardio" : name.Trim();
        activity.Intensity = intensity;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteActivityAsync(int id, int userId)
    {
        var activity = await GetActivityAsync(id, userId);
        if (activity is null) return false;
        _db.CardioActivities.Remove(activity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> GetCurrentWeightKgAsync(int userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        return user?.WeightKg ?? 70m;
    }

    public static decimal EstimateCalories(decimal weightKg, int activeSeconds, decimal? distanceKm, CardioIntensity intensity)
    {
        if (activeSeconds <= 0) return 0;
        var hours = activeSeconds / 3600m;

        decimal met;
        if (distanceKm is > 0)
        {
            var speed = distanceKm.Value / hours;
            met = MetForSpeed(speed);
        }
        else
        {
            met = intensity switch
            {
                CardioIntensity.Low => 3.5m,
                CardioIntensity.Moderate => 6.0m,
                CardioIntensity.High => 9.8m,
                CardioIntensity.Interval => 8.5m,
                _ => 6.0m
            };
        }

        return Math.Round(met * weightKg * hours, 0);
    }

    private static decimal MetForSpeed(decimal speedKmh)
    {
        if (speedKmh <= 0m) return 2.5m;

        var met = speedKmh <= 7m
            ? 0.6m + 0.7m * speedKmh
            : 2.1m + 0.78m * speedKmh;

        return met < 2.0m ? 2.0m : met;
    }

    public async Task<CardioLog> SaveCardioLogAsync(int userId, int? activityId, string activityName, CardioIntensity intensity,
        int activeSeconds, int restSeconds, int laps, string? lapSplitsCsv, decimal? distanceKm, decimal calories, DateTime startedAt)
    {
        var log = new CardioLog
        {
            UserId = userId,
            CardioActivityId = activityId,
            Date = DateOnly.FromDateTime(startedAt),
            ActivityName = string.IsNullOrWhiteSpace(activityName) ? "Cardio" : activityName.Trim(),
            Intensity = intensity,
            ActiveTimeSec = activeSeconds,
            RestTimeSec = restSeconds,
            Laps = laps,
            LapSplitsCsv = lapSplitsCsv,
            DistanceKm = distanceKm,
            CaloriesBurned = calories,
            StartedAt = startedAt,
            CreatedAt = DateTime.UtcNow
        };
        _db.CardioLogs.Add(log);
        await _db.SaveChangesAsync();
        return log;
    }

    public async Task<List<CardioLog>> GetHistoryAsync(int userId, int count = 20) =>
        await _db.CardioLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.StartedAt)
            .Take(count)
            .ToListAsync();

    public async Task<bool> DeleteLogAsync(int id, int userId)
    {
        var log = await _db.CardioLogs.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        if (log is null) return false;
        _db.CardioLogs.Remove(log);
        await _db.SaveChangesAsync();
        return true;
    }
}
