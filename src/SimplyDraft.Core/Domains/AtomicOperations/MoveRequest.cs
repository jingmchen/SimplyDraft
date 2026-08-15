// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.AtomicOperations;

public sealed record MoveRequest(
    string DestinationPath,
    bool Overwrite,
    TaskCompletionSource<bool> Completion
) : FileOperation(Completion);