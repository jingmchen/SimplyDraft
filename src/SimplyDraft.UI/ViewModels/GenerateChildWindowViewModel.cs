// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Configuration.AppSettings;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Editor;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Domains.Markup;
using SimplyDraft.Core.Domains.UI.Inputs;
using SimplyDraft.Core.Domains.UI.Outputs;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Logging;
using SimplyDraft.Engine.Scripting;
using SimplyDraft.Engine.Templates;
using SimplyDraft.Engine.Utils;
using SimplyDraft.UI.Common;
using SimplyDraft.UI.ViewModels.Components;

namespace SimplyDraft.UI.ViewModels;

public sealed partial class GenerateChildWindowViewModel : ObservableObject, IDisposable
{
    private const int PreviewDebounceMs = 300;
    private readonly IScriptingEngine _scripting;
    private readonly IMarkupEngine _markup;
    private readonly ILibrary _library;
    private readonly IDialogService _dialog;
    private readonly ISettingsProvider<AppSettings> _settings;
    private readonly ILogger<GenerateChildWindowViewModel> _logger;
    private PreviewScheduler<ChildGenerationInput, ChildGenerationOutput>? _preview;
    private string _contentPart = "";
    private bool _loading = true; // true until Load() settles - mutes the change handlers
    private TemplateDocument _template = null!;
    private readonly VariableRowSet _rows = new();
    private readonly Dictionary<string, string> _scenarioValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _savedInputs = new(StringComparer.OrdinalIgnoreCase);
    private List<Diagnostic> _includeWarnings = [];

    public string ChildName {get; private set;} = "";
    public string TemplateName => _template.DisplayName;
    public string WindowTitle => $"New child — {ChildName} (from {TemplateName})";
    public string PreviewFontFamily => PagePreview.FontFamily(PreviewPageView, _template.Fm.DocxFont);
    public double PreviewFontSize => PagePreview.FontSize(PreviewPageView, _template.Fm.DocxSizePt);
    public double PreviewMaxWidth => PagePreview.MaxWidth(PreviewPageView);
    public bool ShowPagePreview => PreviewPageView && _template.Fm.HasMarkup;
    public ObservableCollection<ScenarioViewModel> Scenarios {get;} = [];
    public ObservableCollection<VariableRowViewModel> Variables => _rows.Rows;
    public ObservableCollection<string> Diagnostics {get;} = [];

    [ObservableProperty]
    public partial string ScriptText {get; set;}

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
    public partial bool HasScenarios {get; private set;}

    [ObservableProperty]
    public partial VariableRowViewModel? SelectedVariable {get; set;}

    [ObservableProperty]
    public partial bool HasDiagnostics {get; private set;}

    [ObservableProperty]
    public partial string StatusText {get; set;}

    public GenerateChildWindowViewModel(
        IScriptingEngine scripting,
        IMarkupEngine markup,
        ILibrary library,
        IDialogService dialog,
        ISettingsProvider<AppSettings> settings,
        ILogger<GenerateChildWindowViewModel> logger)
    {
        _scripting = scripting ?? throw new ArgumentNullException(nameof(scripting));
        _markup = markup ?? throw new ArgumentNullException(nameof(markup));
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ScriptText = "";
        PreviewText = "";
        StatusText = "";
        PreviewPageView = true;

        _rows.ValueChanged += OnRowValueChanged;
    }

    public void Load(TemplateDocument template, string childName)
    {
        _template = template;
        ChildName = childName;
        _loading = true;

        var (script, content) = BodySplitter.Split(template.Body);
        ScriptText = script;

        var (expanded, incWarns) = _library.ExpandIncludes(content);
        _contentPart = expanded;
        _includeWarnings = incWarns;
        _loading = false;

        RebuildFromScript();

        _preview = new PreviewScheduler<ChildGenerationInput, ChildGenerationOutput>(
            CreatePreviewSnapshot, ComputeGeneration, ApplyPreview, PreviewDebounceMs, OnPreviewError);

        _preview.RunNow();
    }

    public void Dispose()
    {
        _rows.ValueChanged -= OnRowValueChanged;
        _preview?.Dispose();
    }

    public async Task<string?> TryGenerateAsync()
    {
        var output = ComputeGeneration(CreateSnapshot(GenerationMode.Export));

        if (!output.Result.Success)
        {
            await _dialog.ChooseAsync(
                "Generate failed",
                "The document could not be generated:\n\n" + string.Join("\n", output.Result.Diagnostics),
                "OK");
            return null;
        }

        try
        {
            var path = await _library.CreateBakedChildAsync(_template.FilePath, ChildName, output.Result.Text, _template.Fm);
            LogCreateChildSuccess(ChildName, TemplateName, path);
            return path;
        }
        catch (Exception ex)
        {
            StatusText = "Generate failed: " + ex.Message;
            LogCreateChildFailed(ex, ChildName);
            return null;
        }
    }

