using Microsoft.EntityFrameworkCore;
using FitnessApp.Models;
using ProgramEntity = FitnessApp.Models.Program;

namespace FitnessApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<UserEquipment> UserEquipment => Set<UserEquipment>();
    public DbSet<UserInjury> UserInjuries => Set<UserInjury>();
    public DbSet<MuscleGroup> MuscleGroups => Set<MuscleGroup>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseMuscleGroup> ExerciseMuscleGroups => Set<ExerciseMuscleGroup>();
    public DbSet<ExerciseEquipment> ExerciseEquipment => Set<ExerciseEquipment>();
    public DbSet<ExerciseAlternative> ExerciseAlternatives => Set<ExerciseAlternative>();
    public DbSet<ExerciseMedia> ExerciseMedia => Set<ExerciseMedia>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<ProgramEntity> Programs => Set<ProgramEntity>();
    public DbSet<WorkoutLog> WorkoutLogs => Set<WorkoutLog>();
    public DbSet<ExerciseLog> ExerciseLogs => Set<ExerciseLog>();
    public DbSet<ExerciseSetLog> ExerciseSetLogs => Set<ExerciseSetLog>();
    public DbSet<CardioLog> CardioLogs => Set<CardioLog>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<FoodPreparation> FoodPreparations => Set<FoodPreparation>();
    public DbSet<FoodLog> FoodLogs => Set<FoodLog>();
    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();
    public DbSet<PersonalRecord> PersonalRecords => Set<PersonalRecord>();
    public DbSet<DailyLog> DailyLogs => Set<DailyLog>();
    public DbSet<UserGoal> UserGoals => Set<UserGoal>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Bio).HasMaxLength(500);
            e.Property(u => u.AvatarUrl).HasMaxLength(512);
            e.Property(u => u.WeightKg).HasPrecision(5, 2);
            e.Property(u => u.HeightCm).HasPrecision(5, 2);
        });

        mb.Entity<Equipment>(e =>
        {
            e.HasIndex(eq => eq.Name).IsUnique();
            e.Property(eq => eq.Name).HasMaxLength(100).IsRequired();
            e.HasData(
                new Equipment { Id = 1, Name = "Bodyweight Only" },
                new Equipment { Id = 2, Name = "Dumbbells" },
                new Equipment { Id = 3, Name = "Barbell & Plates" },
                new Equipment { Id = 4, Name = "Kettlebells" },
                new Equipment { Id = 5, Name = "Resistance Bands" },
                new Equipment { Id = 6, Name = "Pull-Up Bar" },
                new Equipment { Id = 7, Name = "Bench" },
                new Equipment { Id = 8, Name = "Cable Machine" },
                new Equipment { Id = 9, Name = "Smith Machine" },
                new Equipment { Id = 10, Name = "Leg Press" },
                new Equipment { Id = 11, Name = "Treadmill" },
                new Equipment { Id = 12, Name = "Stationary Bike" },
                new Equipment { Id = 13, Name = "Rowing Machine" },
                new Equipment { Id = 14, Name = "Medicine Ball" },
                new Equipment { Id = 15, Name = "TRX / Suspension Trainer" },
                new Equipment { Id = 16, Name = "Full Gym Access" }
            );
        });

        mb.Entity<UserEquipment>(e =>
        {
            e.HasKey(ue => new { ue.UserId, ue.EquipmentId });
            e.HasOne(ue => ue.User).WithMany(u => u.UserEquipment).HasForeignKey(ue => ue.UserId);
            e.HasOne(ue => ue.Equipment).WithMany(eq => eq.UserEquipment).HasForeignKey(ue => ue.EquipmentId);
        });

        mb.Entity<UserInjury>(e =>
        {
            e.Property(ui => ui.Description).HasMaxLength(500).IsRequired();
            e.Property(ui => ui.AffectedArea).HasMaxLength(100);
            e.HasOne(ui => ui.User).WithMany(u => u.Injuries).HasForeignKey(ui => ui.UserId);
        });

        mb.Entity<MuscleGroup>(e =>
        {
            e.HasIndex(mg => mg.Name).IsUnique();
            e.Property(mg => mg.Name).HasMaxLength(100).IsRequired();
            e.Property(mg => mg.BodyRegion).HasMaxLength(50);
        });

        mb.Entity<Exercise>(e =>
        {
            e.Property(ex => ex.Name).HasMaxLength(200).IsRequired();
            e.Property(ex => ex.Description).HasMaxLength(2000).IsRequired();
            e.Property(ex => ex.ExerciseType).HasMaxLength(100);
            e.Property(ex => ex.MetValue).HasPrecision(4, 2);
            e.Property(ex => ex.RepTimeSec).HasPrecision(6, 2);
        });

        mb.Entity<ExerciseMuscleGroup>(e =>
        {
            e.HasKey(em => new { em.ExerciseId, em.MuscleGroupId });
            e.HasOne(em => em.Exercise).WithMany(ex => ex.ExerciseMuscleGroups).HasForeignKey(em => em.ExerciseId);
            e.HasOne(em => em.MuscleGroup).WithMany(mg => mg.ExerciseMuscleGroups).HasForeignKey(em => em.MuscleGroupId);
        });

        mb.Entity<ExerciseEquipment>(e =>
        {
            e.HasKey(ee => new { ee.ExerciseId, ee.EquipmentId });
            e.HasOne(ee => ee.Exercise).WithMany(ex => ex.ExerciseEquipment).HasForeignKey(ee => ee.ExerciseId);
            e.HasOne(ee => ee.Equipment).WithMany(eq => eq.ExerciseEquipment).HasForeignKey(ee => ee.EquipmentId);
        });

        mb.Entity<ExerciseAlternative>(e =>
        {
            e.HasKey(ea => new { ea.ExerciseId, ea.AlternativeExerciseId });
            e.HasOne(ea => ea.Exercise).WithMany(ex => ex.Alternatives).HasForeignKey(ea => ea.ExerciseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ea => ea.AlternativeExercise).WithMany(ex => ex.AlternativeOf).HasForeignKey(ea => ea.AlternativeExerciseId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<ExerciseMedia>(e =>
        {
            e.Property(em => em.Url).HasMaxLength(512).IsRequired();
            e.Property(em => em.Title).HasMaxLength(200);
            e.HasOne(em => em.Exercise).WithMany(ex => ex.Media).HasForeignKey(em => em.ExerciseId);
        });

        mb.Entity<Workout>(e =>
        {
            e.Property(w => w.Name).HasMaxLength(200).IsRequired();
            e.HasOne(w => w.User).WithMany(u => u.Workouts).HasForeignKey(w => w.UserId);
            e.HasOne(w => w.Program).WithMany(p => p.Workouts).HasForeignKey(w => w.ProgramId).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<WorkoutExercise>(e =>
        {
            e.HasOne(we => we.Workout).WithMany(w => w.WorkoutExercises).HasForeignKey(we => we.WorkoutId);
            e.HasOne(we => we.Exercise).WithMany(ex => ex.WorkoutExercises).HasForeignKey(we => we.ExerciseId);
        });

        mb.Entity<ProgramEntity>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Description).HasMaxLength(1000);
            e.HasOne(p => p.CreatedByUser).WithMany().HasForeignKey(p => p.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<WorkoutLog>(e =>
        {
            e.HasIndex(wl => new { wl.UserId, wl.Date });
            e.Property(wl => wl.TotalCaloriesBurned).HasPrecision(8, 2);
            e.HasOne(wl => wl.User).WithMany(u => u.WorkoutLogs).HasForeignKey(wl => wl.UserId);
            e.HasOne(wl => wl.Workout).WithMany(w => w.WorkoutLogs).HasForeignKey(wl => wl.WorkoutId).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<ExerciseLog>(e =>
        {
            e.Property(el => el.CaloriesBurned).HasPrecision(8, 2);
            e.Property(el => el.Notes).HasMaxLength(500);
            e.HasOne(el => el.WorkoutLog).WithMany(wl => wl.ExerciseLogs).HasForeignKey(el => el.WorkoutLogId);
            e.HasOne(el => el.Exercise).WithMany().HasForeignKey(el => el.ExerciseId);
        });

        mb.Entity<ExerciseSetLog>(e =>
        {
            e.Property(es => es.WeightKg).HasPrecision(6, 2);
            e.HasOne(es => es.ExerciseLog).WithMany(el => el.SetLogs).HasForeignKey(es => es.ExerciseLogId);
        });

        mb.Entity<CardioLog>(e =>
        {
            e.HasIndex(cl => new { cl.UserId, cl.Date });
            e.Property(cl => cl.ActivityName).HasMaxLength(200).IsRequired();
            e.Property(cl => cl.DistanceKm).HasPrecision(6, 2);
            e.Property(cl => cl.CaloriesBurned).HasPrecision(8, 2);
            e.Property(cl => cl.Notes).HasMaxLength(500);
            e.HasOne(cl => cl.User).WithMany(u => u.CardioLogs).HasForeignKey(cl => cl.UserId);
        });

        mb.Entity<Food>(e =>
        {
            e.Property(f => f.Name).HasMaxLength(200).IsRequired();
            e.Property(f => f.CaloriesPer100g).HasPrecision(7, 2);
            e.Property(f => f.ProteinPer100g).HasPrecision(6, 2);
            e.Property(f => f.CarbsPer100g).HasPrecision(6, 2);
            e.Property(f => f.FatPer100g).HasPrecision(6, 2);
        });

        mb.Entity<FoodPreparation>(e =>
        {
            e.Property(fp => fp.Method).HasMaxLength(200).IsRequired();
            e.Property(fp => fp.CalorieModifier).HasPrecision(4, 2);
            e.HasOne(fp => fp.Food).WithMany(f => f.Preparations).HasForeignKey(fp => fp.FoodId);
        });

        mb.Entity<FoodLog>(e =>
        {
            e.HasIndex(fl => new { fl.UserId, fl.Date });
            e.Property(fl => fl.QuantityGrams).HasPrecision(7, 2);
            e.Property(fl => fl.CaloriesConsumed).HasPrecision(8, 2);
            e.HasOne(fl => fl.User).WithMany(u => u.FoodLogs).HasForeignKey(fl => fl.UserId);
            e.HasOne(fl => fl.Food).WithMany(f => f.FoodLogs).HasForeignKey(fl => fl.FoodId);
            e.HasOne(fl => fl.Preparation).WithMany().HasForeignKey(fl => fl.PreparationId).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<BodyMeasurement>(e =>
        {
            e.HasIndex(bm => new { bm.UserId, bm.Date });
            e.Property(bm => bm.WeightKg).HasPrecision(5, 2);
            e.Property(bm => bm.BodyFatPercent).HasPrecision(4, 2);
            e.Property(bm => bm.ChestCm).HasPrecision(5, 2);
            e.Property(bm => bm.WaistCm).HasPrecision(5, 2);
            e.Property(bm => bm.HipsCm).HasPrecision(5, 2);
            e.Property(bm => bm.BicepsCm).HasPrecision(5, 2);
            e.Property(bm => bm.ThighsCm).HasPrecision(5, 2);
            e.HasOne(bm => bm.User).WithMany(u => u.BodyMeasurements).HasForeignKey(bm => bm.UserId);
        });

        mb.Entity<PersonalRecord>(e =>
        {
            e.HasIndex(pr => new { pr.UserId, pr.ExerciseId });
            e.Property(pr => pr.WeightKg).HasPrecision(6, 2);
            e.HasOne(pr => pr.User).WithMany(u => u.PersonalRecords).HasForeignKey(pr => pr.UserId);
            e.HasOne(pr => pr.Exercise).WithMany().HasForeignKey(pr => pr.ExerciseId);
        });

        mb.Entity<DailyLog>(e =>
        {
            e.HasIndex(dl => new { dl.UserId, dl.Date }).IsUnique();
            e.Property(dl => dl.Notes).HasMaxLength(1000);
            e.Property(dl => dl.TotalCaloriesBurned).HasPrecision(8, 2);
            e.Property(dl => dl.TotalCaloriesConsumed).HasPrecision(8, 2);
            e.HasOne(dl => dl.User).WithMany(u => u.DailyLogs).HasForeignKey(dl => dl.UserId);
        });

        mb.Entity<UserGoal>(e =>
        {
            e.Property(ug => ug.Title).HasMaxLength(200).IsRequired();
            e.Property(ug => ug.Description).HasMaxLength(500);
            e.Property(ug => ug.TargetValue).HasPrecision(10, 2);
            e.Property(ug => ug.CurrentValue).HasPrecision(10, 2);
            e.Property(ug => ug.Unit).HasMaxLength(50);
            e.HasOne(ug => ug.User).WithMany(u => u.Goals).HasForeignKey(ug => ug.UserId);
        });
    }
}
