using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Infrastructure.Constants;

namespace SimplyDraft.Infrastructure.Services;

public sealed class AppPaths : IAppPaths
{
    public string AppDataFolder {get;}
    public string LogsFolder {get;}
    public string BundledAppSettingsFile {get;}
    public string UserAppSettingsFile {get;}
    public string LatestLogFile {get;}

    public AppPaths(IAppInfo appInfo)
    {
        ArgumentNullException.ThrowIfNull(appInfo);
        
        AppDataFolder =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appInfo.Product
            );
        
        Directory.CreateDirectory(AppDataFolder);
        
        LogsFolder =
            Path.Combine(
                AppDataFolder,
                InfrastructureConstants.UserData.FolderName.Logs
            );
        
        Directory.CreateDirectory(LogsFolder);
        
        BundledAppSettingsFile =
            Path.Combine(
                AppContext.BaseDirectory,
                InfrastructureConstants.Bundled.FileName.AppSettings
            );
        
        UserAppSettingsFile =
            Path.Combine(
                AppDataFolder,
                InfrastructureConstants.UserData.FileName.AppSettings
            );
        
        LatestLogFile =
            Path.Combine(
                LogsFolder,
                InfrastructureConstants.UserData.FileName.LatestLog
            );
    }
}