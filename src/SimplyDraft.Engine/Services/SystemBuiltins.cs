// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Core.Domains.Scripting.Expressions;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Services;

public sealed class SystemBuiltins : IBuiltinProvider
{
    public Value? Lookup(BuiltinContext context, string ns, string member)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(ns);
        ArgumentNullException.ThrowIfNull(member);

        if (ns.Equals(ScriptingConstants.Builtins.System, StringComparison.OrdinalIgnoreCase))
            return LookupSystem(context.Now, member.ToLowerInvariant());
        if (ns.Equals(ScriptingConstants.Builtins.Doc, StringComparison.OrdinalIgnoreCase))
            return LookupDoc(context.Doc, member.ToLowerInvariant());
        return null;
    }

    private static Value? LookupSystem(DateTime now, string member) => member switch
    {
        ScriptingConstants.Builtins.SystemMembers.Now => Value.DateTimeVal(now),
        ScriptingConstants.Builtins.SystemMembers.Date => Value.DateVal(now.Date),
        ScriptingConstants.Builtins.SystemMembers.Time => Value.TimeVal(now.TimeOfDay),
        ScriptingConstants.Builtins.SystemMembers.Year => Value.Num(now.Year),
        ScriptingConstants.Builtins.SystemMembers.Month => Value.Num(now.Month),
        ScriptingConstants.Builtins.SystemMembers.Day => Value.Num(now.Day),
        ScriptingConstants.Builtins.SystemMembers.UserName => Value.Str(Environment.UserName),
        ScriptingConstants.Builtins.SystemMembers.Machine => Value.Str(Environment.MachineName),
        ScriptingConstants.Builtins.SystemMembers.Os => Value.Str(OperatingSystemName()),
        _ => null
    };

    private static Value? LookupDoc(DocInfo doc, string member) => member switch
    {
        ScriptingConstants.Builtins.DocMembers.Name => Value.Str(doc.Name),
        ScriptingConstants.Builtins.DocMembers.Template => Value.Str(doc.TemplateName),
        ScriptingConstants.Builtins.DocMembers.Created => Value.DateTimeVal(doc.Created),
        ScriptingConstants.Builtins.DocMembers.Modified => Value.DateTimeVal(doc.Modified),
        _ => null
    };

    private static string OperatingSystemName()
        => OperatingSystem.IsWindows() ? "Windows"
         : OperatingSystem.IsMacOS() ? "macOS"
         : OperatingSystem.IsLinux() ? "Linux"
         : "Other";
}