// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting.Completion;

public sealed record CompletionItem(
    string Text,
    string InsertText,
    string Description,
    int CaretBack = 0,
    double Priority = 0
);