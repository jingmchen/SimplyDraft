// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Core.Domains.Scripting.Expressions;
using SimplyDraft.Engine.Utils;

namespace SimplyDraft.Engine.Scripting;

public sealed class ScriptScope
{
    private static readonly IReadOnlyDictionary<string, Value> NoFallback = new Dictionary<string, Value>(0);
    public Dictionary<string, Value> Variables {get;}
    public BuiltinContext BuiltinContext {get;}
    public CultureInfo FormatCulture {get;}
    public IReadOnlyDictionary<string, Value> InputFallback {get;}

    public ScriptScope(
        Dictionary<string, Value> variables,
        BuiltinContext builtinContext,
        CultureInfo formatCulture,
        IReadOnlyDictionary<string, Value>? inputFallback = null)
    {
        Variables = variables ?? throw new ArgumentNullException(nameof(variables));
        BuiltinContext = builtinContext ?? throw new ArgumentNullException(nameof(builtinContext));
        FormatCulture = formatCulture ?? throw new ArgumentNullException(nameof(formatCulture));
        InputFallback = inputFallback ?? NoFallback;
    }

    public Value? ResolveBuiltin(string ns, string member)
        => SystemBuiltins.Lookup(BuiltinContext, ns, member);
}