// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Extensions.Logging;

namespace SimplyDraft.UI.Common.MVVM;

public abstract class RelayCommandBase : ICommand
{
    private readonly ILogger? _logger; // Logger Source Context will be the ViewModel that logs it
    public event EventHandler? CanExecuteChanged;

    protected RelayCommandBase(ILogger? logger = null)
        => _logger = logger;

    public abstract bool CanExecute(object? parameter);
    public abstract void Execute(object? parameter);
    protected void Run(Action body)
    {
        try
        {
            body();
        }
        catch (Exception ex)
        {
            OnException(ex);
        }
    }

    protected virtual void OnException(Exception ex)
    {
        Trace.TraceError("Command failed: {0}", ex);
        if (_logger is not null)
            RelayCommandLog.CommandFailed(_logger, ex);
    }

    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}