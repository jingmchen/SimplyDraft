// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Common;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Editor;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Domains.Markup;
using SimplyDraft.Core.Domains.UI;
using SimplyDraft.Core.Domains.UI.Inputs;
using SimplyDraft.Core.Domains.UI.Outputs;
using SimplyDraft.Core.Enums;
using SimplyDraft.Engine.Templates;
using SimplyDraft.Engine.Utils;
using SimplyDraft.UI.Common;
using SimplyDraft.UI.ViewModels.Components;

namespace SimplyDraft.UI.ViewModels;

public sealed partial class EditorWindowViewModel : ObservableObject, IDisposable
{
    private const int PreviewDebounceMs = 300;
    private const double MinEditorFontSize = 9;
    private const double MaxEditorFontSize = 28;
    private const double DefaultEditorFontSize = 13;
    private readonly IScriptingEngine _scripting;
    private readonly IMarkupEngine _markup;
    private readonly IExportService _exportService;
    private readonly ILibrary _library;
    private readonly IDialogService _dialog;
    private readonly IAppSettingsProvider _settings;
    private readonly ILogger<EditorWindowViewModel> _logger;
    private readonly VariableRowSet _rows = new();
    private readonly PreviewScheduler<EditorGenerationInput, EditorGenerationOutput> _preview;
    private FrontMatter _fm = new();
    private string _childBody = "";
    private bool _loading = true;
    private string FullTemplateBody => IsTemplate ? BodySplitter.Join(ScriptText, ContentText) : _childBody;
    private static readonly Regex LatexLikeCommand = new(@"\\[a-zA-Z]+", RegexOptions.Compiled);
    public LibraryItem Item {get; private set;} = null!;
    public bool IsTemplate {get; private set;}
    public bool CanExport => !IsTemplate;
    public bool ShowDeclarations => IsTemplate;
    public ObservableCollection<VariableRowViewModel> Variables => _rows.Rows;
    public bool ShowPagePreview => PreviewPageView && MarkupOn;
    public int DocxFontSizePt => PagePreview.DocxPointSize(_fm.DocxSizePt);
    public string PreviewFontFamily => PagePreview.FontFamily(PreviewPageView, _fm.DocxFont);
    public double PreviewFontSize => PagePreview.FontSize(PreviewPageView, _fm.DocxSizePt);
    public double PreviewMaxWidth => PagePreview.MaxWidth(PreviewPageView);
    public string WindowTitle => (Dirty ? "● " : "") + Item.Name + " — SimplyDraft";
    public ObservableCollection<string> Diagnostics {get;} = [];

    [ObservableProperty]
    public partial string ScriptText {get; set;}

