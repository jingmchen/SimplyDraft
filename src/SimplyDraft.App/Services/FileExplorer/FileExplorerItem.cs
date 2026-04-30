using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimplyDraft.App.Services.FileExplorer;

// Observable vm node that represents a single file or directory inside file explorer tree
// Mutable UI state (selection, expand, drag-over, etc.) is kept here so XAML bindings can react
public sealed partial class FileExplorerItem : ObservableObject
{
    // ─── IDENTITY ──────────────────────────────
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private string _extension = string.Empty;

    // ─── FILESYSTEM METADATA ───────────────────
    public bool IsDirectory {get; init;}
    public long Size {get; init;}
    public DateTime CreatedAt {get; init;}
    public DateTime LastModifiedAt {get; init;}
    public bool IsHidden {get; init;}
    public bool IsSymLink {get; init;}
    public bool IsReadOnly {get; init;}

    // ─── UI STATE ──────────────────────────────
    [ObservableProperty]
    private bool _isHovered;
    
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isCut;

    [ObservableProperty]
    private bool _isDragOver;

    // ─── HIERARCHY ─────────────────────────────
    public ObservableCollection<FileExplorerItem> Children {get;} = new();
    public FileExplorerItem? Parent {get; set;}

    // ─── CONVENIENCE ───────────────────────────

    public bool IsFile => !IsDirectory;

    // Human-readable file size
    public string FormattedSize => IsDirectory ? string.Empty : FormatBytes(Size);

    // Tooltip on hover
    public string Tooltip
    {
        get
        {
            if (IsDirectory)
                return $"{FullPath}\nModified: {LastModifiedAt:yyyy-MM-dd HH:mm}";
            return $"{FullPath}\nSize: {FormattedSize}\nModified: {LastModifiedAt:yyyy-MM-dd HH:mm}";
        }
    }

    private static readonly FileIconMapping _iconMapping = new();
    public string Icon => _iconMapping.GetIcon(Name, Extension, IsDirectory, IsExpanded);

    // Raise property-changed for Icon when IsExpanded toggles (for folder open/close icons).
    partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(Icon));

    // ─── FACTORY ───────────────────────────────
    public static FileExplorerItem FromPath(string path, FileExplorerItem? parent = null)
    {
        FileSystemInfo info = Directory.Exists(path)
            ? new DirectoryInfo(path)
            : new FileInfo(path);
        
        var attrs = info.Attributes;
        var ext = info is FileInfo fi
            ? fi.Extension.ToLowerInvariant()
            : string.Empty;
        
        return new FileExplorerItem
        {
            Name = info.Name,
            FullPath = info.FullName,
            Extension = ext,
            IsDirectory = info is DirectoryInfo,
            Size = info is FileInfo fileInfo ? fileInfo.Length : 0L,
            CreatedAt = info.CreationTime,
            LastModifiedAt = info.LastWriteTime,
            IsHidden = attrs.HasFlag(FileAttributes.Hidden),
            IsSymLink = attrs.HasFlag(FileAttributes.ReparsePoint),
            IsReadOnly = attrs.HasFlag(FileAttributes.ReadOnly),
            Parent = parent
        };
    }

    // ─── HELPERS ───────────────────────────────
    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1_024 => $"{bytes} B",
        < 1_048_576 => $"{bytes / 1_024.0:F1} KB",
        < 1_073_741_824 => $"{bytes / 1_048_576.0:F1} MB",
        _ => $"{bytes / 1_073_741_824.0:F2} GB"
    };
}