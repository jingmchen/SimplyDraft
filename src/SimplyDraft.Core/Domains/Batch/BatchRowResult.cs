namespace SimplyDraft.Core.Domains.Batch;

public sealed record BatchRowResult(
    int RowNumber,
    string FileName,
    bool Ok,
    string Message
);