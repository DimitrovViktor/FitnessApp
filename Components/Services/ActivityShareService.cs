using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;

namespace FitnessApp.Services;

public class ActivityShareService
{
    private readonly AppDbContext _db;
    private readonly WorkoutService _workouts;

    public ActivityShareService(AppDbContext db, WorkoutService workouts)
    {
        _db = db;
        _workouts = workouts;
    }

    public async Task<List<ActivityPickDto>> GetMyActivitiesAsync(int meId)
    {
        var workouts = await _workouts.GetUserWorkoutsAsync(meId);
        var programs = await _workouts.GetUserProgramsAsync(meId);

        var list = new List<ActivityPickDto>();
        foreach (var w in workouts.OrderBy(x => x.Name))
            list.Add(new ActivityPickDto("workout", w.Id, w.Name, $"{w.WorkoutExercises.Count} exercises"));
        foreach (var p in programs.OrderBy(x => x.Name))
            list.Add(new ActivityPickDto("program", p.Id, p.Name, $"{p.Workouts.Count} workouts"));
        return list;
    }

    public async Task<Dictionary<int, SharedActivityDto>> GetWorkoutSummariesAsync(List<int> workoutIds)
    {
        var map = new Dictionary<int, SharedActivityDto>();
        if (workoutIds.Count == 0) return map;

        var rows = await _db.Workouts
            .Include(x => x.WorkoutExercises)
            .Where(x => workoutIds.Contains(x.Id))
            .ToListAsync();

        foreach (var w in rows)
            map[w.Id] = new SharedActivityDto("workout", w.Id, w.Name, WorkoutLine(w.WorkoutExercises));
        return map;
    }

    public async Task<Dictionary<int, SharedActivityDto>> GetProgramSummariesAsync(List<int> programIds)
    {
        var map = new Dictionary<int, SharedActivityDto>();
        if (programIds.Count == 0) return map;

        var rows = await _db.Programs
            .Include(x => x.Workouts)
            .Where(x => programIds.Contains(x.Id))
            .ToListAsync();

        foreach (var p in rows)
            map[p.Id] = new SharedActivityDto("program", p.Id, p.Name, ProgramLine(p.DurationWeeks, p.DaysPerWeek, p.Workouts.Count));
        return map;
    }

    public async Task<SharedActivityDto?> GetSummaryAsync(int? workoutId, int? programId)
    {
        if (workoutId is not null)
        {
            var map = await GetWorkoutSummariesAsync(new List<int> { workoutId.Value });
            return map.TryGetValue(workoutId.Value, out var w) ? w : null;
        }
        if (programId is not null)
        {
            var map = await GetProgramSummariesAsync(new List<int> { programId.Value });
            return map.TryGetValue(programId.Value, out var p) ? p : null;
        }
        return null;
    }

    public async Task<WorkoutActivityDto?> GetWorkoutPreviewAsync(int workoutId)
    {
        var w = await _db.Workouts
            .Include(x => x.WorkoutExercises).ThenInclude(we => we.Exercise).ThenInclude(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Include(x => x.Program)
            .FirstOrDefaultAsync(x => x.Id == workoutId);
        if (w is null) return null;

        var exercises = w.WorkoutExercises.OrderBy(x => x.SortOrder).Select(we => new ActivityExerciseDto(
            we.Exercise.Name,
            string.Join(", ", we.Exercise.ExerciseMuscleGroups.Where(m => m.IsPrimary).Select(m => m.MuscleGroup.Name)),
            we.Sets,
            we.Reps)).ToList();

        return new WorkoutActivityDto(w.Id, w.Name, w.Program?.Name,
            w.WorkoutExercises.Count,
            w.WorkoutExercises.Sum(x => x.Sets),
            (int)Math.Round(WorkoutService.EstimateWorkoutMinutesFromWE(w.WorkoutExercises)),
            exercises);
    }

    public async Task<ProgramActivityDto?> GetProgramPreviewAsync(int programId)
    {
        var p = await _db.Programs
            .Include(x => x.Workouts).ThenInclude(w => w.WorkoutExercises)
            .FirstOrDefaultAsync(x => x.Id == programId);
        if (p is null) return null;

        var workouts = p.Workouts.OrderBy(x => x.SortOrder).Select(w => new ProgramWorkoutDto(
            w.Name, w.WorkoutExercises.Count, w.WorkoutExercises.Sum(x => x.Sets))).ToList();

        return new ProgramActivityDto(p.Id, p.Name, p.TargetLevel, p.TargetGoal,
            p.DurationWeeks, p.DaysPerWeek, p.Description, workouts);
    }

    public async Task AddWorkoutToMeAsync(int meId, int workoutId) => await _workouts.CopyWorkoutToUserAsync(workoutId, meId);

    public async Task AddProgramToMeAsync(int meId, int programId) => await _workouts.CopyProgramToUserAsync(programId, meId);

    private static string WorkoutLine(ICollection<WorkoutExercise> exercises) =>
        $"{exercises.Count} exercises \u00b7 {exercises.Sum(x => x.Sets)} sets \u00b7 ~{(int)Math.Round(WorkoutService.EstimateWorkoutMinutesFromWE(exercises))} min";

    private static string ProgramLine(int weeks, int days, int workoutCount) =>
        $"{weeks}w \u00b7 {days}d/wk \u00b7 {workoutCount} workouts";
}

public record SharedActivityDto(string Kind, int RefId, string Name, string Line);

public record ActivityPickDto(string Kind, int Id, string Name, string Line);

public record WorkoutActivityDto(int Id, string Name, string? ProgramName, int ExerciseCount, int SetCount, int Minutes, List<ActivityExerciseDto> Exercises);

public record ActivityExerciseDto(string Name, string Muscles, int Sets, int Reps);

public record ProgramActivityDto(int Id, string Name, string? Level, string? Goal, int Weeks, int Days, string? Description, List<ProgramWorkoutDto> Workouts);

public record ProgramWorkoutDto(string Name, int ExerciseCount, int SetCount);
