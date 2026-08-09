// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Domains.Export;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Enums;
using SimplyDraft.Infrastructure.Constants;
using SimplyDraft.Infrastructure.Export;

namespace SimplyDraft.Infrastructure.Services;

public sealed class DocxExporter : IDocumentExporter
{
    private readonly IMarkupEngine _markup;
    public DocumentKind Id => DocumentKind.Docx;
    public string DisplayName => "Word document (.docx) — portable";
    public string FileExtension => InfrastructureConstants.FileExtension.Docx;

    public DocxExporter(IMarkupEngine markup)
        => _markup = markup ?? throw new ArgumentNullException(nameof(markup));
    
    public bool IsAvailable() => true;
    public Task ExportAsync(GeneratedDocument doc, string outputPath, ExportOptions options, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(doc);

        return Task.Run(() =>
        {
            if (doc.HasMarkup)
                DocxWriter.WriteMarkup(
                    outputPath, _markup.Parse(doc.Text), doc.FontName, doc.FontSizePt, doc.PageHeader, doc.BaseDirectory);
            else
                DocxWriter.Write(
                    outputPath, doc.Text, doc.FontName, doc.FontSizePt, doc.PageHeader);
        }, ct);
    }
}