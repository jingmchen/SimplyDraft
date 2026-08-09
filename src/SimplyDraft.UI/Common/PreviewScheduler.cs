// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Threading;

namespace SimplyDraft.UI.Common;

public sealed class PreviewScheduler<TIn, TOut> : IDisposable
{
    private readonly Func<TIn> _snapshot;
    private readonly Func<TIn, TOut> _compute;
    private readonly Action<TOut> _apply;
    private readonly Action<Exception>? _onError;
    private readonly int _delayMs;
    private CancellationTokenSource? _cts;

    public PreviewScheduler(
        Func<TIn> snapshot,
        Func<TIn, TOut> compute,
        Action<TOut> apply,
        int delayMs,
        Action<Exception>? onError = null)
    {
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _compute = compute ?? throw new ArgumentNullException(nameof(compute));
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        _delayMs = delayMs;
        _onError = onError;
    }

    // ─── PUBLIC METHODS ────────────────────────
    public void RunNow()
    {
        _cts?.Cancel();
        _cts = null;

        try
        {
            _apply(_compute(_snapshot()));
        }
        catch (Exception ex)
        {
            Report(ex);
        }
    }

    public void Schedule()
    {
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;
        var input = _snapshot();

        _ = Task.Run(async () =>
        {
            try { await Task.Delay(_delayMs, cts.Token); }
            catch (TaskCanceledException) { return; }

            // Compute runs on a thread-pool thread: an unguarded throw here became an UNOBSERVED task
            // exception (process-terminating under ThrowUnobservedTaskExceptions) and silently dropped
            // the render. Catch it and surface via the UI thread instead.
            TOut output;

            try
            {
                output = _compute(input);
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() => { if (!cts.IsCancellationRequested) Report(ex); });
                return;
            }

            if (cts.IsCancellationRequested)
                return;
            
            Dispatcher.UIThread.Post(() =>
            {
                if (cts.IsCancellationRequested)
                    return;
                try { _apply(output); }
                catch (Exception ex) { Report(ex); }
            });
        });
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    // ─── PRIVATE METHODS ───────────────────────
    private void Report(Exception ex)
    {
        if (_onError != null)
            _onError(ex);
        else
            System.Diagnostics.Trace.TraceError("Preview failed: " + ex);
    }
}