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
