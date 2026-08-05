// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Batch;

public sealed class BatchResult
{
    public List<BatchRowResult> Rows {get;} = [];
    public string? ReportPath {get; set;}
    public int OkCount => Rows.Count(r => r.Ok);
    public int FailCount => Rows.Count(r => !r.Ok);
}