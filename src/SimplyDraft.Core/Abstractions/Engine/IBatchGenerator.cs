// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Batch;

namespace SimplyDraft.Core.Abstractions.Engine;

public interface IBatchGenerator
{
    Task<BatchResult> RunAsync(
        BatchRequest request,
        IProgress<(int Done, int Total)>? progress,
        CancellationToken ct);
}
