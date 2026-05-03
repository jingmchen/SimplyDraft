using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Threading;
using Gdk;
using SimplyDraft.App.Models;

namespace SimplyDraft.App.Services.FileExplorer;

public sealed class FileExplorerService : IFileExplorerService, IDisposable
{
    // ─── FACTORY-INJECTED ───────────────────────────
    private readonly FileWatcherService _watcher;

    // ─── PRIVATE ───────────────────────────────
    private string _rootPath = string.Empty;
    private int _internalOpCount; // Non-zero => watcher suppressed
    
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
        FileWatcherService watcher,
        FileExplorerOptions? options = null
    )
    {
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
        SuppressWatcher();
        try
        {
            var parentPath = parent?.FullPath ?? _rootPath;
            var fullPath = GetUniquePath(parentPath, name, isFile:false);

            Directory.CreateDirectory(fullPath);

            var item = FileExplorerItem.FromPath(fullPath, parent);
            InsertSorted(parent is not null ? parent.Children : RootItems, item);
            ItemCreated?.Invoke(this, item);
            return item;
        }
        finally
        {
            ResumeWatcher();
        }
    }

    public void RenameItem(FileExplorerItem item, string newName)
    {
        ArgumentNullException.ThrowIfNull(item);

        SuppressWatcher();
        try
        {
            if (string.IsNullOrWhiteSpace(newName) || newName == item.Name) return;

            var parentPath = Path.GetDirectoryName(item.FullPath)!;
            var newFullPath = Path.Combine(parentPath, newName);

            if (item.IsDirectory)
                Directory.Move(item.FullPath, newFullPath);
            else
                File.Move(item.FullPath, newFullPath);
            
            item.FullPath = newFullPath;
            item.Name = Path.GetFileName(newFullPath);
            item.Extension = item.IsFile ? Path.GetExtension(newFullPath).ToLowerInvariant() : string.Empty;
            item.IsEditing = false;

            ItemRenamed?.Invoke(this, item);
        }
        finally
        {
            ResumeWatcher();
        }
    }

    public FileExplorerItem CreateFile(FileExplorerItem? parent, string name)
    {
        SuppressWatcher();
        try
        {
            var parentPath = parent?.FullPath ?? _rootPath;
            var fullPath = GetUniquePath(parentPath, name, isFile:true);

            File.WriteAllText(fullPath, string.Empty);

            var item = FileExplorerItem.FromPath(fullPath, parent);
            InsertSorted(parent is not null ? parent.Children : RootItems, item);
            ItemCreated?.Invoke(this, item);
            return item;
        }
        finally
        {
            ResumeWatcher();
        }
    }

    public void DeleteItem(FileExplorerItem item)
    {
        SuppressWatcher();
        try
        {
            ArgumentNullException.ThrowIfNull(item);

            var path = item.FullPath;

            if (item.IsDirectory && Directory.Exists(path))
                Directory.Delete(path, recursive:true);
            else if (item.IsFile && File.Exists(path))
                File.Delete(path);
            
            RemoveFromParent(item);
            SelectedItems.Remove(item);
            ItemDeleted?.Invoke(this, path);
        }
        finally
        {
            ResumeWatcher();
        }
    }

    public void DeleteMultipleItems(IEnumerable<FileExplorerItem> items)
    {
        foreach (var item in items.ToList())
            DeleteItem(item);
    }

    public void ExpandItem(FileExplorerItem item)
    {
        if (item.IsDirectory)
            item.IsExpanded = true;
    }

    public void CollapseItem(FileExplorerItem item)
    {
        if (item.IsDirectory)
            item.IsExpanded = false;
    }

    public void ExpandAll(FileExplorerItem item)
    {
        if (!item.IsDirectory) return;
        item.IsExpanded = true;
        foreach (var child in item.Children)
            ExpandAll(child);
    }

    public void CollapseAll(FileExplorerItem item)
    {
        if (!item.IsDirectory) return;
        item.IsExpanded = false;
        foreach(var child in item.Children)
            CollapseAll(child);
    }

    public bool CanDrop(FileExplorerItem source, FileExplorerItem? targetFolder)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (targetFolder is null) return true;
        if (!targetFolder.IsDirectory) return false;
        if (source == targetFolder) return false;
        return !IsAncestor(ancestor: source, candidate: targetFolder);
    }

    public void MoveItem(FileExplorerItem source, FileExplorerItem? targetFolder)
    {
        if (!CanDrop(source, targetFolder)) return;

        SuppressWatcher();
        try
        {
            var targetPath = targetFolder?.FullPath ?? _rootPath;
            var newPath = GetUniquePath(targetPath, source.Name, isFile:source.IsFile);

            if (source.IsDirectory)
                Directory.Move(source.FullPath, newPath);
            else
                File.Move(source.FullPath, newPath);
            
            RemoveFromParent(source);

            source.FullPath = newPath;
            source.Name = Path.GetFileName(newPath);
            source.Extension = source.IsFile ? Path.GetExtension(newPath).ToLowerInvariant() : string.Empty;
            source.Parent = targetFolder;

            InsertSorted(targetFolder is not null ? targetFolder.Children : RootItems, source);

            if (targetFolder is not null)
                targetFolder.IsExpanded = true;
        }
        finally
        {
            ResumeWatcher();
        }
    }

    public void ClipboardCopyItems(IEnumerable<FileExplorerItem> items)
        => Clipboard.SetCopy(items);
    
    public void ClipboardCutItems(IEnumerable<FileExplorerItem> items)
        => Clipboard.SetCut(items);
    
    public void ClipboardPasteItems(FileExplorerItem? targetFolder)
    {
        if (!Clipboard.HasItems) return;

        SuppressWatcher();
        try
        {
            var targetPath = targetFolder?.FullPath ?? _rootPath;

            foreach (var item in Clipboard.Snapshot())
            {
                var newPath = GetUniquePath(targetPath, item.Name, isFile:item.IsFile);

                if (Clipboard.Operation == ClipboardOperation.Copy)
                {
                    if (item.IsDirectory)
                        CopyDirectory(item.FullPath, newPath);
                    else
                        File.Copy(item.FullPath, newPath);
                    
                    var copy = BuildSingleItem(newPath, targetFolder);
                    InsertSorted(targetFolder is not null ? targetFolder.Children : RootItems, copy);
                    ItemCreated?.Invoke(this, copy);
                }
                else
                {
                    MoveItem(item, targetFolder);
                    item.IsCut = false;
                }
            }

            if (Clipboard.Operation == ClipboardOperation.Cut)
                Clipboard.SetNone();
            
            if (targetFolder is not null)
                targetFolder.IsExpanded = true;
        }
        finally
        {
            ResumeWatcher();
        }
    }

    public void Dispose()
        => _watcher.Dispose();
    
    // ─── FILEWATCHER CALLBACK ──────────────────
    // Invoked on thread-pool thread when debounced watcher fires
    private void OnWatcherChanged(object? sender, FileSystemEventArgs e)
    {
        if (_internalOpCount > 0) return;

        Dispatcher.UIThread.Post(() =>
        {
            var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectExpandedPaths(RootItems, expandedPaths);
            RootItems.Clear();
            SelectedItems.Clear();

            foreach (var item in BuildTree(_rootPath, parent:null))
                RootItems.Add(item);
            
            if (Options.AutoExpandRootOnOpen)
                foreach (var root in RootItems)
                    root.IsExpanded = true;
            
            // Restore expansion for folders that were open before rebuild
            RestoreExpandedPaths(RootItems, expandedPaths);

            ExplorerRefreshed?.Invoke(this, EventArgs.Empty);
        });
    }
    
    // ─── PRIVATE HELPERS ───────────────────────
    // Stops watcher before any internal operation to prevent unnecessary tree rebuilds
    private void SuppressWatcher()
    {
        if (Interlocked.Increment(ref _internalOpCount) == 1)
            _watcher.Stop();
    }

    // Restarts with new watcher after the last internal operation
    private void ResumeWatcher()
    {
        if (Interlocked.Decrement(ref _internalOpCount) == 0 &&
            Options.EnableFileWatcher &&
            !string.IsNullOrEmpty(_rootPath))
        {
            _watcher.Start(_rootPath);
        }
    }

    private static void CollectExpandedPaths(IEnumerable<FileExplorerItem> items, HashSet<string> paths)
    {
        foreach (var item in items)
        {
            if (!item.IsDirectory || !item.IsExpanded) continue;
            paths.Add(item.FullPath);
            CollectExpandedPaths(item.Children, paths);
        }
    }

    private static void RestoreExpandedPaths(IEnumerable<FileExplorerItem> items, HashSet<string> paths)
    {
        foreach (var item in items)
        {
            if (!item.IsDirectory) continue;
            if (paths.Contains(item.FullPath))
            {
                item.IsExpanded = true;
                RestoreExpandedPaths(item.Children, paths);
            }
        }
    }

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
                item.Children.Add(c);
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
            foreach (var child in BuildTree(path, item))
                item.Children.Add(child);

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
            if (!item.IsDirectory && existing.IsDirectory) {break;}
            if (item.IsDirectory == existing.IsDirectory &&
                string.Compare(item.Name, existing.Name, StringComparison.OrdinalIgnoreCase) < 0)
                break;
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