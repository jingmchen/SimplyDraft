using System.Globalization;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Domains.Scripting;

namespace SimplyDraft.Engine.Scripting;

public sealed class ScriptScope
{
    public Dictionary<string, Value> Vars {get;}
    public IBuiltinProvider Builtins {get;}
    public CultureInfo FormatCulture {get;}
    public IReadOnlyDictionary<string, Value> InputFallback {get;}
    private static readonly IReadOnlyDictionary<string, Value> NoFallback = new Dictionary<string, Value>(0);
    
    public ScriptScope(
        Dictionary<string, Value> vars, IBuiltinProvider builtins, CultureInfo formatCulture,
        IReadOnlyDictionary<string, Value>? inputFallback = null
    )
    {
        Vars = vars;
        Builtins = builtins;
        FormatCulture = formatCulture;
        InputFallback = inputFallback ?? NoFallback;
    }
}