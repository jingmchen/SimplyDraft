using SimplyDraft.Core.Domains.Batch;

namespace SimplyDraft.Core.Abstractions.Engine;

public interface IBatchGenerator
{
    Task<BatchResult> RunAsync(BatchRequest request, IProgress<(int Done, int Total)>? progress, CancellationToken cancellationToken);
}
