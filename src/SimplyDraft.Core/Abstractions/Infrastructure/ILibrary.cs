// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Library;

namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface ILibrary
{
    bool ToSeed {get;}

    List<LibraryItem> Scan();

    string CreateTemplate(string name);
    TemplateDocument LoadTemplate(string path);
    void SaveTemplate(TemplateDocument doc);

    string CreateChild(string templatePath, string name);
    string CreateBakedChild(string templatePath, string name, string generatedText, FrontMatter templateFm);
    ChildDocument LoadChild(string path);
    void SaveChild(ChildDocument doc);

    string Duplicate(LibraryItem item);
    string Rename(LibraryItem item, string newName);
    void MoveToTrash(LibraryItem item);
    void PurgeTrash(int days);
    
    (string Text, List<Diagnostic> Warnings) ExpandIncludes(string contentText);
    (DateTime Created, DateTime Modified) GetTimestamps(string filePath);
    IReadOnlyList<string> ListSeedTemplates();
    string CreateTemplateFromSeed(string templateName);
    int SeedMissingTemplates();
}