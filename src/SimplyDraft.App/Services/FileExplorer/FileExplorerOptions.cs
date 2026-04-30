using Gtk;
using SimplyDraft.App.Configuration;

namespace SimplyDraft.App.Services.FileExplorer;

public enum IconsLocation {Left, Right, Hidden}

public sealed class FileExplorerOptions
{
    public double InitialWidth {get; init;} = AppConstants.Services.FileExplorer.DefaultPanelDimensions.InitialWidth;
    public double InitialHeight {get; init;} = AppConstants.Services.FileExplorer.DefaultPanelDimensions.InitialHeight;
    public double MinWidth {get; init;} = AppConstants.Services.FileExplorer.DefaultPanelDimensions.MinWidth;
    public double MaxWidth {get; init;} = AppConstants.Services.FileExplorer.DefaultPanelDimensions.MaxWidth;
    public bool IsResizable {get; init;} = AppConstants.Services.FileExplorer.DefaultPanelDimensions.IsResizable;
    public IconsLocation IconsLocation {get; init;} = IconsLocation.Left;

    public IReadOnlySet<string> IgnoredExtensions {get; init;}
        = new HashSet<string>(
            AppConstants.Services.FileExplorer.IgnoredExtensions.Files,
            StringComparer.OrdinalIgnoreCase
        );
    
    // Allowlist (if empty, allow all other than IgnoredExtensions)
    public IReadOnlySet<string> AllowedExtensions {get; init;}
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    
    public IReadOnlySet<string> IgnoredFolderNames {get; init;}
        = new HashSet<string>(
            AppConstants.Services.FileExplorer.IgnoredExtensions.Folders,
            StringComparer.OrdinalIgnoreCase
        );
    
    public bool ShowHiddenFiles {get; init;} = false;
    public bool ShowFileSize {get; init;} = true;
    public bool MultiSelect {get; init;} = true;
    public bool AutoExpandRootOnOpen {get; init;} = true;
    public int MaxSearchResults {get; init;} = 200;
    public bool EnableFileWatcher {get; init;} = true;
    public TimeSpan FileWatcherDebounce {get; init;} = TimeSpan.FromMilliseconds(400);
}