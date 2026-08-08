// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Avalonia.Threading;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Abstractions.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;

namespace SimplyDraft.UI.Services;

public sealed class ConsoleLogService : IConsoleLogService, ILogger, ILoggerProvider
{
    private const int Cap = 400;
    private readonly IAppInfo _appInfo;
    public ObservableCollection<string> Entries {get;} = [];

    public ConsoleLogService(IAppInfo appInfo)
        => _appInfo = appInfo ?? throw new ArgumentNullException(nameof(appInfo));

    // ─── INTERFACE METHODS ─────────────────────
    public void Clear()
    {
        if (Dispatcher.UIThread.CheckAccess())
            Entries.Clear();
        else
            Dispatcher.UIThread.Post(Entries.Clear);
    }

    // ─── ILOGGER METHODS ───────────────────────
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null;
    
    public bool IsEnabled(LogLevel logLevel)
        => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;
        
        string message = formatter(state, exception);
        
        if (exception != null)
            message += " — " + exception.Message;
        
        Append(message);
    }

    // ─── ILOGGERPROVIDER METHODS ───────────────
    public ILogger CreateLogger(string categoryName)
        => categoryName.StartsWith(_appInfo.Product, StringComparison.Ordinal)
            ? this
            : NullLogger.Instance;
    
    public void Dispose() { }

    // ─── PRIVATE METHODS ───────────────────────
    private void Append(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        
        if (Dispatcher.UIThread.CheckAccess())
            Add(message);
        else
            Dispatcher.UIThread.Post(() => Add(message));
    }

    private void Add(string message)
    {
        Entries.Add(DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "  " + message);
        
        while (Entries.Count > Cap)
            Entries.RemoveAt(0);
    }
}