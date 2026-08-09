// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.UI.Common;
using SimplyDraft.UI.ViewModels;
using SimplyDraft.UI.ViewModels.Components;

namespace SimplyDraft.UI.Views;

public sealed partial class MainWindow : Window, IDisposable
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ThemeMenuController _themeMenu;
    private GridLength _consoleHeight = new(110);
    private bool _disposed;

    public MainWindow(MainWindowViewModel viewModel, IThemeService theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();

        DataContext = viewModel;
        _themeMenu = new ThemeMenuController(theme, ThemeMenu, AccentMenu);

        ApplyConsoleVisibility(viewModel.ConsoleVisible);
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ((INotifyCollectionChanged)viewModel.ConsoleEntries).CollectionChanged += OnConsoleEntries;
        LibraryList.AddHandler(PointerPressedEvent, OnLibraryPointerPressed, RoutingStrategies.Tunnel);

        Closed += OnClosed;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;

        _themeMenu.Dispose();
        LibraryList.RemoveHandler(PointerPressedEvent, OnLibraryPointerPressed);

        if (_viewModel is not { } vm)
            return;
        
        vm.PropertyChanged -= OnViewModelPropertyChanged;
        
        ((INotifyCollectionChanged)vm.ConsoleEntries).CollectionChanged -= OnConsoleEntries;
        
        vm.Dispose();
    }

    private void OnLibraryPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(LibraryList).Properties.IsRightButtonPressed)
            return;

        if ((e.Source as Control)?.FindAncestorOfType<ListBoxItem>(includeSelf: true) is {DataContext: LibraryItemViewModel item})
            _viewModel.Library.SelectedItem = item;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is { } vm && e.PropertyName == nameof(MainWindowViewModel.ConsoleVisible))
            ApplyConsoleVisibility(vm.ConsoleVisible);
    }

    private void OnClosed(object? sender, EventArgs e)
        => Dispose();

    private void OnItemDoubleTapped(object? sender, TappedEventArgs e) => _viewModel?.OpenSelected();
    private void OnExit(object? sender, RoutedEventArgs e) => Close();
    private void ApplyConsoleVisibility(bool on)
    {
        if (!on && CenterGrid.RowDefinitions[2].Height.Value > 0)
            _consoleHeight = CenterGrid.RowDefinitions[2].Height;
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
}