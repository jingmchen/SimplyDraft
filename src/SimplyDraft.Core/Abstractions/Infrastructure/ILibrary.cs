// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Library;

namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface ILibrary
{
    List<LibraryItem> Scan();

    Task<string> CreateTemplateAsync(string name);
    TemplateDocument LoadTemplate(string path);
    Task SaveTemplateAsync(TemplateDocument doc);

    Task<string> CreateChildAsync(string templatePath, string name);
    Task<string> CreateBakedChildAsync(string templatePath, string name, string generatedText, FrontMatter templateFm);
    ChildDocument LoadChild(string path);
    Task SaveChildAsync(ChildDocument doc);

    Task<string> DuplicateAsync(LibraryItem item);
    Task<string> RenameAsync(LibraryItem item, string newName);
    Task MoveToTrashAsync(LibraryItem item);
    void PurgeTrash(int days);
    
    (string Text, List<Diagnostic> Warnings) ExpandIncludes(string contentText);
    (DateTime Created, DateTime Modified) GetTimestamps(string filePath);
    IReadOnlyList<string> ListSeedTemplates();
    Task<string> CreateTemplateFromSeedAsync(string templateName, string? newName = null);
    Task<int> SeedMissingTemplatesAsync();
}