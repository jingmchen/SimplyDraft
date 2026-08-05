// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Exporting;
using SimplyDraft.Core.Domains.Generation;

namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface IDocumentExporter
{
    string Id {get;}
    string DisplayName {get;}
    string FileExtension {get;}
    bool IsAvailable();
    Task ExportAsync(GeneratedDocument doc, string outputPath, ExportOptions options, CancellationToken ct);
}