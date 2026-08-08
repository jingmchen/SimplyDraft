// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Domains.Export;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Enums;
using SimplyDraft.Infrastructure.Constants;

namespace SimplyDraft.Infrastructure.Services;

public sealed class TxtExporter : IDocumentExporter
{
    private readonly IMarkupEngine _markup;
    public DocumentKind Id => DocumentKind.Docx;
    public string DisplayName => "Plain text (.txt)";
    public string FileExtension => InfrastructureConstants.FileExtension.Txt;

    public TxtExporter(IMarkupEngine markup)
        => _markup = markup ?? throw new ArgumentNullException(nameof(markup));
    
    public bool IsAvailable() => true;
    public Task ExportAsync(GeneratedDocument doc, string outputPath, ExportOptions options, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(options);

        var source = doc.HasMarkup
            ? _markup.Run(doc.Text, wrap: true).Rendered
            : doc.Text;
        
        var text = source.Replace("\r\n", "\n").Replace('\r', '\n');

        bool crlf = options.NewLine == NewLineMode.CrLf || (options.NewLine == NewLineMode.Platform && Environment.NewLine == "\r\n");
        
        if (crlf)
            text = text.Replace("\n", "\r\n");
        
        return File.WriteAllTextAsync(outputPath, text, new UTF8Encoding(options.WriteBom), ct);
    }
}