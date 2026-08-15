// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Enums;
using SimplyDraft.UI.Utils;

namespace SimplyDraft.UI.ViewModels.Components;

public sealed partial class LibraryBrowserViewModel : ObservableObject, IHoverTracker, IDisposable
{
    private readonly ILibrary _library;
    private readonly ILibraryWatcher _libraryWatcher;
    private readonly ILibraryPaths _libraryPaths;
    private List<LibraryItem> _all = [];
    public ObservableCollection<string> Sections {get;} = ["Templates", "Children"];
    public ObservableCollection<LibraryItemViewModel> Items {get;} = [];
    public LibraryItem? Target => (HoveredItem ?? SelectedItem)?.Item;
    public IReadOnlyList<LibraryItem> All => _all;

    [ObservableProperty]
    public partial string SelectedSection {get; set;}

    [ObservableProperty]
    public partial string SearchText {get; set;}

    [ObservableProperty]
    public partial LibraryItemViewModel? SelectedItem {get; set;}

    [ObservableProperty]
    public partial LibraryItemViewModel? HoveredItem {get; private set;}
    
    public event Action<string>? StatusReported;

    public LibraryBrowserViewModel(ILibrary library, ILibraryWatcher libraryWatcher, ILibraryPaths libraryPaths)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _libraryWatcher = libraryWatcher ?? throw new ArgumentNullException(nameof(libraryWatcher));
        _libraryPaths = libraryPaths ?? throw new ArgumentNullException(nameof(libraryPaths));
        
        SelectedSection = "Templates";
        SearchText = "";

        _libraryWatcher.Changed += OnWatcherChanged;
    }

    public void Refresh()
    {
        try
        {
            _all = _library.Scan();
            StatusReported?.Invoke($"{_libraryPaths.Root}  —  {_all.Count} item(s)");
        }
        catch (Exception ex)
        {
            _all = [];
            StatusReported?.Invoke("Could not read the library: " + ex.Message);
        }
        ApplyFilter();
    }

    public void RebuildAfterRelocation()
    {
        _libraryWatcher.Rebuild();
        Refresh();
    }

    public void SetHovered(object? item) => HoveredItem = item as LibraryItemViewModel;

    public void ClearHovered(object? item)
    {
        if (ReferenceEquals(HoveredItem, item))
            HoveredItem = null;
    }

    public void Dispose() => _libraryWatcher.Changed -= OnWatcherChanged;

    partial void OnSelectedSectionChanged(string value) => ApplyFilter();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void OnWatcherChanged() => DispatcherHelper.PostOnUIThread(Refresh);

    private void ApplyFilter()
    {
        var kind = SelectedSection == "Children"
            ? LibraryItemKind.Child
            : LibraryItemKind.Template;
        
        var selectedPath = SelectedItem?.Item.FilePath;
        IEnumerable<LibraryItem> q = _all.Where(i => i.Kind == kind);
        
        if (!string.IsNullOrWhiteSpace(SearchText))
            q = q.Where(i => i.Name.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase));
        
        Items.Clear();
        HoveredItem = null;

        foreach (var i in q)
            Items.Add(new LibraryItemViewModel(i));
        
        if (selectedPath != null)
            SelectedItem = Items.FirstOrDefault(v => v.Item.FilePath == selectedPath);
    }
}