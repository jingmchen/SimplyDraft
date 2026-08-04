namespace SimplyDraft.Core.Domains.Batch;

public sealed class BatchResult
{
    public List<BatchRowResult> Rows {get;} = [];
    public string? ReportPath {get; set;}
    public int OkCount => Rows.Count(r => r.Ok);
    public int FailCount => Rows.Count(r => !r.Ok);
}