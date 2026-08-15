// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.AtomicOperations;

public abstract record FileOperation(
    TaskCompletionSource<bool> Completion
);