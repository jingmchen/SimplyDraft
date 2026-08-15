// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using System.Collections.ObjectModel;
using Serilog.Core;
using Serilog.Events;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.UI.Utils;

namespace SimplyDraft.UI.Services;

public sealed class ConsoleLogService : IConsoleLogService, ILogEventSink
{
    private const int Cap = 400;
    public ObservableCollection<string> Entries {get;} = [];

    public ConsoleLogService() { }

    // ─── INTERFACE METHODS ─────────────────────
    public void Clear()
        => DispatcherHelper.PostOnUIThread(Entries.Clear);
    
    // ─── SERILOG SINK (ILogEventSink) ──────────
    public void Emit(LogEvent logEvent)
    {
        string message = logEvent.RenderMessage(CultureInfo.InvariantCulture);

        if (logEvent.Exception != null)
            message += " — " + logEvent.Exception.Message;
        
        if (string.IsNullOrWhiteSpace(message))
            return;
        
        DispatcherHelper.PostOnUIThread(() =>
        {
            Entries.Add(DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "  " + message);
            while (Entries.Count > Cap)
                Entries.RemoveAt(0);
        });
    }
}