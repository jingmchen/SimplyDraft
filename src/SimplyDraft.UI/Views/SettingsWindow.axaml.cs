// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Controls;
using Avalonia.Interactivity;
using SimplyDraft.UI.ViewModels;

namespace SimplyDraft.UI.Views;

public sealed partial class SettingsWindow : Window
{
    private readonly SettingsWindowViewModel _viewModel;

    public SettingsWindow(SettingsWindowViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        _viewModel.Save();
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
        => Close(false);
}