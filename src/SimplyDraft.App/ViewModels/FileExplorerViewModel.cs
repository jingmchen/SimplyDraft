using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SimplyDraft.App.Configuration;
using SimplyDraft.App.Models;
using SimplyDraft.App.Services.FileExplorer;

namespace SimplyDraft.App.ViewModels;

// State machine to handle File Explorer Service methods
public sealed partial class FileExplorerViewModel : ObservableObject
{
    // ─── DI-INJECTED ───────────────────────────
    private readonly ILogger<FileExplorerViewModel> _logger;
    private readonly IFileExplorerService _service;

    // ─── PRIVATE STATES ────────────────────────
    private FileExplorerItem? _pendingCreateParent;
    private FileExplorerItem? _pendingRenameItem;
    private bool _creatingFolder;

    // ─── OBSERVABLE PROPERTIES ─────────────────
    [ObservableProperty] private string _statusMessage = "No folder loaded.";
    [ObservableProperty] private bool _isCreatingFolder;
    [ObservableProperty] private bool _isCreatingFile;
    [ObservableProperty] private string _newItemName = string.Empty;
    [ObservableProperty] private string _renameText = string.Empty;
    [ObservableProperty] private FileExplorerItem? _contextItem; // The item that was most recently right-clicked or keyboard-focused.
    [ObservableProperty] private bool _hasDirectory;

    // ─── COLLECTIONS FROM SERVICE ──────────────
    public ObservableCollection<FileExplorerItem> RootItems => _service.RootItems;
    public ObservableCollection<FileExplorerItem> SelectedItems => _service.SelectedItems;

    // ─── CONSTRUCTOR ───────────────────────────
    public FileExplorerViewModel(ILogger<FileExplorerViewModel> logger, IFileExplorerService service)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(service);
        _logger = logger;
        _service = service;

