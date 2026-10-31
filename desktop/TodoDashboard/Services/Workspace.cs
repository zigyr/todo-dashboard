using System.IO;

namespace TodoDashboard.Services;

public static class Workspace
{
    public static string Resolve()
    {
        var custom = Environment.GetEnvironmentVariable("TODO_DASHBOARD_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(custom))
        {
            return custom!;
        }

#if DEBUG
        var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
        if (repoRoot is not null)
        {
            return Path.Combine(repoRoot, "data");
        }
#endif

        var portable = Path.Combine(AppContext.BaseDirectory, "data");
        if (TryPrepare(portable))
        {
            return portable;
        }

        var profile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "TodoDashboard");
        if (TryPrepare(profile))
        {
            return profile;
        }

        var local = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TodoDashboard");
        if (TryPrepare(local))
        {
            return local;
        }

        return portable;
    }

    public static void Ensure(string root)
    {
        foreach (var folder in new[] { "sources", "reviews", "exports", "quarantine" })
        {
            Directory.CreateDirectory(Path.Combine(root, folder));
        }
    }

    private static bool TryPrepare(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindRepoRoot(string start)
    {
        var directory = new DirectoryInfo(start);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        return null;
    }
}
