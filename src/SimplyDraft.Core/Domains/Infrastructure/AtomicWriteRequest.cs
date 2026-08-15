// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;

namespace SimplyDraft.Core.Domains.Infrastructure;

public sealed record AtomicWriteRequest(
    string Contents,
    Encoding? Encoding,
    TaskCompletionSource<bool> Completion
);