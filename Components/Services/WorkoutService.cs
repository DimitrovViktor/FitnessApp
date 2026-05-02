using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;
using FitnessApp.Models.Enums;
using ProgramEntity = FitnessApp.Models.Program;

namespace FitnessApp.Services;

public class WorkoutService
{
    private readonly AppDbContext _db;

    public WorkoutService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Workout>> GetUserWorkoutsAsync(int userId)
    {
        return await _db.Workouts
            .Include(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Include(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.Media)
            .Include(w => w.Program)
            .Where(w => w.UserId == userId && (w.Program == null || !w.Program.IsPreBuilt))
            .OrderBy(w => w.SortOrder).ThenBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<List<Workout>> GetPremadeWorkoutsAsync()
    {
        return await _db.Workouts
            .Include(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Include(w => w.Program)
            .Where(w => w.User.IsAdmin && w.ProgramId != null)
            .OrderBy(w => w.Program!.Name).ThenBy(w => w.SortOrder)
            .ToListAsync();
    }

    public async Task<Workout?> GetWorkoutByIdAsync(int id, int userId)
    {
        return await _db.Workouts
            .Include(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Include(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.ExerciseEquipment).ThenInclude(ee => ee.Equipment)
            .Include(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.Media)
            .Include(w => w.Program)
            .FirstOrDefaultAsync(w => w.Id == id && (w.UserId == userId || w.User.IsAdmin));
    }

    public async Task<List<ProgramEntity>> GetAllProgramsAsync()
    {
        return await _db.Programs
            .Include(p => p.Workouts).ThenInclude(w => w.WorkoutExercises)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<ProgramEntity>> GetUserProgramsAsync(int userId)
    {
        return await _db.Programs
            .Include(p => p.Workouts).ThenInclude(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Where(p => p.CreatedByUserId == userId && !p.IsPreBuilt)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<ProgramEntity>> GetPrebuiltProgramsAsync()
    {
        return await _db.Programs
            .Include(p => p.Workouts).ThenInclude(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Where(p => p.IsPreBuilt)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<ProgramEntity?> GetProgramByIdAsync(int id)
    {
        return await _db.Programs
            .Include(p => p.Workouts).ThenInclude(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Include(p => p.Workouts).ThenInclude(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
                .ThenInclude(e => e.ExerciseEquipment).ThenInclude(ee => ee.Equipment)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<ProgramEntity> CopyProgramToUserAsync(int programId, int userId)
    {
        var source = await _db.Programs
            .Include(p => p.Workouts).ThenInclude(w => w.WorkoutExercises)
            .FirstOrDefaultAsync(p => p.Id == programId);

        if (source is null) return null!;

        var program = new ProgramEntity
        {
            Name = source.Name,
            Description = source.Description,
            DurationWeeks = source.DurationWeeks,
            DaysPerWeek = source.DaysPerWeek,
            TargetLevel = source.TargetLevel,
            TargetGoal = source.TargetGoal,
            IsPreBuilt = false,
            CreatedByUserId = userId
        };

        _db.Programs.Add(program);
        await _db.SaveChangesAsync();

        foreach (var sw in source.Workouts.OrderBy(w => w.SortOrder))
        {
            var workout = new Workout
            {
                Name = sw.Name,
                UserId = userId,
                ProgramId = program.Id,
                SortOrder = sw.SortOrder
            };
            _db.Workouts.Add(workout);
            await _db.SaveChangesAsync();

            foreach (var we in sw.WorkoutExercises)
            {
                _db.WorkoutExercises.Add(new WorkoutExercise
                {
                    WorkoutId = workout.Id,
                    ExerciseId = we.ExerciseId,
                    Sets = we.Sets,
                    Reps = we.Reps,
                    RestTimeSec = we.RestTimeSec,
                    SortOrder = we.SortOrder
                });
            }
            await _db.SaveChangesAsync();
        }

        return program;
    }

    public async Task<bool> UpdateProgramAsync(int programId, int userId, string name, string? description, int durationWeeks, int daysPerWeek, string? targetLevel, string? targetGoal)
    {
        var program = await _db.Programs.FirstOrDefaultAsync(p => p.Id == programId && p.CreatedByUserId == userId && !p.IsPreBuilt);
        if (program is null) return false;

        program.Name = name.Trim();
        program.Description = description?.Trim();
        program.DurationWeeks = durationWeeks;
        program.DaysPerWeek = daysPerWeek;
        program.TargetLevel = targetLevel;
        program.TargetGoal = targetGoal;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteProgramAsync(int programId, int userId)
    {
        var program = await _db.Programs
            .Include(p => p.Workouts).ThenInclude(w => w.WorkoutExercises)
            .FirstOrDefaultAsync(p => p.Id == programId && p.CreatedByUserId == userId && !p.IsPreBuilt);

        if (program is null) return false;

        foreach (var w in program.Workouts)
        {
            _db.WorkoutExercises.RemoveRange(w.WorkoutExercises);
        }
        _db.Workouts.RemoveRange(program.Workouts);
        _db.Programs.Remove(program);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Exercise>> GetExerciseLibraryAsync()
    {
        return await _db.Exercises
            .Include(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Include(e => e.ExerciseEquipment).ThenInclude(ee => ee.Equipment)
            .Include(e => e.Media)
            .Include(e => e.Alternatives).ThenInclude(a => a.AlternativeExercise)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<List<MuscleGroup>> GetAllMuscleGroupsAsync()
    {
        return await _db.MuscleGroups.OrderBy(m => m.Name).ToListAsync();
    }

    public async Task<List<Equipment>> GetAllEquipmentAsync()
    {
        return await _db.Equipment.OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        return await _db.Users.Include(u => u.UserEquipment).FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<Workout> CreateWorkoutAsync(int userId, UserWorkoutFormData data)
    {
        var workout = new Workout
        {
            Name = data.Name.Trim(),
            UserId = userId,
            ProgramId = null,
            SortOrder = data.SortOrder
        };

        _db.Workouts.Add(workout);
        await _db.SaveChangesAsync();

        for (int i = 0; i < data.Exercises.Count; i++)
        {
            var ex = data.Exercises[i];
            _db.WorkoutExercises.Add(new WorkoutExercise
            {
                WorkoutId = workout.Id,
                ExerciseId = ex.ExerciseId,
                Sets = ex.Sets,
                Reps = ex.Reps,
                RestTimeSec = ex.RestTimeSec,
                SortOrder = i
            });
        }

        await _db.SaveChangesAsync();
        return workout;
    }

    public async Task<bool> UpdateWorkoutAsync(int id, int userId, UserWorkoutFormData data)
    {
        var workout = await _db.Workouts
            .Include(w => w.WorkoutExercises)
            .Include(w => w.Program)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (workout is null || (workout.Program is not null && workout.Program.IsPreBuilt)) return false;

        workout.Name = data.Name.Trim();
        workout.SortOrder = data.SortOrder;

        _db.WorkoutExercises.RemoveRange(workout.WorkoutExercises);

        for (int i = 0; i < data.Exercises.Count; i++)
        {
            var ex = data.Exercises[i];
            _db.WorkoutExercises.Add(new WorkoutExercise
            {
                WorkoutId = id,
                ExerciseId = ex.ExerciseId,
                Sets = ex.Sets,
                Reps = ex.Reps,
                RestTimeSec = ex.RestTimeSec,
                SortOrder = i
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteWorkoutAsync(int id, int userId)
    {
        var workout = await _db.Workouts
            .Include(w => w.WorkoutExercises)
            .Include(w => w.Program)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (workout is null || (workout.Program is not null && workout.Program.IsPreBuilt)) return false;

        _db.WorkoutExercises.RemoveRange(workout.WorkoutExercises);
        _db.Workouts.Remove(workout);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Workout> CopyWorkoutToUserAsync(int sourceWorkoutId, int userId)
    {
        var source = await _db.Workouts
            .Include(w => w.WorkoutExercises)
            .FirstOrDefaultAsync(w => w.Id == sourceWorkoutId);

        if (source is null) return null!;

        var workout = new Workout
        {
            Name = source.Name,
            UserId = userId,
            ProgramId = null,
            SortOrder = 0
        };

        _db.Workouts.Add(workout);
        await _db.SaveChangesAsync();

        foreach (var we in source.WorkoutExercises)
        {
            _db.WorkoutExercises.Add(new WorkoutExercise
            {
                WorkoutId = workout.Id,
                ExerciseId = we.ExerciseId,
                Sets = we.Sets,
                Reps = we.Reps,
                RestTimeSec = we.RestTimeSec,
                SortOrder = we.SortOrder
            });
        }

        await _db.SaveChangesAsync();
        return workout;
    }

    public static decimal EstimateWorkoutCalories(List<BuilderExerciseEntry> exercises, decimal userWeightKg)
    {
        decimal total = 0;
        foreach (var entry in exercises)
        {
            if (entry.Exercise is null) continue;
            total += EstimateExerciseCalories(entry.Exercise, entry.Sets, entry.Reps, entry.RestTimeSec ?? 60, userWeightKg);
        }
        return Math.Round(total, 1);
    }

    public static decimal EstimateExerciseCalories(Exercise exercise, int sets, int reps, int restSec, decimal userWeightKg)
    {
        decimal activeMinutes = EstimateExerciseMinutes(exercise, sets, reps, restSec);
        decimal caloriesPerMinute = (exercise.MetValue * 3.5m * userWeightKg) / 200m;
        return Math.Round(caloriesPerMinute * activeMinutes, 1);
    }

    public static decimal EstimateExerciseMinutes(Exercise exercise, int sets, int reps, int restSec)
    {
        if (exercise.MovementType == MovementType.Cardio)
            return (sets * reps) / 60m;

        decimal repTime = exercise.RepTimeSec ?? 3.5m;
        decimal workSec = sets * reps * repTime;
        decimal restTotal = Math.Max(0, sets - 1) * restSec;
        return (workSec + restTotal) / 60m;
    }

    public static decimal EstimateWorkoutMinutes(List<BuilderExerciseEntry> exercises)
    {
        decimal total = 0;
        foreach (var entry in exercises)
        {
            if (entry.Exercise is null) continue;
            total += EstimateExerciseMinutes(entry.Exercise, entry.Sets, entry.Reps, entry.RestTimeSec ?? 60);
        }
        return Math.Round(total, 0);
    }

    public static decimal EstimateWorkoutMinutesFromWE(IEnumerable<WorkoutExercise> exercises)
    {
        decimal total = 0;
        foreach (var we in exercises)
        {
            if (we.Exercise is null) continue;
            total += EstimateExerciseMinutes(we.Exercise, we.Sets, we.Reps, we.RestTimeSec ?? 60);
        }
        return Math.Round(total, 0);
    }

    public static (int Sets, int Reps, int RestSec) GetRecommendation(PrimaryGoal? goal, ExerciseLevel level, string? exerciseType)
    {
        bool isCompound = string.Equals(exerciseType, "Compound", StringComparison.OrdinalIgnoreCase);

        return goal switch
        {
            PrimaryGoal.Strength => isCompound ? (5, 5, 180) : (4, 8, 120),
            PrimaryGoal.Hypertrophy => isCompound ? (4, 10, 90) : (3, 12, 60),
            PrimaryGoal.Endurance => isCompound ? (3, 15, 45) : (3, 20, 30),
            PrimaryGoal.FatLoss => isCompound ? (3, 12, 45) : (3, 15, 30),
            _ => isCompound ? (3, 10, 90) : (3, 12, 60)
        };
    }

    public static List<string> GetWorkoutWarnings(List<BuilderExerciseEntry> exercises, FitnessLevel? userLevel)
    {
        var warnings = new List<string>();
        if (exercises.Count == 0) return warnings;

        int totalSets = exercises.Sum(e => e.Sets);
        int maxSets = userLevel switch
        {
            FitnessLevel.Beginner => 16,
            FitnessLevel.Intermediate => 24,
            FitnessLevel.Advanced => 32,
            _ => 20
        };

        if (totalSets > maxSets)
            warnings.Add($"High volume: {totalSets} total sets (max recommended: {maxSets} for {userLevel?.ToString() ?? "your"} level).");

        var muscleSetCounts = new Dictionary<string, int>();
        foreach (var entry in exercises.Where(e => e.Exercise is not null))
        {
            foreach (var mg in entry.Exercise!.ExerciseMuscleGroups.Where(m => m.IsPrimary))
            {
                muscleSetCounts.TryAdd(mg.MuscleGroup.Name, 0);
                muscleSetCounts[mg.MuscleGroup.Name] += entry.Sets;
            }
        }

        int muscleMax = userLevel switch
        {
            FitnessLevel.Beginner => 10,
            FitnessLevel.Intermediate => 16,
            FitnessLevel.Advanced => 20,
            _ => 12
        };

        foreach (var (muscle, sets) in muscleSetCounts)
        {
            if (sets > muscleMax)
                warnings.Add($"{muscle}: {sets} sets is excessive (max: {muscleMax}).");
        }

        return warnings;
    }

    public static List<Exercise> GetSuggestions(List<BuilderExerciseEntry> current, List<Exercise> all)
    {
        if (current.Count == 0)
            return all.Where(e => string.Equals(e.ExerciseType, "Compound", StringComparison.OrdinalIgnoreCase)).Take(6).ToList();

        var usedIds = current.Select(e => e.ExerciseId).ToHashSet();
        var coveredMuscles = current
            .Where(e => e.Exercise is not null)
            .SelectMany(e => e.Exercise!.ExerciseMuscleGroups.Where(m => m.IsPrimary).Select(m => m.MuscleGroup.Name))
            .ToHashSet();

        return all
            .Where(e => !usedIds.Contains(e.Id))
            .Where(e => e.ExerciseMuscleGroups.Any(m => m.IsPrimary && !coveredMuscles.Contains(m.MuscleGroup.Name)))
            .Take(6)
            .ToList();
    }

    public static string GetYouTubeEmbedUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";

        string videoId = "";

        if (url.Contains("youtube.com/watch"))
        {
            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            videoId = query["v"] ?? "";
        }
        else if (url.Contains("youtu.be/"))
        {
            videoId = url.Split("youtu.be/").Last().Split('?').First();
        }
        else if (url.Contains("youtube.com/embed/"))
        {
            videoId = url.Split("embed/").Last().Split('?').First();
        }

        if (string.IsNullOrEmpty(videoId)) return url;

        return $"https://www.youtube.com/embed/{videoId}?autoplay=1&mute=1&loop=1&playlist={videoId}&controls=0&modestbranding=1";
    }
}

public class UserWorkoutFormData
{
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public List<BuilderExerciseEntry> Exercises { get; set; } = new();
}

public class BuilderExerciseEntry
{
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }
    public int Sets { get; set; } = 3;
    public int Reps { get; set; } = 10;
    public int? RestTimeSec { get; set; } = 60;
}
