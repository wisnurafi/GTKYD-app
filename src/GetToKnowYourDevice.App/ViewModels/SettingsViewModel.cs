using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GetToKnowYourDevice.Core.Settings;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>
/// Backs the Settings page. Edits the shared <see cref="AppSettings"/> instance and persists it.
/// Privacy defaults stay local-first: external requests and public IP lookup are off unless the
/// user explicitly enables them here.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly ILogger<SettingsViewModel> _logger;

    public SettingsViewModel(ISettingsService settings, ILogger<SettingsViewModel> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>Direct access to the live settings object for two-way binding in XAML.</summary>
    public AppSettings Settings => _settings.Current;

    [ObservableProperty] private string? _savedMessage;

    [RelayCommand]
    public async Task SaveAsync()
    {
        try
        {
            await _settings.SaveAsync();
            SavedMessage = "Settings saved";
            _logger.LogInformation("Settings saved");
        }
        catch (Exception ex)
        {
            SetError($"Failed to save settings: {ex.Message}");
            _logger.LogError(ex, "Save settings failed");
        }
    }
}
