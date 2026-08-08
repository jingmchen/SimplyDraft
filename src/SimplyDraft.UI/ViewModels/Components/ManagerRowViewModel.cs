// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using CommunityToolkit.Mvvm.ComponentModel;
using SimplyDraft.UI.Common.MVVM;

namespace SimplyDraft.UI.ViewModels.Components;

public sealed partial class ManagerRowViewModel : ViewModelBase
{
    private string _type;
    public IReadOnlyList<string> TypeChoices {get;} = ["text", "number", "date", "time", "yesno"];

    public string Type
    {
        get => _type;
        set => SetProperty(ref _type, string.IsNullOrWhiteSpace(value) ? "text" : value);
    }

    [ObservableProperty]
    public partial string Name {get; set;} = "";

    public ManagerRowViewModel(string name, string type)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _type = string.IsNullOrWhiteSpace(type)
            ? "text"
            : type;
    }
}