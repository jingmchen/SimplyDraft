// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using CommunityToolkit.Mvvm.ComponentModel;
using SimplyDraft.UI.Common.MVVM;

namespace SimplyDraft.UI.ViewModels.Components;

public sealed partial class ScenarioViewModel : ViewModelBase
{
    private readonly Action<string, string?> _picked;
    public string Variable {get;}
    public IReadOnlyList<string> Options {get;}
    
    [ObservableProperty]
    public partial string? Selected {get; set;}

    public ScenarioViewModel(string variable, IReadOnlyList<string> options, Action<string, string?> picked)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variable);
        
        Variable = variable;
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _picked = picked ?? throw new ArgumentNullException(nameof(picked));
    }

    partial void OnSelectedChanged(string? value) => _picked(Variable, value);
}