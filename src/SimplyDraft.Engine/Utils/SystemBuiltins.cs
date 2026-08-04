using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Utils;

public sealed class SystemBuiltins : IBuiltinProvider
{
    private readonly DateTime _now;
    private readonly DocInfo _doc;
    
    public SystemBuiltins(DateTime now, DocInfo doc)
    {
        _now = now;
        _doc = doc;
    }

    public Value? Lookup(string ns, string member)
    {
        if (ns.Equals(ScriptingConstants.Builtins.System, StringComparison.OrdinalIgnoreCase))
        {
            return member.ToLowerInvariant() switch
            {
                ScriptingConstants.Builtins.SystemMembers.Now => Value.DateTimeVal(_now),
                ScriptingConstants.Builtins.SystemMembers.Date => Value.DateVal(_now.Date),
                ScriptingConstants.Builtins.SystemMembers.Time => Value.TimeVal(_now.TimeOfDay),
                ScriptingConstants.Builtins.SystemMembers.Year => Value.Num(_now.Year),
                ScriptingConstants.Builtins.SystemMembers.Month => Value.Num(_now.Month),
                ScriptingConstants.Builtins.SystemMembers.Day => Value.Num(_now.Day),
                ScriptingConstants.Builtins.SystemMembers.UserName => Value.Str(Environment.UserName),
                ScriptingConstants.Builtins.SystemMembers.Machine => Value.Str(Environment.MachineName),
                ScriptingConstants.Builtins.SystemMembers.Os => Value.Str(OperatingSystem.IsWindows() ? "Windows"
                    : OperatingSystem.IsMacOS() ? "macOS"
                    : OperatingSystem.IsLinux() ? "Linux" : "Other"),
                _ => null
            };
        }
        if (ns.Equals(ScriptingConstants.Builtins.Doc, StringComparison.OrdinalIgnoreCase))
        {
            return member.ToLowerInvariant() switch
            {
                ScriptingConstants.Builtins.DocMembers.Name => Value.Str(_doc.Name),
                ScriptingConstants.Builtins.DocMembers.Template => Value.Str(_doc.TemplateName),
                ScriptingConstants.Builtins.DocMembers.Created => Value.DateTimeVal(_doc.Created),
                ScriptingConstants.Builtins.DocMembers.Modified => Value.DateTimeVal(_doc.Modified),
                _ => null
            };
        }
        return null;
    }
}