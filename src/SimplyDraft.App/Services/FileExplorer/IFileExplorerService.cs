using System.Collections.ObjectModel;
using SimplyDraft.App.Models;

namespace SimplyDraft.App.Services.FileExplorer;

public interface IFileExplorerService
{
    ObservableCollection<FileExplorerItem> RootItems {get;}
    ObservableCollection <FileExplorerItem> SelectedItems {get;}
    FileExplorerClipboard Clipboard {get;}
    FileExplorerOptions Options {get;}

    // Methods
    void LoadDirectory(string rootPath); // Load directory tree, create if not exists. Start/ restart File Watcher for rootPath
    void RefreshDirectory();
    FileExplorerItem CreateFile(FileExplorerItem? parent, string name);
    FileExplorerItem CreateFolder(FileExplorerItem? parent, string name);
    void RenameItem(FileExplorerItem item, string newName);
    void DeleteItem(FileExplorerItem item);
    void DeleteMultipleItems(IEnumerable<FileExplorerItem> items);
    void ExpandItem(FileExplorerItem item);
    void CollapseItem(FileExplorerItem item);
    void ExpandAll(FileExplorerItem item);
    void CollapseAll(FileExplorerItem item);
    bool CanDrop(FileExplorerItem source, FileExplorerItem? targetFolder); // Returns true if can drag-and-drop to destination
    void MoveItem(FileExplorerItem source, FileExplorerItem? targetFolder);
    void ClipboardCopyItems(IEnumerable<FileExplorerItem> items);
    void ClipboardCutItems(IEnumerable<FileExplorerItem> items);
    void ClipboardPasteItems(FileExplorerItem? targetFolder);

    // Events
    event EventHandler<FileExplorerItem>? ItemCreated;
    event EventHandler<FileExplorerItem>? ItemRenamed;
    event EventHandler<string>? ItemDeleted; // string for path
    event EventHandler? ExplorerRefreshed;
}