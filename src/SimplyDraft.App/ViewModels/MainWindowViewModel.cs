using CommunityToolkit.Mvvm.ComponentModel;

namespace SimplyDraft.App.ViewModels;

public sealed class NavigationBarItems
{
    public string Label {get; init;} = string.Empty;
    public string Icon {get; init;} = string.Empty;
    public string Key {get; init;} = string.Empty;
}

public sealed partial class MainWindowViewModel : ObservableObject
{
    
}