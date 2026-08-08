// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;

namespace SimplyDraft.UI.Common.MVVM;

public sealed class RelayCommand : RelayCommandBase
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null, ILogger? logger = null) : base(logger)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public override bool CanExecute(object? parameter)
        => _canExecute?.Invoke() ?? true;
    
    public override void Execute(object? parameter)
        => Run(_execute);
}