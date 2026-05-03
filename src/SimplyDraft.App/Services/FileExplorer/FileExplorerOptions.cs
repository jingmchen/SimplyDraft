using SimplyDraft.App.Configuration;

namespace SimplyDraft.App.Services.FileExplorer;

public enum IconsLocation {Left, Right, Hidden}

public sealed record FileExplorerOptions
{
    // Panel dimensions
    public double InitialWidth {get; init;} = 260;
    public double InitialHeight {get; init;} = double.NaN;
    public double MinWidth {get; init;} = 150;
    public double MaxWidth {get; init;} = 700;
    public bool IsResizable {get; init;} = true;

    // Icon placement
    public IconsLocation IconsLocation {get; init;} = IconsLocation.Left;

    // Filtering
    public IReadOnlySet<string> IgnoredExtensions {get; init;}
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".obj", ".pdb", ".cache", ".suo", ".user", ".DotSettings.user"
        };
    
    // Allowlist (if empty, allow all other than IgnoredExtensions)
    public IReadOnlySet<string> AllowedExtensions {get; init;}
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    
    public IReadOnlySet<string> IgnoredFolderNames {get; init;}
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", ".git", ".vs", ".idea", "node_modules", "__pycache__", ".svn"
        };
    
    public bool ShowHiddenFiles {get; init;} = false;
    public bool ShowFileSize {get; init;} = true;
    public bool MultiSelect {get; init;} = true;

    // Parameters
    public string NewFileName {get; init;} = "New-File";
    public string NewFileExt {get; init;} = ".json";
    public string NewFolderName {get; init;} = "New Folder";
    public string? RootPath {get; init;} = string.Empty;
    public bool AutoExpandRootOnOpen {get; init;} = true;
    public int MaxSearchResults {get; init;} = 200;
    public bool EnableFileWatcher {get; init;} = true;
    public TimeSpan FileWatcherDebounce {get; init;} = TimeSpan.FromMilliseconds(AppConstants.Service.FileWatcher.DebounceMs);
}