using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;
using FitnessApp.Models.Enums;
using ProgramEntity = FitnessApp.Models.Program;

namespace FitnessApp.Services;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedMuscleGroupsAsync(db);
        await SeedExercisesAsync(db);
        await SeedFoodsAsync(db);
        await SeedProgramsAsync(db);
        await SeedPremadeWorkoutsAsync(db);
    }

    private static async Task SeedMuscleGroupsAsync(AppDbContext db)
    {
        if (await db.MuscleGroups.AnyAsync()) return;

        db.MuscleGroups.AddRange(
            new MuscleGroup { Name = "Chest", BodyRegion = "Upper Body" },
            new MuscleGroup { Name = "Upper Chest", BodyRegion = "Upper Body" },
            new MuscleGroup { Name = "Lats", BodyRegion = "Upper Body" },
            new MuscleGroup { Name = "Upper Back", BodyRegion = "Upper Body" },
            new MuscleGroup { Name = "Traps", BodyRegion = "Upper Body" },
            new MuscleGroup { Name = "Front Delts", BodyRegion = "Shoulders" },
            new MuscleGroup { Name = "Side Delts", BodyRegion = "Shoulders" },
            new MuscleGroup { Name = "Rear Delts", BodyRegion = "Shoulders" },
            new MuscleGroup { Name = "Biceps", BodyRegion = "Arms" },
            new MuscleGroup { Name = "Triceps", BodyRegion = "Arms" },
            new MuscleGroup { Name = "Forearms", BodyRegion = "Arms" },
            new MuscleGroup { Name = "Quadriceps", BodyRegion = "Legs" },
            new MuscleGroup { Name = "Hamstrings", BodyRegion = "Legs" },
            new MuscleGroup { Name = "Glutes", BodyRegion = "Legs" },
            new MuscleGroup { Name = "Calves", BodyRegion = "Legs" },
            new MuscleGroup { Name = "Hip Flexors", BodyRegion = "Legs" },
            new MuscleGroup { Name = "Abs", BodyRegion = "Core" },
            new MuscleGroup { Name = "Obliques", BodyRegion = "Core" },
            new MuscleGroup { Name = "Lower Back", BodyRegion = "Core" }
        );

        await db.SaveChangesAsync();
    }

    private static async Task SeedExercisesAsync(AppDbContext db)
    {
        if (await db.Exercises.AnyAsync()) return;

        var muscles = await db.MuscleGroups.ToDictionaryAsync(m => m.Name, m => m.Id);
        var equipment = await db.Equipment.ToDictionaryAsync(e => e.Name, e => e.Id);

        foreach (var def in BuildExerciseList())
        {
            var exercise = new Exercise
            {
                Name = def.Name, Description = def.Description, ExerciseType = def.ExerciseType,
                DifficultyRating = def.Difficulty, Level = def.Level, MovementType = def.Movement,
                MetValue = def.Met, RepTimeSec = def.RepTime
            };

            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();

            foreach (var (muscleName, isPrimary) in def.Muscles)
                if (muscles.TryGetValue(muscleName, out var mgId))
                    db.ExerciseMuscleGroups.Add(new ExerciseMuscleGroup { ExerciseId = exercise.Id, MuscleGroupId = mgId, IsPrimary = isPrimary });

            foreach (var equipName in def.Equipment)
                if (equipment.TryGetValue(equipName, out var eqId))
                    db.ExerciseEquipment.Add(new ExerciseEquipment { ExerciseId = exercise.Id, EquipmentId = eqId });

            if (def.VideoUrl is not null)
                db.ExerciseMedia.Add(new ExerciseMedia { ExerciseId = exercise.Id, MediaType = MediaType.Video, Url = def.VideoUrl, Title = def.Name, SortOrder = 0 });

            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedFoodsAsync(AppDbContext db)
    {
        if (await db.Foods.AnyAsync()) return;
        db.Foods.AddRange(BuildFoodList());
        await db.SaveChangesAsync();
    }

    private static async Task SeedProgramsAsync(AppDbContext db)
    {
        if (await db.Programs.AnyAsync()) return;

        db.Programs.AddRange(
            new ProgramEntity { Name = "Push Pull Legs", Description = "A 6-day split grouping exercises by movement pattern. Each muscle group is hit twice per week for optimal hypertrophy.", DurationWeeks = 12, DaysPerWeek = 6, TargetLevel = FitnessLevel.Intermediate.ToString(), TargetGoal = PrimaryGoal.Hypertrophy.ToString(), IsPreBuilt = true },
            new ProgramEntity { Name = "Upper Lower Split", Description = "A 4-day program alternating upper and lower body with adequate recovery between sessions.", DurationWeeks = 12, DaysPerWeek = 4, TargetLevel = FitnessLevel.Intermediate.ToString(), TargetGoal = PrimaryGoal.Strength.ToString(), IsPreBuilt = true },
            new ProgramEntity { Name = "Full Body 3-Day", Description = "Three full-body sessions per week hitting every major muscle group each session with compound movements.", DurationWeeks = 8, DaysPerWeek = 3, TargetLevel = FitnessLevel.Beginner.ToString(), TargetGoal = PrimaryGoal.GeneralHealth.ToString(), IsPreBuilt = true },
            new ProgramEntity { Name = "Bro Split", Description = "A 5-day body-part split dedicating one session to each major muscle group with maximum volume.", DurationWeeks = 12, DaysPerWeek = 5, TargetLevel = FitnessLevel.Advanced.ToString(), TargetGoal = PrimaryGoal.Hypertrophy.ToString(), IsPreBuilt = true },
            new ProgramEntity { Name = "Beginner Fundamentals", Description = "A structured introduction to resistance training focused on proper form and building a strength base.", DurationWeeks = 8, DaysPerWeek = 3, TargetLevel = FitnessLevel.Beginner.ToString(), TargetGoal = PrimaryGoal.GeneralHealth.ToString(), IsPreBuilt = true },
            new ProgramEntity { Name = "Strength Builder", Description = "A 12-week strength program built around squat, bench, deadlift, and overhead press with linear periodisation.", DurationWeeks = 12, DaysPerWeek = 4, TargetLevel = FitnessLevel.Intermediate.ToString(), TargetGoal = PrimaryGoal.Strength.ToString(), IsPreBuilt = true },
            new ProgramEntity { Name = "Fat Loss Circuit", Description = "A 6-week high-intensity program combining resistance training with cardiovascular conditioning for max calorie burn.", DurationWeeks = 6, DaysPerWeek = 4, TargetLevel = FitnessLevel.Intermediate.ToString(), TargetGoal = PrimaryGoal.FatLoss.ToString(), IsPreBuilt = true }
        );

        await db.SaveChangesAsync();
    }

    private static async Task SeedPremadeWorkoutsAsync(AppDbContext db)
    {
        if (await db.Workouts.AnyAsync()) return;

        var admin = await db.Users.FirstOrDefaultAsync(u => u.IsAdmin);
        if (admin is null) return;

        var exercises = await db.Exercises.ToDictionaryAsync(e => e.Name, e => e.Id);
        var programs = await db.Programs.ToDictionaryAsync(p => p.Name, p => p.Id);

        var workouts = new List<(string Program, string Name, int Sort, (string Ex, int Sets, int Reps, int RestSec)[] Items)>
        {
            ("Push Pull Legs", "PPL Push A", 0, new[] {
                ("Barbell Bench Press", 4, 8, 120), ("Incline Dumbbell Press", 3, 10, 90), ("Overhead Press", 3, 10, 90),
                ("Lateral Raise", 3, 15, 60), ("Tricep Pushdown", 3, 12, 60), ("Cable Flye", 3, 12, 60)
            }),
            ("Push Pull Legs", "PPL Pull A", 1, new[] {
                ("Barbell Row", 4, 8, 120), ("Pull-Up", 3, 8, 120), ("Seated Cable Row", 3, 10, 90),
                ("Face Pull", 3, 15, 60), ("Barbell Curl", 3, 10, 60), ("Hammer Curl", 3, 12, 60)
            }),
            ("Push Pull Legs", "PPL Legs A", 2, new[] {
                ("Barbell Back Squat", 4, 8, 180), ("Romanian Deadlift", 3, 10, 120), ("Leg Press", 3, 12, 90),
                ("Leg Curl", 3, 12, 60), ("Standing Calf Raise", 4, 15, 45), ("Plank", 3, 1, 60)
            }),
            ("Push Pull Legs", "PPL Push B", 3, new[] {
                ("Incline Barbell Bench Press", 4, 8, 120), ("Dumbbell Shoulder Press", 3, 10, 90), ("Dips", 3, 10, 90),
                ("Lateral Raise", 4, 12, 60), ("Skull Crushers", 3, 10, 60), ("Close Grip Bench Press", 3, 10, 90)
            }),
            ("Push Pull Legs", "PPL Pull B", 4, new[] {
                ("Conventional Deadlift", 4, 5, 180), ("Lat Pulldown", 3, 10, 90), ("Dumbbell Row", 3, 10, 90),
                ("Reverse Flye", 3, 15, 60), ("Dumbbell Curl", 3, 12, 60), ("Wrist Curl", 3, 15, 45)
            }),
            ("Push Pull Legs", "PPL Legs B", 5, new[] {
                ("Front Squat", 4, 8, 150), ("Bulgarian Split Squat", 3, 10, 90), ("Hip Thrust", 3, 12, 90),
                ("Leg Extension", 3, 15, 60), ("Seated Calf Raise", 4, 15, 45), ("Hanging Leg Raise", 3, 12, 60)
            }),

            ("Upper Lower Split", "Upper A", 0, new[] {
                ("Barbell Bench Press", 4, 6, 150), ("Barbell Row", 4, 6, 150), ("Overhead Press", 3, 8, 120),
                ("Lat Pulldown", 3, 10, 90), ("Dumbbell Curl", 3, 10, 60), ("Tricep Pushdown", 3, 10, 60)
            }),
            ("Upper Lower Split", "Lower A", 1, new[] {
                ("Barbell Back Squat", 4, 6, 180), ("Romanian Deadlift", 3, 8, 120), ("Leg Press", 3, 10, 90),
                ("Leg Curl", 3, 12, 60), ("Standing Calf Raise", 4, 12, 45), ("Plank", 3, 1, 60)
            }),
            ("Upper Lower Split", "Upper B", 2, new[] {
                ("Incline Dumbbell Press", 4, 8, 120), ("Chin-Up", 4, 8, 120), ("Dumbbell Shoulder Press", 3, 10, 90),
                ("Seated Cable Row", 3, 10, 90), ("Hammer Curl", 3, 12, 60), ("Skull Crushers", 3, 10, 60)
            }),
            ("Upper Lower Split", "Lower B", 3, new[] {
                ("Conventional Deadlift", 4, 5, 180), ("Front Squat", 3, 8, 150), ("Bulgarian Split Squat", 3, 10, 90),
                ("Hip Thrust", 3, 10, 90), ("Seated Calf Raise", 4, 15, 45), ("Ab Wheel Rollout", 3, 10, 60)
            }),

            ("Full Body 3-Day", "Full Body A", 0, new[] {
                ("Barbell Back Squat", 3, 8, 150), ("Barbell Bench Press", 3, 8, 120), ("Barbell Row", 3, 8, 120),
                ("Dumbbell Shoulder Press", 2, 10, 90), ("Plank", 3, 1, 60)
            }),
            ("Full Body 3-Day", "Full Body B", 1, new[] {
                ("Conventional Deadlift", 3, 5, 180), ("Incline Dumbbell Press", 3, 10, 90), ("Lat Pulldown", 3, 10, 90),
                ("Lateral Raise", 3, 12, 60), ("Barbell Curl", 2, 10, 60)
            }),
            ("Full Body 3-Day", "Full Body C", 2, new[] {
                ("Goblet Squat", 3, 10, 90), ("Dips", 3, 8, 90), ("Dumbbell Row", 3, 10, 90),
                ("Face Pull", 3, 15, 60), ("Glute Bridge", 3, 12, 60)
            }),

            ("Bro Split", "Chest Day", 0, new[] {
                ("Barbell Bench Press", 4, 8, 120), ("Incline Dumbbell Press", 4, 10, 90), ("Cable Flye", 3, 12, 60),
                ("Incline Barbell Bench Press", 3, 10, 90), ("Dips", 3, 10, 90)
            }),
            ("Bro Split", "Back Day", 1, new[] {
                ("Conventional Deadlift", 4, 5, 180), ("Barbell Row", 4, 8, 120), ("Pull-Up", 3, 8, 120),
                ("Seated Cable Row", 3, 10, 90), ("Dumbbell Pullover", 3, 12, 60)
            }),
            ("Bro Split", "Shoulder Day", 2, new[] {
                ("Overhead Press", 4, 8, 120), ("Dumbbell Shoulder Press", 3, 10, 90), ("Lateral Raise", 4, 15, 60),
                ("Face Pull", 3, 15, 60), ("Reverse Flye", 3, 15, 60), ("Barbell Shrug", 4, 10, 60)
            }),
            ("Bro Split", "Leg Day", 3, new[] {
                ("Barbell Back Squat", 5, 6, 180), ("Romanian Deadlift", 4, 8, 120), ("Leg Press", 3, 12, 90),
                ("Leg Extension", 3, 15, 60), ("Leg Curl", 3, 12, 60), ("Standing Calf Raise", 4, 15, 45)
            }),
            ("Bro Split", "Arms Day", 4, new[] {
                ("Barbell Curl", 4, 10, 60), ("Close Grip Bench Press", 4, 8, 90), ("Hammer Curl", 3, 12, 60),
                ("Skull Crushers", 3, 10, 60), ("Preacher Curl", 3, 12, 60), ("Tricep Pushdown", 3, 12, 60)
            }),

            ("Beginner Fundamentals", "Beginner A", 0, new[] {
                ("Goblet Squat", 3, 10, 90), ("Push-Up", 3, 8, 60), ("Lat Pulldown", 3, 10, 90),
                ("Glute Bridge", 3, 12, 60), ("Plank", 3, 1, 60)
            }),
            ("Beginner Fundamentals", "Beginner B", 1, new[] {
                ("Leg Press", 3, 10, 90), ("Dumbbell Shoulder Press", 3, 10, 90), ("Seated Cable Row", 3, 10, 90),
                ("Dumbbell Curl", 2, 12, 60), ("Dead Bug", 3, 10, 45)
            }),
            ("Beginner Fundamentals", "Beginner C", 2, new[] {
                ("Walking Lunge", 3, 10, 90), ("Incline Dumbbell Press", 3, 10, 90), ("Dumbbell Row", 3, 10, 90),
                ("Face Pull", 3, 12, 60), ("Crunches", 3, 15, 45)
            }),

            ("Strength Builder", "Strength Squat", 0, new[] {
                ("Barbell Back Squat", 5, 5, 240), ("Bulgarian Split Squat", 3, 8, 120), ("Leg Extension", 3, 10, 60),
                ("Standing Calf Raise", 3, 12, 45), ("Plank", 3, 1, 60)
            }),
            ("Strength Builder", "Strength Bench", 1, new[] {
                ("Barbell Bench Press", 5, 5, 240), ("Close Grip Bench Press", 3, 8, 120), ("Incline Dumbbell Press", 3, 8, 90),
                ("Lateral Raise", 3, 12, 60), ("Tricep Pushdown", 3, 10, 60)
            }),
            ("Strength Builder", "Strength Deadlift", 2, new[] {
                ("Conventional Deadlift", 5, 3, 300), ("Barbell Row", 4, 6, 150), ("Good Morning", 3, 8, 120),
                ("Chin-Up", 3, 8, 120), ("Barbell Curl", 3, 10, 60)
            }),
            ("Strength Builder", "Strength OHP", 3, new[] {
                ("Overhead Press", 5, 5, 240), ("Dumbbell Shoulder Press", 3, 8, 120), ("Face Pull", 3, 15, 60),
                ("Dips", 3, 8, 90), ("Barbell Shrug", 3, 10, 60)
            }),

            ("Fat Loss Circuit", "Circuit A", 0, new[] {
                ("Goblet Squat", 4, 12, 30), ("Push-Up", 4, 12, 30), ("Barbell Row", 4, 12, 30),
                ("Walking Lunge", 3, 12, 30), ("Plank", 3, 1, 30)
            }),
            ("Fat Loss Circuit", "Circuit B", 1, new[] {
                ("Kettlebell Swing", 4, 15, 30), ("Dips", 3, 10, 30), ("Lat Pulldown", 4, 12, 30),
                ("Bulgarian Split Squat", 3, 12, 30), ("Russian Twist", 3, 15, 30)
            }),
            ("Fat Loss Circuit", "Circuit C", 2, new[] {
                ("Barbell Back Squat", 4, 10, 45), ("Overhead Press", 3, 10, 30), ("Dumbbell Row", 4, 10, 30),
                ("Hip Thrust", 3, 12, 30), ("Hanging Leg Raise", 3, 10, 30)
            }),
            ("Fat Loss Circuit", "Circuit D", 3, new[] {
                ("Conventional Deadlift", 4, 8, 60), ("Incline Dumbbell Press", 3, 12, 30), ("Face Pull", 3, 15, 30),
                ("Leg Press", 3, 12, 30), ("Crunches", 3, 20, 30)
            })
        };

        foreach (var (programName, workoutName, sort, items) in workouts)
        {
            if (!programs.TryGetValue(programName, out var programId)) continue;

            var workout = new Workout
            {
                Name = workoutName,
                UserId = admin.Id,
                ProgramId = programId,
                SortOrder = sort
            };

            db.Workouts.Add(workout);
            await db.SaveChangesAsync();

            for (int i = 0; i < items.Length; i++)
            {
                var (exName, sets, reps, rest) = items[i];
                if (!exercises.TryGetValue(exName, out var exId)) continue;

                db.WorkoutExercises.Add(new WorkoutExercise
                {
                    WorkoutId = workout.Id,
                    ExerciseId = exId,
                    Sets = sets,
                    Reps = reps,
                    RestTimeSec = rest,
                    SortOrder = i
                });
            }

            await db.SaveChangesAsync();
        }
    }

    private static List<ExerciseSeed> BuildExerciseList()
    {
        return new List<ExerciseSeed>
        {
            new("Barbell Bench Press", "Lie on a flat bench with feet flat on the floor. Grip the barbell slightly wider than shoulder width. Unrack and lower the bar to mid-chest with elbows at roughly 45 degrees. Press the bar back up to full lockout. Keep shoulder blades retracted and maintain a slight arch in the upper back throughout.", "Compound", 5, ExerciseLevel.Intermediate, MovementType.Push, 5.0m, 4.0m, new[] { ("Chest", true), ("Triceps", false), ("Front Delts", false) }, new[] { "Barbell & Plates", "Bench" }, "https://www.youtube.com/watch?v=rT7DgCr-3pg"),
            new("Incline Dumbbell Press", "Set an adjustable bench to 30-45 degrees. Hold a dumbbell in each hand at shoulder level with palms facing forward. Press the dumbbells up and slightly inward until arms are extended. Lower under control to the starting position.", "Compound", 4, ExerciseLevel.Intermediate, MovementType.Push, 5.0m, 4.0m, new[] { ("Upper Chest", true), ("Front Delts", false), ("Triceps", false) }, new[] { "Dumbbells", "Bench" }, "https://www.youtube.com/watch?v=8iPEnn-ltC8"),
            new("Overhead Press", "Stand with feet shoulder width apart holding a barbell at shoulder height. Brace the core and press the bar overhead until arms are fully locked out. Lower back to shoulder height under control.", "Compound", 5, ExerciseLevel.Intermediate, MovementType.Push, 5.0m, 4.0m, new[] { ("Front Delts", true), ("Side Delts", false), ("Triceps", false) }, new[] { "Barbell & Plates" }, "https://www.youtube.com/watch?v=2yjwXTZQDDI"),
            new("Dumbbell Shoulder Press", "Sit on a bench with back support or stand. Hold dumbbells at shoulder height with palms facing forward. Press overhead until arms are fully extended. Lower back with control.", "Compound", 4, ExerciseLevel.Beginner, MovementType.Push, 5.0m, 4.0m, new[] { ("Front Delts", true), ("Side Delts", false), ("Triceps", false) }, new[] { "Dumbbells" }, null),
            new("Push-Up", "Start in a high plank with hands slightly wider than shoulder width. Keep the body straight from head to heels. Lower the chest to the floor by bending the elbows. Push back up. Engage the core throughout.", "Compound", 3, ExerciseLevel.Beginner, MovementType.Push, 8.0m, 3.0m, new[] { ("Chest", true), ("Triceps", false), ("Front Delts", false), ("Abs", false) }, new[] { "Bodyweight Only" }, null),
            new("Dips", "Grip parallel bars with arms fully extended. Lean slightly forward. Lower by bending elbows until upper arms are parallel to floor. Press back up to lockout.", "Compound", 5, ExerciseLevel.Intermediate, MovementType.Push, 8.0m, 3.5m, new[] { ("Chest", true), ("Triceps", true), ("Front Delts", false) }, new[] { "Bodyweight Only" }, null),
            new("Lateral Raise", "Stand with dumbbells at sides with slight bend in elbows. Raise dumbbells out to sides until arms are parallel to floor. Lower under control. Avoid momentum.", "Isolation", 2, ExerciseLevel.Beginner, MovementType.Push, 3.5m, 3.0m, new[] { ("Side Delts", true) }, new[] { "Dumbbells" }, null),
            new("Tricep Pushdown", "Stand facing a cable machine with attachment at high pulley. Grip with elbows pinned to sides. Extend forearms downward until arms are straight. Return with control.", "Isolation", 2, ExerciseLevel.Beginner, MovementType.Push, 3.5m, 3.0m, new[] { ("Triceps", true) }, new[] { "Cable Machine" }, null),
            new("Skull Crushers", "Lie on a flat bench holding a bar with narrow grip above chest. Keeping upper arms vertical, bend elbows to lower bar toward forehead. Extend to return.", "Isolation", 3, ExerciseLevel.Intermediate, MovementType.Push, 3.5m, 3.5m, new[] { ("Triceps", true) }, new[] { "Barbell & Plates", "Bench" }, null),
            new("Cable Flye", "Set pulleys to chest height. Stand in centre with handle in each hand. With slight elbow bend, bring hands together in front of chest. Return with control.", "Isolation", 3, ExerciseLevel.Intermediate, MovementType.Push, 3.5m, 3.5m, new[] { ("Chest", true) }, new[] { "Cable Machine" }, null),
            new("Barbell Row", "Hinge at hips until torso is roughly 45 degrees. Grip barbell outside knees. Pull bar to lower chest. Lower with control. Keep back flat.", "Compound", 5, ExerciseLevel.Intermediate, MovementType.Pull, 5.0m, 4.0m, new[] { ("Upper Back", true), ("Lats", true), ("Biceps", false), ("Rear Delts", false) }, new[] { "Barbell & Plates" }, "https://www.youtube.com/watch?v=FWJR5Ve8bnQ"),
            new("Pull-Up", "Hang from bar with overhand grip wider than shoulder width. Pull up until chin clears bar. Lower to dead hang. Avoid swinging.", "Compound", 6, ExerciseLevel.Intermediate, MovementType.Pull, 8.0m, 4.0m, new[] { ("Lats", true), ("Upper Back", false), ("Biceps", false), ("Forearms", false) }, new[] { "Pull-Up Bar" }, null),
            new("Lat Pulldown", "Sit at pulldown machine with thighs secured. Wide overhand grip. Pull bar to upper chest. Return with control. Keep chest up.", "Compound", 4, ExerciseLevel.Beginner, MovementType.Pull, 5.0m, 3.5m, new[] { ("Lats", true), ("Upper Back", false), ("Biceps", false) }, new[] { "Cable Machine" }, null),
            new("Seated Cable Row", "Sit at cable row station with knees slightly bent. Pull handle to lower chest squeezing shoulder blades. Extend arms back with control.", "Compound", 4, ExerciseLevel.Beginner, MovementType.Pull, 5.0m, 3.5m, new[] { ("Upper Back", true), ("Lats", false), ("Biceps", false) }, new[] { "Cable Machine" }, null),
            new("Dumbbell Row", "One hand and knee on bench. Hold dumbbell in free hand. Pull to hip driving elbow back. Lower under control. Keep back flat.", "Compound", 3, ExerciseLevel.Beginner, MovementType.Pull, 5.0m, 3.5m, new[] { ("Lats", true), ("Upper Back", false), ("Biceps", false), ("Rear Delts", false) }, new[] { "Dumbbells", "Bench" }, null),
            new("Face Pull", "Cable at upper chest height with rope. Pull toward face driving elbows high. Externally rotate at end. Return with control.", "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 3.0m, new[] { ("Rear Delts", true), ("Traps", false) }, new[] { "Cable Machine" }, null),
            new("Barbell Curl", "Stand with underhand grip at arm's length. Keeping elbows pinned, curl to shoulder height. Lower under control. No swinging.", "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 3.0m, new[] { ("Biceps", true), ("Forearms", false) }, new[] { "Barbell & Plates" }, null),
            new("Dumbbell Curl", "Stand with dumbbells at arm's length palms forward. Curl to shoulder height keeping elbows stationary. Lower under control.", "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 3.0m, new[] { ("Biceps", true), ("Forearms", false) }, new[] { "Dumbbells" }, null),
            new("Hammer Curl", "Stand holding dumbbells with palms facing each other. Curl up maintaining neutral wrist. Lower under control. Targets brachioradialis.", "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 3.0m, new[] { ("Biceps", true), ("Forearms", true) }, new[] { "Dumbbells" }, null),
            new("Chin-Up", "Underhand grip at shoulder width. Pull up until chin clears bar. Lower to dead hang. Greater biceps emphasis than pull-up.", "Compound", 5, ExerciseLevel.Intermediate, MovementType.Pull, 8.0m, 4.0m, new[] { ("Lats", true), ("Biceps", true), ("Upper Back", false) }, new[] { "Pull-Up Bar" }, null),
            new("Barbell Back Squat", "Bar across upper traps. Feet shoulder width, toes slightly out. Squat until thighs parallel. Drive through full foot to stand. Keep chest up.", "Compound", 7, ExerciseLevel.Intermediate, MovementType.Squat, 6.0m, 4.5m, new[] { ("Quadriceps", true), ("Glutes", true), ("Hamstrings", false), ("Lower Back", false), ("Abs", false) }, new[] { "Barbell & Plates" }, "https://www.youtube.com/watch?v=ultWZbUMPL8"),
            new("Front Squat", "Bar resting on front delts with elbows high. Squat keeping torso upright until thighs parallel or below. Stand back up. Demands more quad and core.", "Compound", 7, ExerciseLevel.Advanced, MovementType.Squat, 6.0m, 4.5m, new[] { ("Quadriceps", true), ("Glutes", false), ("Abs", false) }, new[] { "Barbell & Plates" }, null),
            new("Goblet Squat", "Hold dumbbell or kettlebell at chest. Squat between legs keeping chest up and elbows inside knees. Stand back up. Great for learning squat mechanics.", "Compound", 3, ExerciseLevel.Beginner, MovementType.Squat, 5.0m, 4.0m, new[] { ("Quadriceps", true), ("Glutes", true) }, new[] { "Dumbbells", "Kettlebells" }, null),
            new("Leg Press", "Sit in machine with feet shoulder width on platform. Lower by bending knees. Press away without full lockout. Keep lower back on pad.", "Compound", 4, ExerciseLevel.Beginner, MovementType.Squat, 5.0m, 3.5m, new[] { ("Quadriceps", true), ("Glutes", false), ("Hamstrings", false) }, new[] { "Leg Press" }, null),
            new("Bulgarian Split Squat", "Rear foot elevated on bench. Hold dumbbells at sides. Lower back knee toward floor. Push through front foot to return. Builds single-leg strength.", "Compound", 5, ExerciseLevel.Intermediate, MovementType.Squat, 5.0m, 4.0m, new[] { ("Quadriceps", true), ("Glutes", true), ("Hamstrings", false) }, new[] { "Dumbbells", "Bench" }, null),
            new("Leg Extension", "Sit in machine with pad on lower shins. Extend legs until straight. Squeeze quads at top. Lower with control.", "Isolation", 2, ExerciseLevel.Beginner, MovementType.Squat, 3.5m, 3.0m, new[] { ("Quadriceps", true) }, new[] { "Full Gym Access" }, null),
            new("Walking Lunge", "Hold dumbbells at sides. Step forward into long stride, lower back knee toward floor. Step forward into next lunge. Alternate legs.", "Compound", 5, ExerciseLevel.Intermediate, MovementType.Squat, 6.0m, 3.5m, new[] { ("Quadriceps", true), ("Glutes", true), ("Hamstrings", false) }, new[] { "Dumbbells" }, null),
            new("Conventional Deadlift", "Feet hip width, bar over mid-foot. Hinge and grip outside knees. Flat back, drive through floor to stand. Lockout by squeezing glutes. Lower hips back.", "Compound", 8, ExerciseLevel.Intermediate, MovementType.Hinge, 6.0m, 5.0m, new[] { ("Hamstrings", true), ("Glutes", true), ("Lower Back", true), ("Traps", false), ("Forearms", false) }, new[] { "Barbell & Plates" }, "https://www.youtube.com/watch?v=op9kVnSso6Q"),
            new("Romanian Deadlift", "Hold barbell at hip height. Slight knee bend, push hips back lowering bar along legs. Feel hamstring stretch. Drive hips forward to return. Keep bar close.", "Compound", 5, ExerciseLevel.Intermediate, MovementType.Hinge, 6.0m, 4.5m, new[] { ("Hamstrings", true), ("Glutes", true), ("Lower Back", false) }, new[] { "Barbell & Plates" }, null),
            new("Hip Thrust", "Upper back against bench, loaded barbell across hips. Feet flat, knees at 90 degrees. Drive hips up to straight line. Squeeze glutes. Lower with control.", "Compound", 4, ExerciseLevel.Intermediate, MovementType.Hinge, 5.0m, 3.5m, new[] { ("Glutes", true), ("Hamstrings", false) }, new[] { "Barbell & Plates", "Bench" }, null),
            new("Kettlebell Swing", "Feet wider than shoulder width. Hinge to swing kettlebell between legs. Snap hips forward to chest height. Let gravity pull back. Power from hips.", "Compound", 5, ExerciseLevel.Intermediate, MovementType.Hinge, 8.0m, null, new[] { ("Glutes", true), ("Hamstrings", true), ("Lower Back", false), ("Abs", false) }, new[] { "Kettlebells" }, null),
            new("Good Morning", "Bar across upper back. Slight knee bend, push hips back and hinge forward until torso nearly parallel. Drive hips forward. Keep back flat.", "Compound", 5, ExerciseLevel.Intermediate, MovementType.Hinge, 5.0m, 4.0m, new[] { ("Hamstrings", true), ("Lower Back", true), ("Glutes", false) }, new[] { "Barbell & Plates" }, null),
            new("Leg Curl", "Lie face down on machine with pad above ankles. Curl legs toward glutes. Lower with control. Keep hips on pad.", "Isolation", 2, ExerciseLevel.Beginner, MovementType.Hinge, 3.5m, 3.0m, new[] { ("Hamstrings", true) }, new[] { "Full Gym Access" }, null),
            new("Glute Bridge", "Lie face up, knees bent, feet flat. Drive hips up to straight line. Squeeze glutes at top. Lower back down. Foundational glute exercise.", "Isolation", 1, ExerciseLevel.Beginner, MovementType.Hinge, 3.5m, 3.0m, new[] { ("Glutes", true), ("Hamstrings", false) }, new[] { "Bodyweight Only" }, null),
            new("Plank", "Forearms and toes, body in straight line. Core braced, glutes squeezed, hips level. Hold for time. Avoid hip sag.", "Isometric", 3, ExerciseLevel.Beginner, MovementType.Isometric, 3.8m, null, new[] { ("Abs", true), ("Obliques", false), ("Lower Back", false) }, new[] { "Bodyweight Only" }, null),
            new("Dead Bug", "Lie on back, arms to ceiling, hips and knees at 90. Extend opposite arm and leg toward floor. Return and switch. Press lower back into floor.", "Isolation", 2, ExerciseLevel.Beginner, MovementType.Isometric, 3.8m, 4.0m, new[] { ("Abs", true), ("Hip Flexors", false) }, new[] { "Bodyweight Only" }, null),
            new("Russian Twist", "Sit with knees bent, feet off ground, torso at 45 degrees. Rotate side to side touching weight beside each hip. Control the rotation.", "Isolation", 3, ExerciseLevel.Beginner, MovementType.Rotation, 3.8m, 3.0m, new[] { ("Obliques", true), ("Abs", false) }, new[] { "Bodyweight Only", "Medicine Ball" }, null),
            new("Hanging Leg Raise", "Hang from bar with arms extended. Raise straight legs to parallel or higher. Lower with control. Avoid swinging.", "Isolation", 5, ExerciseLevel.Intermediate, MovementType.Isometric, 3.8m, 4.0m, new[] { ("Abs", true), ("Hip Flexors", false), ("Obliques", false) }, new[] { "Pull-Up Bar" }, null),
            new("Ab Wheel Rollout", "Kneel holding ab wheel. Roll forward extending body while keeping core tight. Pull back using abdominals. Avoid lower back collapse.", "Compound", 6, ExerciseLevel.Intermediate, MovementType.Isometric, 5.0m, 4.0m, new[] { ("Abs", true), ("Obliques", false), ("Lower Back", false) }, new[] { "Bodyweight Only" }, null),
            new("Side Plank", "Lie on side supported by forearm and foot. Lift hips to straight line. Hold with core engaged. Switch sides.", "Isometric", 3, ExerciseLevel.Beginner, MovementType.Isometric, 3.8m, null, new[] { ("Obliques", true), ("Abs", false) }, new[] { "Bodyweight Only" }, null),
            new("Cable Woodchop", "Cable at high position, stand sideways. Pull handle diagonally across body from high to low rotating torso. Control return. Core drives rotation.", "Isolation", 3, ExerciseLevel.Intermediate, MovementType.Rotation, 3.8m, 3.5m, new[] { ("Obliques", true), ("Abs", false) }, new[] { "Cable Machine" }, null),
            new("Crunches", "Lie face up, knees bent, feet flat. Hands behind head. Curl upper body toward knees. Lift only shoulders off floor. Lower with control.", "Isolation", 1, ExerciseLevel.Beginner, MovementType.Isometric, 3.8m, 2.5m, new[] { ("Abs", true) }, new[] { "Bodyweight Only" }, null),
            new("Farmer's Walk", "Hold heavy dumbbells at sides. Walk forward with controlled steps, torso upright, core tight. Walk for distance or time.", "Compound", 4, ExerciseLevel.Beginner, MovementType.Carry, 6.0m, null, new[] { ("Forearms", true), ("Traps", true), ("Abs", false) }, new[] { "Dumbbells", "Kettlebells" }, null),
            new("Standing Calf Raise", "Stand on edge of step, balls of feet on platform, heels hanging. Rise onto toes. Lower heels below platform for full stretch.", "Isolation", 1, ExerciseLevel.Beginner, MovementType.Squat, 3.5m, 2.5m, new[] { ("Calves", true) }, new[] { "Bodyweight Only" }, null),
            new("Seated Calf Raise", "Sit at calf raise machine, pads on lower thighs. Press through toes lifting weight. Lower for full stretch. Targets soleus.", "Isolation", 1, ExerciseLevel.Beginner, MovementType.Squat, 3.5m, 2.5m, new[] { ("Calves", true) }, new[] { "Full Gym Access" }, null),
            new("Treadmill Running", "Run on treadmill at steady pace. Upright posture with slight forward lean. Land with feet under hips. Adjust speed and incline to fitness level.", "Cardio", 3, ExerciseLevel.Beginner, MovementType.Cardio, 9.8m, null, new[] { ("Quadriceps", false), ("Hamstrings", false), ("Calves", false), ("Glutes", false) }, new[] { "Treadmill" }, null),
            new("Stationary Cycling", "Sit with slight knee bend at bottom of pedal stroke. Pedal at steady cadence adjusting resistance. Core engaged, no rocking.", "Cardio", 2, ExerciseLevel.Beginner, MovementType.Cardio, 8.0m, null, new[] { ("Quadriceps", false), ("Hamstrings", false), ("Calves", false) }, new[] { "Stationary Bike" }, null),
            new("Rowing Machine", "Feet strapped in. Drive with legs first, lean back slightly, pull handle to lower chest. Return arms, lean forward, bend knees. Fluid sequence.", "Cardio", 3, ExerciseLevel.Beginner, MovementType.Cardio, 7.0m, null, new[] { ("Upper Back", false), ("Lats", false), ("Quadriceps", false), ("Hamstrings", false) }, new[] { "Rowing Machine" }, null),
            new("Jump Rope", "Hold handles at hip height. Swing rope overhead and jump just high enough to clear. Land softly on balls of feet. Wrists turn the rope.", "Cardio", 4, ExerciseLevel.Intermediate, MovementType.Cardio, 12.3m, null, new[] { ("Calves", false), ("Quadriceps", false) }, new[] { "Bodyweight Only" }, null),
            new("Chest Supported Row", "Face down on incline bench 30-45 degrees. Dumbbells hanging. Pull to sides of chest squeezing shoulder blades. Lower under control.", "Compound", 3, ExerciseLevel.Beginner, MovementType.Pull, 5.0m, 3.5m, new[] { ("Upper Back", true), ("Lats", false), ("Rear Delts", false), ("Biceps", false) }, new[] { "Dumbbells", "Bench" }, null),
            new("Reverse Flye", "Bent forward at hips, torso nearly parallel. Dumbbells hanging, palms facing. Raise out to sides squeezing shoulder blades. Lower under control.", "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 3.0m, new[] { ("Rear Delts", true), ("Upper Back", false) }, new[] { "Dumbbells" }, null),
            new("Incline Barbell Bench Press", "Bench at 30-45 degrees. Grip bar wider than shoulder width. Lower to upper chest. Press to lockout. Shifts emphasis to upper chest.", "Compound", 5, ExerciseLevel.Intermediate, MovementType.Push, 5.0m, 4.0m, new[] { ("Upper Chest", true), ("Front Delts", false), ("Triceps", false) }, new[] { "Barbell & Plates", "Bench" }, null),
            new("Close Grip Bench Press", "Lie on flat bench, grip at shoulder width or narrower. Lower to lower chest keeping elbows close. Press to lockout. Emphasises triceps.", "Compound", 5, ExerciseLevel.Intermediate, MovementType.Push, 5.0m, 4.0m, new[] { ("Triceps", true), ("Chest", false), ("Front Delts", false) }, new[] { "Barbell & Plates", "Bench" }, null),
            new("Sumo Deadlift", "Wide stance, toes out. Grip inside knees at shoulder width. Flat back, drive through floor. Wide stance shifts emphasis to quads and glutes.", "Compound", 8, ExerciseLevel.Advanced, MovementType.Hinge, 6.0m, 5.0m, new[] { ("Glutes", true), ("Quadriceps", true), ("Hamstrings", false), ("Lower Back", false) }, new[] { "Barbell & Plates" }, null),
            new("Hack Squat", "Stand in hack squat machine, shoulders under pads. Lower by bending knees until parallel. Press back up. Torso fixed, focus on legs.", "Compound", 4, ExerciseLevel.Intermediate, MovementType.Squat, 5.0m, 4.0m, new[] { ("Quadriceps", true), ("Glutes", false) }, new[] { "Full Gym Access" }, null),
            new("Preacher Curl", "Sit at preacher bench, upper arms on pad. Curl weight toward shoulders. Lower to full extension. Pad prevents momentum.", "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 3.5m, new[] { ("Biceps", true) }, new[] { "Dumbbells", "Bench" }, null),
            new("Wrist Curl", "Sit with forearms on thighs, wrists over knees. Underhand grip. Curl wrists upward. Lower with control. Builds forearm size.", "Isolation", 1, ExerciseLevel.Beginner, MovementType.Pull, 2.5m, 2.5m, new[] { ("Forearms", true) }, new[] { "Dumbbells" }, null),
            new("Barbell Shrug", "Hold barbell in front at shoulder width. Shrug shoulders straight up toward ears. Hold briefly. Lower with control. No rolling.", "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 2.5m, new[] { ("Traps", true) }, new[] { "Barbell & Plates" }, null),
            new("Dumbbell Pullover", "Lie on bench, single dumbbell above chest, arms slightly bent. Lower behind head in arc until arms in line with torso. Pull back using lats and chest.", "Compound", 3, ExerciseLevel.Intermediate, MovementType.Pull, 4.0m, 4.0m, new[] { ("Lats", true), ("Chest", false) }, new[] { "Dumbbells", "Bench" }, null)
        };
    }

    private static List<Food> BuildFoodList()
    {
        return new List<Food>
        {
            new() { Name = "Chicken Breast (raw)", CaloriesPer100g = 120, ProteinPer100g = 22.5m, CarbsPer100g = 0, FatPer100g = 2.6m },
            new() { Name = "Chicken Thigh (raw)", CaloriesPer100g = 177, ProteinPer100g = 17.3m, CarbsPer100g = 0, FatPer100g = 12.0m },
            new() { Name = "Salmon Fillet (raw)", CaloriesPer100g = 208, ProteinPer100g = 20.4m, CarbsPer100g = 0, FatPer100g = 13.4m },
            new() { Name = "Tuna (canned in water)", CaloriesPer100g = 116, ProteinPer100g = 25.5m, CarbsPer100g = 0, FatPer100g = 0.8m },
            new() { Name = "Lean Beef Mince (5% fat)", CaloriesPer100g = 137, ProteinPer100g = 21.4m, CarbsPer100g = 0, FatPer100g = 5.0m },
            new() { Name = "Whole Eggs", CaloriesPer100g = 155, ProteinPer100g = 12.6m, CarbsPer100g = 1.1m, FatPer100g = 11.0m },
            new() { Name = "Egg Whites", CaloriesPer100g = 52, ProteinPer100g = 10.9m, CarbsPer100g = 0.7m, FatPer100g = 0.2m },
            new() { Name = "Greek Yoghurt (0% fat)", CaloriesPer100g = 59, ProteinPer100g = 10.2m, CarbsPer100g = 3.6m, FatPer100g = 0.4m },
            new() { Name = "Cottage Cheese (low fat)", CaloriesPer100g = 72, ProteinPer100g = 12.4m, CarbsPer100g = 2.7m, FatPer100g = 1.0m },
            new() { Name = "Whey Protein Powder", CaloriesPer100g = 380, ProteinPer100g = 75.0m, CarbsPer100g = 10.0m, FatPer100g = 5.0m },
            new() { Name = "White Rice (uncooked)", CaloriesPer100g = 365, ProteinPer100g = 6.6m, CarbsPer100g = 80.0m, FatPer100g = 0.6m },
            new() { Name = "Brown Rice (uncooked)", CaloriesPer100g = 362, ProteinPer100g = 7.5m, CarbsPer100g = 76.2m, FatPer100g = 2.7m },
            new() { Name = "Rolled Oats", CaloriesPer100g = 389, ProteinPer100g = 13.2m, CarbsPer100g = 66.3m, FatPer100g = 6.9m },
            new() { Name = "Sweet Potato (raw)", CaloriesPer100g = 86, ProteinPer100g = 1.6m, CarbsPer100g = 20.1m, FatPer100g = 0.1m },
            new() { Name = "White Potato (raw)", CaloriesPer100g = 77, ProteinPer100g = 2.0m, CarbsPer100g = 17.5m, FatPer100g = 0.1m },
            new() { Name = "Wholemeal Bread", CaloriesPer100g = 247, ProteinPer100g = 13.0m, CarbsPer100g = 41.3m, FatPer100g = 3.4m },
            new() { Name = "White Pasta (uncooked)", CaloriesPer100g = 371, ProteinPer100g = 13.0m, CarbsPer100g = 74.7m, FatPer100g = 1.5m },
            new() { Name = "Banana", CaloriesPer100g = 89, ProteinPer100g = 1.1m, CarbsPer100g = 22.8m, FatPer100g = 0.3m },
            new() { Name = "Apple", CaloriesPer100g = 52, ProteinPer100g = 0.3m, CarbsPer100g = 13.8m, FatPer100g = 0.2m },
            new() { Name = "Blueberries", CaloriesPer100g = 57, ProteinPer100g = 0.7m, CarbsPer100g = 14.5m, FatPer100g = 0.3m },
            new() { Name = "Broccoli", CaloriesPer100g = 34, ProteinPer100g = 2.8m, CarbsPer100g = 6.6m, FatPer100g = 0.4m },
            new() { Name = "Spinach (raw)", CaloriesPer100g = 23, ProteinPer100g = 2.9m, CarbsPer100g = 3.6m, FatPer100g = 0.4m },
            new() { Name = "Avocado", CaloriesPer100g = 160, ProteinPer100g = 2.0m, CarbsPer100g = 8.5m, FatPer100g = 14.7m },
            new() { Name = "Olive Oil", CaloriesPer100g = 884, ProteinPer100g = 0, CarbsPer100g = 0, FatPer100g = 100.0m },
            new() { Name = "Peanut Butter", CaloriesPer100g = 588, ProteinPer100g = 25.1m, CarbsPer100g = 20.0m, FatPer100g = 50.4m },
            new() { Name = "Almonds", CaloriesPer100g = 579, ProteinPer100g = 21.2m, CarbsPer100g = 21.6m, FatPer100g = 49.9m },
            new() { Name = "Cheddar Cheese", CaloriesPer100g = 403, ProteinPer100g = 24.9m, CarbsPer100g = 1.3m, FatPer100g = 33.1m },
            new() { Name = "Whole Milk", CaloriesPer100g = 61, ProteinPer100g = 3.2m, CarbsPer100g = 4.8m, FatPer100g = 3.3m },
            new() { Name = "Tofu (firm)", CaloriesPer100g = 144, ProteinPer100g = 17.3m, CarbsPer100g = 2.8m, FatPer100g = 8.7m },
            new() { Name = "Red Lentils (uncooked)", CaloriesPer100g = 358, ProteinPer100g = 24.6m, CarbsPer100g = 60.1m, FatPer100g = 1.1m },
            new() { Name = "Chickpeas (canned)", CaloriesPer100g = 139, ProteinPer100g = 7.0m, CarbsPer100g = 22.5m, FatPer100g = 2.6m },
            new() { Name = "Quinoa (uncooked)", CaloriesPer100g = 368, ProteinPer100g = 14.1m, CarbsPer100g = 64.2m, FatPer100g = 6.1m },
            new() { Name = "Turkey Breast (raw)", CaloriesPer100g = 104, ProteinPer100g = 23.7m, CarbsPer100g = 0, FatPer100g = 0.7m },
            new() { Name = "White Fish (cod, raw)", CaloriesPer100g = 82, ProteinPer100g = 17.8m, CarbsPer100g = 0, FatPer100g = 0.7m },
            new() { Name = "Prawns (raw)", CaloriesPer100g = 99, ProteinPer100g = 20.1m, CarbsPer100g = 0.9m, FatPer100g = 1.7m }
        };
    }

    private record ExerciseSeed(
        string Name, string Description, string ExerciseType, int Difficulty,
        ExerciseLevel Level, MovementType Movement, decimal Met, decimal? RepTime,
        (string Name, bool IsPrimary)[] Muscles, string[] Equipment, string? VideoUrl
    );
}
