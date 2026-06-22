using System.Text.Json;
using GetToKnowYourDevice.Core.Settings;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Settings;

/// <summary>
/// Persists <see cref="AppSettings"/> as JSON in the application data directory so settings
/// survive restarts. Load failures fall back to defaults rather than crashing startup.
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private readonly string _filePath;
    private readonly ILogger<JsonSettingsService> _logger;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public AppSettings Current { get; private set; } = new();

    public JsonSettingsService(string settingsDirectory, ILogger<JsonSettingsService> logger)
    {
        Directory.CreateDirectory(settingsDirectory);
        _filePath = Path.Combine(settingsDirectory, "settings.json");
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogInformation("No settings file; using defaults.");
                return;
            }
            await using var stream = File.OpenRead(_filePath);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, Options, ct)
                .ConfigureAwait(false);
            if (loaded is not null) Current = loaded;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings; using defaults.");
            Current = new AppSettings();
        }
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        try
        {
            var tmp = _filePath + ".tmp";
            await using (var stream = File.Create(tmp))
                await JsonSerializer.SerializeAsync(stream, Current, Options, ct).ConfigureAwait(false);
            File.Move(tmp, _filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings.");
        }
    }
}
