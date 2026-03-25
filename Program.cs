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
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

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
