// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Documents.Segments;

public sealed class InlineExpressionSegment : Segment
{
    public string Source {get;}

    public InlineExpressionSegment(string source, int line, int column) : base(line, column)
        => Source = source ?? throw new ArgumentNullException(nameof(source));
}