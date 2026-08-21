// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using Microsoft.Extensions.Logging;
using SimplyDraft.Infrastructure.Constants;

namespace SimplyDraft.Infrastructure.Utils;

public sealed class LogsHandler
{
    private readonly ILogger<LogsHandler> _logger;
    private readonly string _logsFolderPath;
    private readonly string _latestLogFilePath;

    public LogsHandler(ILogger<LogsHandler> logger, string logsFolderPath, string latestLogFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logsFolderPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(latestLogFilePath);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logsFolderPath = logsFolderPath;
        _latestLogFilePath = latestLogFilePath;

        Directory.CreateDirectory(logsFolderPath);
    }

    public void ArchivePreviousLatestLogFile()
    {
        if (!File.Exists(_latestLogFilePath)) return;
        
        var date = DateTime.Now.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
        var archiveLogBaseName = Path.GetFileNameWithoutExtension(InfrastructureConstants.UserData.FileName.ArchivedLog);
        var archiveLogExt = Path.GetExtension(InfrastructureConstants.UserData.FileName.ArchivedLog);
        var candidate = Path.Combine(_logsFolderPath, $"{archiveLogBaseName}-{date}{archiveLogExt}");
        
        for (var i = 1; File.Exists(candidate); i++)
        {
            candidate = Path.Combine(_logsFolderPath, $"{archiveLogBaseName}-{date}({i}){archiveLogExt}");
        }

        try
        {
            File.Move(_latestLogFilePath, candidate);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _logger.LogWarning(ex, "Could not archive previous log file at {Path}", _latestLogFilePath);
        }
    }

    public void CleanupOldLogs(int retainedFileCountLimit = 7)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(retainedFileCountLimit);
        
        var archiveLogBaseName = Path.GetFileNameWithoutExtension(InfrastructureConstants.UserData.FileName.ArchivedLog);
        var archiveLogExt = Path.GetExtension(InfrastructureConstants.UserData.FileName.ArchivedLog);

        var filesToDelete = Directory.EnumerateFiles(_logsFolderPath, $"{archiveLogBaseName}-*{archiveLogExt}")
            .Select(f => new FileInfo(f))
            .OrderByDescending(fi => fi.LastWriteTimeUtc)
            .Skip(retainedFileCountLimit);
        
        foreach (var file in filesToDelete)
        {
            try
            {
                file.Delete();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug(ex, "Could not delete archived log {FileName}", file.Name);
            }
        }
    }
}