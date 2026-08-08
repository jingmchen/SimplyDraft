// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.UI.Common.MVVM;
using SimplyDraft.UI.ViewModels.Components;

namespace SimplyDraft.UI.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
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

    public ICommand NewTemplateCommand {get;}
    public ICommand NewChildCommand {get;}
    public ICommand OpenCommand {get;}
    public ICommand DuplicateCommand {get;}
    public ICommand RenameCommand {get;}
    public ICommand DeleteCommand {get;}
    public ICommand ExportCommand {get;}
    public ICommand BatchCommand {get;}
    public ICommand RevealCommand {get;}
    public ICommand SettingsCommand {get;}
    public ICommand TrashDroppedCommand {get;}

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

        NewTemplateCommand = new RelayCommandAsync(_libraryActions.NewTemplateAsync, logger: _logger);
        NewChildCommand = new RelayCommandAsync(NewChildAsync, logger: _logger);
        OpenCommand = new RelayCommand(OpenTarget, logger: _logger);
        DuplicateCommand = new RelayCommand(DuplicateTarget, logger: _logger);
        RenameCommand = new RelayCommandAsync(RenameAsync, logger: _logger);
        DeleteCommand = new RelayCommandAsync(DeleteAsync, logger: _logger);
        ExportCommand = new RelayCommandAsync(ExportAsync, logger: _logger);
        BatchCommand = new RelayCommandAsync(BatchAsync, logger: _logger);
        RevealCommand = new RelayCommand(RevealTarget, logger: _logger);
        SettingsCommand = new RelayCommandAsync(OpenSettingsAsync, logger: _logger);
        TrashDroppedCommand = new RelayCommand<string?>(TrashDropped, CanTrashDrop, logger: _logger);

        Library.Refresh();
    }

    // ─── PUBLIC METHODS ────────────────────────
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

    // ─── PRIVATE METHODS ───────────────────────
    private void OnBrowserStatus(string s) => StatusText = s;

    private void OnActionStatus(string s) => StatusText = s;

    private void OnSectionRequested(string section) => Library.SelectedSection = section;

    private Task NewChildAsync() => _libraryActions.NewChildAsync(Library.Target);

    private void OpenTarget() => _libraryActions.Open(Library.Target);

    private void DuplicateTarget() => _libraryActions.Duplicate(Library.Target);

    private Task RenameAsync() => _libraryActions.RenameAsync(Library.Target);

    private Task DeleteAsync() => _libraryActions.DeleteAsync(Library.Target);

    private Task ExportAsync() => _libraryActions.ExportAsync(Library.Target);

    private Task BatchAsync() => _libraryActions.BatchAsync(Library.Target);

    private void RevealTarget() => _libraryActions.Reveal(Library.Target);

    private bool CanTrashDrop(string? path)
        => path != null && Library.All.Any(i => i.FilePath == path);

    private void TrashDropped(string? path)
    {
        if (path is null)
            return;
        
        var item = Library.All.FirstOrDefault(i => i.FilePath == path);
        
        if (item != null)
            _libraryActions.MoveToTrash(item);
    }

    private async Task OpenSettingsAsync()
    {
        var saved = await _window.OpenSettingsAsync();
        if (saved)
            Library.RebuildAfterRelocation(); // relocation swapped the Library instance — watch the new one + rescan
    }
}