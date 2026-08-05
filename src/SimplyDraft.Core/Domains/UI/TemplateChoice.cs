// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Library;

namespace SimplyDraft.Core.Domains.UI;

public sealed record TemplateChoice(LibraryItem Item)
{
    public override string ToString() => Item.Name;
}