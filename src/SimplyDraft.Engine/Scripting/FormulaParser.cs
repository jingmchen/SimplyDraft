// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Engine.Constants;
using SimplyDraft.Core.Domains.Scripting.Expressions;
using SimplyDraft.Core.Exceptions;

namespace SimplyDraft.Engine.Scripting;

public static class FormulaParser
{
    public static (Dictionary<string, Value> Vars, Dictionary<string, string> FormulaSrc) Seed(
        IReadOnlyDictionary<string, string> defaults,
        IReadOnlyDictionary<string, string>? childValues)
    {
        var mergedRawValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in defaults)
            mergedRawValues[entry.Key] = entry.Value;
        
        if (childValues != null)
            foreach (var entry in childValues)
                mergedRawValues[entry.Key] = entry.Value;

        var variables = new Dictionary<string, Value>(StringComparer.OrdinalIgnoreCase);
        var formulaSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in mergedRawValues)
        {
            var rawValue = entry.Value ?? "";
            
            if (rawValue.Length > 1 && rawValue[0] == ScriptingConstants.Formula.EscapeChar && (rawValue[1] == ScriptingConstants.Formula.Prefix || rawValue[1] == ScriptingConstants.Formula.EscapeChar))
            {
                variables[entry.Key] = Value.Str(rawValue[1..]);
            }
            else if (rawValue.Length > 1 && rawValue[0] == ScriptingConstants.Formula.Prefix)
            {
                formulaSources[entry.Key] = rawValue[1..];
                variables[entry.Key] = Value.Str(""); // defined-but-pending until evaluated
            }
            else
            {
                variables[entry.Key] = Value.Str(rawValue);
            }
        }
        return (variables, formulaSources);
    }

    public static bool EvaluateAll(Dictionary<string, string> formulaSources,
        Dictionary<string, Value> variables,
        Interpreter interpreter,
        GenerationResult result,
        bool isPreview)
    {
        var expressions = new Dictionary<string, Expression>(StringComparer.OrdinalIgnoreCase);
        var dependencies = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        bool allParsed = true;

        foreach (var entry in formulaSources)
        {
            try
            {
                var expression = Parser.ParseExpressionOnly(entry.Value);
                expressions[entry.Key] = expression;
                var referencedNames = new List<string>();
                CollectVarRefs(expression, referencedNames);

                dependencies[entry.Key] = referencedNames
                    .Where(formulaSources.ContainsKey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (ScriptException ex)
            {
                result.Diagnostics.Add(new Diagnostic(
                    ex.Diagnostic.Code,
                    DiagnosticSeverity.Error,
                    $"in formula for {{{entry.Key}}}: {ex.Diagnostic.Message}",
                    ex.Diagnostic.Line, ex.Diagnostic.Column));

                if (!isPreview)
                    allParsed = false;
            }
        }

        if (!allParsed)
            return false;

        var evaluated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in formulaSources.Keys)
            if (!expressions.ContainsKey(name))
                evaluated.Add(name);
        
        bool madeProgress = true;

        while (madeProgress)
        {
            madeProgress = false;
            
            foreach (var name in expressions.Keys.ToList())
            {
                if (evaluated.Contains(name))
                    continue;
                
                if (dependencies[name].Any(dependency => !evaluated.Contains(dependency)))
                    continue;
                
                try
                {
                    variables[name] = interpreter.Evaluate(expressions[name]);
                }
                catch (ScriptException ex)
                {
                    result.Diagnostics.Add(new Diagnostic(
                        ex.Diagnostic.Code,
                        DiagnosticSeverity.Error,
                        $"in formula for {{{name}}}: {ex.Diagnostic.Message}",
                        ex.Diagnostic.Line, ex.Diagnostic.Column));

                    if (!isPreview)
                        return false;
                    
                    variables[name] = Value.Str("");
                }
                catch (Exception ex)
                {
                    result.Diagnostics.Add(new Diagnostic(
                        DiagnosticCode.SyntaxError,
                        DiagnosticSeverity.Error,
                        $"in formula for {{{name}}}: internal error: {ex.Message}",
                        1, 1));
                    
                    if (!isPreview)
                        return false;
                    
                    variables[name] = Value.Str("");
                }
                evaluated.Add(name);
                madeProgress = true;
            }
        }

        var cyclicNames = expressions.Keys.Where(name => !evaluated.Contains(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
        
        if (cyclicNames.Count > 0)
        {
            result.Diagnostics.Add(new Diagnostic(
                DiagnosticCode.CircularFormula,
                DiagnosticSeverity.Error,
                "circular reference between formula variables: " + string.Join(", ", cyclicNames.Select(name => "{" + name + "}")),
                1, 1));
            
            if (!isPreview)
                return false;
            
            foreach (var name in cyclicNames)
                variables[name] = Value.Str("");
        }
        return true;
    }

    public static void ValidateDeclaredTypes(
        IReadOnlyDictionary<string, string> types,
        IReadOnlyDictionary<string, Value> variables,
        List<Diagnostic> diagnostics)
    {
        foreach (var entry in types)
        {
            if (!variables.TryGetValue(entry.Key, out var value))
                continue;
            var text = value.Render();

            if (text.Length == 0)
                continue;
            
            bool isValid =
                entry.Value.ToLowerInvariant() switch
                {
                    "number" => double.TryParse(text, NumberStyles.Float | NumberStyles.AllowLeadingSign,
                        CultureInfo.InvariantCulture, out _),
                    
                    "date" => DateTime.TryParseExact(text, ScriptingConstants.Temporal.IsoDate, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out _),
                    
                    "time" => TimeSpan.TryParseExact(text, ScriptingConstants.Temporal.TimeFormats, CultureInfo.InvariantCulture, out _),
                    
                    "yesno" => text.ToLowerInvariant() is "yes" or "no" or "true" or "false" or "1" or "0",
                    
                    _ => true
                };
            
            if (!isValid)
            {
                var truncated = text.Length > 40 ? text[..40] + "…" : text;

                diagnostics.Add(new Diagnostic(
                    DiagnosticCode.DeclaredTypeMismatch,
                    DiagnosticSeverity.Warning,
                    $"value \"{truncated}\" of {{{entry.Key}}} doesn't match declared type {entry.Value}",
                    1, 1));
            }
        }
    }

    private static void CollectVarRefs(Expression root, List<string> referencedNames)
    {
        var pending = new Stack<Expression>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var expression = pending.Pop();
            if (expression is NameExpression nameExpr)
                referencedNames.Add(nameExpr.Name);
            
            foreach (var child in expression.SubExpressions)
                pending.Push(child);
        }
    }
}