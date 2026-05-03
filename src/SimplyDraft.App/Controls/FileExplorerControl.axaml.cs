using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SimplyDraft.App.Configuration;
using SimplyDraft.App.Models;
using SimplyDraft.App.ViewModels;

namespace SimplyDraft.App.Controls;

public partial class FileExplorerControl : UserControl
{
    // ─── DRAG STATE ────────────────────────────
    private FileExplorerItem? _dragSource;
    private Point _dragStartPoint;
    private bool _isDragging;
    
    // ─── AUTO-EXPAND STATE ─────────────────────
    private FileExplorerItem? _autoExpandTarget;
    private CancellationTokenSource? _autoExpandCts;

    // ─── DROP HIGHLIGHT TRACKING ───────────────
    private FileExplorerItem? _lastDragOverItem;
    
    // ─── KEYBOARD MODIFIER TRACKING ────────────
    private bool _isCtrlHeld;

    // ─── CONTROLS ──────────────────────────────
    private TreeView? _tree;

    // ─── CONSTRUCTOR ───────────────────────────
    public FileExplorerControl()
    {
        InitializeComponent();

        _tree = this.FindControl<TreeView>("ExplorerTree");

        if (_tree is null) return;

        _tree.SelectionChanged += OnTreeSelectionChanged;
        _tree.ContainerPrepared += OnContainerPrepared;

        DragDrop.SetAllowDrop(_tree, true);
        _tree.AddHandler(DragDrop.DragOverEvent, OnTreeDragOver);
        _tree.AddHandler(DragDrop.DropEvent, OnTreeDrop);
    }

    // ViewModel shortcut
    private FileExplorerViewModel? VM => DataContext as FileExplorerViewModel;

