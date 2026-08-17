// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Logging;
using SimplyDraft.Infrastructure.Utils;

namespace SimplyDraft.Infrastructure.Services;

public abstract partial class SettingsProvider<T> : ISettingsProvider<T> where T : class, new()
{
    private readonly ILogger _logger;
    private readonly object _gate = new();
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = {new JsonStringEnumConverter()}
    };
    public T Current {get; private set;} = null!;

    protected SettingsProvider(ILogger logger, string settingsPath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsPath = settingsPath;

        var dir = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        
        Reload();
    }

    // ─── PUBLIC METHODS ────────────────────────
    public void Save()
    {
        lock(_gate)
            WriteToDisk(Current);
    }

    public void Reload()
    {
        lock(_gate)
            ReloadCore();
    }

    // ─── PRIVATE METHODS ───────────────────────
    private void WriteToDisk(T settings)
    {
        var json = JsonSerializer.Serialize(settings, _jsonOptions);

        try
        {
            AtomicFile.WriteTo(_settingsPath, json);
        }
        catch (Exception ex)
        {
            LogFileUnableToSave(ex, Path.GetFileName(_settingsPath));
        }
    }

    private void ReloadCore()
    {
        if (!File.Exists(_settingsPath))
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                var fileName = Path.GetFileName(_settingsPath);
                LogFileNotFoundCreateDefaults(fileName);
            }
            ApplyDefaults();
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<T>(json, _jsonOptions);

            if (settings is null)
            {
                LogFileInvalidOrEmptyCreateDefaults(Path.GetFileName(_settingsPath));
                ApplyDefaults();
                return;
            }

            Current = Sanitize(settings);
            Save();
        }
        catch (Exception ex)
        {
            LogFileUnableToReadCreateDefaults(ex, Path.GetFileName(_settingsPath));
            ApplyDefaults();
        }
    }

    private void ApplyDefaults()
    {
        Current = new T();
        Save();
    }

    protected abstract T Sanitize(T settings);

    [LoggerMessage(
        EventId = LogEventIDs.Infrastructure.SettingsProvider.FileNotFound,
        Level = LogLevel.Information,
        Message = "File: '{FileName}' - Unable to be found. Reverting to factory defaults.")]
    private partial void LogFileNotFoundCreateDefaults(string fileName);

    [LoggerMessage(
        EventId = LogEventIDs.Infrastructure.SettingsProvider.FileUnableToRead,
        Level = LogLevel.Warning,
        Message = "File: '{FileName}' - Unable to be read. Reverting to factory defaults.")]
    private partial void LogFileUnableToReadCreateDefaults(Exception ex, string fileName);

    [LoggerMessage(
        EventId = LogEventIDs.Infrastructure.SettingsProvider.FileInvalidOrEmpty,
        Level = LogLevel.Warning,
        Message = "File: '{FileName}' was empty or invalid. Reverting to factory defaults.")]
    private partial void LogFileInvalidOrEmptyCreateDefaults(string fileName);

    [LoggerMessage(
        EventId = LogEventIDs.Infrastructure.SettingsProvider.FileUnableToSave,
        Level = LogLevel.Warning,
        Message = "File: '{FileName}' - Unable to save.")]
    private partial void LogFileUnableToSave(Exception ex, string fileName);

    [LoggerMessage(
        EventId = LogEventIDs.Infrastructure.SettingsProvider.TempCleanupFailed,
        Level = LogLevel.Debug,
        Message = "Temp File: Could not be removed at '{Path}'; it may be overwritten on the next successful save.")]
    private partial void LogTempCleanupFailed(Exception ex, string path);
}