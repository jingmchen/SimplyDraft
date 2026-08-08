// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Markup;
using SimplyDraft.Core.Domains.Markup.Blocks;

namespace SimplyDraft.Core.Abstractions.Engine;

public interface IRenderEngine
{
    string Render(MarkupDocument document);
    string Render(MarkupDocument document, bool wrap);
    string Render(IReadOnlyList<Block> blocks, bool wrap);
}