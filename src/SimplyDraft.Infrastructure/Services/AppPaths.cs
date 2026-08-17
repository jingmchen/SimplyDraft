// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Infrastructure.Constants;

namespace SimplyDraft.Infrastructure.Services;

public sealed class AppPaths : IAppPaths
{
    public string AppDataFolder {get;}
    public string UserStateFolder {get;}
    public string LogsFolder {get;}
    public string BundledAppSettingsFile {get;}
    public string UserAppSettingsFile {get;}
    public string UserStateSettingsFile {get;}
    public string LatestLogFile {get;}

    public AppPaths(IAppInfo appInfo)
    {
        ArgumentNullException.ThrowIfNull(appInfo);
        
        AppDataFolder =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appInfo.Product);
        
        Directory.CreateDirectory(AppDataFolder);
        
        UserStateFolder =
            Path.Combine(
                AppDataFolder,
                InfrastructureConstants.UserData.FolderName.UserState);
        
        Directory.CreateDirectory(UserStateFolder);

        LogsFolder =
            Path.Combine(
                AppDataFolder,
                InfrastructureConstants.UserData.FolderName.Logs);
        
        Directory.CreateDirectory(LogsFolder);
        
        BundledAppSettingsFile =
            Path.Combine(
                AppContext.BaseDirectory,
                InfrastructureConstants.Bundled.FileName.AppSettings);
        
        UserAppSettingsFile =
            Path.Combine(
                AppDataFolder,
                InfrastructureConstants.UserData.FileName.AppSettings);
        
        UserStateSettingsFile =
            Path.Combine(
                UserStateFolder,
                InfrastructureConstants.UserData.FileName.UserStateSettings);
        
        LatestLogFile =
            Path.Combine(
                LogsFolder,
                InfrastructureConstants.UserData.FileName.LatestLog);
    }
}