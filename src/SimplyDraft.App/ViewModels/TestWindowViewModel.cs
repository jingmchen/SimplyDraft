using CommunityToolkit.Mvvm.ComponentModel;
using SimplyDraft.App.Services.FileExplorer;

namespace SimplyDraft.App.ViewModels;

public sealed partial class TestWindowViewModel : ObservableObject
{
    private readonly IFileExplorerFactory _fileExplorerFactory;
    public FileExplorerViewModel FileExplorerPanel {get; init;}
    public TestWindowViewModel(IFileExplorerFactory fileExplorerFactory)
    {
        _fileExplorerFactory = fileExplorerFactory;
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "test"
        );
        FileExplorerPanel = _fileExplorerFactory.Create(path);
    }
}