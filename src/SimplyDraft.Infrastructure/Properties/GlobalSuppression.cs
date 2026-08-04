using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "Cold path; runs once at application startup.",
    Scope = "member",
    Target = "~M:SimplyDraft.Infrastructure.Utils.LogsHandler.ArchivePreviousLatestLogFile")]

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "Cold path; runs once at application startup.",
    Scope = "member",
    Target = "~M:SimplyDraft.Infrastructure.Utils.LogsHandler.CleanupOldLogs(System.Int32)")]