    // ─── PRIVATE METHODS ───────────────────────
    // Browse for root dir of File Explorer
    private async void OnBrowseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select root folder",
                AllowMultiple = false
            }
        );

        if (folders.Count > 0)
            VM?.LoadDirectory(folders[0].Path.LocalPath);
    }

    // Pressing Enter in the path box commits the typed path
    private void OnPathBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not TextBox tb) return;

        var path = tb.Text?.Trim();
        if (!string.IsNullOrEmpty(path))
            VM?.LoadDirectory(path);
        
        e.Handled = true;
    }

    // Container lifecycle
    private static void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is not TreeViewItem tvi) return;
        if (tvi.DataContext is FileExplorerItem item)
            tvi.IsExpanded = item.IsExpanded;
        
        tvi.PropertyChanged += (_, pe) =>
        {
            if (pe.Property == TreeViewItem.IsExpandedProperty && tvi.DataContext is FileExplorerItem model)
                model.IsExpanded = tvi.IsExpanded;
        };
    }

    // Selection
    private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (VM is null) return;

        var isMultiSelect = _isCtrlHeld;

        // Sync added items
        foreach (var added in e.AddedItems)
            if (added is FileExplorerItem item)
                VM.SelectItem(item, multiSelect:isMultiSelect);
        
        // Keep collection in sync when deselection
        foreach (var removed in e.RemovedItems)
            if (removed is FileExplorerItem item)
                VM.DeselectItem(item);
    }

    // Keyboard
    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        if (VM is null) return;

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        // Ctrl + SHIFT + N -> New folder
        if (ctrl && e.KeyModifiers.HasFlag(KeyModifiers.Shift) && e.Key == Key.N)
        {
            VM.BeginCreateFolderCommand.Execute(VM.ContextItem);
            e.Handled = true;
            return;
        }

        VM.HandleKeyboardShortcut(e.Key.ToString(), ctrl);

        e.Handled = e.Key is Key.Delete or Key.F2 or Key.F5 or Key.Escape ||
                    (ctrl && e.Key is Key.C or Key.X or Key.V);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _isCtrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        OnTreeKeyDown(this, e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        _isCtrlHeld = e.KeyModifiers.HasFlag(KeyModifiers.Control);
    }

    // Row pointer events
    private void OnRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed) return;
        if (sender is not Control{DataContext: FileExplorerItem item}) return;

        _dragSource = item;
        _dragStartPoint = e.GetPosition(this);
        _isDragging = false;
    }

    private async void OnRowPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragSource is null || _isDragging) return;
        if (!e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed)
        {
            ResetDragState();
            return;
        }

        var delta = e.GetPosition(this) - _dragStartPoint;
        if (Math.Abs(delta.X) < AppConstants.Control.FileExplorer.DragThreshold && Math.Abs(delta.Y) < AppConstants.Control.FileExplorer.DragThreshold)
            return;
        
        // Threshold exceeded - start dragging
        _isDragging = true;
        var source = _dragSource;

        // Capture pointer so subsequent move/release events are routed here regardless of where the pointer travels
        e.Pointer.Capture(sender as IInputElement);

        var data = new DataObject();
        data.Set("FileExplorerItem", source);

        await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);

        // Post-drop cleanup
        ClearDragOverHighlight();
        CancelAutoExpand();
        _isDragging = false;
        _dragSource = null;
    }

    private void OnRowPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);

        if (!_isDragging)
            _dragSource = null; // A click - selection, to be handled by TreeView
    }

    // Tree-level DragOver / Drop
    private void OnTreeDragOver(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains("FileExplorerItem"))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }
 
        var source     = e.Data.Get("FileExplorerItem") as FileExplorerItem;
        var hitItem    = HitTestItem(e);
        var dropTarget = ResolveDropFolder(hitItem);
 
        // Update per-row drag-over highlight.
        if (_lastDragOverItem != hitItem)
        {
            ClearDragOverHighlight();
            if (hitItem is not null)
            {
                hitItem.IsDragOver = true;
                _lastDragOverItem  = hitItem;
            }
        }
 
        // Auto-expand collapsed folders when the pointer lingers.
        if (dropTarget is { IsDirectory: true } && dropTarget != _autoExpandTarget)
            StartAutoExpand(dropTarget);
        else if (dropTarget is null || !dropTarget.IsDirectory)
            CancelAutoExpand();
 
        e.DragEffects = source is not null && VM?.CanDrop(source, dropTarget) == true
            ? DragDropEffects.Move
            : DragDropEffects.None;
 
        e.Handled = true;
    }

    private void OnTreeDrop(object? sender, DragEventArgs e)
    {
        ClearDragOverHighlight();
        CancelAutoExpand();

        if (!e.Data.Contains("FileExplorerItem")) return;

        var source = e.Data.Get("FileExplorerItem") as FileExplorerItem;
        var hitItem = HitTestItem(e);
        var dropTarget = ResolveDropFolder(hitItem);

        if (source is not null && VM?.CanDrop(source, dropTarget) == true)
            VM.Drop(source, dropTarget);
        
        e.Handled = true;
    }

    // Drag helpers
    // Walks the visual tree at the drag pointer position and returns the FileExplorerItem whose row is under the pointer, if any
    private FileExplorerItem? HitTestItem(DragEventArgs e)
    {
        if (_tree is null) return null;
        
        var hit = _tree.InputHitTest(e.GetPosition(_tree)) as Visual;

        while (hit is not null)
        {
            if (hit is Control{DataContext:FileExplorerItem item})
                return item;
            hit = hit.GetVisualParent();
        }
        return null;
    }

    private FileExplorerItem? HitTestItemFromPoint(Point position)
    {
        if (_tree is null) return null;
        var hit = _tree.InputHitTest(position) as Visual;

        while (hit is not null)
        {
            if (hit is Control{DataContext:FileExplorerItem item})
                return item;
            hit = hit.GetVisualParent();
        }
        return null;
    }

    private void OnScrollViewerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed) return;
        if (HitTestItemFromPoint(e.GetPosition(_tree)) is not null) return;
        if (_tree is not null)
            _tree.SelectedItem = null;
        VM?.ClearSelection();
        e.Handled = true;
    }

    // Resolves the folder that will receive the dropped item.
    // If folder, drop into it. If file, drop into parent folder. If null, drop at root
    private static FileExplorerItem? ResolveDropFolder(FileExplorerItem? item)
        => item switch
        {
            null => null,
            {IsDirectory:true} => item,
            _ => item.Parent
        };
    
    private void ClearDragOverHighlight()
    {
        if (_lastDragOverItem is not null)
        {
            _lastDragOverItem.IsDragOver = false;
            _lastDragOverItem = null;
        }
    }

    private void ResetDragState()
    {
        _dragSource = null;
        _isDragging = false;
        ClearDragOverHighlight();
        CancelAutoExpand();
    }

    // Auto-expand on hover
    private void StartAutoExpand(FileExplorerItem folder)
    {
        CancelAutoExpand();
        _autoExpandTarget = folder;
        _autoExpandCts = new CancellationTokenSource();
        var token = _autoExpandCts.Token;

        Task.Delay(AppConstants.Control.FileExplorer.AutoExpandDelayMs, token).ContinueWith(_ =>
        {
            if (token.IsCancellationRequested) return;

            Dispatcher.UIThread.Post(() =>
            {
                if (!token.IsCancellationRequested && _autoExpandTarget == folder)
                    folder.IsExpanded = true;
            });
        }, TaskScheduler.Default);
    }

    private void CancelAutoExpand()
    {
        _autoExpandCts?.Cancel();
        _autoExpandCts = null;
        _autoExpandTarget = null;
    }
}