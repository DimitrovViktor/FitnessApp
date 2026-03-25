using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;
using FitnessApp.Models.Enums;
using ProgramEntity = FitnessApp.Models.Program;

namespace FitnessApp.Services;

public class AdminService
{
    private readonly AppDbContext _db;

    public AdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsAdminAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user?.IsAdmin == true;
    }

    public async Task<AdminCounts> GetCountsAsync()
    {
        return new AdminCounts
        {
            Users = await _db.Users.CountAsync(),
            Exercises = await _db.Exercises.CountAsync(),
            Workouts = await _db.Workouts.CountAsync(),
            Equipment = await _db.Equipment.CountAsync(),
            MuscleGroups = await _db.MuscleGroups.CountAsync(),
            Foods = await _db.Foods.CountAsync(),
            Programs = await _db.Programs.CountAsync(),
            WorkoutLogs = await _db.WorkoutLogs.CountAsync()
        };
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _db.Users.OrderBy(u => u.Id).ToListAsync();
    }

    public async Task<List<Equipment>> GetAllEquipmentAsync()
    {
        return await _db.Equipment.OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<Equipment> CreateEquipmentAsync(string name)
    {
        var item = new Equipment { Name = name.Trim() };
        _db.Equipment.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> UpdateEquipmentAsync(int id, string name)
    {
        var item = await _db.Equipment.FindAsync(id);
        if (item is null) return false;
        item.Name = name.Trim();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteEquipmentAsync(int id)
    {
        var item = await _db.Equipment.FindAsync(id);
        if (item is null) return false;
        _db.Equipment.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<MuscleGroup>> GetAllMuscleGroupsAsync()
    {
        return await _db.MuscleGroups.OrderBy(m => m.Name).ToListAsync();
    }

    public async Task<MuscleGroup> CreateMuscleGroupAsync(MuscleGroupFormData data)
    {
        var item = new MuscleGroup { Name = data.Name.Trim(), BodyRegion = string.IsNullOrWhiteSpace(data.BodyRegion) ? null : data.BodyRegion.Trim() };
        _db.MuscleGroups.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> UpdateMuscleGroupAsync(int id, MuscleGroupFormData data)
    {
        var item = await _db.MuscleGroups.FindAsync(id);
        if (item is null) return false;
        item.Name = data.Name.Trim();
        item.BodyRegion = string.IsNullOrWhiteSpace(data.BodyRegion) ? null : data.BodyRegion.Trim();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMuscleGroupAsync(int id)
    {
        var item = await _db.MuscleGroups.FindAsync(id);
        if (item is null) return false;
        _db.MuscleGroups.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Food>> GetAllFoodsAsync()
    {
        return await _db.Foods.OrderBy(f => f.Name).ToListAsync();
    }

    public async Task<Food> CreateFoodAsync(FoodFormData data)
    {
        var item = new Food
        {
            Name = data.Name.Trim(),
            CaloriesPer100g = data.CaloriesPer100g,
            ProteinPer100g = data.ProteinPer100g,
            CarbsPer100g = data.CarbsPer100g,
            FatPer100g = data.FatPer100g
        };
        _db.Foods.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> UpdateFoodAsync(int id, FoodFormData data)
    {
        var item = await _db.Foods.FindAsync(id);
        if (item is null) return false;
        item.Name = data.Name.Trim();
        item.CaloriesPer100g = data.CaloriesPer100g;
        item.ProteinPer100g = data.ProteinPer100g;
        item.CarbsPer100g = data.CarbsPer100g;
        item.FatPer100g = data.FatPer100g;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteFoodAsync(int id)
    {
        var item = await _db.Foods.FindAsync(id);
        if (item is null) return false;
        _db.Foods.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ProgramEntity>> GetAllProgramsAsync()
    {
        return await _db.Programs.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<ProgramEntity> CreateProgramAsync(ProgramFormData data)
    {
        var item = new ProgramEntity
        {
            Name = data.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim(),
            DurationWeeks = data.DurationWeeks,
            DaysPerWeek = data.DaysPerWeek,
            TargetLevel = string.IsNullOrWhiteSpace(data.TargetLevel) ? null : data.TargetLevel,
            TargetGoal = string.IsNullOrWhiteSpace(data.TargetGoal) ? null : data.TargetGoal,
            IsPreBuilt = data.IsPreBuilt,
            CreatedByUserId = data.CreatedByUserId
        };
        _db.Programs.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> UpdateProgramAsync(int id, ProgramFormData data)
    {
        var item = await _db.Programs.FindAsync(id);
        if (item is null) return false;
        item.Name = data.Name.Trim();
        item.Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim();
        item.DurationWeeks = data.DurationWeeks;
        item.DaysPerWeek = data.DaysPerWeek;
        item.TargetLevel = string.IsNullOrWhiteSpace(data.TargetLevel) ? null : data.TargetLevel;
        item.TargetGoal = string.IsNullOrWhiteSpace(data.TargetGoal) ? null : data.TargetGoal;
        item.IsPreBuilt = data.IsPreBuilt;
        item.CreatedByUserId = data.CreatedByUserId;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteProgramAsync(int id)
    {
        var item = await _db.Programs.FindAsync(id);
        if (item is null) return false;
        _db.Programs.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Exercise>> GetAllExercisesAsync()
    {
        return await _db.Exercises
            .Include(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Include(e => e.ExerciseEquipment).ThenInclude(ee => ee.Equipment)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<Exercise?> GetExerciseByIdAsync(int id)
    {
        return await _db.Exercises
            .Include(e => e.ExerciseMuscleGroups).ThenInclude(em => em.MuscleGroup)
            .Include(e => e.ExerciseEquipment).ThenInclude(ee => ee.Equipment)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Exercise> CreateExerciseAsync(ExerciseFormData data)
    {
        var exercise = new Exercise
        {
            Name = data.Name.Trim(),
            Description = data.Description.Trim(),
            ExerciseType = string.IsNullOrWhiteSpace(data.ExerciseType) ? null : data.ExerciseType.Trim(),
            DifficultyRating = data.DifficultyRating,
            Level = data.Level,
            MovementType = data.MovementType,
            MetValue = data.MetValue,
            RepTimeSec = data.RepTimeSec
        };

        _db.Exercises.Add(exercise);
        await _db.SaveChangesAsync();

        foreach (var mgId in data.MuscleGroupIds)
            _db.ExerciseMuscleGroups.Add(new ExerciseMuscleGroup { ExerciseId = exercise.Id, MuscleGroupId = mgId, IsPrimary = data.PrimaryMuscleGroupIds.Contains(mgId) });

        foreach (var eqId in data.EquipmentIds)
            _db.ExerciseEquipment.Add(new ExerciseEquipment { ExerciseId = exercise.Id, EquipmentId = eqId });

        await _db.SaveChangesAsync();
        return exercise;
    }

    public async Task<bool> UpdateExerciseAsync(int id, ExerciseFormData data)
    {
        var exercise = await _db.Exercises
            .Include(e => e.ExerciseMuscleGroups)
            .Include(e => e.ExerciseEquipment)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise is null) return false;

        exercise.Name = data.Name.Trim();
        exercise.Description = data.Description.Trim();
        exercise.ExerciseType = string.IsNullOrWhiteSpace(data.ExerciseType) ? null : data.ExerciseType.Trim();
        exercise.DifficultyRating = data.DifficultyRating;
        exercise.Level = data.Level;
        exercise.MovementType = data.MovementType;
        exercise.MetValue = data.MetValue;
        exercise.RepTimeSec = data.RepTimeSec;

        _db.ExerciseMuscleGroups.RemoveRange(exercise.ExerciseMuscleGroups);
        _db.ExerciseEquipment.RemoveRange(exercise.ExerciseEquipment);

        foreach (var mgId in data.MuscleGroupIds)
            _db.ExerciseMuscleGroups.Add(new ExerciseMuscleGroup { ExerciseId = id, MuscleGroupId = mgId, IsPrimary = data.PrimaryMuscleGroupIds.Contains(mgId) });

        foreach (var eqId in data.EquipmentIds)
            _db.ExerciseEquipment.Add(new ExerciseEquipment { ExerciseId = id, EquipmentId = eqId });

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteExerciseAsync(int id)
    {
        var exercise = await _db.Exercises
            .Include(e => e.ExerciseMuscleGroups)
            .Include(e => e.ExerciseEquipment)
            .Include(e => e.Media)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise is null) return false;

        _db.ExerciseMuscleGroups.RemoveRange(exercise.ExerciseMuscleGroups);
        _db.ExerciseEquipment.RemoveRange(exercise.ExerciseEquipment);
        _db.ExerciseMedia.RemoveRange(exercise.Media);
        _db.Exercises.Remove(exercise);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Workout>> GetAllWorkoutsAsync()
    {
        return await _db.Workouts
            .Include(w => w.User)
            .Include(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
            .Include(w => w.Program)
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<Workout?> GetWorkoutByIdAsync(int id)
    {
        return await _db.Workouts
            .Include(w => w.User)
            .Include(w => w.WorkoutExercises).ThenInclude(we => we.Exercise)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<Workout> CreateWorkoutAsync(WorkoutFormData data)
    {
        var workout = new Workout
        {
            Name = data.Name.Trim(),
            UserId = data.UserId,
            ProgramId = data.ProgramId,
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

    public async Task<bool> UpdateWorkoutAsync(int id, WorkoutFormData data)
    {
        var workout = await _db.Workouts
            .Include(w => w.WorkoutExercises)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workout is null) return false;

        workout.Name = data.Name.Trim();
        workout.UserId = data.UserId;
        workout.ProgramId = data.ProgramId;
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

    public async Task<bool> DeleteWorkoutAsync(int id)
    {
        var workout = await _db.Workouts
            .Include(w => w.WorkoutExercises)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workout is null) return false;

        _db.WorkoutExercises.RemoveRange(workout.WorkoutExercises);
        _db.Workouts.Remove(workout);
        await _db.SaveChangesAsync();
        return true;
    }
}

public class AdminCounts
{
    public int Users { get; set; }
    public int Exercises { get; set; }
    public int Workouts { get; set; }
    public int Equipment { get; set; }
    public int MuscleGroups { get; set; }
    public int Foods { get; set; }
    public int Programs { get; set; }
    public int WorkoutLogs { get; set; }
}

public class ExerciseFormData
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? ExerciseType { get; set; }
    public int DifficultyRating { get; set; } = 1;
    public ExerciseLevel Level { get; set; }
    public MovementType MovementType { get; set; }
    public decimal MetValue { get; set; }
    public decimal? RepTimeSec { get; set; }
    public List<int> MuscleGroupIds { get; set; } = new();
    public List<int> PrimaryMuscleGroupIds { get; set; } = new();
    public List<int> EquipmentIds { get; set; } = new();
}

public class WorkoutFormData
{
    public string Name { get; set; } = "";
    public int UserId { get; set; }
    public int? ProgramId { get; set; }
    public int SortOrder { get; set; }
    public List<WorkoutExerciseEntry> Exercises { get; set; } = new();
}

public class WorkoutExerciseEntry
{
    public int ExerciseId { get; set; }
    public int Sets { get; set; } = 3;
    public int Reps { get; set; } = 10;
    public int? RestTimeSec { get; set; } = 60;
}

public class MuscleGroupFormData
{
    public string Name { get; set; } = "";
    public string? BodyRegion { get; set; }
}

public class FoodFormData
{
    public string Name { get; set; } = "";
    public decimal CaloriesPer100g { get; set; }
    public decimal? ProteinPer100g { get; set; }
    public decimal? CarbsPer100g { get; set; }
    public decimal? FatPer100g { get; set; }
}

public class ProgramFormData
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int DurationWeeks { get; set; } = 8;
    public int DaysPerWeek { get; set; } = 3;
    public string? TargetLevel { get; set; }
    public string? TargetGoal { get; set; }
    public bool IsPreBuilt { get; set; }
    public int? CreatedByUserId { get; set; }
}
