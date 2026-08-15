// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.Concurrent;
using System.Text;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Infrastructure.Utils;

namespace SimplyDraft.Infrastructure.Services;

public sealed class AtomicFileAsync : IAtomicFileAsync
{
    private readonly ConcurrentDictionary<string, Task> _queue = new(PathComparer);
    private readonly ConcurrentDictionary<string, byte> _pendingCleanup = new(PathComparer);
    private static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    
    public AtomicFileAsync() { }

    // ─── PUBLIC METHODS ────────────────────────
    public Task WriteAsync(string path, string contents, Encoding? encoding = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contents);

        return EnqueueAsync(path, fullPath =>
            AtomicFile.WriteTo(
                path: fullPath,
                contents: contents,
                encoding: encoding,
                cleanupFailed: AddToCleanup
            ));
    }

    public Task MoveAsync(string sourcePath, string destinationPath, bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        string fullDestination = Path.GetFullPath(destinationPath);

        return EnqueueAsync(
            sourcePath,
            fullPath => File.Move(fullPath, fullDestination, overwrite));
    }

    public Task DeleteAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return EnqueueAsync(
            path,
            File.Delete);
    }

    public Task FlushAsync()
        => Task.WhenAll(_queue.Values.ToArray());

    // ─── PRIVATE METHODS ───────────────────────
    private async Task EnqueueAsync(string path, Action<string> operation)
    {
        string fullPath = Path.GetFullPath(path);

        var turnDone = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Task previousTurn = SwitchQueue(fullPath, turnDone.Task);

        try
        {
            await previousTurn.ConfigureAwait(false);

            await Task.Run(() =>
            {
                RetryOldCleanups();
                operation(fullPath);
            }).ConfigureAwait(false);
        }
        finally
        {
            turnDone.SetResult();
            _queue.TryRemove(KeyValuePair.Create(fullPath, turnDone.Task));
        }
    }

    private Task SwitchQueue(string fullPath, Task newQueue)
    {
        while (true)
        {
            if (_queue.TryGetValue(fullPath, out Task? currentQueue))
            {
                if (_queue.TryUpdate(fullPath, newQueue, currentQueue))
                    return currentQueue;
            }
            else if (_queue.TryAdd(fullPath, newQueue))
            {
                return Task.CompletedTask;
            }

            // Retry
        }
    }

    private void RetryOldCleanups()
    {
        foreach (string tempPath in _pendingCleanup.Keys)
        {
            if (_pendingCleanup.TryRemove(tempPath, out _) && !AtomicFile.TryDelete(tempPath))
                AddToCleanup(tempPath);
        }
    }

    private void AddToCleanup(string tempPath)
        => _pendingCleanup.TryAdd(tempPath, 0);
}