using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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

    // ─── CONTROLS ──────────────────────────────
    private TreeView? _tree;

    // ─── CONSTRUCTOR ───────────────────────────
    public FileExplorerControl()
    {
        InitializeComponent();

        _tree = this.FindControl<TreeView>("ExplorerTree");

        if (_tree is null) return;

        _tree.SelectionChanged += OnTreeSelectionChanged;
    }

    // ViewModel shortcut
    private FileExplorerViewModel? VM => DataContext as FileExplorerViewModel;

    // ─── PRIVATE METHODS ───────────────────────
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

        foreach (var added in e.AddedItems)
        {
            if (added is FileExplorerItem item)
                VM.SelectItem(item, multiSelect:false);
        }
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
        OnTreeKeyDown(this, e);
    }

    // Row pointer events
    private void OnRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(sender as Visual);
        if (!point.Properties.IsLeftButtonPressed) return;
        if (sender is not Control{DataContext: FileExplorerItem item}) return;

        _dragSource = item;
        _dragStartPoint = e.GetPosition(this);
        _isDragging = false;

        e.Pointer.Capture(sender as IInputElement);
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