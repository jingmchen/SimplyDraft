namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface IFileSystem
{
    Task<string> ReadAllTextAsync(string path, CancellationToken ct);
    Task WriteAllTextAsync(string path, string text, CancellationToken ct);
    bool FileExists(string path);
    void CreateDirectory(string path);
}