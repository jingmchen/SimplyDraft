// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.AtomicOperations;

public sealed record DeleteRequest(
    TaskCompletionSource<bool> Completion
) : FileOperation(Completion);