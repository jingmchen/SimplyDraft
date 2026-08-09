// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.UI;

public sealed record HelpTopic(
    string Title,
    string? Intro,
    IReadOnlyList<HelpEntry> Entries,
    string? Note = null
);