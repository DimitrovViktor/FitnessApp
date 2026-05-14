using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;

namespace FitnessApp.Services;

public class SettingsService
{
    private readonly AppDbContext _db;
    private const decimal PoundsPerKilogram = 2.20462262185m;
    private const decimal MilesPerKilometer = 0.621371192237m;
    private const decimal KilojoulesPerKilocalorie = 4.184m;

    public event Action<string>? ThemeChanged;

    public SettingsService(AppDbContext db)
    {
        _db = db;
    }

    public static IReadOnlyList<ThemeOption> Themes { get; } = new[]
    {
        new ThemeOption("night", "Night", false),
        new ThemeOption("midnight", "Midnight", false),
        new ThemeOption("day", "Day", true),
        new ThemeOption("lavender", "Lavender", true)
    };

    public async Task<UserSettings> GetSettingsAsync(int userId)
    {
        var settings = await _db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
        if (settings is not null)
        {
            Normalize(settings);
            return settings;
        }

        settings = new UserSettings { UserId = userId };
        _db.UserSettings.Add(settings);
        await _db.SaveChangesAsync();
        return settings;
    }

    public async Task SaveSettingsAsync(UserSettings settings)
    {
        Normalize(settings);
        var existing = await _db.UserSettings.FirstOrDefaultAsync(s => s.UserId == settings.UserId);
        if (existing is null)
        {
            _db.UserSettings.Add(settings);
            await _db.SaveChangesAsync();
            ThemeChanged?.Invoke(settings.Theme);
            return;
        }

        existing.Theme = settings.Theme;
        existing.WeightUnit = settings.WeightUnit;
        existing.DistanceUnit = settings.DistanceUnit;
        existing.EnergyUnit = settings.EnergyUnit;
        existing.CalendarStart = settings.CalendarStart;
        existing.RestTimerDefault = settings.RestTimerDefault;
        existing.RestTimerSound = settings.RestTimerSound;
        existing.AutoStartRestTimer = settings.AutoStartRestTimer;
        existing.ShowWeightInSets = settings.ShowWeightInSets;
        existing.ConfirmBeforeSkip = settings.ConfirmBeforeSkip;
        existing.WorkoutReminders = settings.WorkoutReminders;
        existing.SocialVisibility = settings.SocialVisibility;
        await _db.SaveChangesAsync();
        ThemeChanged?.Invoke(existing.Theme);
    }

    public async Task<string> GetThemeAsync(int userId)
    {
        var settings = await GetSettingsAsync(userId);
        return settings.Theme;
    }

    public async Task SetThemeAsync(int userId, string theme)
    {
        var settings = await GetSettingsAsync(userId);
        settings.Theme = NormalizeTheme(theme);
        await _db.SaveChangesAsync();
        ThemeChanged?.Invoke(settings.Theme);
    }

    public static string GetThemeLabel(string theme) => Themes.FirstOrDefault(t => t.Key == theme)?.Label ?? "Night";

    public static string NormalizeTheme(string? value)
    {
        if (value == "sunrise") return "lavender";
        return Themes.Any(t => t.Key == value) ? value! : "night";
    }

    public static string NormalizeWeightUnit(string? value) => value == "lbs" ? "lbs" : "kg";

    public static string NormalizeDistanceUnit(string? value) => value == "mi" ? "mi" : "km";

    public static string NormalizeEnergyUnit(string? value) => value is "cal" or "kj" ? value : "kcal";

    public static string WeightLabel(string weightUnit) => NormalizeWeightUnit(weightUnit) == "lbs" ? "lbs" : "kg";

    public static string DistanceLabel(string distanceUnit) => NormalizeDistanceUnit(distanceUnit) == "mi" ? "mi" : "km";

    public static string EnergyLabel(string energyUnit) => NormalizeEnergyUnit(energyUnit) switch
    {
        "cal" => "Cal",
        "kj" => "kJ",
        _ => "kcal"
    };

    public static decimal? FromKg(decimal? value, string weightUnit)
    {
        if (value is null) return null;
        return NormalizeWeightUnit(weightUnit) == "lbs" ? Math.Round(value.Value * PoundsPerKilogram, 1) : value;
    }

    public static decimal? ToKg(decimal? value, string weightUnit)
    {
        if (value is null) return null;
        return NormalizeWeightUnit(weightUnit) == "lbs" ? Math.Round(value.Value / PoundsPerKilogram, 2) : value;
    }

    public static decimal FromKm(decimal value, string distanceUnit) => NormalizeDistanceUnit(distanceUnit) == "mi" ? Math.Round(value * MilesPerKilometer, 2) : value;

    public static decimal ToKm(decimal value, string distanceUnit) => NormalizeDistanceUnit(distanceUnit) == "mi" ? Math.Round(value / MilesPerKilometer, 2) : value;

    public static decimal FromKcal(decimal value, string energyUnit) => NormalizeEnergyUnit(energyUnit) switch
    {
        "kj" => Math.Round(value * KilojoulesPerKilocalorie, 0),
        _ => Math.Round(value, 0)
    };

    public static decimal ToKcal(decimal value, string energyUnit) => NormalizeEnergyUnit(energyUnit) switch
    {
        "kj" => Math.Round(value / KilojoulesPerKilocalorie, 1),
        _ => value
    };

    public static string FormatWeight(decimal? valueKg, string weightUnit, string empty = "—")
    {
        var display = FromKg(valueKg, weightUnit);
        return display.HasValue ? $"{display.Value:0.#} {WeightLabel(weightUnit)}" : empty;
    }

    public static string FormatWeight(decimal valueKg, string weightUnit) => $"{FromKg(valueKg, weightUnit) ?? valueKg:0.#} {WeightLabel(weightUnit)}";

    public static string FormatEnergy(decimal valueKcal, string energyUnit) => $"{FromKcal(valueKcal, energyUnit):0} {EnergyLabel(energyUnit)}";

    private static void Normalize(UserSettings settings)
    {
        settings.Theme = NormalizeTheme(settings.Theme);
        settings.WeightUnit = NormalizeWeightUnit(settings.WeightUnit);
        settings.DistanceUnit = NormalizeDistanceUnit(settings.DistanceUnit);
        settings.EnergyUnit = NormalizeEnergyUnit(settings.EnergyUnit);
        settings.CalendarStart = settings.CalendarStart == "sunday" ? "sunday" : "monday";
        settings.SocialVisibility = settings.SocialVisibility is "friends" or "private" ? settings.SocialVisibility : "public";
        settings.RestTimerDefault = settings.RestTimerDefault is 30 or 45 or 60 or 90 or 120 ? settings.RestTimerDefault : 60;
    }
}

public record ThemeOption(string Key, string Label, bool IsLight);
