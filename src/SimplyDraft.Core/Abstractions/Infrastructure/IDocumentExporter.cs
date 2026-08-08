// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Export;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface IDocumentExporter
{
    DocumentKind Id {get;}
    string DisplayName {get;}
    string FileExtension {get;}
    bool IsAvailable();
    Task ExportAsync(GeneratedDocument doc, string outputPath, ExportOptions options, CancellationToken ct);
}