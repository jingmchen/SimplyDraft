namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface IAppPaths
{
    string AppDataFolder {get;}
    string LogsFolder {get;}
    string BundledAppSettingsFile {get;}
    string UserAppSettingsFile {get;}
    string LatestLogFile {get;}
}