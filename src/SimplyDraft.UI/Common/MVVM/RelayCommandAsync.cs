// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;

namespace SimplyDraft.UI.Common.MVVM;

public sealed class RelayCommandAsync : RelayCommandBase
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public RelayCommandAsync(Func<Task> execute, Func<bool>? canExecute = null, ILogger? logger = null) : base(logger)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public override bool CanExecute(object? parameter)
        => !_isExecuting && (_canExecute?.Invoke() ?? true);
    
    public override async void Execute(object? parameter)
        => await ExecuteAsync(parameter);

    public async Task ExecuteAsync(object? parameter)
    {
        try
        {
            if (!CanExecute(parameter))
                return;
            _isExecuting = true;
            RaiseCanExecuteChanged();
            await _execute();
        }
        catch (Exception ex)
        {
            OnException(ex);
        }
        finally
        {
            if (_isExecuting)
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
    }
}