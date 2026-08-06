// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Document.Segments;

public sealed class ScriptSegment : Segment
{
    public string Source {get;}

    public ScriptSegment(string source, int line, int column) : base(line, column)
        => Source = source ?? throw new ArgumentNullException(nameof(source));
}