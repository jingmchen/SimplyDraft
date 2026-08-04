using System.Globalization;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Abstractions.Engine;

public interface IScriptingEngine
{
    GenerationResult Run(GenerationRequest request);
    (GenerationResult Gen, FrontMatter TemplateFm, string Name) GenerateItem(
        ILibrary library, LibraryItem item, GenerationMode mode, MissingVariablePolicy policy, CultureInfo culture);
}