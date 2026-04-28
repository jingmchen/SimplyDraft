using SimplyDraft.Core.Models;

namespace SimplyDraft.Core.Services;

public interface IDocumentService
{
    List<(int Index, string Text)> ExtractParagraphs(string filePath);
    Dictionary<string, int> FindExistingVariables(string filePath);
    bool InjectVariable(
        string filePath, string selectedText, string variableKey,
        VariableType type = VariableType.Instance, int paragraphIndex = -1
    );
    void ExportToChild(string filePath, string outputPath, List<Variable> variables);
}