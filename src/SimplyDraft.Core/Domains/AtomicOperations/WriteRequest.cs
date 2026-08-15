// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;

namespace SimplyDraft.Core.Domains.AtomicOperations;

public sealed record WriteRequest(
    string Contents,
    Encoding? Encoding,
    TaskCompletionSource<bool> Completion
) : FileOperation(Completion);