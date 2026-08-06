// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Markup.Inlines;

namespace SimplyDraft.Core.Domains.Markup.Blocks;

public sealed class ImageBlock : Block
{
    public string Path {get; set;} = "";
    public bool Centered {get; set;}
    public int FigureNumber {get; set;}
    public List<Inline> Caption {get;} = [];
}