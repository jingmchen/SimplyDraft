// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Abstractions.UI;

public interface IExportService
{
    Task<string?> ExportAsync(
        string suggestedBaseName, DocumentKind formatKind,
        GenerationResult result, FrontMatter templateFm, string? baseDirectory = null
    );
    
    (GenerationResult Result, FrontMatter TemplateFm, string Name) GenerateItem(LibraryItem item, GenerationMode mode);
}