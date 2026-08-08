// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Batch;

public sealed record BatchRowResult(
    int RowNumber,
    string FileName,
    bool Ok,
    string Message
);