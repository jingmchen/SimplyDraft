using System.Text.Json.Serialization;

namespace SimplyDraft.Core.Models;

public enum VariableType
{
    Fixed, // Informational variable
    Instance, // Instance variable
    Global, // Every occurrence
    Conditional // Conditional variable
}

public sealed class ConditionalRule
{
    public string WhenValue {get; set;} = string.Empty;
    public string ThenSubstitute {get; set;} = string.Empty;
}

public abstract class Variable
{
    public string Key {get; set;} = string.Empty;
    public string Label {get; set;} = string.Empty;
    public VariableType Type {get; set;}
    public string Description {get; set;} = string.Empty;
    public string Content {get; set;} = string.Empty;
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
    public int Occurrences {get; set;}

    [JsonIgnore]
    public string? RuntimeValue { get; set; }

    public Variable()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Occurrences = 1;
    }
}

public sealed class FixedVariable : Variable
{
    // Placeholder
}

public sealed class InstanceVariable : Variable
{
    public int? ParagraphIndex {get; set;}
}

public sealed class GlobalVariable : Variable
{
    
}

public sealed class ConditionalVariable : Variable
{
    public List<ConditionalRule> ConditionalRules { get; set; } = new();
}