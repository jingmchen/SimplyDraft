namespace SimplyDraft.App.Configuration;

public static class AppKeys
{
    public static class AppSettings
    {
        public static class LoggingSection
        {
            public const string Title = "Logs";
            public const string OutputToFileKey = "OutputToFile";
            public const string MinimumLevelKey = "MinimumLevel";
            public const string RetainedFileCountLimitKey = "RetainedFileCountLimit";
        }

        public static class ThemeSection
        {
            public const string Title = "Themes";
            public const string FollowSystemThemeKey = "FollowSystemTheme";
            public const string AutoSaveKey = "AutoSave";
            public const string Themekey = "Theme";
            public const string AccentKey = "Accent";
        }
    }
}