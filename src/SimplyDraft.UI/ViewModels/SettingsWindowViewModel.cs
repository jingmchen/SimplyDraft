// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Configuration.AppSettings;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.UI.ViewModels;

public sealed partial class SettingsWindowViewModel : ObservableObject
{
    private readonly ISettingsProvider<AppSettings> _settings;
    private readonly ILogger<SettingsWindowViewModel> _logger;
    public IReadOnlyList<DocumentKind> Formats {get;} = [DocumentKind.Txt, DocumentKind.Docx];
    public IReadOnlyList<MissingVariablePolicy> Policies {get;} =
        [MissingVariablePolicy.ErrorOnExport, MissingVariablePolicy.LeavePlaceholder, MissingVariablePolicy.EmptyString];
    public IReadOnlyList<NewLineMode> NewLines {get;} = [NewLineMode.Platform, NewLineMode.Lf, NewLineMode.CrLf];
    public IReadOnlyList<CultureMode> Cultures {get;} = [CultureMode.System, CultureMode.Invariant];

    [ObservableProperty]
    public partial MissingVariablePolicy Policy {get; set;}

    [ObservableProperty]
    public partial CultureMode Culture {get; set;}

    [ObservableProperty]
    public partial DocumentKind DefaultFormat {get; set;}

    [ObservableProperty]
    public partial NewLineMode NewLine {get; set;}

    [ObservableProperty]
    public partial bool TxtBom {get; set;}

    [ObservableProperty]
    public partial string StatusText {get; set;} = "";

    public SettingsWindowViewModel(ISettingsProvider<AppSettings> settings, ILogger<SettingsWindowViewModel> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Policy = _settings.Current.Generation.Policy;
        Culture = _settings.Current.Generation.FormatCulture;
        DefaultFormat = _settings.Current.Export.DefaultFormat;
        NewLine = _settings.Current.Export.TxtNewLine;
        TxtBom = _settings.Current.Export.TxtBom;
        StatusText = "";
    }

    public void Save()
    {
        try
        {
            var s = _settings.Current;
            s.Export = new ExportSettings
            {
                DefaultFormat = DefaultFormat,
                TxtNewLine = NewLine,
                TxtBom = TxtBom
            };

            s.Generation = new GenerationSettings
            {
                Policy = Policy,
                FormatCulture = Culture
            };

            _settings.Save();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not save settings");
            StatusText = ex.Message;
        }
    }
}