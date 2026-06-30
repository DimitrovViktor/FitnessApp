namespace FitnessApp.Services;

public class ProfilePresenceState
{
    public event Action? OnChange;

    public void NotifyChanged() => OnChange?.Invoke();
}

public static class PresenceStatus
{
    public const string Online = "online";
    public const string Away = "away";
    public const string Dnd = "dnd";
    public const string Invisible = "invisible";

    public static readonly (string Key, string Label)[] Options = new[]
    {
        (Online, "Online"),
        (Away, "Away"),
        (Dnd, "Do Not Disturb"),
        (Invisible, "Invisible")
    };

    public static string Normalize(string? status) =>
        status is Online or Away or Dnd or Invisible ? status! : Online;

    public static string Label(string? status) => status switch
    {
        Away => "Away",
        Dnd => "Do Not Disturb",
        Invisible => "Invisible",
        _ => "Online"
    };
}
