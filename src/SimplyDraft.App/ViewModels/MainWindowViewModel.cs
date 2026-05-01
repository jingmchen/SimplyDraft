using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SimplyDraft.App.Services.FileExplorer;

namespace SimplyDraft.App.ViewModels;

/// <summary>
/// Root ViewModel for <c>MainWindow</c>.
/// Owns the template list, the file explorer, and the global status bar.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private FileExplorerViewModel fileExplorerVM {get;}

    public MainWindowViewModel(IFileExplorerService explorerService)
    {
        var explorerService = new FileExplorerService(
            
        );
    }
}