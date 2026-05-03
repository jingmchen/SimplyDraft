using Microsoft.Extensions.Logging;
using SimplyDraft.App.Configuration;
using SimplyDraft.App.ViewModels;

namespace SimplyDraft.App.Services.FileExplorer;

// Factory implementation for File Explorer (FileExplorerService, FileWatcherService, and FileExplorerViewModel)
// Provides runtime parameterisation (FileExplorerOptions)
// Manually constructs and pairs the full FileExplorer object graph (FileWatcherService → FileExplorerService → FileExplorerViewModel)
// per Create() call, with runtime-supplied FileExplorerOptions.
public sealed class FileExplorerFactory : IFileExplorerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private static readonly FileExplorerOptions DefaultOptions = new();

    public FileExplorerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public FileExplorerViewModel Create(FileExplorerOptions options)
    {
        var watcher = new FileWatcherService(
            _loggerFactory.CreateLogger<FileWatcherService>(),
            TimeSpan.FromMilliseconds(AppConstants.Service.FileWatcher.DebounceMs)
        );

        var service = new FileExplorerService(
            watcher,
            options
        );

        var vm = new FileExplorerViewModel(
            _loggerFactory.CreateLogger<FileExplorerViewModel>(),
            service
        );

        if (!string.IsNullOrWhiteSpace(options.RootPath))
            vm.LoadDirectory(options.RootPath);
        
        return vm;
    }

    public FileExplorerViewModel Create(string rootPath)
        => Create(DefaultOptions with {RootPath = rootPath});

    public FileExplorerViewModel CreateDefault()
        => Create(DefaultOptions);
}