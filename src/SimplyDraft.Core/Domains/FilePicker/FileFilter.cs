// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.FilePicker;

public sealed record FileFilter(
    string Name,
    IReadOnlyList<string> Patterns
);