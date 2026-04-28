using SimplyDraft.App.Configuration;

namespace SimplyDraft.App.Utils;

/// <summary>
/// Handles all log file operations that fall outside of Serilog scope
/// </summary>
public static class LogsHandler
{
    public static void ArchivePreviousLogs()
    {
        var latestLog = AppConstants.File.Path.LatestLog;

        if (!File.Exists(latestLog))
        {
            return;
        }

        var date = DateTime.Now.ToString("dd-MM-yyyy");
        var archiveLog = AppConstants.File.Path.ArchivedLog.Insert(
            AppConstants.File.Path.ArchivedLog.IndexOf(AppConstants.File.Name.LogFileExtension), $"-{date}"
        );

        if (File.Exists(archiveLog))
        {
            var baseName = archiveLog;
            int i = 0;
            while (File.Exists(archiveLog))
            {
                i ++;
                archiveLog = baseName.Insert(
                    baseName.IndexOf(AppConstants.File.Name.LogFileExtension), $"-{i}"
                );
            }
        }

        File.Copy(latestLog, archiveLog);
        File.Delete(latestLog);
    }

    public static void CleanOldLogs(int retainedDays = 7)
    {
        var cutoff = DateTime.Now.AddDays(-retainedDays);

        foreach (var file in Directory.GetFiles(AppConstants.Directory.Path.Logs).Where(f => f.EndsWith(AppConstants.File.Name.LogFileExtension)))
        {
            if (File.GetCreationTime(file) < cutoff)
            {
                File.Delete(file);
            }
        }
    }
}