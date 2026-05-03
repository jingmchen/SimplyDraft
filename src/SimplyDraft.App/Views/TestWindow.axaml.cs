using Avalonia.Controls;
using SimplyDraft.App.ViewModels;

namespace SimplyDraft.App.Views;

public sealed partial class TestWindow : Window
{
    public TestWindow(TestWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}