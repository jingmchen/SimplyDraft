// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Markup.Blocks;

public sealed class TableBlock : Block
{
    public List<RowBlock> Rows {get;} = [];
    public List<char> Alignments {get;} = [];
    public int ColumnCount =>
        Rows.Count == 0
            ? 0
            : Rows.Max(r => r.Cells.Count);
}