        _service.ItemCreated += (_, item) => SetStatus($"Created '{item.Name}'");
        _service.ItemRenamed += (_, item) => SetStatus($"Renamed to '{item.Name}'");
        _service.ItemDeleted += (_, path) => SetStatus($"Deleted '{Path.GetFileName(path)}'");
        _service.ExplorerRefreshed += OnExplorerRefreshed;
    }

    // ─── PUBLIC API ────────────────────────────
    public void LoadDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        _service.LoadDirectory(path);
        HasDirectory = true;
        SetStatus($"Loaded: {Path.GetFileName(path)}");
    }

    public void SelectItem(FileExplorerItem item, bool multiSelect)
    {
        if (!multiSelect)
        {
            foreach (var prev in SelectedItems.ToList())
                prev.IsSelected = false;
            
            SelectedItems.Clear();
        }

        if (!SelectedItems.Contains(item))
        {
            item.IsSelected = true;
            SelectedItems.Add(item);
        }

        ContextItem = item;
        SetStatus(BuildSelectionStatus());
    }

    public void HandleKeyboardShortcut(string key, bool ctrl)
    {
        if (ctrl)
        {
            switch (key)
            {
                case AppConstants.Service.FileExplorer.KeyboardShortcuts.CopyCommand: CopyCommand.Execute(null); break;
                case AppConstants.Service.FileExplorer.KeyboardShortcuts.CutCommand: CutCommand.Execute(null); break;
                case AppConstants.Service.FileExplorer.KeyboardShortcuts.PasteCommand: PasteCommand.Execute(null); break;
            }
        }
        else
        {
            switch (key)
            {
                case AppConstants.Service.FileExplorer.KeyboardShortcuts.DeleteCommand: DeleteMultipleCommand.Execute(null); break;
                case AppConstants.Service.FileExplorer.KeyboardShortcuts.RenameCommand: BeginRenameCommand.Execute(ContextItem); break;
                case AppConstants.Service.FileExplorer.KeyboardShortcuts.RefreshCommand: RefreshDirectoryCommand.Execute(null); break;
                case AppConstants.Service.FileExplorer.KeyboardShortcuts.CancelCommand: CancelCreateCommand.Execute(null); CancelRenameCommand.Execute(null); break;
            }
        }
    }

    public bool CanDrop(FileExplorerItem source, FileExplorerItem? target)
        => _service.CanDrop(source, target);
    
    public void Drop(FileExplorerItem source, FileExplorerItem? target)
        => _service.MoveItem(source, target);
    
    // ─── RELAY COMMANDS ────────────────────────
    [RelayCommand]
    private void BeginCreateFolder(FileExplorerItem? parent)
    {
        _pendingCreateParent = parent ?? ContextItem;
        _creatingFolder = true;
        IsCreatingFile = false;
        IsCreatingFolder = true;
        NewItemName = "New Folder";
    }

    [RelayCommand]
    private void BeginCreateFile(FileExplorerItem? parent)
    {
        _pendingCreateParent = parent ?? ContextItem;
        _creatingFolder = false;
        IsCreatingFolder = false;
        IsCreatingFile = true;
        NewItemName = "new-file.txt";
    }

    [RelayCommand]
    private void ConfirmCreate()
    {
        var name = NewItemName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            CancelCreate();
            return;
        }

        try
        {
            if (_creatingFolder)
                _service.CreateFolder(_pendingCreateParent, name);
            else
                _service.CreateFile(_pendingCreateParent, name);
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
        finally
        {
            CancelCreate();
        }
    }

    [RelayCommand]
    private void CancelCreate()
    {
        IsCreatingFolder = false;
        IsCreatingFile = false;
        NewItemName = string.Empty;
        _pendingCreateParent = null;
    }

    [RelayCommand]
    private void BeginRename(FileExplorerItem? item)
    {
        var target = item ?? ContextItem;
        if (target is null) return;

        _pendingRenameItem = target;
        RenameText = target.Name;
        target.IsEditing = true;
    }

    [RelayCommand]
    private void ConfirmRename()
    {
        if (_pendingRenameItem is null) return;

        try
        {
            _service.RenameItem(_pendingRenameItem, RenameText.Trim());
        }
        catch (Exception ex)
        {
            SetStatus($"Rename failed: {ex.Message}");
            _pendingRenameItem.IsEditing = false;
        }
        finally
        {
            _pendingRenameItem = null;
            RenameText = string.Empty;
        }
    }

    [RelayCommand]
    private void CancelRename()
    {
        if (_pendingRenameItem is not null)
            _pendingRenameItem.IsEditing = false;
        
        _pendingRenameItem = null;
        RenameText = string.Empty;
    }

    [RelayCommand]
    private void Copy()
    {
        if (SelectedItems.Count == 0) return;
        _service.ClipboardCopyItems(SelectedItems);
        SetStatus($"Copied {SelectedItems.Count} item(s).");
    }

    [RelayCommand]
    private void Cut()
    {
        if (SelectedItems.Count == 0) return;
        _service.ClipboardCutItems(SelectedItems);
        SetStatus($"Cut {SelectedItems.Count} item(s).");
    }

    [RelayCommand]
    private void Paste()
    {
        if (!_service.Clipboard.HasItems) return;
        var targetFolder = ResolveDropTarget(ContextItem);
        _service.ClipboardPasteItems(targetFolder);
    }

    [RelayCommand]
    private void DeleteItem(FileExplorerItem? item)
    {
        if (item is null) return;
        _service.DeleteItem(item);
    }

    [RelayCommand]
    private void DeleteMultiple()
    {
        if (SelectedItems.Count == 0) return;
        _service.DeleteMultipleItems(SelectedItems.ToList());
    }

    [RelayCommand]
    private void RefreshDirectory()
    {
        _service.RefreshDirectory();
        SetStatus("Refreshed directory.");
    }

    [RelayCommand]
    private void ExpandAll(FileExplorerItem? item)
    {
        if (item is null) return;
        _service.ExpandAll(item);
    }

    [RelayCommand]
    private void CollapseAll(FileExplorerItem? item)
    {
        if (item is null) return;
        _service.CollapseAll(item);
    }

    // ─── PRIVATE HELPERS ───────────────────────
    private void OnExplorerRefreshed(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            HasDirectory = _service.RootItems.Count > 0;
            SetStatus($"Refreshed — {CountItems()} item(s).");
        });
    }

    private static FileExplorerItem? ResolveDropTarget(FileExplorerItem? item)
        => item switch
        {
            null => null,
            {IsDirectory: true} => item,
            _ => item.Parent
        };
    
    private string BuildSelectionStatus()
    {
        return SelectedItems.Count switch
        {
            0 => string.Empty,
            1 => SelectedItems[0].Name,
            _ => $"{SelectedItems.Count} items selected",
        };
    }

    private int CountItems()
    {
        var count = 0;
        foreach (var root in _service.RootItems)
            count += CountRecursive(root);
        return count;
    }

    private static int CountRecursive(FileExplorerItem item)
    {
        var count = 1;
        foreach (var child in item.Children)
            count += CountRecursive(child);
        return count;
    }

    private void SetStatus(string message)
        => StatusMessage = message;
}