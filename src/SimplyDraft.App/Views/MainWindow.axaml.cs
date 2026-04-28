using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SimplyDraft.App.ViewModels;

namespace SimplyDraft.App.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = viewModel;
    }
}