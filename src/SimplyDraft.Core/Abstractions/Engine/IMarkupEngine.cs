// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Markup;

namespace SimplyDraft.Core.Abstractions.Engine;

public interface IMarkupEngine
{
    MarkupDocument Parse(string text);
    MarkupResult Run(string generatedText, bool wrap);
}