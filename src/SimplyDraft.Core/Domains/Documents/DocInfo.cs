// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Documents;

public sealed record DocInfo(
    string Name,
    string TemplateName,
    DateTime Created,
    DateTime Modified
);