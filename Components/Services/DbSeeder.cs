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

        var exercises = BuildExerciseList();

        foreach (var def in exercises)
        {
            var exercise = new Exercise
            {
                Name = def.Name,
                Description = def.Description,
                ExerciseType = def.ExerciseType,
                DifficultyRating = def.Difficulty,
                Level = def.Level,
                MovementType = def.Movement,
                MetValue = def.Met,
                RepTimeSec = def.RepTime
            };

            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();

            foreach (var (muscleName, isPrimary) in def.Muscles)
            {
                if (muscles.TryGetValue(muscleName, out var mgId))
                    db.ExerciseMuscleGroups.Add(new ExerciseMuscleGroup { ExerciseId = exercise.Id, MuscleGroupId = mgId, IsPrimary = isPrimary });
            }

            foreach (var equipName in def.Equipment)
            {
                if (equipment.TryGetValue(equipName, out var eqId))
                    db.ExerciseEquipment.Add(new ExerciseEquipment { ExerciseId = exercise.Id, EquipmentId = eqId });
            }

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
            new ProgramEntity
            {
                Name = "Push Pull Legs",
                Description = "A 6-day split that groups exercises by movement pattern. Push days train chest, shoulders, and triceps. Pull days train back and biceps. Leg days train quads, hamstrings, glutes, and calves. Each muscle group is hit twice per week for optimal hypertrophy.",
                DurationWeeks = 12,
                DaysPerWeek = 6,
                TargetLevel = FitnessLevel.Intermediate.ToString(),
                TargetGoal = PrimaryGoal.Hypertrophy.ToString(),
                IsPreBuilt = true
            },
            new ProgramEntity
            {
                Name = "Upper Lower Split",
                Description = "A 4-day program alternating between upper body and lower body sessions. Provides a balanced approach with adequate recovery time between sessions targeting the same muscles. Ideal for those who want solid results without training every day.",
                DurationWeeks = 12,
                DaysPerWeek = 4,
                TargetLevel = FitnessLevel.Intermediate.ToString(),
                TargetGoal = PrimaryGoal.Strength.ToString(),
                IsPreBuilt = true
            },
            new ProgramEntity
            {
                Name = "Full Body 3-Day",
                Description = "Three full-body sessions per week with a rest day between each. Every major muscle group is trained each session with compound movements. High frequency per muscle group makes this efficient for strength and general fitness.",
                DurationWeeks = 8,
                DaysPerWeek = 3,
                TargetLevel = FitnessLevel.Beginner.ToString(),
                TargetGoal = PrimaryGoal.GeneralHealth.ToString(),
                IsPreBuilt = true
            },
            new ProgramEntity
            {
                Name = "Bro Split",
                Description = "A 5-day body-part split dedicating one session to each major muscle group: chest, back, shoulders, legs, and arms. Each muscle gets maximum volume in a single session with a full week to recover before being trained again.",
                DurationWeeks = 12,
                DaysPerWeek = 5,
                TargetLevel = FitnessLevel.Advanced.ToString(),
                TargetGoal = PrimaryGoal.Hypertrophy.ToString(),
                IsPreBuilt = true
            },
            new ProgramEntity
            {
                Name = "Beginner Fundamentals",
                Description = "A structured introduction to resistance training focused on learning proper form and building a base of strength. Starts with machines and bodyweight movements, gradually introducing free weights. Progressive overload is built in week to week.",
                DurationWeeks = 8,
                DaysPerWeek = 3,
                TargetLevel = FitnessLevel.Beginner.ToString(),
                TargetGoal = PrimaryGoal.GeneralHealth.ToString(),
                IsPreBuilt = true
            },
            new ProgramEntity
            {
                Name = "Couch to 5K",
                Description = "An 8-week progressive running program that takes complete beginners from walking to running 5 kilometres continuously. Alternates between walking and running intervals, gradually increasing running duration each week.",
                DurationWeeks = 8,
                DaysPerWeek = 3,
                TargetLevel = FitnessLevel.Beginner.ToString(),
                TargetGoal = PrimaryGoal.Endurance.ToString(),
                IsPreBuilt = true
            },
            new ProgramEntity
            {
                Name = "Strength Builder",
                Description = "A 12-week strength-focused program built around the squat, bench press, deadlift, and overhead press. Uses linear periodisation with heavy compound lifts at low rep ranges. Accessories target weak points and muscular balance.",
                DurationWeeks = 12,
                DaysPerWeek = 4,
                TargetLevel = FitnessLevel.Intermediate.ToString(),
                TargetGoal = PrimaryGoal.Strength.ToString(),
                IsPreBuilt = true
            },
            new ProgramEntity
            {
                Name = "Fat Loss Circuit",
                Description = "A 6-week high-intensity program combining resistance training with cardiovascular conditioning. Uses circuit-style workouts with minimal rest to keep heart rate elevated. Pairs compound lifts with bodyweight cardio bursts for maximum calorie burn.",
                DurationWeeks = 6,
                DaysPerWeek = 4,
                TargetLevel = FitnessLevel.Intermediate.ToString(),
                TargetGoal = PrimaryGoal.FatLoss.ToString(),
                IsPreBuilt = true
            }
        );

        await db.SaveChangesAsync();
    }

    private static List<ExerciseSeed> BuildExerciseList()
    {
        return new List<ExerciseSeed>
        {
            new("Barbell Bench Press",
                "Lie on a flat bench with feet flat on the floor. Grip the barbell slightly wider than shoulder width. Unrack and lower the bar to mid-chest with elbows at roughly 45 degrees. Press the bar back up to full lockout. Keep shoulder blades retracted and maintain a slight arch in the upper back throughout.",
                "Compound", 5, ExerciseLevel.Intermediate, MovementType.Push, 5.0m, 4.0m,
                new[] { ("Chest", true), ("Triceps", false), ("Front Delts", false) },
                new[] { "Barbell & Plates", "Bench" }),

            new("Incline Dumbbell Press",
                "Set an adjustable bench to 30-45 degrees. Hold a dumbbell in each hand at shoulder level with palms facing forward. Press the dumbbells up and slightly inward until arms are extended. Lower under control to the starting position. The incline angle emphasises the upper portion of the chest.",
                "Compound", 4, ExerciseLevel.Intermediate, MovementType.Push, 5.0m, 4.0m,
                new[] { ("Upper Chest", true), ("Front Delts", false), ("Triceps", false) },
                new[] { "Dumbbells", "Bench" }),

            new("Overhead Press",
                "Stand with feet shoulder width apart holding a barbell at shoulder height with an overhand grip. Brace the core and press the bar overhead until arms are fully locked out. Lower the bar back to shoulder height under control. Keep the ribcage down and avoid excessive arching of the lower back.",
                "Compound", 5, ExerciseLevel.Intermediate, MovementType.Push, 5.0m, 4.0m,
                new[] { ("Front Delts", true), ("Side Delts", false), ("Triceps", false) },
                new[] { "Barbell & Plates" }),

            new("Dumbbell Shoulder Press",
                "Sit on a bench with back support or stand with feet shoulder width apart. Hold dumbbells at shoulder height with palms facing forward. Press the dumbbells overhead until arms are fully extended. Lower them back to shoulder height with control.",
                "Compound", 4, ExerciseLevel.Beginner, MovementType.Push, 5.0m, 4.0m,
                new[] { ("Front Delts", true), ("Side Delts", false), ("Triceps", false) },
                new[] { "Dumbbells" }),

            new("Push-Up",
                "Start in a high plank position with hands slightly wider than shoulder width. Keep the body in a straight line from head to heels. Lower the chest to the floor by bending the elbows. Push back up to the starting position. Engage the core throughout and avoid letting the hips sag or pike up.",
                "Compound", 3, ExerciseLevel.Beginner, MovementType.Push, 8.0m, 3.0m,
                new[] { ("Chest", true), ("Triceps", false), ("Front Delts", false), ("Abs", false) },
                new[] { "Bodyweight Only" }),

            new("Dips",
                "Grip parallel bars and support your body with arms fully extended. Lean the torso slightly forward. Lower the body by bending the elbows until upper arms are at least parallel to the floor. Press back up to full lockout. A greater forward lean targets the chest more while staying upright emphasises the triceps.",
                "Compound", 5, ExerciseLevel.Intermediate, MovementType.Push, 8.0m, 3.5m,
                new[] { ("Chest", true), ("Triceps", true), ("Front Delts", false) },
                new[] { "Bodyweight Only" }),

            new("Lateral Raise",
                "Stand with dumbbells at your sides and a slight bend in the elbows. Raise the dumbbells out to the sides until arms are parallel to the floor. Keep a slight forward lean and lead with the elbows. Lower under control. Avoid using momentum or shrugging the traps.",
                "Isolation", 2, ExerciseLevel.Beginner, MovementType.Push, 3.5m, 3.0m,
                new[] { ("Side Delts", true) },
                new[] { "Dumbbells" }),

            new("Tricep Pushdown",
                "Stand facing a cable machine with a straight bar or rope attachment at the high pulley. Grip the attachment with elbows pinned to your sides. Extend the forearms downward until arms are fully straight. Return to the starting position with control. Keep the upper arms stationary throughout.",
                "Isolation", 2, ExerciseLevel.Beginner, MovementType.Push, 3.5m, 3.0m,
                new[] { ("Triceps", true) },
                new[] { "Cable Machine" }),

            new("Skull Crushers",
                "Lie on a flat bench holding an EZ bar or barbell with a narrow grip above the chest. Keeping the upper arms vertical, bend the elbows to lower the bar toward the forehead. Extend the elbows to press the bar back to the starting position. Keep the elbows pointing toward the ceiling throughout.",
                "Isolation", 3, ExerciseLevel.Intermediate, MovementType.Push, 3.5m, 3.5m,
                new[] { ("Triceps", true) },
                new[] { "Barbell & Plates", "Bench" }),

            new("Cable Flye",
                "Set the pulleys to chest height on a cable crossover machine. Stand in the centre with a handle in each hand and step forward slightly. With a slight bend in the elbows, bring the hands together in front of the chest in an arc. Return to the starting position with arms wide. Squeeze the chest at peak contraction.",
                "Isolation", 3, ExerciseLevel.Intermediate, MovementType.Push, 3.5m, 3.5m,
                new[] { ("Chest", true) },
                new[] { "Cable Machine" }),

            new("Barbell Row",
                "Stand with feet shoulder width apart, hinge at the hips until the torso is roughly 45 degrees to the floor. Grip the barbell with hands just outside the knees. Pull the bar into the lower chest or upper abdomen. Lower the bar back to arm's length with control. Keep the back flat and core braced throughout.",
                "Compound", 5, ExerciseLevel.Intermediate, MovementType.Pull, 5.0m, 4.0m,
                new[] { ("Upper Back", true), ("Lats", true), ("Biceps", false), ("Rear Delts", false) },
                new[] { "Barbell & Plates" }),

            new("Pull-Up",
                "Hang from a pull-up bar with an overhand grip slightly wider than shoulder width. Pull the body up until the chin clears the bar by driving the elbows down and back. Lower under control to a full dead hang. Avoid swinging or kipping. Initiate the movement by depressing and retracting the shoulder blades.",
                "Compound", 6, ExerciseLevel.Intermediate, MovementType.Pull, 8.0m, 4.0m,
                new[] { ("Lats", true), ("Upper Back", false), ("Biceps", false), ("Forearms", false) },
                new[] { "Pull-Up Bar" }),

            new("Lat Pulldown",
                "Sit at a lat pulldown machine with thighs secured under the pads. Grip the bar with a wide overhand grip. Pull the bar down to the upper chest by driving the elbows toward the hips. Return the bar to the top with control. Keep the chest up and avoid leaning back excessively.",
                "Compound", 4, ExerciseLevel.Beginner, MovementType.Pull, 5.0m, 3.5m,
                new[] { ("Lats", true), ("Upper Back", false), ("Biceps", false) },
                new[] { "Cable Machine" }),

            new("Seated Cable Row",
                "Sit at a cable row station with feet on the footrests and knees slightly bent. Grip the handle and sit upright. Pull the handle to the lower chest by squeezing the shoulder blades together. Extend the arms back to the starting position with control. Avoid rounding the back at the bottom.",
                "Compound", 4, ExerciseLevel.Beginner, MovementType.Pull, 5.0m, 3.5m,
                new[] { ("Upper Back", true), ("Lats", false), ("Biceps", false) },
                new[] { "Cable Machine" }),

            new("Dumbbell Row",
                "Place one hand and one knee on a flat bench with the other foot on the floor. Hold a dumbbell in the free hand with arm extended. Pull the dumbbell to the hip by driving the elbow back and up. Lower under control. Keep the back flat and avoid rotating the torso.",
                "Compound", 3, ExerciseLevel.Beginner, MovementType.Pull, 5.0m, 3.5m,
                new[] { ("Lats", true), ("Upper Back", false), ("Biceps", false), ("Rear Delts", false) },
                new[] { "Dumbbells", "Bench" }),

            new("Face Pull",
                "Set a cable pulley to upper chest height with a rope attachment. Grip the rope with palms facing down and step back. Pull the rope toward the face by driving the elbows high and wide. Externally rotate the shoulders at the end so fists point toward the ceiling. Return to the starting position with control.",
                "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 3.0m,
                new[] { ("Rear Delts", true), ("Traps", false) },
                new[] { "Cable Machine" }),

            new("Barbell Curl",
                "Stand with feet shoulder width apart holding a barbell with an underhand grip at arm's length. Keeping the elbows pinned to the sides, curl the bar up to shoulder height. Lower the bar back down under control. Avoid swinging the body or using momentum. Keep the wrists neutral.",
                "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 3.0m,
                new[] { ("Biceps", true), ("Forearms", false) },
                new[] { "Barbell & Plates" }),

            new("Dumbbell Curl",
                "Stand with a dumbbell in each hand at arm's length with palms facing forward. Curl the dumbbells up to shoulder height while keeping the elbows stationary at the sides. Lower under control to full arm extension. Can be performed alternating or simultaneously.",
                "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 3.0m,
                new[] { ("Biceps", true), ("Forearms", false) },
                new[] { "Dumbbells" }),

            new("Hammer Curl",
                "Stand holding dumbbells at your sides with palms facing each other in a neutral grip. Curl the dumbbells up to shoulder height while maintaining the neutral wrist position. Lower under control. This grip variation targets the brachioradialis and brachialis in addition to the biceps.",
                "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 3.0m,
                new[] { ("Biceps", true), ("Forearms", true) },
                new[] { "Dumbbells" }),

            new("Chin-Up",
                "Hang from a bar with an underhand grip at shoulder width. Pull the body up until the chin clears the bar. Lower under control to a full dead hang. The supinated grip places greater emphasis on the biceps compared to a standard pull-up while still heavily engaging the back muscles.",
                "Compound", 5, ExerciseLevel.Intermediate, MovementType.Pull, 8.0m, 4.0m,
                new[] { ("Lats", true), ("Biceps", true), ("Upper Back", false) },
                new[] { "Pull-Up Bar" }),

            new("Barbell Back Squat",
                "Position a barbell across the upper traps and rear delts. Stand with feet shoulder width apart and toes turned slightly out. Bend at the hips and knees to lower until thighs are at least parallel to the floor. Drive through the full foot to stand back up. Keep the chest up and knees tracking over the toes.",
                "Compound", 7, ExerciseLevel.Intermediate, MovementType.Squat, 6.0m, 4.5m,
                new[] { ("Quadriceps", true), ("Glutes", true), ("Hamstrings", false), ("Lower Back", false), ("Abs", false) },
                new[] { "Barbell & Plates" }),

            new("Front Squat",
                "Rest a barbell across the front delts with elbows high and upper arms parallel to the floor. Stand with feet shoulder width apart. Squat down keeping the torso as upright as possible until thighs are parallel or below. Stand back up by driving through the full foot. The front-loaded position demands more quad engagement and core stability.",
                "Compound", 7, ExerciseLevel.Advanced, MovementType.Squat, 6.0m, 4.5m,
                new[] { ("Quadriceps", true), ("Glutes", false), ("Abs", false) },
                new[] { "Barbell & Plates" }),

            new("Goblet Squat",
                "Hold a dumbbell or kettlebell vertically against the chest with both hands cupping one end. Stand with feet slightly wider than shoulder width. Squat down between the legs keeping the chest up and elbows inside the knees. Stand back up. An excellent movement for learning squat mechanics.",
                "Compound", 3, ExerciseLevel.Beginner, MovementType.Squat, 5.0m, 4.0m,
                new[] { ("Quadriceps", true), ("Glutes", true) },
                new[] { "Dumbbells", "Kettlebells" }),

            new("Leg Press",
                "Sit in a leg press machine with feet shoulder width apart on the platform. Release the safety handles and lower the platform by bending the knees toward the chest. Press the platform away until legs are extended but not fully locked. Keep the lower back pressed into the pad throughout.",
                "Compound", 4, ExerciseLevel.Beginner, MovementType.Squat, 5.0m, 3.5m,
                new[] { ("Quadriceps", true), ("Glutes", false), ("Hamstrings", false) },
                new[] { "Leg Press" }),

            new("Bulgarian Split Squat",
                "Stand about two feet in front of a bench and place the top of one foot on the bench behind you. Hold dumbbells at your sides. Lower the back knee toward the floor by bending the front leg. Push through the front foot to return to standing. Keep the torso upright throughout. Builds single-leg strength and addresses imbalances.",
                "Compound", 5, ExerciseLevel.Intermediate, MovementType.Squat, 5.0m, 4.0m,
                new[] { ("Quadriceps", true), ("Glutes", true), ("Hamstrings", false) },
                new[] { "Dumbbells", "Bench" }),

            new("Leg Extension",
                "Sit in a leg extension machine with the pad resting on the front of the lower shins. Grip the handles and extend the legs until fully straight. Squeeze the quads at the top. Lower back to the starting position with control. Keep the back pressed firmly against the pad.",
                "Isolation", 2, ExerciseLevel.Beginner, MovementType.Squat, 3.5m, 3.0m,
                new[] { ("Quadriceps", true) },
                new[] { "Full Gym Access" }),

            new("Walking Lunge",
                "Stand upright holding dumbbells at your sides or a barbell across the upper back. Step forward into a long stride and lower the back knee toward the floor. Push off the front foot and step the back foot forward into the next lunge. Alternate legs with each step. Keep the torso upright throughout.",
                "Compound", 5, ExerciseLevel.Intermediate, MovementType.Squat, 6.0m, 3.5m,
                new[] { ("Quadriceps", true), ("Glutes", true), ("Hamstrings", false) },
                new[] { "Dumbbells" }),

            new("Conventional Deadlift",
                "Stand with feet hip width apart with the barbell over mid-foot. Hinge at the hips and grip the bar just outside the knees. With a flat back, drive through the floor to stand up, pulling the bar along the body. Lockout by squeezing the glutes at the top. Lower the bar by pushing the hips back. The king of posterior chain exercises.",
                "Compound", 8, ExerciseLevel.Intermediate, MovementType.Hinge, 6.0m, 5.0m,
                new[] { ("Hamstrings", true), ("Glutes", true), ("Lower Back", true), ("Traps", false), ("Forearms", false) },
                new[] { "Barbell & Plates" }),

            new("Romanian Deadlift",
                "Hold a barbell at hip height with an overhand grip. With a slight bend in the knees, push the hips back and lower the bar along the front of the legs. Go down until you feel a strong stretch in the hamstrings. Drive the hips forward to return to standing. Keep the bar close to the body and back flat throughout.",
                "Compound", 5, ExerciseLevel.Intermediate, MovementType.Hinge, 6.0m, 4.5m,
                new[] { ("Hamstrings", true), ("Glutes", true), ("Lower Back", false) },
                new[] { "Barbell & Plates" }),

            new("Hip Thrust",
                "Sit on the floor with the upper back against a bench and a loaded barbell across the hips. Plant the feet flat with knees bent at 90 degrees. Drive through the heels to lift the hips until the body forms a straight line from knees to shoulders. Squeeze the glutes hard at the top. Lower the hips back down with control.",
                "Compound", 4, ExerciseLevel.Intermediate, MovementType.Hinge, 5.0m, 3.5m,
                new[] { ("Glutes", true), ("Hamstrings", false) },
                new[] { "Barbell & Plates", "Bench" }),

            new("Kettlebell Swing",
                "Stand with feet slightly wider than shoulder width holding a kettlebell with both hands. Hinge at the hips to swing the kettlebell between the legs. Snap the hips forward explosively to swing the kettlebell to chest height. Let gravity pull it back and hinge again. The power comes from the hip drive, not the arms.",
                "Compound", 5, ExerciseLevel.Intermediate, MovementType.Hinge, 8.0m, null,
                new[] { ("Glutes", true), ("Hamstrings", true), ("Lower Back", false), ("Abs", false) },
                new[] { "Kettlebells" }),

            new("Good Morning",
                "Stand with a barbell across the upper back as you would for a squat. With a slight bend in the knees, push the hips back and hinge forward until the torso is nearly parallel to the floor. Drive the hips forward to return to standing. Keep the back flat and core braced. Use moderate weight as this heavily loads the posterior chain.",
                "Compound", 5, ExerciseLevel.Intermediate, MovementType.Hinge, 5.0m, 4.0m,
                new[] { ("Hamstrings", true), ("Lower Back", true), ("Glutes", false) },
                new[] { "Barbell & Plates" }),

            new("Leg Curl",
                "Lie face down on a leg curl machine with the pad resting against the back of the lower legs just above the ankles. Curl the legs up toward the glutes by bending the knees. Lower back to the starting position with control. Keep the hips pressed into the pad to avoid compensating.",
                "Isolation", 2, ExerciseLevel.Beginner, MovementType.Hinge, 3.5m, 3.0m,
                new[] { ("Hamstrings", true) },
                new[] { "Full Gym Access" }),

            new("Glute Bridge",
                "Lie face up on the floor with knees bent and feet flat. Drive through the heels to lift the hips off the floor until the body forms a straight line from knees to shoulders. Squeeze the glutes at the top. Lower the hips back to the floor. A foundational glute activation exercise suitable for all levels.",
                "Isolation", 1, ExerciseLevel.Beginner, MovementType.Hinge, 3.5m, 3.0m,
                new[] { ("Glutes", true), ("Hamstrings", false) },
                new[] { "Bodyweight Only" }),

            new("Plank",
                "Support the body on forearms and toes with the body in a straight line from head to heels. Keep the core braced, glutes squeezed, and hips level. Avoid letting the hips sag toward the floor or pike up. Breathe normally throughout. Hold for time rather than reps.",
                "Isometric", 3, ExerciseLevel.Beginner, MovementType.Isometric, 3.8m, null,
                new[] { ("Abs", true), ("Obliques", false), ("Lower Back", false) },
                new[] { "Bodyweight Only" }),

            new("Dead Bug",
                "Lie on the back with arms extended toward the ceiling and hips and knees bent at 90 degrees. Slowly extend one arm overhead and the opposite leg toward the floor simultaneously. Return to the starting position and repeat on the other side. Press the lower back into the floor throughout to maintain core engagement.",
                "Isolation", 2, ExerciseLevel.Beginner, MovementType.Isometric, 3.8m, 4.0m,
                new[] { ("Abs", true), ("Hip Flexors", false) },
                new[] { "Bodyweight Only" }),

            new("Russian Twist",
                "Sit on the floor with knees bent and feet slightly off the ground. Lean the torso back to roughly 45 degrees. Hold a weight or medicine ball with both hands. Rotate the torso side to side, touching the weight to the floor beside each hip. Keep the chest up and core engaged. Control the rotation, do not use momentum.",
                "Isolation", 3, ExerciseLevel.Beginner, MovementType.Rotation, 3.8m, 3.0m,
                new[] { ("Obliques", true), ("Abs", false) },
                new[] { "Bodyweight Only", "Medicine Ball" }),

            new("Hanging Leg Raise",
                "Hang from a pull-up bar with arms fully extended. Keeping the legs straight, raise them until they are parallel to the floor or higher. Lower them back down under control. Avoid swinging. To increase difficulty, raise the legs all the way to the bar. To decrease difficulty, bend the knees.",
                "Isolation", 5, ExerciseLevel.Intermediate, MovementType.Isometric, 3.8m, 4.0m,
                new[] { ("Abs", true), ("Hip Flexors", false), ("Obliques", false) },
                new[] { "Pull-Up Bar" }),

            new("Ab Wheel Rollout",
                "Kneel on the floor holding an ab wheel with both hands. Slowly roll the wheel forward extending the body as far as you can while keeping the core tight and back flat. Pull the wheel back toward the knees using the abdominals to return to the starting position. Avoid letting the lower back collapse.",
                "Compound", 6, ExerciseLevel.Intermediate, MovementType.Isometric, 5.0m, 4.0m,
                new[] { ("Abs", true), ("Obliques", false), ("Lower Back", false) },
                new[] { "Bodyweight Only" }),

            new("Side Plank",
                "Lie on one side supported by the forearm and the side of the bottom foot. Lift the hips off the floor until the body forms a straight line from head to feet. Hold the position while keeping the core engaged and hips stacked. Avoid letting the hips drop. Hold for time then switch sides.",
                "Isometric", 3, ExerciseLevel.Beginner, MovementType.Isometric, 3.8m, null,
                new[] { ("Obliques", true), ("Abs", false) },
                new[] { "Bodyweight Only" }),

            new("Cable Woodchop",
                "Set a cable pulley to the high position. Stand sideways to the machine and grip the handle with both hands. Pull the handle diagonally across the body from high to low while rotating the torso. Control the return to the starting position. Keep the arms relatively straight and let the rotation come from the core.",
                "Isolation", 3, ExerciseLevel.Intermediate, MovementType.Rotation, 3.8m, 3.5m,
                new[] { ("Obliques", true), ("Abs", false) },
                new[] { "Cable Machine" }),

            new("Crunches",
                "Lie face up with knees bent and feet flat on the floor. Place hands behind the head or across the chest. Curl the upper body toward the knees by contracting the abdominals. Lift only the shoulders and upper back off the floor. Lower back down with control. Avoid pulling on the neck.",
                "Isolation", 1, ExerciseLevel.Beginner, MovementType.Isometric, 3.8m, 2.5m,
                new[] { ("Abs", true) },
                new[] { "Bodyweight Only" }),

            new("Farmer's Walk",
                "Stand holding a heavy dumbbell or kettlebell in each hand at your sides. Walk forward with controlled steps keeping the torso upright, shoulders back, and core tight. Maintain a strong grip throughout. Walk for a set distance or time. Builds grip strength, core stability, and overall conditioning.",
                "Compound", 4, ExerciseLevel.Beginner, MovementType.Carry, 6.0m, null,
                new[] { ("Forearms", true), ("Traps", true), ("Abs", false) },
                new[] { "Dumbbells", "Kettlebells" }),

            new("Standing Calf Raise",
                "Stand on the edge of a step or calf raise machine with the balls of the feet on the platform and heels hanging off. Rise up onto the toes as high as possible, squeezing the calves at the top. Lower the heels below the platform for a full stretch. Use a slow controlled tempo for best results.",
                "Isolation", 1, ExerciseLevel.Beginner, MovementType.Squat, 3.5m, 2.5m,
                new[] { ("Calves", true) },
                new[] { "Bodyweight Only" }),

            new("Seated Calf Raise",
                "Sit at a seated calf raise machine with the pads resting on the lower thighs. Place the balls of the feet on the footplate. Press up through the toes lifting the weight. Lower the heels for a full stretch. The bent knee position targets the soleus muscle of the calf more than the standing variation.",
                "Isolation", 1, ExerciseLevel.Beginner, MovementType.Squat, 3.5m, 2.5m,
                new[] { ("Calves", true) },
                new[] { "Full Gym Access" }),

            new("Treadmill Running",
                "Run on a treadmill at a steady pace. Maintain an upright posture with a slight forward lean. Land with feet under the hips and avoid overstriding. Swing the arms naturally. Adjust speed and incline based on fitness level and training goals. A versatile cardiovascular exercise suitable for all levels.",
                "Cardio", 3, ExerciseLevel.Beginner, MovementType.Cardio, 9.8m, null,
                new[] { ("Quadriceps", false), ("Hamstrings", false), ("Calves", false), ("Glutes", false) },
                new[] { "Treadmill" }),

            new("Stationary Cycling",
                "Sit on a stationary bike with the seat adjusted so there is a slight bend in the knee at the bottom of the pedal stroke. Grip the handlebars lightly. Pedal at a steady cadence adjusting resistance as needed. Keep the core engaged and avoid rocking side to side. Effective low-impact cardio for all fitness levels.",
                "Cardio", 2, ExerciseLevel.Beginner, MovementType.Cardio, 8.0m, null,
                new[] { ("Quadriceps", false), ("Hamstrings", false), ("Calves", false) },
                new[] { "Stationary Bike" }),

            new("Rowing Machine",
                "Sit on the rower with feet strapped in and knees bent. Grip the handle with an overhand grip. Drive with the legs first, then lean back slightly, then pull the handle to the lower chest. Return by extending the arms, leaning forward, then bending the knees. Maintain a fluid sequence throughout each stroke.",
                "Cardio", 3, ExerciseLevel.Beginner, MovementType.Cardio, 7.0m, null,
                new[] { ("Upper Back", false), ("Lats", false), ("Quadriceps", false), ("Hamstrings", false) },
                new[] { "Rowing Machine" }),

            new("Jump Rope",
                "Stand holding a jump rope with handles at hip height. Swing the rope overhead and jump with both feet just high enough to clear it. Land softly on the balls of the feet. Keep the elbows close to the body and turn the rope primarily with the wrists. Maintain a steady rhythm and upright posture.",
                "Cardio", 4, ExerciseLevel.Intermediate, MovementType.Cardio, 12.3m, null,
                new[] { ("Calves", false), ("Quadriceps", false) },
                new[] { "Bodyweight Only" }),

            new("Stair Climber",
                "Step onto a stair climber machine and grip the side rails lightly for balance. Step at a steady pace pushing through the full foot on each step. Maintain an upright posture and avoid leaning heavily on the rails. Adjust the speed based on fitness level. Excellent for building lower body endurance and cardiovascular fitness.",
                "Cardio", 4, ExerciseLevel.Beginner, MovementType.Cardio, 9.0m, null,
                new[] { ("Quadriceps", false), ("Glutes", false), ("Calves", false) },
                new[] { "Full Gym Access" }),

            new("Chest Supported Row",
                "Lie face down on an incline bench set to 30-45 degrees. Hold a dumbbell in each hand with arms hanging straight down. Pull the dumbbells up to the sides of the chest by squeezing the shoulder blades together. Lower under control. The bench support eliminates momentum and lower back strain.",
                "Compound", 3, ExerciseLevel.Beginner, MovementType.Pull, 5.0m, 3.5m,
                new[] { ("Upper Back", true), ("Lats", false), ("Rear Delts", false), ("Biceps", false) },
                new[] { "Dumbbells", "Bench" }),

            new("Reverse Flye",
                "Bend forward at the hips until the torso is nearly parallel to the floor. Hold dumbbells with arms hanging down and palms facing each other. Raise the dumbbells out to the sides by squeezing the shoulder blades together. Lower under control. Keep a slight bend in the elbows throughout.",
                "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 3.0m,
                new[] { ("Rear Delts", true), ("Upper Back", false) },
                new[] { "Dumbbells" }),

            new("Incline Barbell Bench Press",
                "Set a bench to 30-45 degrees. Lie back and grip the barbell slightly wider than shoulder width. Unrack and lower the bar to the upper chest. Press the bar back up to lockout. Keep shoulder blades retracted and feet flat. The incline shifts emphasis to the upper chest and front delts.",
                "Compound", 5, ExerciseLevel.Intermediate, MovementType.Push, 5.0m, 4.0m,
                new[] { ("Upper Chest", true), ("Front Delts", false), ("Triceps", false) },
                new[] { "Barbell & Plates", "Bench" }),

            new("Close Grip Bench Press",
                "Lie on a flat bench and grip the barbell at shoulder width or slightly narrower. Lower the bar to the lower chest keeping elbows close to the body. Press the bar back up to lockout. The narrow grip shifts emphasis from the chest to the triceps while still allowing heavy loads.",
                "Compound", 5, ExerciseLevel.Intermediate, MovementType.Push, 5.0m, 4.0m,
                new[] { ("Triceps", true), ("Chest", false), ("Front Delts", false) },
                new[] { "Barbell & Plates", "Bench" }),

            new("Sumo Deadlift",
                "Stand with a wide stance and toes pointed out. Grip the barbell with hands inside the knees using a shoulder-width grip. With a flat back, drive through the floor to stand up. The wide stance shortens the range of motion and shifts emphasis to the quads and glutes compared to conventional.",
                "Compound", 8, ExerciseLevel.Advanced, MovementType.Hinge, 6.0m, 5.0m,
                new[] { ("Glutes", true), ("Quadriceps", true), ("Hamstrings", false), ("Lower Back", false) },
                new[] { "Barbell & Plates" }),

            new("Hack Squat",
                "Stand in a hack squat machine with shoulders under the pads and feet shoulder width apart on the platform. Release the safety handles and lower by bending the knees until thighs are parallel. Press back up to the starting position. Keeps the torso fixed allowing you to focus entirely on the legs.",
                "Compound", 4, ExerciseLevel.Intermediate, MovementType.Squat, 5.0m, 4.0m,
                new[] { ("Quadriceps", true), ("Glutes", false) },
                new[] { "Full Gym Access" }),

            new("Preacher Curl",
                "Sit at a preacher curl bench with the upper arms resting on the pad. Hold a barbell or dumbbells with an underhand grip. Curl the weight up toward the shoulders. Lower under control to full extension. The pad prevents momentum and isolates the biceps through the full range of motion.",
                "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 3.5m,
                new[] { ("Biceps", true) },
                new[] { "Dumbbells", "Bench" }),

            new("Wrist Curl",
                "Sit on a bench with forearms resting on the thighs and wrists hanging over the knees. Hold a barbell or dumbbells with an underhand grip. Curl the wrists upward squeezing the forearms. Lower back down with control. Builds forearm size and grip endurance.",
                "Isolation", 1, ExerciseLevel.Beginner, MovementType.Pull, 2.5m, 2.5m,
                new[] { ("Forearms", true) },
                new[] { "Dumbbells" }),

            new("Barbell Shrug",
                "Stand holding a barbell in front of the thighs with an overhand grip at shoulder width. Shrug the shoulders straight up toward the ears as high as possible. Hold the contraction briefly at the top. Lower back down with control. Avoid rolling the shoulders forward or backward.",
                "Isolation", 2, ExerciseLevel.Beginner, MovementType.Pull, 3.5m, 2.5m,
                new[] { ("Traps", true) },
                new[] { "Barbell & Plates" }),

            new("Dumbbell Pullover",
                "Lie on a flat bench holding a single dumbbell with both hands above the chest with arms slightly bent. Lower the dumbbell behind the head in an arc until the arms are in line with the torso. Pull the dumbbell back over the chest using the lats and chest. Keep the core braced throughout.",
                "Compound", 3, ExerciseLevel.Intermediate, MovementType.Pull, 4.0m, 4.0m,
                new[] { ("Lats", true), ("Chest", false) },
                new[] { "Dumbbells", "Bench" })
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
            new() { Name = "Strawberries", CaloriesPer100g = 32, ProteinPer100g = 0.7m, CarbsPer100g = 7.7m, FatPer100g = 0.3m },
            new() { Name = "Broccoli", CaloriesPer100g = 34, ProteinPer100g = 2.8m, CarbsPer100g = 6.6m, FatPer100g = 0.4m },
            new() { Name = "Spinach (raw)", CaloriesPer100g = 23, ProteinPer100g = 2.9m, CarbsPer100g = 3.6m, FatPer100g = 0.4m },
            new() { Name = "Avocado", CaloriesPer100g = 160, ProteinPer100g = 2.0m, CarbsPer100g = 8.5m, FatPer100g = 14.7m },
            new() { Name = "Olive Oil", CaloriesPer100g = 884, ProteinPer100g = 0, CarbsPer100g = 0, FatPer100g = 100.0m },
            new() { Name = "Peanut Butter", CaloriesPer100g = 588, ProteinPer100g = 25.1m, CarbsPer100g = 20.0m, FatPer100g = 50.4m },
            new() { Name = "Almonds", CaloriesPer100g = 579, ProteinPer100g = 21.2m, CarbsPer100g = 21.6m, FatPer100g = 49.9m },
            new() { Name = "Walnuts", CaloriesPer100g = 654, ProteinPer100g = 15.2m, CarbsPer100g = 13.7m, FatPer100g = 65.2m },
            new() { Name = "Cheddar Cheese", CaloriesPer100g = 403, ProteinPer100g = 24.9m, CarbsPer100g = 1.3m, FatPer100g = 33.1m },
            new() { Name = "Whole Milk", CaloriesPer100g = 61, ProteinPer100g = 3.2m, CarbsPer100g = 4.8m, FatPer100g = 3.3m },
            new() { Name = "Skimmed Milk", CaloriesPer100g = 34, ProteinPer100g = 3.4m, CarbsPer100g = 5.0m, FatPer100g = 0.1m },
            new() { Name = "Tofu (firm)", CaloriesPer100g = 144, ProteinPer100g = 17.3m, CarbsPer100g = 2.8m, FatPer100g = 8.7m },
            new() { Name = "Red Lentils (uncooked)", CaloriesPer100g = 358, ProteinPer100g = 24.6m, CarbsPer100g = 60.1m, FatPer100g = 1.1m },
            new() { Name = "Chickpeas (canned)", CaloriesPer100g = 139, ProteinPer100g = 7.0m, CarbsPer100g = 22.5m, FatPer100g = 2.6m },
            new() { Name = "Quinoa (uncooked)", CaloriesPer100g = 368, ProteinPer100g = 14.1m, CarbsPer100g = 64.2m, FatPer100g = 6.1m },
            new() { Name = "Honey", CaloriesPer100g = 304, ProteinPer100g = 0.3m, CarbsPer100g = 82.4m, FatPer100g = 0 },
            new() { Name = "Dark Chocolate (70%)", CaloriesPer100g = 598, ProteinPer100g = 7.8m, CarbsPer100g = 45.9m, FatPer100g = 42.6m },
            new() { Name = "White Fish (cod, raw)", CaloriesPer100g = 82, ProteinPer100g = 17.8m, CarbsPer100g = 0, FatPer100g = 0.7m },
            new() { Name = "Prawns (raw)", CaloriesPer100g = 99, ProteinPer100g = 20.1m, CarbsPer100g = 0.9m, FatPer100g = 1.7m },
            new() { Name = "Turkey Breast (raw)", CaloriesPer100g = 104, ProteinPer100g = 23.7m, CarbsPer100g = 0, FatPer100g = 0.7m },
            new() { Name = "Basmati Rice (uncooked)", CaloriesPer100g = 360, ProteinPer100g = 7.0m, CarbsPer100g = 78.0m, FatPer100g = 0.6m },
            new() { Name = "Couscous (uncooked)", CaloriesPer100g = 376, ProteinPer100g = 12.8m, CarbsPer100g = 77.4m, FatPer100g = 0.6m },
            new() { Name = "Mixed Vegetables (frozen)", CaloriesPer100g = 65, ProteinPer100g = 3.3m, CarbsPer100g = 13.1m, FatPer100g = 0.3m }
        };
    }

    private record ExerciseSeed(
        string Name,
        string Description,
        string ExerciseType,
        int Difficulty,
        ExerciseLevel Level,
        MovementType Movement,
        decimal Met,
        decimal? RepTime,
        (string Name, bool IsPrimary)[] Muscles,
        string[] Equipment
    );
}
