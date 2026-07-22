using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;
using FitnessApp.Models.Enums;

namespace FitnessApp.Services;

public class DashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        return await _db.Users
            .Include(u => u.UserEquipment)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }


    public async Task<List<WorkoutSchedule>> GetSchedulesForRangeAsync(int userId, DateOnly start, DateOnly end, bool includeCancelled = false)
    {
        var query = _db.WorkoutSchedules
            .Include(ws => ws.Workout).ThenInclude(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
            .Include(ws => ws.Workout).ThenInclude(w => w.Program)
            .Where(ws => ws.UserId == userId && ws.ScheduledDate >= start && ws.ScheduledDate <= end);

        if (!includeCancelled)
        {
            query = query.Where(ws => ws.Status != ScheduleStatus.Cancelled);
        }

        return await query
            .OrderBy(ws => ws.ScheduledDate)
            .ThenBy(ws => ws.ScheduledTime)
            .ThenBy(ws => ws.Workout.Name)
            .ToListAsync();
    }

    public async Task<List<WorkoutSchedule>> GetSchedulesForMonthAsync(int userId, int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        return await _db.WorkoutSchedules
            .Include(ws => ws.Workout).ThenInclude(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
            .Include(ws => ws.Workout).ThenInclude(w => w.Program)
            .Where(ws => ws.UserId == userId && ws.ScheduledDate >= start && ws.ScheduledDate <= end)
            .OrderBy(ws => ws.ScheduledDate).ThenBy(ws => ws.ScheduledTime)
            .ToListAsync();
    }

    public async Task<List<WorkoutSchedule>> GetSchedulesForDateAsync(int userId, DateOnly date)
    {
        return await _db.WorkoutSchedules
            .Include(ws => ws.Workout).ThenInclude(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Include(ws => ws.Workout).ThenInclude(w => w.Program)
            .Where(ws => ws.UserId == userId && ws.ScheduledDate == date)
            .OrderBy(ws => ws.ScheduledTime)
            .ToListAsync();
    }

    public async Task<WorkoutSchedule?> GetScheduleByIdAsync(int id, int userId)
    {
        return await _db.WorkoutSchedules
            .Include(ws => ws.Workout).ThenInclude(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Include(ws => ws.Workout).ThenInclude(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.ExerciseEquipment).ThenInclude(ee => ee.Equipment)
            .FirstOrDefaultAsync(ws => ws.Id == id && ws.UserId == userId);
    }

    public async Task<WorkoutSchedule> ScheduleWorkoutAsync(int userId, int workoutId, DateOnly date, TimeOnly? time)
    {
        var schedule = new WorkoutSchedule
        {
            UserId = userId,
            WorkoutId = workoutId,
            ScheduledDate = date,
            ScheduledTime = time,
            Status = ScheduleStatus.Scheduled
        };

        _db.WorkoutSchedules.Add(schedule);
        await _db.SaveChangesAsync();
        return schedule;
    }


    public async Task<WorkoutSchedule> ScheduleWorkoutIfMissingAsync(int userId, int workoutId, DateOnly date, TimeOnly? time)
    {
        var existing = await _db.WorkoutSchedules
            .FirstOrDefaultAsync(ws => ws.UserId == userId
                && ws.WorkoutId == workoutId
                && ws.ScheduledDate == date
                && ws.ScheduledTime == time
                && ws.Status != ScheduleStatus.Cancelled);

        if (existing is not null)
        {
            return existing;
        }

        return await ScheduleWorkoutAsync(userId, workoutId, date, time);
    }

    public async Task<bool> StartWorkoutAsync(int scheduleId, int userId)
    {
        var schedule = await _db.WorkoutSchedules.FirstOrDefaultAsync(ws => ws.Id == scheduleId && ws.UserId == userId);
        if (schedule is null) return false;

        schedule.Status = ScheduleStatus.InProgress;
        schedule.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteWorkoutAsync(int scheduleId, int userId)
    {
        var schedule = await _db.WorkoutSchedules.FirstOrDefaultAsync(ws => ws.Id == scheduleId && ws.UserId == userId);
        if (schedule is null) return false;

        schedule.Status = ScheduleStatus.Completed;
        schedule.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PostponeWorkoutAsync(int scheduleId, int userId, int minutesToAdd)
    {
        var schedule = await _db.WorkoutSchedules.FirstOrDefaultAsync(ws => ws.Id == scheduleId && ws.UserId == userId);
        if (schedule is null) return false;

        if (schedule.ScheduledTime.HasValue)
        {
            var newTime = schedule.ScheduledTime.Value.AddMinutes(minutesToAdd);
            if (newTime < schedule.ScheduledTime.Value)
            {
                schedule.ScheduledDate = schedule.ScheduledDate.AddDays(1);
            }
            schedule.ScheduledTime = newTime;
        }

        schedule.Status = ScheduleStatus.Postponed;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelScheduleAsync(int scheduleId, int userId)
    {
        var schedule = await _db.WorkoutSchedules.FirstOrDefaultAsync(ws => ws.Id == scheduleId && ws.UserId == userId);
        if (schedule is null) return false;

        schedule.Status = ScheduleStatus.Cancelled;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteScheduleAsync(int scheduleId, int userId)
    {
        var schedule = await _db.WorkoutSchedules.FirstOrDefaultAsync(ws => ws.Id == scheduleId && ws.UserId == userId);
        if (schedule is null) return false;

        _db.WorkoutSchedules.Remove(schedule);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<WorkoutSchedule?> GetDueScheduleAsync(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var now = TimeOnly.FromDateTime(DateTime.Now);

        return await _db.WorkoutSchedules
            .Include(ws => ws.Workout)
            .Where(ws => ws.UserId == userId
                && ws.ScheduledDate == today
                && ws.Status == ScheduleStatus.Scheduled
                && ws.ScheduledTime.HasValue
                && ws.ScheduledTime.Value <= now
                && ws.ScheduledTime.Value >= now.AddMinutes(-15))
            .OrderBy(ws => ws.ScheduledTime)
            .FirstOrDefaultAsync();
    }

    public async Task<WorkoutSchedule?> GetActiveSessionAsync(int userId)
    {
        return await _db.WorkoutSchedules
            .Include(ws => ws.Workout).ThenInclude(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Include(ws => ws.Workout).ThenInclude(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.ExerciseEquipment).ThenInclude(ee => ee.Equipment)
            .Where(ws => ws.UserId == userId && ws.Status == ScheduleStatus.InProgress)
            .OrderByDescending(ws => ws.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Workout>> GetUserWorkoutsAsync(int userId)
    {
        return await _db.Workouts
            .Include(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
            .Include(w => w.Program)
            .Where(w => w.UserId == userId && (w.Program == null || !w.Program.IsPreBuilt))
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<int> GetWeeklyWorkoutCountAsync(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        return await _db.WorkoutSchedules
            .CountAsync(ws => ws.UserId == userId
                && ws.ScheduledDate >= weekStart
                && ws.ScheduledDate <= today
                && ws.Status == ScheduleStatus.Completed);
    }

    public async Task<int> GetStreakAsync(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        int streak = 0;
        var date = today;

        while (true)
        {
            var hasCompleted = await _db.WorkoutSchedules
                .AnyAsync(ws => ws.UserId == userId && ws.ScheduledDate == date && ws.Status == ScheduleStatus.Completed);

            if (!hasCompleted) break;
            streak++;
            date = date.AddDays(-1);
        }

        return streak;
    }

    public async Task<int> GetWeeklyCardioCountAsync(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        return await _db.CardioLogs
            .CountAsync(cl => cl.UserId == userId && cl.Date >= weekStart && cl.Date <= today);
    }

    public async Task<decimal> GetBurnedCaloriesForDateAsync(int userId, DateOnly date)
    {
        var workoutBurned = await _db.WorkoutLogs
            .Where(wl => wl.UserId == userId && wl.Date == date)
            .SumAsync(wl => (decimal?)wl.TotalCaloriesBurned) ?? 0m;

        var cardioBurned = await _db.CardioLogs
            .Where(cl => cl.UserId == userId && cl.Date == date)
            .SumAsync(cl => (decimal?)cl.CaloriesBurned) ?? 0m;

        return workoutBurned + cardioBurned;
    }

    public async Task<List<WorkoutLog>> GetWorkoutLogsForDateAsync(int userId, DateOnly date)
    {
        return await _db.WorkoutLogs
            .Include(wl => wl.Workout)
            .Where(wl => wl.UserId == userId && wl.Date == date)
            .OrderByDescending(wl => wl.StartedAt)
            .ToListAsync();
    }

    public async Task<List<CardioLog>> GetCardioLogsForDateAsync(int userId, DateOnly date)
    {
        return await _db.CardioLogs
            .Where(cl => cl.UserId == userId && cl.Date == date)
            .OrderByDescending(cl => cl.StartedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteWorkoutSessionAsync(int workoutLogId, int userId)
    {
        var log = await _db.WorkoutLogs
            .Include(wl => wl.ExerciseLogs).ThenInclude(el => el.SetLogs)
            .FirstOrDefaultAsync(wl => wl.Id == workoutLogId && wl.UserId == userId);
        if (log is null) return false;

        if (log.WorkoutId is not null)
        {
            var schedule = await _db.WorkoutSchedules
                .Where(ws => ws.UserId == userId && ws.WorkoutId == log.WorkoutId && ws.ScheduledDate == log.Date && ws.Status == ScheduleStatus.Completed)
                .OrderByDescending(ws => ws.CompletedAt)
                .FirstOrDefaultAsync();
            if (schedule is not null) _db.WorkoutSchedules.Remove(schedule);
        }

        foreach (var el in log.ExerciseLogs)
            _db.ExerciseSetLogs.RemoveRange(el.SetLogs);
        _db.ExerciseLogs.RemoveRange(log.ExerciseLogs);
        _db.WorkoutLogs.Remove(log);
        await _db.SaveChangesAsync();

        var daily = await _db.DailyLogs.FirstOrDefaultAsync(dl => dl.UserId == userId && dl.Date == log.Date);
        if (daily is not null)
        {
            daily.TotalCaloriesBurned = await _db.WorkoutLogs
                .Where(wl => wl.UserId == userId && wl.Date == log.Date)
                .SumAsync(wl => (decimal?)wl.TotalCaloriesBurned) ?? 0m;
            await _db.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> DeleteCardioSessionAsync(int cardioLogId, int userId)
    {
        var log = await _db.CardioLogs.FirstOrDefaultAsync(cl => cl.Id == cardioLogId && cl.UserId == userId);
        if (log is null) return false;
        _db.CardioLogs.Remove(log);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteWorkoutSessionByScheduleAsync(int scheduleId, int userId)
    {
        var schedule = await _db.WorkoutSchedules.FirstOrDefaultAsync(ws => ws.Id == scheduleId && ws.UserId == userId);
        if (schedule is null) return false;

        var logs = await _db.WorkoutLogs
            .Include(wl => wl.ExerciseLogs).ThenInclude(el => el.SetLogs)
            .Where(wl => wl.UserId == userId && wl.WorkoutId == schedule.WorkoutId && wl.Date == schedule.ScheduledDate)
            .ToListAsync();

        foreach (var log in logs)
        {
            foreach (var el in log.ExerciseLogs)
                _db.ExerciseSetLogs.RemoveRange(el.SetLogs);
            _db.ExerciseLogs.RemoveRange(log.ExerciseLogs);
        }
        _db.WorkoutLogs.RemoveRange(logs);
        _db.WorkoutSchedules.Remove(schedule);
        await _db.SaveChangesAsync();

        var daily = await _db.DailyLogs.FirstOrDefaultAsync(dl => dl.UserId == userId && dl.Date == schedule.ScheduledDate);
        if (daily is not null)
        {
            daily.TotalCaloriesBurned = await _db.WorkoutLogs
                .Where(wl => wl.UserId == userId && wl.Date == schedule.ScheduledDate)
                .SumAsync(wl => (decimal?)wl.TotalCaloriesBurned) ?? 0m;
            await _db.SaveChangesAsync();
        }

        return true;
    }

    public async Task<WorkoutSchedule> QuickStartWorkoutAsync(int userId, int workoutId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var now = TimeOnly.FromDateTime(DateTime.Now);

        var schedule = new WorkoutSchedule
        {
            UserId = userId,
            WorkoutId = workoutId,
            ScheduledDate = today,
            ScheduledTime = now,
            Status = ScheduleStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        _db.WorkoutSchedules.Add(schedule);
        await _db.SaveChangesAsync();
        return schedule;
    }

    public async Task SaveLiveSessionStateAsync(int scheduleId, int userId, string json)
    {
        var schedule = await _db.WorkoutSchedules.FirstOrDefaultAsync(ws => ws.Id == scheduleId && ws.UserId == userId);
        if (schedule is null) return;
        schedule.LiveSessionJson = json;
        await _db.SaveChangesAsync();
    }
}
