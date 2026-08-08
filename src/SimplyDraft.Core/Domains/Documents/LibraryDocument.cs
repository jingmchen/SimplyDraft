// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Documents;

public abstract class LibraryDocument
{
    public string FilePath {get; set;} = "";
    public string Body {get; set;} = "";
    public FrontMatter Fm {get; set;} = new();
    public IReadOnlyList<Diagnostic> LoadDiagnostics {get; set;} = [];

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Fm.Name)
            ? Path.GetFileNameWithoutExtension(FilePath)
            : Fm.Name;
}