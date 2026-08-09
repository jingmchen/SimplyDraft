// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;

namespace SimplyDraft.UI.Common.MVVM;

public sealed class RelayCommand<T> : RelayCommandBase
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null, ILogger? logger = null) : base(logger)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public override bool CanExecute(object? parameter)
    {
        if (parameter is not null && parameter is not T)
            return false;
        
        return _canExecute?.Invoke(Cast(parameter)) ?? true;
    }

    public override void Execute(object? parameter)
        => Run(() => _execute(Cast(parameter)));

    private static T? Cast(object? parameter)
        => parameter is null
            ? default
            : (T)parameter;
}