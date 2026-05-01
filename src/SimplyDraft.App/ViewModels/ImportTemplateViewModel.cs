using Avalonia.Markup.Xaml.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimplyDraft.App.Services;
using SimplyDraft.Core.Configuration;

namespace SimplyDraft.App.ViewModels;

public sealed record ImportResult(string DocxPath, string TemplateName, string Author);
public sealed partial class ImportTemplateViewModel : ObservableObject
{
    private static readonly string[] SupportedExtensions = [
        CoreKeys.Document.FileExtension.DocumentKey,
        CoreKeys.Document.FileExtension.TextKey
    ];

    private readonly IFilePickerService _filePicker;

    public ImportTemplateViewModel(IFilePickerService filePicker)
    {
        _filePicker = filePicker;
    }

    // ─── BINDABLE PROPERTIES ───────────────────
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    private string _docxPath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    private string _templateName = string.Empty;

    [ObservableProperty]
    private string _author = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // ─── EVENTS ────────────────────────────────
    // Raised when the window should close - user cancelled
    public event EventHandler<ImportResult?>? RequestClose;

    // ─── COMMANDS ──────────────────────────────
    [RelayCommand]
    private async Task Browse()
    {
        var path = await _filePicker.PickFileAsync();
        if (path is not null)
        {
            ApplyPickedFile(path);
        }
    }

    [RelayCommand(CanExecute = nameof(CanImport))]
    private void Import()
    {
        ErrorMessage = string.Empty;

        var ext = Path.GetExtension(DocxPath).ToLowerInvariant();

        if (!SupportedExtensions.Contains(ext))
        {
            ErrorMessage = "Selected file extension is not supported.";
            return;
        }

        if (!File.Exists(DocxPath))
        {
            ErrorMessage = "File not found.";
            return;
        }

        var author = string.IsNullOrWhiteSpace(Author) ? Environment.UserName : Author.Trim();
        RequestClose?.Invoke(this, new ImportResult(DocxPath, TemplateName.Trim(), author));
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, null);

    private bool CanImport() =>
        !string.IsNullOrWhiteSpace(DocxPath) &&
        !string.IsNullOrWhiteSpace(TemplateName);
    
    private void ApplyPickedFile(string path)
    {
        DocxPath = path;

        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            TemplateName = Path.GetFileNameWithoutExtension(path);
        }
    }
}