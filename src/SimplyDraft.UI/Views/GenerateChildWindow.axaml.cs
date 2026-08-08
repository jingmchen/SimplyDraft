// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.UI.Common.Editor;
using SimplyDraft.UI.ViewModels;
using SimplyDraft.UI.Views.Dialogs;

namespace SimplyDraft.UI.Views;

public sealed partial class GenerateChildWindow : Window
{
    private readonly GenerateChildWindowViewModel _viewModel;
    private readonly IRenderEngine _renderer;
    private bool _syncing;

    public GenerateChildWindow(GenerateChildWindowViewModel viewModel, IRenderEngine renderer)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        InitializeComponent();
    }

    public void Load(TemplateDocument template, string childName)
    {
        _viewModel.Load(template, childName);
        DataContext = _viewModel;
        PagePreviewControl.Renderer = _renderer;

        ScriptEditor.Text = _viewModel.ScriptText;
        ScriptEditor.Options.ConvertTabsToSpaces = true;
        ScriptEditor.Options.IndentationSize = 4;
        ScriptEditor.Options.HighlightCurrentLine = true;

        if (EditorSyntax.Script != null)
            ScriptEditor.SyntaxHighlighting = EditorSyntax.Script;
        
        ScriptEditor.TextChanged += OnScriptEditorTextChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        ScriptCompletion.Attach(ScriptEditor, CollectCompletionNames);
        EditorSyntax.PaletteChanged += OnPaletteChanged;

        Closed += OnClosed;
    }

    private IReadOnlyList<string> CollectCompletionNames()
        => _viewModel.Variables.Select(v => v.Name).Concat(_viewModel.Scenarios.Select(s => s.Variable)).ToList();

    private void OnScriptEditorTextChanged(object? sender, EventArgs e)
    {
        if (!_syncing)
            _viewModel.ScriptText = ScriptEditor.Text ?? "";
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(GenerateChildWindowViewModel.ScriptText)) return;
        if (ScriptEditor.Text == _viewModel.ScriptText) return;
        _syncing = true;
        try { ScriptEditor.Text = _viewModel.ScriptText; }
        finally { _syncing = false; }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        EditorSyntax.PaletteChanged -= OnPaletteChanged;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.Dispose();
    }

    private void OnScriptHelp(object? sender, RoutedEventArgs e) => HelpDialog.ShowScript(this);

    private void OnPaletteChanged()
    {
        if (EditorSyntax.Script != null) ScriptEditor.SyntaxHighlighting = EditorSyntax.Script;
    }

    private async void Generate_Click(object? sender, RoutedEventArgs e)
    {
        var path = await _viewModel.TryGenerateAsync();
        if (path != null) Close(path);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}