using SimplyDraft.App.ViewModels;

namespace SimplyDraft.App.Services.FileExplorer;

// Creates configured FileExplorerViewModel instances via DI (transient pairs: service - VM for each calls via DI)
public interface IFileExplorerFactory
{
    FileExplorerViewModel Create(FileExplorerOptions options);
    FileExplorerViewModel Create(string rootPath);
    FileExplorerViewModel CreateDefault();
}