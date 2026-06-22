using System.Runtime.Versioning;

namespace GetToKnowYourDevice.App.Services;

/// <summary>
/// Resolves the local application data directory for the app's database, settings, and logs.
/// Works both packaged (uses the package's local folder) and unpackaged (uses %LOCALAPPDATA%).
/// Local-first: nothing here touches the network.
/// </summary>
[SupportedOSPlatform("windows10.0.17763.0")]
public static class AppPaths
{
    public static string GetAppDataDirectory()
    {
        string baseDir;
        try
        {
            // Packaged app: redirected per-package local folder.
            baseDir = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        }
        catch
        {
            // Unpackaged fallback.
            baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GetToKnowYourDevice");
        }
        Directory.CreateDirectory(baseDir);
        return baseDir;
    }
}
