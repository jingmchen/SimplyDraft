using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Gdk;
using Microsoft.Extensions.Logging;

namespace SimplyDraft.App.Services.FileExplorer;

public sealed class FileExplorerService : IFileExplorerService, IDisposable
{
    // ─── DI-INJECTED ───────────────────────────
    private readonly ILogger<FileExplorerService> _logger;
    private readonly FileWatcherService _watcher;

    // ─── PRIVATE ───────────────────────────────
    private string _rootPath = string.Empty;
    
    // ─── CONTRACT ──────────────────────────────
    public ObservableCollection<FileExplorerItem> RootItems {get;} = new();
    public ObservableCollection<FileExplorerItem> SelectedItems {get;} = new();
    public FileExplorerClipboard Clipboard {get;} = new();
    public FileExplorerOptions Options {get;} = new();

    // ─── EVENTS ────────────────────────────────
    public event EventHandler<FileExplorerItem>? ItemCreated;
    public event EventHandler<FileExplorerItem>? ItemRenamed;
    public event EventHandler<string>? ItemDeleted;
    public event EventHandler? ExplorerRefreshed;

    // ─── CONSTRUCTOR ───────────────────────────
    public FileExplorerService(
        ILogger<FileExplorerService> logger,
        FileWatcherService watcher,
        FileExplorerOptions? options = null
    )
    {
        _logger = logger;
        _watcher = watcher;
        Options = options ?? new FileExplorerOptions();
        
        if (Options.EnableFileWatcher)
            _watcher.Changed += OnWatcherChanged;
    }

    // ─── INTERFACE IMPLEMENTATION ──────────────
    public void LoadDirectory(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        _rootPath = rootPath;
        RootItems.Clear();
        SelectedItems.Clear();

        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);
        
        foreach (var item in BuildTree(rootPath, parent:null))
            RootItems.Add(item);
        
        if (Options.AutoExpandRootOnOpen)
            foreach (var root in RootItems)
                root.IsExpanded = true;
        
        if (Options.EnableFileWatcher)
            _watcher.Start(rootPath);
        
        ExplorerRefreshed?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshDirectory()
        => LoadDirectory(_rootPath);
    
    public FileExplorerItem CreateFolder(FileExplorerItem? parent, string name)
    {
        var parentPath = parent?.FullPath ?? _rootPath;
        var fullPath = GetUniquePath(parentPath, name, isFile:false);

        Directory.CreateDirectory(fullPath);

        var item = FileExplorerItem.FromPath(fullPath, parent);
        InsertSorted(parent is not null ? parent.Children : RootItems, item);
        ItemCreated?.Invoke(this, item);
        return item;
    }

    public void RenameItem(FileExplorerItem item)
    public void Dispose()
        => _watcher.Dispose();
    
    // ─── FILEWATCHER CALLBACK ──────────────────
    // Invoked on thread-pool thread when debounced watcher fires
    private void OnWatcherChanged(object? sender, FileSystemEventArgs e)
    {
        RootItems.Clear(); // Only safe if callers use Dispatcher.UIThread.Post
        foreach (var item in BuildTree(_rootPath, parent:null))
            RootItems.Add(item);
        
        ExplorerRefreshed?.Invoke(this, EventArgs.Empty);
    }
    
    // ─── PRIVATE HELPERS ───────────────────────
    private List<FileExplorerItem> BuildTree(string path, FileExplorerItem? parent)
    {
        var items = new List<FileExplorerItem>();

        IEnumerable<string> dirs;
        IEnumerable<string> files;

        try
        {
            dirs = Directory.GetDirectories(path).OrderBy(d => d, StringComparer.OrdinalIgnoreCase);
            files = Directory.GetFiles(path).OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
        }
        catch (UnauthorizedAccessException)
        {
            return items; // Skip silently
        }

        foreach (var dir in dirs)
        {
            var folderName = Path.GetFileName(dir);
            if (Options.IgnoredFolderNames.Contains(folderName)) continue;

            var dirInfo = new DirectoryInfo(dir);
            if (!Options.ShowHiddenFiles && dirInfo.Attributes.HasFlag(FileAttributes.Hidden)) continue;

            var item = FileExplorerItem.FromPath(dir, parent);
            var children = BuildTree(dir, item);

            foreach (var c in children)
            {
                item.Children.Add(c);
            }
            items.Add(item);
        }

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            var fileInfo = new FileInfo(file);

            if (!Options.ShowHiddenFiles && fileInfo.Attributes.HasFlag(FileAttributes.Hidden)) continue;
            if (Options.IgnoredExtensions.Contains(ext)) continue;
            if (Options.AllowedExtensions.Count > 0 && !Options.AllowedExtensions.Contains(ext)) continue;

            items.Add(FileExplorerItem.FromPath(file, parent));
        }

        return items;
    }

    private FileExplorerItem BuildSingleItem(string path, FileExplorerItem? parent)
    {
        var item = FileExplorerItem.FromPath(path, parent);

        if (item.IsDirectory)
        {
            foreach (var child in BuildTree(path, item))
                item.Children.Add(child);
        }

        return item;
    }

    private void RemoveFromParent(FileExplorerItem item)
    {
        var collection = item.Parent is not null ? item.Parent.Children : RootItems;
        collection.Remove(item);
    }

    private static void InsertSorted(ObservableCollection<FileExplorerItem> collection, FileExplorerItem item)
    {
        var index = 0;

        for (; index < collection.Count; index++)
        {
            var existing = collection[index];

            // Sort folders before files
            if (item.IsDirectory && existing.IsFile) break;
            if (!item.IsDirectory && existing.IsDirectory) {index++; continue;}

            if (
                item.IsDirectory == existing.IsDirectory &&
                string.Compare(item.Name, existing.Name, StringComparison.OrdinalIgnoreCase) < 0
            )
            {
                break;
            }
        }

        collection.Insert(index, item);
    }

    // To prevent dropping a folder into one of its children
    private static bool IsAncestor(FileExplorerItem ancestor, FileExplorerItem candidate)
    {
        var current = candidate.Parent;
        while (current is not null)
        {
            if (current == ancestor) return true;
            current = current.Parent;
        }
        return false;
    }

    // Get unique name - append incrementing suffix to end if file/fldr name already exists.
    private static string GetUniquePath(string parentPath, string name, bool isFile)
    {
        var path = Path.Combine(parentPath, name);
        if (!PathExists(path)) return path;

        var stem = isFile ? Path.GetFileNameWithoutExtension(name) : name;
        var ext = isFile ? Path.GetExtension(name) : string.Empty;
        var counter = 1;

        while (true)
        {
            path = Path.Combine(parentPath, $"{stem} ({counter}){ext}");
            if (!PathExists(path)) return path;
            counter++;
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite:false);
        
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
    }

    private static bool PathExists(string path)
        => File.Exists(path) || Directory.Exists(path);
}