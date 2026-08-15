// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Domains.Infrastructure;
using SimplyDraft.Infrastructure.Utils;

namespace SimplyDraft.Infrastructure.Services;

public sealed class AtomicFileWriter : IAtomicFileWriter
{
    private readonly ConcurrentDictionary<string, Lazy<ChannelWriter<AtomicWriteRequest>>> _queues = new(PathComparer);
    private readonly ConcurrentDictionary<string, byte> _pendingCleanup = new(PathComparer);
    private static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    
    public Task QueueWrite(string path, string contents, Encoding? encoding = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contents);

        string fullPath = Path.GetFullPath(path);

        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        AtomicWriteRequest request = new(contents, encoding, completion);

        ChannelWriter<AtomicWriteRequest> writer = _queues.GetOrAdd(
            fullPath,
            path => new Lazy<ChannelWriter<AtomicWriteRequest>>(() => CreateQueue(path))).Value;

        if (!writer.TryWrite(request))
            throw new IOException($"Unable to queue write for '{path}'");
        
        return completion.Task;
    }

    private ChannelWriter<AtomicWriteRequest> CreateQueue(string path)
    {
        var channel = Channel.CreateUnbounded<AtomicWriteRequest>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            }
        );

        _ = Task.Run(() => ProcessQueueAsync(path, channel.Reader));

        return channel.Writer;
    }

    private async Task ProcessQueueAsync(string path, ChannelReader<AtomicWriteRequest> reader)
    {
        await foreach (AtomicWriteRequest request in reader.ReadAllAsync())
        {
            try
            {
                RetryOldCleanups();

                AtomicFile.WriteTo(
                    path: path,
                    contents: request.Contents,
                    encoding: request.Encoding,
                    AddToCleanup);
                
                request.Completion.TrySetResult(true);
            }
            catch (Exception ex)
            {
                request.Completion.TrySetException(ex);
            }
        }
    }

    private void RetryOldCleanups()
    {
        foreach (string tempPath in _pendingCleanup.Keys)
        {
            if (!_pendingCleanup.TryRemove(tempPath, out _))
                continue;
            
            try { File.Delete(tempPath); }
            catch { AddToCleanup(tempPath); }
        }
    }

    private void AddToCleanup(string tempPath)
        => _pendingCleanup.TryAdd(tempPath, 0);
}