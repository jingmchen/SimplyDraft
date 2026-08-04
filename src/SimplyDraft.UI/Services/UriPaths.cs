using System.Reflection;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.UI.Constants;

namespace SimplyDraft.UI.Services;

public sealed class UriPaths : IUriPaths
{
    public string ThemeTemplate {get;}
    public string AccentTemplate {get;}
    public string StyleTemplate {get;}
    
    public UriPaths(Assembly assembly, IAppInfo appInfo)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(appInfo);
        
        var assemblyName = assembly.GetName().Name ?? appInfo.Product;

        ThemeTemplate =
            $"avares://{assemblyName}.UI/{UIConstants.Bundled.FolderName.Assets}/{UIConstants.Bundled.FolderName.Themes}/{{0}}Theme.axaml";
        
        AccentTemplate =
            $"avares://{assemblyName}.UI/{UIConstants.Bundled.FolderName.Assets}/{UIConstants.Bundled.FolderName.Accents}/{{0}}Accent.axaml";
        
        StyleTemplate =
            $"avares://{assemblyName}.UI/{UIConstants.Bundled.FolderName.Assets}/{UIConstants.Bundled.FolderName.Styles}/{{0}}.axaml";
    }
}