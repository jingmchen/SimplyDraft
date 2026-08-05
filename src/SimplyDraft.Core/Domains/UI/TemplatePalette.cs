// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.UI;

public sealed record TemplatePalette(
    string Escape,
    string Expression,
    string Placeholder,
    string MarkupCommand
);