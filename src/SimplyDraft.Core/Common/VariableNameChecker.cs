namespace SimplyDraft.Core.Common;

public static class VariableNameChecker
{
    public static bool IsValid(string? name) =>
        !string.IsNullOrEmpty(name)
            && (char.IsLetter(name[0]) || name[0] == '_')
            && name.All(c => char.IsLetterOrDigit(c) || c == '_');
}