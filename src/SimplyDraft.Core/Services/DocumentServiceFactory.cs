using SimplyDraft.Core.Configuration;

namespace SimplyDraft.Core.Services;

public static class DocumentServiceFactory
{
    public static IDocumentService For(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            CoreKeys.Document.FileExtension.DocumentKey => new DocxService(),
            CoreKeys.Document.FileExtension.TextKey => new TxtService(),
            _ => throw new NotSupportedException($"Unsupported format: {Path.GetExtension(filePath)}")
        };
}