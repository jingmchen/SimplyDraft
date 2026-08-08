// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Enums;
using SimplyDraft.UI.ViewModels;

namespace SimplyDraft.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly IThemeService _theme;
    private GridLength _consoleHeight = new(110);

    public MainWindow(MainWindowViewModel viewModel, IThemeService theme)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        InitializeComponent();
        DataContext = viewModel;
        SyncThemeChecks();

        ApplyConsoleVisibility(viewModel.ConsoleVisible);
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ((INotifyCollectionChanged)viewModel.ConsoleEntries).CollectionChanged += OnConsoleEntries;
        _theme.ThemeChanged += OnThemeChanged;
        Closed += OnClosed;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is { } vm && e.PropertyName == nameof(MainWindowViewModel.ConsoleVisible))
            ApplyConsoleVisibility(vm.ConsoleVisible);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _theme.ThemeChanged -= OnThemeChanged;
        
        if (_viewModel is not { } vm)
            return;
        
        vm.PropertyChanged -= OnViewModelPropertyChanged;
        
        ((INotifyCollectionChanged)vm.ConsoleEntries).CollectionChanged -= OnConsoleEntries;
        
        vm.Dispose();
    }

    private void OnItemDoubleTapped(object? sender, TappedEventArgs e) => _viewModel?.OpenSelected();

    private void OnExit(object? sender, RoutedEventArgs e) => Close();

    private void ApplyConsoleVisibility(bool on)
    {
        if (!on && CenterGrid.RowDefinitions[2].Height.Value > 0)
            _consoleHeight = CenterGrid.RowDefinitions[2].Height;   // remember the dragged size
        CenterGrid.RowDefinitions[1].Height = on ? new GridLength(6) : new GridLength(0);
        CenterGrid.RowDefinitions[2].Height = on ? _consoleHeight : new GridLength(0);
    }

    private void OnConsoleEntries(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (ConsoleList.ItemCount > 0)
            ConsoleList.ScrollIntoView(ConsoleList.ItemCount - 1);
    }

    private void OnConsoleClear(object? sender, RoutedEventArgs e) => _viewModel?.ClearConsole();

    private void OnConsoleHide(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is { } vm)
            vm.ConsoleVisible = false;
    }

    private void OnThemePick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem {Tag: string tag} && Enum.TryParse<AppTheme>(tag, out var theme))
            _theme.SetTheme(theme);
    }

    private void OnAccentPick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem {Tag: string tag} && Enum.TryParse<AppAccent>(tag, out var accent))
            _theme.SetAccent(accent);
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e) => SyncThemeChecks();

    private void SyncThemeChecks()
    {
        var theme = _theme.CurrentTheme;
        var accent = _theme.CurrentAccent;
        ThemeSystem.IsChecked = theme == AppTheme.System;
        ThemeDarkNavy.IsChecked = theme == AppTheme.DarkNavy;
        ThemeDarkGraphite.IsChecked = theme == AppTheme.DarkGraphite;
        ThemeBlack.IsChecked = theme == AppTheme.Black;
        ThemeLight.IsChecked = theme == AppTheme.Light;
        ThemeWhite.IsChecked = theme == AppTheme.White;
        AccentIndigo.IsChecked = accent == AppAccent.Indigo;
        AccentBlue.IsChecked = accent == AppAccent.Blue;
        AccentTeal.IsChecked = accent == AppAccent.Teal;
        AccentAmber.IsChecked = accent == AppAccent.Amber;
        AccentBlack.IsChecked = accent == AppAccent.Black;
        AccentWhite.IsChecked = accent == AppAccent.White;
    }
}