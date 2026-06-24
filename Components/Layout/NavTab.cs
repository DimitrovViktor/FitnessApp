using Microsoft.AspNetCore.Components;

namespace FitnessApp.Components.Layout;

public sealed record NavTab(string Path, string Label);

public static class HubNav
{
    public static string CurrentPath(NavigationManager navigation)
    {
        var relative = navigation.ToBaseRelativePath(navigation.Uri);
        var cut = relative.IndexOfAny(new[] { '?', '#' });
        if (cut >= 0) relative = relative[..cut];
        relative = relative.Trim('/').ToLowerInvariant();
        return relative.Length == 0 ? "dashboard" : relative;
    }

    public static bool IsInGroup(NavigationManager navigation, params string[] paths)
    {
        var current = CurrentPath(navigation);
        foreach (var path in paths)
        {
            if (string.Equals(current, path, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
