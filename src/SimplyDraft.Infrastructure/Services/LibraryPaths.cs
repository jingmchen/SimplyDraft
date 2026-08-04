using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Infrastructure.Constants;

namespace SimplyDraft.Infrastructure.Services;

public sealed class LibraryPaths : ILibraryPaths
{
    public string Root {get;}
    public string TemplatesFolder {get;}
    public string ChildrenFolder {get;}
    public string ExportsFolder {get;}
    public string TrashFolder {get;}

    public LibraryPaths(IAppPaths appPaths)
    {
        ArgumentNullException.ThrowIfNull(appPaths);

        Root = appPaths.AppDataFolder;

        TemplatesFolder =
            Path.Combine(
                Root,
                InfrastructureConstants.UserData.FolderName.DocumentsParent,
                InfrastructureConstants.UserData.FolderName.Templates
            );
        
        Directory.CreateDirectory(TemplatesFolder);

        ChildrenFolder =
            Path.Combine(
                Root,
                InfrastructureConstants.UserData.FolderName.DocumentsParent,
                InfrastructureConstants.UserData.FolderName.Children
            );
        
        Directory.CreateDirectory(ChildrenFolder);
        
        ExportsFolder =
            Path.Combine(
                Root,
                InfrastructureConstants.UserData.FolderName.DocumentsParent,
                InfrastructureConstants.UserData.FolderName.Exports
            );
        
        Directory.CreateDirectory(ExportsFolder);
        
        TrashFolder =
            Path.Combine(
                Root,
                InfrastructureConstants.UserData.FolderName.DocumentsParent,
                InfrastructureConstants.UserData.FolderName.Trash
            );
        
        Directory.CreateDirectory(TrashFolder);
    }
}