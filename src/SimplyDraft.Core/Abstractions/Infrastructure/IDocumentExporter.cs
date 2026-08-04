using SimplyDraft.Core.Domains.Exporting;
using SimplyDraft.Core.Domains.Generation;

namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface IDocumentExporter
{
    string Id {get;}
    string DisplayName {get;}
    string FileExtension {get;}
    bool IsAvailable();
    Task ExportAsync(GeneratedDocument doc, ExportOptions options, string outputPath, CancellationToken ct);
}