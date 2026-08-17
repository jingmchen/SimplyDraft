// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Export;

public sealed class ExporterCatalog
{
    public IReadOnlyList<IDocumentExporter> All {get;}
    
    public ExporterCatalog(IEnumerable<IDocumentExporter> exporters)
    {
        ArgumentNullException.ThrowIfNull(exporters);
        All = [.. exporters];
    }

    public IEnumerable<IDocumentExporter> Available
        => All.Where(exporter => exporter.IsAvailable());
    
    public IDocumentExporter Resolve(DocumentKind kind)
        => All.FirstOrDefault(exporter => exporter.Id == kind)
            ?? throw new InvalidOperationException($"No document exporter is registered for {kind}.");
}