    [ObservableProperty]
    public partial string ContentText {get; set;}

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPagePreview))]
    public partial bool MarkupOn {get; set;}

    [ObservableProperty]
    public partial VariableRowViewModel? SelectedVariable {get; set;}

    [ObservableProperty]
    public partial string PreviewText {get; set;}

    [ObservableProperty]
    public partial MarkupDocument? PreviewDocument {get; set;}

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewFontFamily))]
    [NotifyPropertyChangedFor(nameof(PreviewFontSize))]
    [NotifyPropertyChangedFor(nameof(PreviewMaxWidth))]
    [NotifyPropertyChangedFor(nameof(ShowPagePreview))]
    public partial bool PreviewPageView {get; set;}

    [ObservableProperty]
    public partial string StatusText {get; set;}

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    public partial bool Dirty {get; private set;}

    [ObservableProperty]
    public partial double EditorFontSize {get; private set;}

    public EditorWindowViewModel(
        IScriptingEngine scripting,
        IMarkupEngine markup,
        IExportService exportService,
        ILibrary library,
        IDialogService dialog,
        IAppSettingsProvider settings,
        ILogger<EditorWindowViewModel> logger)
    {
        _scripting = scripting ?? throw new ArgumentNullException(nameof(scripting));
        _markup = markup ?? throw new ArgumentNullException(nameof(markup));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ScriptText = "";
        ContentText = "";
        PreviewText = "";
        StatusText = "";
        PreviewPageView = true;
        EditorFontSize = DefaultEditorFontSize;

        _rows.ValueChanged += OnRowValueChanged;
        _preview = new PreviewScheduler<EditorGenerationInput, EditorGenerationOutput>(
            CreatePreviewSnapshot, ComputeGeneration, ApplyPreview, PreviewDebounceMs, OnPreviewError);
    }

    public void Load(LibraryItem item)
    {
        Item = item;
        IsTemplate = item.Kind == LibraryItemKind.Template;
        _loading = true;

        try
        {
            if (IsTemplate)
                LoadTemplateItem();
            else
                LoadChildItem();
            _rows.ApplyTypes(_fm.Types);
        }
        catch (Exception ex)
        {
            StatusText = "Load failed: " + ex.Message;
        }
        finally
        {
            _loading = false;
        }
        _preview.RunNow();
    }

    public void Dispose()
    {
        _rows.ValueChanged -= OnRowValueChanged;
        _preview.Dispose();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (IsTemplate)
            {
                _fm.Variables.Clear();
                
                foreach (var r in Variables)
                    _fm.Variables[r.Name] = "";
                
                _library.SaveTemplate(new TemplateDocument
                {
                    FilePath = Item.FilePath,
                    Fm = _fm,
                    Body = FullTemplateBody
                });
            }
            else
            {
                _library.SaveChild(new ChildDocument
                {
                    FilePath = Item.FilePath,
                    Fm = _fm,
                    Body = _childBody
                });
            }

            Dirty = false;
            var dateTime = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            StatusText = $"Saved at {dateTime}";
            LogSavedSuccess(dateTime);
        }
        catch (Exception ex)
        {
            StatusText = "Save failed: " + ex.Message;
            LogSavedFailed(ex, Item.Name);
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportTxtAsync() => await ExportAsync(DocumentKind.Txt);

    [RelayCommand]
    private async Task ExportDocxAsync() => await ExportAsync(DocumentKind.Docx);

    [RelayCommand]
    private async Task OpenVariablesAsync()
    {
        if (!IsTemplate)
            return;
        
        var rows = Variables
            .Select(v => new VariableDeclaration(v.Name, v.TypeName))
            .ToList();
        
        var result = await _dialog.EditVariablesAsync(rows);
        
        if (result is null)
            return;

        _rows.Clear();
        _fm.Types.Clear();

        foreach (var r in result)
        {
            var row = new VariableRowViewModel(r.Name, "", isImplicit: false) { TypeName = r.Type };
            _rows.Add(row);
            if (r.Type is not ("text" or ""))
                _fm.Types[r.Name] = r.Type;
        }
        
        MarkDirty();
        _preview.Schedule();
        StatusText = "Variable declarations updated.";
    }

    [RelayCommand]
    private async Task AddVariableAsync()
    {
        if (!IsTemplate)
            return;
        
        var name = await _dialog.PromptAsync("Add variable", "Variable name (letters, digits, underscore):");
        
        if (string.IsNullOrWhiteSpace(name))
            return;
        
        name = name.Trim();

        if (!IsValidVarName(name))
        {
            StatusText = $"'{name}' is not a valid variable name.";
            return;
        }

        if (_rows.Has(name))
        {
            SelectedVariable = _rows.Find(name);
            return;
        }

        _rows.Add(new VariableRowViewModel(name, "", isImplicit: false));
        MarkDirty();
        _preview.Schedule();
    }

    [RelayCommand]
    private async Task RemoveVariableAsync()
    {
        if (!IsTemplate || SelectedVariable is null)
            return;
        
        var row = SelectedVariable;
        var refRegex = PlaceholderRefRegex(row.Name);
        int uses = refRegex.Count(ContentText);
        bool stripped = false;

        if (uses > 0)
        {
            var choice = await _dialog.ChooseAsync("Remove variable",
                $"'{row.Name}' is used by {{{row.Name}}} {uses} time(s) in the content.\n\n" +
                "Remove the declaration and delete those placeholders too?",
                "Cancel", "Keep placeholders", "Remove everywhere");
            
            if (choice == 0)
                return;
            if (choice == 2)
            {
                ContentText = refRegex.Replace(ContentText, "");
                stripped = true;
            }
        }

        var found = _rows.Find(row.Name);

        if (found != null)
        {
            int idx = Variables.IndexOf(found);
            if (idx >= 0)
                _rows.RemoveAt(idx);
        }

        _fm.Types.Remove(row.Name);
        MarkDirty();
        _preview.Schedule();

        StatusText = stripped
            ? $"Removed '{row.Name}' and {uses} placeholder(s) from the content."
            : uses > 0
                ? $"Removed '{row.Name}' — its {{{row.Name}}} placeholders remain, so it stays listed (marked *)."
                : $"Removed '{row.Name}'.";
    }

    [RelayCommand]
    private void ZoomEditor(double delta)
        => EditorFontSize = Math.Clamp(EditorFontSize + Math.Sign(delta), MinEditorFontSize, MaxEditorFontSize);

    private void LoadTemplateItem()
    {
        var t = _library.LoadTemplate(Item.FilePath);
        _fm = t.Fm;
        var (script, content) = BodySplitter.Split(t.Body);
        ScriptText = script;
        ContentText = content;
        MarkupOn = _fm.HasMarkup;

        foreach (var kv in _fm.Variables)
            _rows.Add(new VariableRowViewModel(kv.Key, "", isImplicit: false));
        
        foreach (var name in TemplateParser.ScanUserVariables(FullTemplateBody))
            if (!_rows.Has(name))
                _rows.Add(new VariableRowViewModel(name, "", isImplicit: true));
        
        if (_fm.Variables.Values.Any(v => !string.IsNullOrEmpty(v)))
            StatusText = "Note: default values found in this file are ignored — templates are abstract.";
    }

    private void LoadChildItem()
    {
        var c = _library.LoadChild(Item.FilePath);
        _fm = c.Fm;
        _childBody = c.Body;
        ScriptText = "";
        ContentText = c.Body;
        MarkupOn = c.Fm.HasMarkup;
        StatusText = c.IsBaked
            ? "Generated child — content is final; use File ▸ Export."
            : "Unknown or Errored Type. Generate a new child from its template (main window: File ▸ New Child).";
    }

    partial void OnScriptTextChanged(string value)
    {
        if (_loading || !IsTemplate)
            return;
        MarkDirty();
        _preview.Schedule();
    }

    partial void OnContentTextChanged(string value)
    {
        if (_loading || !IsTemplate)
            return;
        MarkDirty();
        _preview.Schedule();
    }

    partial void OnMarkupOnChanged(bool value)
    {
        if (_loading || !IsTemplate)
            return;
        _fm.HasMarkup = value;
        MarkDirty();
        _preview.Schedule();
    }

    partial void OnPreviewPageViewChanged(bool value)
    {
        if (_loading)
            return;
        _preview.RunNow();
    }

    private void OnRowValueChanged()
    {
        if (_loading)
            return;
        MarkDirty();
        _preview.Schedule();
    }

    private void MarkDirty() => Dirty = true;
    
    private EditorGenerationInput CreatePreviewSnapshot() => CreateSnapshot(GenerationMode.Preview);

    private EditorGenerationInput CreateSnapshot(GenerationMode mode)
    {
        var (created, modified) = _library.GetTimestamps(Item.FilePath);
        var doc = new DocInfo(
            Item.Name,
            IsTemplate ? Item.Name : (string.IsNullOrWhiteSpace(_fm.Name) ? (Item.TemplateRef ?? "") : _fm.Name!),
            created,
            modified);
        
        if (IsTemplate)
        {
            return new EditorGenerationInput(
                true,
                FullTemplateBody,
                doc,
                _settings.Current.GenerationSection.Policy,
                mode, _settings.Current.GenerationSection.Culture,
                new Dictionary<string, string>(_fm.Types, StringComparer.OrdinalIgnoreCase),
                ExpandIncludes: true,
                PreviewInputs: Variables.Select(r => r.Name).ToArray());
        }

        return new EditorGenerationInput(
            false,
            _childBody,
            doc,
            _settings.Current.GenerationSection.Policy,
            mode,
            _settings.Current.GenerationSection.Culture,
            new Dictionary<string, string>(_fm.Types, StringComparer.OrdinalIgnoreCase),
            ExpandIncludes: false);
    }

    private EditorGenerationOutput ComputeGeneration(EditorGenerationInput input)
    {
        string body = input.TemplateBody;
        List<Diagnostic>? incWarns = null;

        if (input.ExpandIncludes)
        {
            var (script, content) = BodySplitter.Split(body);
            (var expanded, incWarns) = _library.ExpandIncludes(content);
            body = BodySplitter.Join(script, expanded);
        }

        var gen = _scripting.Run(new GenerationRequest
        {
            TemplateBody = body,
            Doc = input.Doc,
            Policy = input.Policy,
            Mode = input.Mode,
            FormatCulture = input.Culture,
            VariableTypes = input.Types,
            PreviewInputs = input.PreviewInputs
        });

        if (incWarns is {Count: > 0})
            gen.Diagnostics.InsertRange(0, incWarns);
        
        var scanned = input.IsTemplate
            ? TemplateParser.ScanUserVariables(body)
            : [];
        
        return new EditorGenerationOutput(gen, scanned);
    }

    private void ApplyPreview(EditorGenerationOutput output)
    {
        Diagnostics.Clear();
        
        foreach (var d in output.Result.Diagnostics)
            Diagnostics.Add(d.ToString());
        
        if (MarkupOn)
        {
            var markup = _markup.Run(output.Result.Text, wrap: !PreviewPageView);

            foreach (var w in markup.Document.Warnings)
                Diagnostics.Add(w.ToString());
            
            PreviewText = markup.Rendered;
            PreviewDocument = markup.Document;
        }
        else
        {
            PreviewText = output.Result.Text;
            PreviewDocument = null;

            if (IsTemplate && LatexLikeCommand.IsMatch(output.Result.Text))
                Diagnostics.Add("hint: LaTeX commands found but markup is OFF — tick \"LaTeX markup\" above the preview to render them");
        }

        if (IsTemplate)
            _rows.ReconcileImplicit(output.ScannedVariables);
    }

    private void OnPreviewError(Exception ex) => StatusText = "Preview failed: " + ex.Message;

    private async Task ExportAsync(DocumentKind formatKind)
    {
        if (IsTemplate)
        {
            StatusText = "Templates are abstract — generate a child first (main window: File ▸ New Child).";
            return;
        }
        
        try
        {
            var output = ComputeGeneration(CreateSnapshot(GenerationMode.Export));
            var path = await _exportService.ExportAsync(Item.Name, formatKind, output.Result, _fm,
                Path.GetDirectoryName(Item.FilePath));
            if (path != null)
                StatusText = "Exported to " + path;
        }
        catch (Exception ex)
        {
            StatusText = "Export failed: " + ex.Message;
            LogExportFailed(ex, Item.Name);
        }
    }

    private static bool IsValidVarName(string name) => VariableNameChecker.IsValid(name);

    private static Regex PlaceholderRefRegex(string name)
        => new($@"(?<!\{{)\{{{Regex.Escape(name)}\}}(?!\}})", RegexOptions.IgnoreCase);
    
    [LoggerMessage(
        EventId = 9001,
        Level = LogLevel.Information,
        Message = "Saved at {DateTime}.")]
    private partial void LogSavedSuccess(string dateTime);

    [LoggerMessage(
        EventId = 9002,
        Level = LogLevel.Error,
        Message = "Save failed for \"{itemName}\"")]
    private partial void LogSavedFailed(Exception ex, string itemName);

    [LoggerMessage(
        EventId = 9003,
        Level = LogLevel.Error,
        Message = "Export failed for \"{itemName}\"")]
    private partial void LogExportFailed(Exception ex, string itemName);
}