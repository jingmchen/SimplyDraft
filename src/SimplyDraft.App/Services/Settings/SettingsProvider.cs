using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SimplyDraft.App.Configuration;

namespace SimplyDraft.App.Services.Settings;

public sealed class SettingsProvider : ISettingsProvider
{
    // ─── DI-INJECTED ───────────────────────────
    private readonly ILogger<SettingsProvider> _logger = null!;
    
    // ─── INITIALIZATION ────────────────────────
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = {new JsonStringEnumConverter()}
    };
    private readonly string _settingsPath;

    // ─── CONTRACT ──────────────────────────────
    public AppSettings Current {get; private set;} = null!;

    // ─── CONSTRUCTOR ───────────────────────────
    public SettingsProvider(ILogger<SettingsProvider> logger)
    {
        _logger = logger;
        _settingsPath = AppConstants.File.Path.UserDataAppSettings;
        Reload();
    }

    // ─── EXPOSED METHODS ───────────────────────
    public void Save()
    {
        var json = JsonSerializer.Serialize(Current, JsonOpts);
        File.WriteAllText(_settingsPath, json);
    }

    public void Reload()
    {
        if (!File.Exists(_settingsPath))
        {
            FallbackToDefault("Unable to locate file");
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOpts);

            if (settings == null)
            {
                FallbackToDefault("Failed to read file");
                return;
            }
            Current = settings;
        }
        catch
        {
            FallbackToDefault("Unexpected error");
        }
    }

    // ─── INTERNAL METHODS ──────────────────────
    private void FallbackToDefault(string? loggerMessage)
    {
        if (!string.IsNullOrEmpty(loggerMessage))
        {
            _logger.LogWarning(
                "{Message} when loading {FileName} at path: {FilePath} - reverting to defaults.",
                loggerMessage, AppConstants.File.Name.UserDataAppSettings, _settingsPath
            );
        }
        Current = new AppSettings();
        Save();
    }
}