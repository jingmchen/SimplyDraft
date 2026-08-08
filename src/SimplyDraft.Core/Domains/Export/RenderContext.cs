// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Export;

public sealed class RenderContext
{
    public List<DocxMediaItem> Media {get;} = [];
    public bool HasTableOfContents {get; set;}
    public int DrawingCount {get; set;}
    public string? BaseDirectory {get; set;}
}