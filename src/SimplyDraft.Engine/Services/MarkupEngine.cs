// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Domains.Markup;
using SimplyDraft.Engine.Markup;

namespace SimplyDraft.Engine.Services;

public sealed class MarkupEngine : IMarkupEngine
{
    private readonly IRenderEngine _renderer;

    public MarkupEngine(IRenderEngine renderer)
        => _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

    public MarkupDocument Parse(string text)
        => Parse(text, DateTime.Now, CultureInfo.InvariantCulture);
    
    public static MarkupDocument Parse(string text, DateTime today, CultureInfo? culture = null)
        => new MarkupParser(today, culture ?? CultureInfo.InvariantCulture).Parse(text ?? "");

    public MarkupResult Run(string generatedText, bool wrap)
    {
        // Parse
        var document = Parse(generatedText);

        // Render
        var rendered = _renderer.Render(document, wrap);
        
        return new MarkupResult(document, rendered);
    }
}