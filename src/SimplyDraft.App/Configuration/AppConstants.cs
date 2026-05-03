namespace SimplyDraft.App.Configuration;

/// <summary>
/// Contains fixed values used across the application.
/// </summary>
public static class AppConstants
{
    public static class AppInfo
    {
        public const string Name = "SimplyDraft";
        public const string AssemblyName = "SimplyDraft";
        public const string Author = "JM";
        public const string Version = "1.0.0";
    }
    
    public static class Directory
    {
        public static class Name
        {
            public const string AppData = $"{AppConstants.AppInfo.Name}";
            public const string Logs = "logs";
            public const string Images = "Images";
            public const string Assets = "Assets";
            public const string Themes = "Themes";
            public const string Accents = "Accents";
            public const string Styles = "Styles";
        }

        public static class Path
        {
            public static readonly string Assembly = AppContext.BaseDirectory;
            public static readonly string AppData = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppConstants.Directory.Name.AppData
            );
            public static readonly string Logs = System.IO.Path.Combine(
                AppData, AppConstants.Directory.Name.Logs
            );
            public static readonly string Assets = System.IO.Path.Combine(
                Assembly, AppConstants.Directory.Name.Assets
            );
            public static readonly string Images = System.IO.Path.Combine(
                Assets, AppConstants.Directory.Name.Images
            );
            public static readonly string Themes = System.IO.Path.Combine(
                Assets, AppConstants.Directory.Name.Themes
            );
            public static readonly string Accents = System.IO.Path.Combine(
                Assets, AppConstants.Directory.Name.Accents
            );
            public static readonly string Styles = System.IO.Path.Combine(
                Assets, AppConstants.Directory.Name.Styles
            );
        }
    }

    public static class File
    {
        public static class Name
        {
            public const string BuildOutputAppSettings = "appsettings.json";
            public const string UserDataAppSettings = "appsettings.json";
            public const string BootstrapLog = $"{AppConstants.AppInfo.Name}-bootstrap{AppConstants.File.Name.LogFileExtension}";
            public const string PreStartupLog = $"{AppConstants.AppInfo.Name}-pre-startup{AppConstants.File.Name.LogFileExtension}";
            public const string LatestLog = $"{AppConstants.AppInfo.Name}-latest{AppConstants.File.Name.LogFileExtension}";
            public const string ArchivedLog = $"{AppConstants.AppInfo.Name}{AppConstants.File.Name.LogFileExtension}";
            public const string LogFileExtension = ".log";
        }
        
        public static class Path
        {
            public static readonly string BuildOutputAppSettings = System.IO.Path.Combine(
                AppConstants.Directory.Path.Assembly, AppConstants.File.Name.BuildOutputAppSettings
            );
            public static readonly string UserDataAppSettings = System.IO.Path.Combine(
                AppConstants.Directory.Path.AppData, AppConstants.File.Name.UserDataAppSettings
            );
            public static readonly string BootstrapLog = System.IO.Path.Combine(
                AppConstants.Directory.Path.Logs, AppConstants.File.Name.BootstrapLog
            );
            public static readonly string PreStartupLog = System.IO.Path.Combine(
                AppConstants.Directory.Path.Logs, AppConstants.File.Name.PreStartupLog
            );
            public static readonly string LatestLog = System.IO.Path.Combine(
                AppConstants.Directory.Path.Logs, AppConstants.File.Name.LatestLog
            );
            public static readonly string ArchivedLog = System.IO.Path.Combine(
                AppConstants.Directory.Path.Logs, AppConstants.File.Name.ArchivedLog
            );
        }
    }

    public static class Uri
    {
        public static readonly string ThemeTemplate =
            $"avares://{AppConstants.AppInfo.AssemblyName}/{AppConstants.Directory.Name.Assets}/{AppConstants.Directory.Name.Themes}/{{0}}Theme.axaml";
        public static readonly string AccentTemplate =
            $"avares://{AppConstants.AppInfo.AssemblyName}/{AppConstants.Directory.Name.Assets}/{AppConstants.Directory.Name.Accents}/{{0}}Accent.axaml";
        public static readonly string AppStyles = 
            $"avares://{AppConstants.AppInfo.AssemblyName}/{AppConstants.Directory.Name.Assets}/{AppConstants.Directory.Name.Styles}/{{0}}.axaml";
    }

    public static class Service
    {
        public static class Logger
        {
            public const string LoggingTemplate = "[{@t:dd-MMM-yyyy}] [{@t:HH:mm:ss}] [{@l:u3}]" +
                "{#if SourceContext is not null} [{SourceContext}]{#end}" +
                " [{@m}]" +
                "{#if @x is not null} [{@x}]{#end}" +
                "\n";
        }

        public static class FileExplorer
        {
            public static class KeyboardShortcuts
            {
                public const string CopyCommand = "C";
                public const string CutCommand = "X";
                public const string PasteCommand = "V";
                public const string DeleteCommand = "Delete";
                public const string RenameCommand = "F2";
                public const string RefreshCommand = "F5";
                public const string CancelCommand = "Escape";
            }
        }

        public static class FileWatcher
        {
            public const int DebounceMs = 400;
        }
    }

    public static class Control
    {
        public static class FileExplorer
        {
            public const double DragThreshold = 6.0; // Minimum pointer travel in pixels before a drag is initiated
            public const int AutoExpandDelayMs = 600; // How long (ms) the pointer must hover over a collapsed folder before it auto-expands during a drag

        }
    }
}