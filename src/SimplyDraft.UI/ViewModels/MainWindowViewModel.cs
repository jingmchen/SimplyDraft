// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.UI.ViewModels.Components;
using CommunityToolkit.Mvvm.Input;

namespace SimplyDraft.UI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly ILibraryActions _libraryActions;
    private readonly IConsoleLogService _console;
    private readonly IWindowService _window;
    private readonly ILogger<MainWindowViewModel> _logger;
    public LibraryBrowserViewModel Library {get;}
    public ObservableCollection<string> ConsoleEntries {get;}

    [ObservableProperty]
    public partial string StatusText {get; set;}

    [ObservableProperty]
    public partial bool ConsoleVisible {get; set;}

    public MainWindowViewModel(
        LibraryBrowserViewModel library,
        ILibraryActions libraryActions,
        IConsoleLogService console,
        IWindowService window,
        ILogger<MainWindowViewModel> logger)
    {
        Library = library ?? throw new ArgumentNullException(nameof(library));
        _libraryActions = libraryActions ?? throw new ArgumentNullException(nameof(libraryActions));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ConsoleEntries = _console.Entries;
        StatusText = "";
        ConsoleVisible = true;

        Library.StatusReported += OnBrowserStatus;
        _libraryActions.StatusReported += OnActionStatus;
        _libraryActions.Changed += Library.Refresh;
        _libraryActions.SectionRequested += OnSectionRequested;

        Library.Refresh();
    }

    public void ClearConsole() => _console.Clear();
    public void OpenSelected() => _libraryActions.Open(Library.Target);
    public void Dispose()
    {
        Library.StatusReported -= OnBrowserStatus;
        _libraryActions.StatusReported -= OnActionStatus;
        _libraryActions.Changed -= Library.Refresh;
        _libraryActions.SectionRequested -= OnSectionRequested;
        Library.Dispose();
    }

    [RelayCommand]
    private Task NewTemplateAsync() => _libraryActions.NewTemplateAsync();

    [RelayCommand]
    private async Task NewChildAsync() => await _libraryActions.NewChildAsync(Library.Target);

    [RelayCommand]
    private void Open() => _libraryActions.Open(Library.Target);

    [RelayCommand]
    private async Task DuplicateAsync() => await _libraryActions.DuplicateAsync(Library.Target);

    [RelayCommand]
    private async Task RenameAsync() => await _libraryActions.RenameAsync(Library.Target);

    [RelayCommand]
    private async Task DeleteAsync() => await _libraryActions.DeleteAsync(Library.Target);

    [RelayCommand]
    private async Task ExportAsync() => await _libraryActions.ExportAsync(Library.Target);

    [RelayCommand]
    private void Reveal() => _libraryActions.Reveal(Library.Target);

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var saved = await _window.OpenSettingsAsync();
        if (saved)
            Library.RebuildAfterRelocation();
    }
    
    [RelayCommand(CanExecute = nameof(CanTrashDrop))]
    private async Task TrashDroppedAsync(string? path)
    {
        if (path is null)
            return;
        
        var item = Library.All.FirstOrDefault(i => i.FilePath == path);
        
        if (item != null)
            await _libraryActions.MoveToTrashAsync(item);
    }

    private bool CanTrashDrop(string? path)
        => path != null && Library.All.Any(i => i.FilePath == path);

    private void OnBrowserStatus(string s) => StatusText = s;
    private void OnActionStatus(string s) => StatusText = s;
    private void OnSectionRequested(string section) => Library.SelectedSection = section;
}