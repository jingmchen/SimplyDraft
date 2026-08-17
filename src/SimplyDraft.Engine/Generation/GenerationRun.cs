// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using System.Text;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Documents.Segments;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Core.Domains.Scripting.Expressions;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Exceptions;
using SimplyDraft.Engine.Constants;
using SimplyDraft.Engine.Scripting;

namespace SimplyDraft.Engine.Generation;

public sealed class GenerationRun
{
    private readonly Interpreter _interpreter;
    private readonly GenerationRequest _request;
    private readonly GenerationResult _result;
    private readonly ScriptScope _scope;
    private readonly StringBuilder _output = new();
    private readonly Dictionary<string, Value> _variables;
    private readonly Dictionary<string, string> _formulaSources;
    private readonly HashSet<string> _reportedUndefined = new(StringComparer.OrdinalIgnoreCase);
    public bool IsPreview {get;}
    public string OutputText => _output.ToString();

    public GenerationRun(GenerationRequest request, GenerationResult result)
    {
        var now = request.Clock ?? DateTime.Now;
        var culture = request.FormatCulture ?? CultureInfo.CurrentCulture;

        _request = request;
        _result = result;

        (_variables, _formulaSources) = FormulaParser.Seed(request.TemplateDefaults, request.ChildValues);

        _scope = new ScriptScope(
            _variables,
            new BuiltinContext(now, request.Doc),
            culture,
            BuildInputFallback(request.PreviewInputs, _variables));
        
        _interpreter = new Interpreter(_scope);

        IsPreview = request.Mode == GenerationMode.Preview;
    }

    public bool EvaluateFormulas()
        => _formulaSources.Count == 0 || FormulaParser.EvaluateAll(
            _formulaSources, _variables, _interpreter, _result, IsPreview);
    
    public bool WriteSegments(List<Segment> segments)
    {
        foreach (var segment in segments)
        {
            bool keepGoing = segment switch
            {
                LiteralSegment literal => WriteLiteral(literal),
                ScriptSegment script => RunScript(script),
                PlaceholderSegment {IsBuiltin: true} builtinRef => WriteBuiltin(builtinRef),
                PlaceholderSegment placeholder => WritePlaceholder(placeholder),
                InlineExpressionSegment expression => WriteInlineExpression(expression),
                _ => true
            };
            
            if (!keepGoing)
                return false;
        }
        return true;
    }

    public void ValidateDeclaredTypes()
    {
        if (_request.VariableTypes is {Count: > 0} declaredTypes)
            FormulaParser.ValidateDeclaredTypes(declaredTypes, _variables, _result.Diagnostics);
    }

    private bool WriteLiteral(LiteralSegment literal)
    {
        _output.Append(literal.Text);
        return true;
    }

    private bool RunScript(ScriptSegment script)
    {
        try
        {
            var statements = Parser.ParseScript(script.Source, script.Line);
            _interpreter.Execute(statements);
            return true;
        }
        catch (ScriptException ex)
        {
            return Report(ex.Diagnostic);
        }
        catch (Exception ex)
        {
            return Report(new Diagnostic(
                DiagnosticCode.SyntaxError,
                DiagnosticSeverity.Error,
                "internal error in script: " + ex.Message, script.Line, 1));
        }
    }

    private bool WriteBuiltin(PlaceholderSegment builtinRef)
    {
        var value = _scope.ResolveBuiltin(builtinRef.Namespace, builtinRef.Member);

        if (value is null)
            return Report(new Diagnostic(
                DiagnosticCode.UnknownBuiltin,
                DiagnosticSeverity.Error,
                $"unknown built-in {builtinRef.Namespace}.{builtinRef.Member}",
                builtinRef.Line, builtinRef.Column));
        
        _output.Append(value.Render());
        return true;
    }

    private bool WritePlaceholder(PlaceholderSegment placeholder)
    {
        if (_variables.TryGetValue(placeholder.Name, out var value))
        {
            _output.Append(value.Render());
            return true;
        }
        
        ReportUndefinedOnce(placeholder);
        
        if (IsPreview)
        {
            AppendMissingPlaceholderMarker(placeholder.Name);
            return true;
        }

        switch (_request.Policy)
        {
            case MissingVariablePolicy.ErrorOnExport:
                return false;

            case MissingVariablePolicy.LeavePlaceholder:
                _output.Append(ScriptingConstants.Template.PlaceholderOpen)
                       .Append(placeholder.Name)
                       .Append(ScriptingConstants.Template.PlaceholderClose);
                return true;

            case MissingVariablePolicy.EmptyString:
            default:
                return true;
        }
    }

    private bool WriteInlineExpression(InlineExpressionSegment expression)
    {
        try
        {
            var parsed = Parser.ParseExpressionOnly(expression.Source, expression.Line, expression.Column + 2);
            _output.Append(_interpreter.Evaluate(parsed).Render());
            return true;
        }
        catch (ScriptException ex)
        {
            return Report(ex.Diagnostic);
        }
        catch (Exception ex)
        {
            return Report(new Diagnostic(
                DiagnosticCode.SyntaxError,
                DiagnosticSeverity.Error,
                "internal error in expression: " + ex.Message,
                expression.Line, expression.Column));
        }
    }

    private bool Report(Diagnostic diagnostic)
    {
        _result.Diagnostics.Add(diagnostic);
        
        if (!IsPreview)
            return false;
        
        _output.Append('⟦').Append(ShortCode(diagnostic.Code)).Append(": ").Append(diagnostic.Message).Append('⟧');
        return true;
    }

    private void ReportUndefinedOnce(PlaceholderSegment placeholder)
    {
        if (!_reportedUndefined.Add(placeholder.Name))
            return;
        
        var severity = !IsPreview && _request.Policy == MissingVariablePolicy.ErrorOnExport
            ? DiagnosticSeverity.Error
            : DiagnosticSeverity.Warning;
        
        _result.Diagnostics.Add(new Diagnostic(
            DiagnosticCode.UndefinedVariable,
            severity,
            $"variable {{{placeholder.Name}}} has no value", placeholder.Line, placeholder.Column));
    }

    private void AppendMissingPlaceholderMarker(string name)
        => _output.Append('⟦')
                  .Append(ScriptingConstants.Template.PlaceholderOpen)
                  .Append(name)
                  .Append(ScriptingConstants.Template.PlaceholderClose)
                  .Append('⟧');

    private static Dictionary<string, Value>? BuildInputFallback(
        IReadOnlyCollection<string>? previewInputs,
        Dictionary<string, Value> variables)
    {
        if (previewInputs is not {Count: > 0})
            return null;
        
        var fallback = new Dictionary<string, Value>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var name in previewInputs)
            if (!variables.ContainsKey(name))
                fallback[name] = Value.Str("");
        
        return fallback.Count > 0 ? fallback : null;
    }

    private static string ShortCode(DiagnosticCode code) => "E" + (int)code;
}