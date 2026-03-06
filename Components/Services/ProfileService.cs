using Microsoft.EntityFrameworkCore;
using FitnessApp.Data;
using FitnessApp.Models;
using FitnessApp.Models.Enums;

namespace FitnessApp.Services;

public class ProfileService
{
    private readonly AppDbContext _db;

    public ProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetUserWithProfileAsync(int userId)
    {
        return await _db.Users
            .Include(u => u.UserEquipment)
                .ThenInclude(ue => ue.Equipment)
            .Include(u => u.Injuries)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<List<Equipment>> GetAllEquipmentAsync()
    {
        return await _db.Equipment.OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<bool> SaveOnboardingAsync(int userId, ProfileData data)
    {
        var user = await _db.Users
            .Include(u => u.UserEquipment)
            .Include(u => u.Injuries)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return false;

        ApplyProfileData(user, data);
        user.OnboardingCompleted = true;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateProfileAsync(int userId, ProfileData data)
    {
        var user = await _db.Users
            .Include(u => u.UserEquipment)
            .Include(u => u.Injuries)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return false;

        ApplyProfileData(user, data);
        await _db.SaveChangesAsync();
        return true;
    }

    private void ApplyProfileData(User user, ProfileData data)
    {
        user.FullName = data.FullName;
        user.DateOfBirth = data.DateOfBirth;
        user.WeightKg = data.WeightKg;
        user.HeightCm = data.HeightCm;
        user.FitnessLevel = data.FitnessLevel;
        user.PrimaryGoal = data.PrimaryGoal;
        user.TrainingDaysPerWeek = data.TrainingDaysPerWeek;
        user.Bio = data.Bio;

        user.UserEquipment.Clear();
        foreach (var eqId in data.SelectedEquipmentIds)
            user.UserEquipment.Add(new UserEquipment { UserId = user.Id, EquipmentId = eqId });

        var existingInjuries = user.Injuries.ToList();
        foreach (var injury in existingInjuries)
            _db.UserInjuries.Remove(injury);

        foreach (var injury in data.Injuries.Where(i => !string.IsNullOrWhiteSpace(i.Description)))
        {
            user.Injuries.Add(new UserInjury
            {
                UserId = user.Id,
                Description = injury.Description.Trim(),
                AffectedArea = string.IsNullOrWhiteSpace(injury.AffectedArea) ? null : injury.AffectedArea.Trim(),
                IsActive = injury.IsActive
            });
        }
    }
}

public class ProfileData
{
    public string FullName { get; set; } = "";
    public DateOnly? DateOfBirth { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public FitnessLevel? FitnessLevel { get; set; }
    public PrimaryGoal? PrimaryGoal { get; set; }
    public int? TrainingDaysPerWeek { get; set; }
    public string? Bio { get; set; }
    public List<int> SelectedEquipmentIds { get; set; } = new();
    public List<InjuryEntry> Injuries { get; set; } = new();
}

public class InjuryEntry
{
    public string Description { get; set; } = "";
    public string? AffectedArea { get; set; }
    public bool IsActive { get; set; } = true;
}
