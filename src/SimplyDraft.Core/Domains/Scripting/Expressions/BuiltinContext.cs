// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Documents;

namespace SimplyDraft.Core.Domains.Scripting.Expressions;

public sealed record BuiltinContext(
    DateTime Now,
    DocInfo Doc
);