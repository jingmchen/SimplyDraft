// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using CommunityToolkit.Mvvm.ComponentModel;

namespace SimplyDraft.UI.ViewModels.Components;

public sealed partial class ScenarioViewModel : ObservableObject
{
    public const string FallbackLabel = "(other)";
    private readonly Action<string, string?> _picked;
    public string Variable {get;}
    public IReadOnlyList<string> Options {get;}
    
    [ObservableProperty]
    public partial string? Selected {get; set;}

    public ScenarioViewModel(string variable, IReadOnlyList<string> options, bool hasFallback, Action<string, string?> picked)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variable);
        ArgumentNullException.ThrowIfNull(options);
        
        Variable = variable;
        Options = hasFallback ? new List<string>(options) { FallbackLabel } : options;
        _picked = picked ?? throw new ArgumentNullException(nameof(picked));
    }

    partial void OnSelectedChanged(string? value)
        => _picked(Variable, value == FallbackLabel ? "" : value);
}