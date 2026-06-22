using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Export;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace GetToKnowYourDevice.App.Services;

/// <summary>
/// Saves export artifacts to disk via a file picker (or a known folder). The picker is
/// initialized with the app window handle, as required for packaged WinUI 3 apps.
/// </summary>
[SupportedOSPlatform("windows10.0.17763.0")]
public sealed class FileSaveService(MainWindow window)
{
    /// <summary>Prompts the user for a location and writes the artifact. Returns the path or null if cancelled.</summary>
    public async Task<string?> SaveArtifactAsync(ExportArtifact artifact)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = artifact.FileName
        };

        var ext = Path.GetExtension(artifact.FileName);
        picker.FileTypeChoices.Add(DescribeType(ext), [ext]);

        InitializeWithWindow(picker);

        var file = await picker.PickSaveFileAsync();
        if (file is null) return null;

        await FileIO.WriteBytesAsync(file, artifact.Content);
        return file.Path;
    }

    /// <summary>Writes the artifact directly into a folder (no prompt). Returns the full path.</summary>
    public async Task<string> SaveArtifactToFolderAsync(ExportArtifact artifact, string folder)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, artifact.FileName);
        await File.WriteAllBytesAsync(path, artifact.Content);
        return path;
    }

    private void InitializeWithWindow(object target)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(target, hwnd);
    }

    private static string DescribeType(string ext) => ext.ToLowerInvariant() switch
    {
        ".json" => "JSON Report",
        ".csv" => "CSV File",
        ".zip" => "CSV Bundle (ZIP)",
        ".pdf" => "PDF Report",
        _ => "Export File"
    };
}
