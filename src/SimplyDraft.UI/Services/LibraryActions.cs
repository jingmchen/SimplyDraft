// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Enums;
using SimplyDraft.UI.Common;

namespace SimplyDraft.UI.Services;

public sealed partial class LibraryActions : ILibraryActions
{
    private readonly IExportService _exportService;
    private readonly ILibrary _library;
    private readonly IDialogService _dialog;
    private readonly IWindowService _window;
    private readonly IAppSettingsProvider _settings;
    private readonly ILogger<LibraryActions> _logger;
    public event Action? Changed;
    public event Action<string>? StatusReported;
    public event Action<string>? SectionRequested;

    public LibraryActions(
        IExportService exportService,
        ILibrary library,
        IDialogService dialog,
        IWindowService window,
        IAppSettingsProvider settings,
        ILogger<LibraryActions> logger)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ─── PUBLIC METHODS ────────────────────────
    public async Task NewTemplateAsync()
    {
        try
        {
            var selection = await _dialog.OpenNewTemplateAsync(_library.ListSeedTemplates());

            if (selection is null)
                return;
            
            var name = selection.Name.Trim();
            
            if (name.Length == 0)
                return;
            
            var path = selection.SeedTemplateName is null
                ? _library.CreateTemplate(name)
                : _library.CreateTemplateFromSeed(selection.SeedTemplateName, name);
            
            Changed?.Invoke();
            SectionRequested?.Invoke("Templates");

            _window.OpenEditor(ItemFor(path, LibraryItemKind.Template, name, null));

            StatusReported?.Invoke(selection.SeedTemplateName is null
                ? $"Created \"{name}\"."
                : $"Created \"{name}\" from the \"{selection.SeedTemplateName}\" example.");
        }
        catch (Exception ex)
        {
            StatusReported?.Invoke("Couldn't create the template: " + ex.Message);
        }
    }

    public async Task NewChildAsync(LibraryItem? target)
    {
        try
        {
            var all = _library.Scan();

            LibraryItem? tpl = target is { Kind: LibraryItemKind.Template } t
                ? t
                : await _dialog.PickTemplateAsync(all.Where(i => i.Kind == LibraryItemKind.Template));
            
            if (tpl is null)
            {
                if (!all.Any(i => i.Kind == LibraryItemKind.Template))
                    StatusReported?.Invoke("Create a template first — children are generated from templates.");
                return;
            }

            var name = await _dialog.PromptAsync(
                "New child",
                $"Name for the new child of \"{tpl.Name}\":",
                tpl.Name + " — child");
            
            if (string.IsNullOrWhiteSpace(name))
                return;
            
            var template = _library.LoadTemplate(tpl.FilePath);

            var path = await _window.OpenGenerateChildAsync(template, name.Trim());

            if (path is null)
                return;
            
            var listed = _library.Scan()
                .FirstOrDefault(i => string.Equals(i.FilePath, path, StringComparison.OrdinalIgnoreCase));

            if (listed is null)
                LogGeneratedChildNotScanned(path);
            else if (listed.Kind != LibraryItemKind.Child)
                LogGeneratedChildWrongKind(path, listed.Kind);

            Changed?.Invoke();
            SectionRequested?.Invoke("Children");

            _window.OpenEditor(ItemFor(path, LibraryItemKind.Child, name.Trim(), tpl.Name));

            StatusReported?.Invoke($"Generated \"{name.Trim()}\" — pure content, ready to export.");
        }
        catch (Exception ex)
        {
            StatusReported?.Invoke("Generate child failed: " + ex.Message);
        }
    }

    public void Open(LibraryItem? item)
    {
        if (item is null)
        {
            StatusReported?.Invoke("Select an item first.");
            return;
        }

        try { _window.OpenEditor(item); }
        catch (Exception ex) { StatusReported?.Invoke($"Couldn't open \"{item.Name}\": {ex.Message}"); }
    }

