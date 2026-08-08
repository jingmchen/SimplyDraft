// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Markup.Inlines;

namespace SimplyDraft.Core.Domains.Markup.Blocks;

public sealed class RowBlock : Block
{
    public List<List<Inline>> Cells {get;} = [];
}