    partial void OnScriptTextChanged(string value)
    {
        if (_loading)
            return;
        RebuildFromScript();
        _preview?.Schedule();
    }

    partial void OnPreviewPageViewChanged(bool value)
    {
        if (_loading)
            return;
        _preview?.RunNow();
    }

    private void OnRowValueChanged()
    {
        if (_loading)
            return;
        _preview?.Schedule();
    }

    private void OnScenarioPicked(string variable, string? value)
    {
        if (value is null)
            _scenarioValues.Remove(variable);
        else
            _scenarioValues[variable] = value;
        _preview?.Schedule();
    }

    private void RebuildFromScript()
    {
        var scenarios = ScriptScanner.Scenarios(ScriptText);
        var assigned = new HashSet<string>(ScriptScanner.AssignedNames(ScriptText), StringComparer.OrdinalIgnoreCase);
        var scenarioVars = new HashSet<string>(scenarios.Select(s => s.Variable), StringComparer.OrdinalIgnoreCase);
        var previous = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in Scenarios)
            previous[s.Variable] = s.Selected;
        
        Scenarios.Clear();

        var addedScenarios = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sc in scenarios)
        {
            if (!addedScenarios.Add(sc.Variable))
                continue;
            
            var vm = new ScenarioViewModel(sc.Variable, sc.Options, sc.HasFallback, OnScenarioPicked);

            vm.Selected = previous.TryGetValue(sc.Variable, out var old) && old != null && vm.Options.Contains(old)
                ? old
                : sc.Options[0];
            
            Scenarios.Add(vm);
        }
        foreach (var stale in _scenarioValues.Keys.Where(k => !scenarioVars.Contains(k)).ToList())
            _scenarioValues.Remove(stale);
        
        HasScenarios = Scenarios.Count > 0;

        var wanted = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in _template.Fm.Variables.Keys)
            if (seen.Add(name))
                wanted.Add(name);
        
        foreach (var name in TemplateParser.ScanUserVariables(BodySplitter.Join(ScriptText, _contentPart)))
            if (seen.Add(name))
                wanted.Add(name);
        
        wanted.RemoveAll(n => assigned.Contains(n) || scenarioVars.Contains(n));

        foreach (var r in Variables)
            _savedInputs[r.Name] = r.Value;
        
        _rows.Clear();

        foreach (var n in wanted)
            _rows.Add(new VariableRowViewModel(n, _savedInputs.TryGetValue(n, out var v) ? v : "", isImplicit: false));
        
        _rows.ApplyTypes(_template.Fm.Types);
    }

    private ChildGenerationInput CreatePreviewSnapshot() => CreateSnapshot(GenerationMode.Preview);

    private ChildGenerationInput CreateSnapshot(GenerationMode mode)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in Variables)
            values[r.Name] = r.Value;
        
        foreach (var kv in _scenarioValues)
            values[kv.Key] = kv.Value;
        
        return new ChildGenerationInput(
            BodySplitter.Join(ScriptText, _contentPart),
            values,
            new DocInfo(ChildName, _template.DisplayName, DateTime.Now, DateTime.Now),
            _settings.Current.Generation.Policy,
            mode,
            _settings.Current.Generation.Culture,
            new Dictionary<string, string>(_template.Fm.Types, StringComparer.OrdinalIgnoreCase));
    }

    private ChildGenerationOutput ComputeGeneration(ChildGenerationInput input)
        => new(_scripting.Run(new GenerationRequest
        {
            TemplateBody = input.Body,
            TemplateDefaults = input.Values,
            Doc = input.Doc,
            Policy = input.Policy,
            Mode = input.Mode,
            FormatCulture = input.Culture,
            VariableTypes = input.Types
        }));

    private void ApplyPreview(ChildGenerationOutput output)
    {
        Diagnostics.Clear();

        foreach (var d in _includeWarnings)
            Diagnostics.Add(d.ToString());
        
        foreach (var d in output.Result.Diagnostics)
            Diagnostics.Add(d.ToString());
        
        if (_template.Fm.HasMarkup)
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
        }

        HasDiagnostics = Diagnostics.Count > 0;
    }

    private void OnPreviewError(Exception ex) => StatusText = "Preview failed: " + ex.Message;

    [LoggerMessage(
        EventId = LogEventIDs.UI.GenerateChildWindowViewModel.CreateChildSuccess,
        Level = LogLevel.Information,
        Message = "Generated child \"{Child}\" from \"{Template}\" -> {Path}")]
    private partial void LogCreateChildSuccess(string Child, string Template, string path);

    [LoggerMessage(
        EventId = LogEventIDs.UI.GenerateChildWindowViewModel.CreateChildFailed,
        Level = LogLevel.Error,
        Message = "Generate failed for \"{Child}\"")]
    private partial void LogCreateChildFailed(Exception ex, string Child);
}