    public void Duplicate(LibraryItem? item)
    {
        if (item is null)
        {
            StatusReported?.Invoke("Select an item first.");
            return;
        }

        try { _library.Duplicate(item); Changed?.Invoke(); }
        catch (Exception ex) { StatusReported?.Invoke("Duplicate failed: " + ex.Message); }
    }

    public async Task RenameAsync(LibraryItem? item)
    {
        if (item is null)
        {
            StatusReported?.Invoke("Select an item first.");
            return;
        }

        var name = await _dialog.PromptAsync("Rename", "New name:", item.Name);

        if (string.IsNullOrWhiteSpace(name) || name.Trim() == item.Name)
            return;
        
        try { _library.Rename(item, name.Trim()); Changed?.Invoke(); }
        catch (Exception ex) { StatusReported?.Invoke("Rename failed: " + ex.Message); }
    }

    public async Task DeleteAsync(LibraryItem? item)
    {
        if (item is null)
        {
            StatusReported?.Invoke("Select an item first.");
            return;
        }

        var idx = await _dialog.ChooseAsync("Delete",
            $"Move \"{item.Name}\" to the library trash?\n\n(Items are kept in .trash for {_settings.Current.LibrarySection.TrashPurgeDays} days.)",
            "Cancel", "Delete");
        
        if (idx != 1)
            return;
        
        try { _library.MoveToTrash(item); Changed?.Invoke(); }
        catch (Exception ex) { StatusReported?.Invoke("Delete failed: " + ex.Message); }
    }

    public void MoveToTrash(LibraryItem item)
    {
        try
        {
            _library.MoveToTrash(item);
            Changed?.Invoke();
            StatusReported?.Invoke($"Moved \"{item.Name}\" to trash  ·  drag-drop");
        }
        catch (Exception ex)
        {
            StatusReported?.Invoke("Delete failed: " + ex.Message);
        }
    }

    public async Task ExportAsync(LibraryItem? item)
    {
        if (item is null)
        {
            StatusReported?.Invoke("Select an item first.");
            return;
        }

        if (item.Kind == LibraryItemKind.Template)
        {
            StatusReported?.Invoke("Templates are abstract — generate a child first (File ▸ New Child).");
            return;
        }

        try
        {
            var (gen, fm, name) = _exportService.GenerateItem(item, GenerationMode.Export);
            var path = await _exportService.ExportAsync(name, _settings.Current.ExportSection.DefaultFormat, gen, fm,
                Path.GetDirectoryName(item.FilePath));
            if (path != null)
                StatusReported?.Invoke("Exported to " + path);
        }
        catch (Exception ex)
        {
            StatusReported?.Invoke($"Export failed for \"{item.Name}\": {ex.Message}");
        }
    }

    public async Task BatchAsync(LibraryItem? item)
    {
        if (item is null || item.Kind != LibraryItemKind.Template)
        {
            StatusReported?.Invoke("Select a template to run batch generation.");
            return;
        }
        try
        {
            var tpl = _library.LoadTemplate(item.FilePath);
            await _window.OpenBatchAsync(tpl);
            Changed?.Invoke();
        }
        catch (Exception ex)
        {
            StatusReported?.Invoke("Batch generate failed: " + ex.Message);
        }
    }

    public void Reveal(LibraryItem? item)
    {
        if (item is null)
        {
            StatusReported?.Invoke("Select an item first.");
            return;
        }
        
        try { FileRevealer.Reveal(item.FilePath); }
        catch (Exception ex) { StatusReported?.Invoke("Couldn't reveal the file: " + ex.Message); }
    }

    // ─── PRIVATE METHODS ───────────────────────
    private LibraryItem ItemFor(string path, LibraryItemKind kind, string name, string? tref)
        => new(path, kind, name, tref, _library.GetTimestamps(path).Modified, false);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Warning,
        Message = "Generated child was written to {Path} but Library.Scan().")]
    private partial void LogGeneratedChildNotScanned(string path);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "Generated child at {Path} is scanned but classified as {Kind} instead of Child — it will not appear under the Children section.")]
    private partial void LogGeneratedChildWrongKind(string path, LibraryItemKind kind);
}