namespace SimplyDraft.App.Models;

public enum ClipboardOperation {None, Copy, Cut}

public sealed class FileExplorerClipboard
{
    public ClipboardOperation Operation {get; private set;} = ClipboardOperation.None;
    private readonly List<FileExplorerItem> _items = new();
    public IReadOnlyList<FileExplorerItem> Items => _items;
    public bool HasItems => _items.Count > 0 && Operation != ClipboardOperation.None;
    
    // Copies given items onto the clipboard
    public void SetCopy(IEnumerable<FileExplorerItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        SetNone();
        Operation = ClipboardOperation.Copy;
        _items.AddRange(items);
        foreach (var item in _items)
            item.IsCut = false;
    }

    // Cuts given items, marking them visually as pending move
    public void SetCut(IEnumerable<FileExplorerItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        SetNone();
        Operation = ClipboardOperation.Cut;
        _items.AddRange(items);
        foreach (var item in _items)
            item.IsCut = true;
    }

    // Clears clipboard
    public void SetNone()
    {
        foreach (var item in _items)
            item.IsCut = false;
        _items.Clear();
        Operation = ClipboardOperation.None;
    }

    // Returns snapshot of current items (safe to iterate while mutating)
    public List<FileExplorerItem> Snapshot()
        => _items.ToList();
}