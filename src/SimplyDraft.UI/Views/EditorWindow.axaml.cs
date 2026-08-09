// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.UI.Common;
using SimplyDraft.UI.Common.Editor;
using SimplyDraft.UI.ViewModels;
using SimplyDraft.UI.Views.Dialogs;

namespace SimplyDraft.UI.Views;

public sealed partial class EditorWindow : Window, IDisposable
{
    private const int ContentEditorWidth = 78;
    private readonly EditorWindowViewModel _viewModel;
    private readonly IRenderEngine _renderer;
    private readonly IAppSettingsProvider _settings;
    private readonly ILogger<EditorWindow> _logger;
    private readonly ThemeMenuController _themeMenu;
    private bool _forceClose;
    private bool _syncingEditors;
    private bool _disposed;

    public EditorWindow(
        EditorWindowViewModel viewModel,
        IRenderEngine renderer,
        IAppSettingsProvider settings,
        IThemeService theme,
        ILogger<EditorWindow> logger)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        ArgumentNullException.ThrowIfNull(theme);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        InitializeComponent();
        _themeMenu = new ThemeMenuController(theme, ThemeMenu, AccentMenu);
        Closed += OnClosed;
    }

    public void Load(LibraryItem item)
    {
        _viewModel.Load(item);
        DataContext = _viewModel;
        PagePreviewControl.Renderer = _renderer;
        ConfigureEditors();

        if (!_viewModel.IsTemplate)
        {
            RightPane.RowDefinitions[0].Height = new GridLength(0);
            RightPane.RowDefinitions[1].Height = new GridLength(0);
            RightPane.RowDefinitions[2].Height = new GridLength(0);
            RightPane.RowDefinitions[3].Height = new GridLength(0);
        }

        EditorSyntax.PaletteChanged += OnPaletteChanged;
        Closing += OnClosing;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;

        _themeMenu.Dispose();
        EditorSyntax.PaletteChanged -= OnPaletteChanged;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.Dispose();
    }

    private void ConfigureEditors()
    {
        Setup(ScriptEditor, EditorSyntax.Script, _viewModel.ScriptText);
        Setup(ContentEditor, EditorSyntax.Template, _viewModel.ContentText);
        
        ScriptEditor.TextChanged += OnScriptEditorTextChanged;
        ContentEditor.TextChanged += OnContentEditorTextChanged;
        ContentEditor.WordWrap = _settings.Current.EditorSection.WordWrap;
        WordWrapToggle.IsChecked = _settings.Current.EditorSection.WordWrap;
        ContentEditor.Options.ShowColumnRulers = true;

        ContentEditor.Options.ColumnRulerPositions =
            new[] { ContentEditorWidth, DocxColumnEstimate(_viewModel.DocxFontSizePt) }.Distinct().ToArray();

        if (_viewModel.IsTemplate)
            ScriptCompletion.Attach(ScriptEditor, CollectCompletionNames);
        
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private IReadOnlyList<string> CollectCompletionNames()
        => _viewModel.Variables.Select(v => v.Name).ToList();

    private void Setup(
        TextEditor editor,
        AvaloniaEdit.Highlighting.IHighlightingDefinition? syntax,
        string initial)
    {
        editor.Text = initial;
        editor.IsReadOnly = !_viewModel.IsTemplate;
        editor.Options.ConvertTabsToSpaces = true;
        editor.Options.IndentationSize = 4;
        editor.Options.HighlightCurrentLine = true;

        if (syntax != null)
            editor.SyntaxHighlighting = syntax;
    }

    private void OnScriptEditorTextChanged(object? sender, EventArgs e)
    {
        if (_syncingEditors)
            return;
        
        _viewModel.ScriptText = ScriptEditor.Text ?? "";
    }

    private void OnContentEditorTextChanged(object? sender, EventArgs e)
    {
        if (_syncingEditors)
            return;
        _viewModel.ContentText = ContentEditor.Text ?? "";
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorWindowViewModel.ScriptText))
            Push(ScriptEditor, _viewModel.ScriptText);
        else if (e.PropertyName == nameof(EditorWindowViewModel.ContentText))
            Push(ContentEditor, _viewModel.ContentText);
    }

    private void OnClosed(object? sender, EventArgs e)
        => Dispose();

    private void Push(TextEditor editor, string text)
    {
        if (editor.Text == text)
            return;
        
        _syncingEditors = true;
        
        try { editor.Text = text; }
        finally { _syncingEditors = false; }
    }

    private void OnPaletteChanged()
    {
        if (EditorSyntax.Script != null)
            ScriptEditor.SyntaxHighlighting = EditorSyntax.Script;
        
        if (EditorSyntax.Template != null)
            ContentEditor.SyntaxHighlighting = EditorSyntax.Template;
    }

    private TextEditor? FocusedEditor()
    {
        if (ScriptEditor.TextArea.IsKeyboardFocusWithin)
            return ScriptEditor;
        
        if (ContentEditor.TextArea.IsKeyboardFocusWithin)
            return ContentEditor;
        
        return ContentEditor;
    }

    private void OnEditUndo(object? sender, RoutedEventArgs e) => FocusedEditor()?.Undo();
    private void OnEditRedo(object? sender, RoutedEventArgs e) => FocusedEditor()?.Redo();
    private void OnEditCut(object? sender, RoutedEventArgs e) => FocusedEditor()?.Cut();
    private void OnEditCopy(object? sender, RoutedEventArgs e) => FocusedEditor()?.Copy();
    private void OnEditPaste(object? sender, RoutedEventArgs e) => FocusedEditor()?.Paste();
    private void OnEditSelectAll(object? sender, RoutedEventArgs e) => FocusedEditor()?.SelectAll();
    private void OnCloseMenu(object? sender, RoutedEventArgs e) => Close();
    private void OnScriptHelp(object? sender, RoutedEventArgs e) => HelpDialog.ShowScript(this);
    private void OnMarkupHelp(object? sender, RoutedEventArgs e) => HelpDialog.ShowMarkup(this);

    private void OnWordWrapToggle(object? sender, RoutedEventArgs e)
    {
        bool on = WordWrapToggle.IsChecked == true;
        ContentEditor.WordWrap = on;

        try
        {
            _settings.Current.EditorSection = _settings.Current.EditorSection with { WordWrap = on };
            _settings.Save();
        }
        catch (Exception ex)
        {
            LogUnableToSaveSettings(ex);
        }
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_forceClose || !_viewModel.Dirty)
            return;
        
        e.Cancel = true;
        
        var idx = await ChoiceDialog.ShowAsync(this, "Unsaved changes",
            $"\"{_viewModel.Item.Name}\" has unsaved changes.", "Cancel", "Discard", "Save");
        
        if (idx == 0)
            return;
        
        if (idx == 2)
            await _viewModel.SaveAsync();
        
        _forceClose = true;
        Close();
    }

    private static int DocxColumnEstimate(int sizePt)
        => Math.Max(40, (int)Math.Round(481.89 / (0.49 * Math.Max(6, sizePt))));
    
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Could not save settings.")]
    private partial void LogUnableToSaveSettings(Exception ex);
}