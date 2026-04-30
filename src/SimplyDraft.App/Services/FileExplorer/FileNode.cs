namespace SimplyDraft.App.Services.FileExplorer;

// Immutable snapshot of a single filesystem entry to internally transfer data from disk into observable
public sealed class FileNode
{
    // ─── IDENTITY ──────────────────────────────
    public string Name {get; init;} = string.Empty;
    public string FullPath {get; init;} = string.Empty;
    public string Extension {get; init;} = string.Empty;

    // ─── FILESYSTEM METADATA ───────────────────
    public bool IsDirectory {get; init;}
    public long Size {get; init;}
    public DateTime CreatedAt {get; init;}
    public DateTime LastModifiedAt {get; init;}
    public bool IsHidden {get; init;}
    public bool IsSymLink {get; init;}
    public bool IsReadOnly {get; init;}

    // ─── CONVENIENCE ───────────────────────────
    public string FormattedSize => IsDirectory ? string.Empty : FormatBytes(Size);

    // ─── FACTORY ───────────────────────────────
    public static FileNode FromFileSystemInfo(FileSystemInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        var attrs = info.Attributes;

        return new FileNode
        {
            Name = info.Name,
            FullPath = info.FullName,
            IsDirectory = info is DirectoryInfo,
            Extension = info is FileInfo fi ? fi.Extension.ToLowerInvariant() : string.Empty,
            IsHidden = attrs.HasFlag(FileAttributes.Hidden),
            IsSymLink = attrs.HasFlag(FileAttributes.ReparsePoint),
            IsReadOnly = attrs.HasFlag(FileAttributes.ReadOnly),
            Size = info is FileInfo fileInfo ? fileInfo.Length : 0L,
            CreatedAt = info.CreationTime,
            LastModifiedAt = info.LastWriteTime
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