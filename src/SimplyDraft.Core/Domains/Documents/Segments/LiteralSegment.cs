// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Documents.Segments;

public sealed class LiteralSegment : Segment
{
    public string Text {get;}

    public LiteralSegment(string text, int line, int column) : base(line, column)
        => Text = text ?? throw new ArgumentNullException(nameof(text));
}