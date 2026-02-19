namespace Portless.Core.Services;

public static class StateDirectoryProvider
{
    public static string GetStateDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "portless");
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".portless");
        }
    }

    public static string GetRoutesFilePath()
    {
        var stateDir = GetStateDirectory();
        Directory.CreateDirectory(stateDir); // Ensure exists
        return Path.Combine(stateDir, "routes.json");
    }
}
