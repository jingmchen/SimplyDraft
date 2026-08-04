using System.Globalization;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Domains.Markup;
using SimplyDraft.Engine.Markup;

namespace SimplyDraft.Engine.Services;

public sealed class MarkupEngine : IMarkupEngine
{
    public MarkupDocument Parse(string text)
        => ParseCore(text, DateTime.Now, CultureInfo.InvariantCulture);
    
    private static MarkupDocument ParseCore(string text, DateTime today, CultureInfo? culture = null)
        => new MarkupDocumentBuilder(today, culture ?? CultureInfo.InvariantCulture).Run(text ?? "");

    public MarkupResult Run(string generatedText, bool wrap)
    {
        // Pase
        var document = Parse(generatedText);

        // Render
        var rendered = MarkupRenderer.Render(document, wrap);

        return new MarkupResult(document, rendered);
    }
}