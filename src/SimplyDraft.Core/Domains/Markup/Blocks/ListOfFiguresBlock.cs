// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Markup.Entries;

namespace SimplyDraft.Core.Domains.Markup.Blocks;

public sealed class ListOfFiguresBlock : Block
{
    public List<FigureEntry> Entries {get;} = [];
}