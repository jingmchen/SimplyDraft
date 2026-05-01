using Avalonia.Controls;
using SimplyDraft.App.ViewModels;

namespace SimplyDraft.App.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}