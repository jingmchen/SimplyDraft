// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Domains.Batch;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Export;
using SimplyDraft.Core.Domains.UI;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Export;
using SimplyDraft.Engine.Utils;
using SimplyDraft.UI.Common.MVVM;

namespace SimplyDraft.UI.ViewModels;

public sealed partial class BatchCreateWindowViewModel : ViewModelBase
{
    private readonly IBatchGenerator _batch;
    private readonly ExporterCatalog _exporterCatalog;
    private readonly ILibrary _library;
    private readonly IFilePickerService _filePicker;
    private readonly IFileSystem _fileSystem;
    private readonly ILibraryPaths _libraryPaths;
    private readonly IAppSettingsProvider _settings;
    private readonly ILogger<BatchCreateWindowViewModel> _logger;
    private TemplateDocument _template = null!;
    private static readonly string[] CsvPatterns = ["*.csv", "*.tsv", "*.txt"];
    public IReadOnlyList<DocumentKind> Formats { get; } = [DocumentKind.Txt, DocumentKind.Docx];
    public string TemplateName => _template.DisplayName;

    [ObservableProperty]
    public partial string CsvPath {get; set;}

    [ObservableProperty]
    public partial string OutputDir {get; set;}

    [ObservableProperty]
    public partial DocumentKind FormatKind {get; set;}

    [ObservableProperty]
    public partial string Pattern {get; set;}

    [ObservableProperty]
    public partial double Progress {get; set;}

    [ObservableProperty]
    public partial string Report {get; set;}

    [ObservableProperty]
    public partial bool IsRunning {get; private set;}

    public ICommand BrowseCsvCommand {get;}
    public ICommand BrowseOutCommand {get;}
    public ICommand RunCommand {get;}

    public BatchCreateWindowViewModel(
        IBatchGenerator batch,
        ExporterCatalog exporterCatalog,
        ILibrary library,
        IFilePickerService filePicker,
        IFileSystem fileSystem,
        ILibraryPaths libraryPaths,
        IAppSettingsProvider settings,
        ILogger<BatchCreateWindowViewModel> logger)
    {
        _batch = batch ?? throw new ArgumentNullException(nameof(batch));
        _exporterCatalog = exporterCatalog ?? throw new ArgumentNullException(nameof(exporterCatalog));
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _libraryPaths = libraryPaths ?? throw new ArgumentNullException(nameof(libraryPaths));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        CsvPath = "";
        OutputDir = _libraryPaths.ExportsFolder;
        FormatKind = _settings.Current.ExportSection.DefaultFormat;
        Pattern = "";
        Report = "";

        BrowseCsvCommand = new RelayCommandAsync(BrowseCsvAsync, logger: _logger);
        BrowseOutCommand = new RelayCommandAsync(BrowseOutAsync, logger: _logger);
        RunCommand = new RelayCommandAsync(RunAsync, CanRun, logger: _logger);
    }

    public void Load(TemplateDocument template)
    {
        _template = template;
        Pattern = template.Fm.Variables.Keys.FirstOrDefault() is string v ? "{" + v + "}" : "document";
    }

    partial void OnIsRunningChanged(bool value) => (RunCommand as RelayCommandBase)?.RaiseCanExecuteChanged();

    private bool CanRun() => !IsRunning;

    private async Task BrowseCsvAsync()
    {
        try
        {
            var path = await _filePicker.PickFileAsync("Choose CSV / TSV data file", [new FileFilter("CSV / TSV", CsvPatterns)]);
            if (path != null)
                CsvPath = path;
        }
        catch (Exception ex)
        {
            Report = "Couldn't open the file picker: " + ex.Message;
        }
    }

    private async Task BrowseOutAsync()
    {
        try
        {
            var path = await _filePicker.PickFolderAsync("Choose output folder");
            if (path != null)
                OutputDir = path;
        }
        catch (Exception ex)
        {
            Report = "Couldn't open the folder picker: " + ex.Message;
        }
    }

    private async Task RunAsync()
    {
        if (string.IsNullOrWhiteSpace(CsvPath) || !File.Exists(CsvPath))
        {
            Report = "Choose a CSV file first. The first row must contain variable names.";
            return;
        }

        IsRunning = true;
        Progress = 0;
        Report = "Running…";

        try
        {
            var exporter = _exporterCatalog.Resolve(FormatKind);
            var progress = new Progress<(int Done, int Total)>(OnBatchProgress);
            var (script, content) = BodySplitter.Split(_template.Body);
            var (expanded, includeWarnings) = _library.ExpandIncludes(content);
            var exportOptions = new ExportOptions
            {
                WriteBom = _settings.Current.ExportSection.TxtBom,
                NewLine = _settings.Current.ExportSection.TxtNewLine
            };

            var result = await _batch.RunAsync(new BatchRequest
            {
                Template = new TemplateDocument
                {
                    FilePath = _template.FilePath,
                    Fm = _template.Fm,
                    Body = BodySplitter.Join(script, expanded),
                    LoadDiagnostics = _template.LoadDiagnostics
                },
                CsvPath = CsvPath,
                OutputDir = OutputDir,
                Exporter = exporter,
                Options = exportOptions,
                FileNamePattern = Pattern,
                Policy = _settings.Current.GenerationSection.Policy,
                FormatCulture = _settings.Current.GenerationSection.Culture
            }, progress, CancellationToken.None);

            var lines = result.Rows.Select(FormatRowLine);
            var header = $"{result.OkCount} ok, {result.FailCount} failed.\nReport: {result.ReportPath}\n";

            if (includeWarnings.Count > 0)
                header += "\n\\input warnings (apply to every row):\n" + string.Join("\n", includeWarnings) + "\n";
            Report = header + "\n" + string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            Report = "Batch failed: " + ex.Message;
        }
        finally
        {
            IsRunning = false;
        }
    }

    private void OnBatchProgress((int Done, int Total) p)
        => Progress = p.Total == 0 ? 0 : 100.0 * p.Done / p.Total;

    private static string FormatRowLine(BatchRowResult r)
        => $"{(r.Ok ? "OK " : "ERR")} row {r.RowNumber}: {r.FileName} — {r.Message}";
}