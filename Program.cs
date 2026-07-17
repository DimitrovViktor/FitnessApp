using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using FitnessApp.Components;
using FitnessApp.Data;
using FitnessApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=fitnessapp.db"));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<WorkoutService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<LoggingService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<DietService>();
builder.Services.AddScoped<ProfilePresenceState>();
builder.Services.AddScoped<FriendService>();
builder.Services.AddScoped<ActivityShareService>();
builder.Services.AddScoped<FeedService>();
builder.Services.AddScoped<DirectMessageService>();
builder.Services.AddSingleton<ChatBroker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE WorkoutSchedules ADD COLUMN LiveSessionJson TEXT NULL");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE WorkoutExercises ADD COLUMN SupersetGroup INTEGER NULL");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE WorkoutExercises ADD COLUMN SupersetOrder INTEGER NOT NULL DEFAULT 0");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE WorkoutExercises ADD COLUMN SupersetRestAfterSec INTEGER NULL");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE WorkoutExercises ADD COLUMN SupersetRounds INTEGER NOT NULL DEFAULT 1");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE WorkoutLogs ADD COLUMN Status INTEGER NOT NULL DEFAULT 0");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Programs ADD COLUMN Notes TEXT NULL");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Programs ADD COLUMN Tags TEXT NULL");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Foods ADD COLUMN DietCategory TEXT NOT NULL DEFAULT 'maintenance'");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Foods ADD COLUMN ServingUnit TEXT NOT NULL DEFAULT 'serving'");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Foods ADD COLUMN ServingGrams NUMERIC NOT NULL DEFAULT 100");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Foods ADD COLUMN IsCustom INTEGER NOT NULL DEFAULT 0");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Foods ADD COLUMN CreatedByUserId INTEGER NULL");
    }
    catch { }



    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Foods ADD COLUMN FoodGroup TEXT NOT NULL DEFAULT 'Other'");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE FoodLogs ADD COLUMN MealTime TEXT NULL");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE FoodLogs ADD COLUMN MealName TEXT NOT NULL DEFAULT 'Meal'");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE FoodLogs ADD COLUMN DietScheduleId INTEGER NULL");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS DietPlans (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Description TEXT NULL,
            DietCategory TEXT NOT NULL DEFAULT 'maintenance',
            TargetLevel TEXT NULL,
            TargetGoal TEXT NULL,
            DurationWeeks INTEGER NOT NULL DEFAULT 4,
            MealsPerDay INTEGER NOT NULL DEFAULT 3,
            DailyCaloriesTarget NUMERIC NULL,
            DailyProteinTarget NUMERIC NULL,
            IsPreBuilt INTEGER NOT NULL DEFAULT 0,
            CreatedByUserId INTEGER NULL,
            Notes TEXT NULL,
            Tags TEXT NULL,
            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id))");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS DietPlanFoods (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            DietPlanId INTEGER NOT NULL,
            FoodId INTEGER NOT NULL,
            DayNumber INTEGER NOT NULL DEFAULT 1,
            MealName TEXT NOT NULL DEFAULT 'Meal',
            QuantityGrams NUMERIC NOT NULL DEFAULT 100,
            SortOrder INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (DietPlanId) REFERENCES DietPlans(Id) ON DELETE CASCADE,
            FOREIGN KEY (FoodId) REFERENCES Foods(Id) ON DELETE CASCADE)");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS DietSchedules (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            FoodId INTEGER NOT NULL,
            DietPlanId INTEGER NULL,
            ScheduledDate TEXT NOT NULL,
            ScheduledTime TEXT NULL,
            MealName TEXT NOT NULL DEFAULT 'Meal',
            QuantityGrams NUMERIC NOT NULL DEFAULT 100,
            Status TEXT NOT NULL DEFAULT 'planned',
            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (UserId) REFERENCES Users(Id),
            FOREIGN KEY (FoodId) REFERENCES Foods(Id),
            FOREIGN KEY (DietPlanId) REFERENCES DietPlans(Id))");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS UserSettings (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL UNIQUE,
            Theme TEXT NOT NULL DEFAULT 'night',
            WeightUnit TEXT NOT NULL DEFAULT 'kg',
            DistanceUnit TEXT NOT NULL DEFAULT 'km',
            EnergyUnit TEXT NOT NULL DEFAULT 'kcal',
            CalendarStart TEXT NOT NULL DEFAULT 'monday',
            RestTimerDefault INTEGER NOT NULL DEFAULT 60,
            RestTimerSound INTEGER NOT NULL DEFAULT 1,
            AutoStartRestTimer INTEGER NOT NULL DEFAULT 1,
            ShowWeightInSets INTEGER NOT NULL DEFAULT 1,
            ConfirmBeforeSkip INTEGER NOT NULL DEFAULT 1,
            WorkoutReminders INTEGER NOT NULL DEFAULT 1,
            SocialVisibility TEXT NOT NULL DEFAULT 'public',
            FOREIGN KEY (UserId) REFERENCES Users(Id))");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN AvatarData TEXT NULL");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN Status TEXT NOT NULL DEFAULT 'online'");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ProfileSettings (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL UNIQUE,
            NameVisibility TEXT NOT NULL DEFAULT 'notset',
            BioVisibility TEXT NOT NULL DEFAULT 'notset',
            LevelVisibility TEXT NOT NULL DEFAULT 'notset',
            GoalVisibility TEXT NOT NULL DEFAULT 'notset',
            TrainingDaysVisibility TEXT NOT NULL DEFAULT 'notset',
            WeightVisibility TEXT NOT NULL DEFAULT 'notset',
            HeightVisibility TEXT NOT NULL DEFAULT 'notset',
            AgeVisibility TEXT NOT NULL DEFAULT 'notset',
            MemberSinceVisibility TEXT NOT NULL DEFAULT 'notset',
            WorkoutsVisibility TEXT NOT NULL DEFAULT 'notset',
            FOREIGN KEY (UserId) REFERENCES Users(Id))");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS Conversations (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            User1Id INTEGER NOT NULL,
            User2Id INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            LastMessageAt TEXT NOT NULL,
            FOREIGN KEY (User1Id) REFERENCES Users(Id),
            FOREIGN KEY (User2Id) REFERENCES Users(Id))");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Conversations_User1Id_User2Id ON Conversations (User1Id, User2Id)");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS DirectMessages (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ConversationId INTEGER NOT NULL,
            SenderId INTEGER NOT NULL,
            Content TEXT NULL,
            AttachmentData TEXT NULL,
            AttachmentName TEXT NULL,
            AttachmentType TEXT NULL,
            IsImage INTEGER NOT NULL DEFAULT 0,
            AttachmentSize INTEGER NOT NULL DEFAULT 0,
            IsRead INTEGER NOT NULL DEFAULT 0,
            IsEdited INTEGER NOT NULL DEFAULT 0,
            EditedAt TEXT NULL,
            IsDeleted INTEGER NOT NULL DEFAULT 0,
            DeletedAt TEXT NULL,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY (ConversationId) REFERENCES Conversations(Id),
            FOREIGN KEY (SenderId) REFERENCES Users(Id))");
    }
    catch { }

    try { db.Database.ExecuteSqlRaw("ALTER TABLE DirectMessages ADD COLUMN IsEdited INTEGER NOT NULL DEFAULT 0"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE DirectMessages ADD COLUMN EditedAt TEXT NULL"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE DirectMessages ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE DirectMessages ADD COLUMN DeletedAt TEXT NULL"); } catch { }

    try
    {
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_DirectMessages_ConversationId_CreatedAt ON DirectMessages (ConversationId, CreatedAt)");
    }
    catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS Friendships (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RequesterId INTEGER NOT NULL,
            AddresseeId INTEGER NOT NULL,
            Status INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL,
            RespondedAt TEXT NULL,
            FOREIGN KEY (RequesterId) REFERENCES Users(Id),
            FOREIGN KEY (AddresseeId) REFERENCES Users(Id))");
    }
    catch { }

    try { db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Friendships_RequesterId_AddresseeId ON Friendships (RequesterId, AddresseeId)"); } catch { }
    try { db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Friendships_AddresseeId ON Friendships (AddresseeId)"); } catch { }

    try { db.Database.ExecuteSqlRaw("ALTER TABLE DirectMessages ADD COLUMN SharedWorkoutId INTEGER NULL"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE DirectMessages ADD COLUMN SharedProgramId INTEGER NULL"); } catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS Posts (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            AuthorId INTEGER NOT NULL,
            Content TEXT NULL,
            ImageData TEXT NULL,
            SharedWorkoutId INTEGER NULL,
            SharedProgramId INTEGER NULL,
            IsEdited INTEGER NOT NULL DEFAULT 0,
            EditedAt TEXT NULL,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY (AuthorId) REFERENCES Users(Id))");
    }
    catch { }
    try { db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Posts_CreatedAt ON Posts (CreatedAt)"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Posts ADD COLUMN SharedWorkoutId INTEGER NULL"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Posts ADD COLUMN SharedProgramId INTEGER NULL"); } catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS PostReactions (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            PostId INTEGER NOT NULL,
            UserId INTEGER NOT NULL,
            IsLike INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (PostId) REFERENCES Posts(Id))");
    }
    catch { }
    try { db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_PostReactions_PostId_UserId ON PostReactions (PostId, UserId)"); } catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS PostShares (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            PostId INTEGER NOT NULL,
            UserId INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY (PostId) REFERENCES Posts(Id))");
    }
    catch { }
    try { db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_PostShares_PostId_UserId ON PostShares (PostId, UserId)"); } catch { }
    try { db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PostShares_UserId ON PostShares (UserId)"); } catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS Comments (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            PostId INTEGER NOT NULL,
            ParentCommentId INTEGER NULL,
            AuthorId INTEGER NOT NULL,
            Content TEXT NOT NULL,
            IsEdited INTEGER NOT NULL DEFAULT 0,
            EditedAt TEXT NULL,
            CreatedAt TEXT NOT NULL,
            FOREIGN KEY (PostId) REFERENCES Posts(Id),
            FOREIGN KEY (AuthorId) REFERENCES Users(Id))");
    }
    catch { }
    try { db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Comments_PostId_CreatedAt ON Comments (PostId, CreatedAt)"); } catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS CommentReactions (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CommentId INTEGER NOT NULL,
            UserId INTEGER NOT NULL,
            IsLike INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (CommentId) REFERENCES Comments(Id))");
    }
    catch { }
    try { db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_CommentReactions_CommentId_UserId ON CommentReactions (CommentId, UserId)"); } catch { }

    var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
    var adminUser = db.Users.FirstOrDefault(u => u.Username == "admin");
    if (adminUser is null)
    {
        var (success, _) = await auth.RegisterAsync("Admin", "admin", "admin@example.com", "dev123");
        adminUser = db.Users.FirstOrDefault(u => u.Username == "admin");
        if (adminUser is not null)
        {
            adminUser.IsAdmin = true;
            await db.SaveChangesAsync();
        }
    }

    await DbSeeder.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
