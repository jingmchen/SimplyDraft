using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Document.Segments;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Exceptions;
using SimplyDraft.Engine.Constants;
using SimplyDraft.Engine.Parsing;
using SimplyDraft.Engine.Scripting;
using SimplyDraft.Engine.Utils;

namespace SimplyDraft.Engine.Services;

public sealed class ScriptingEngine : IScriptingEngine
{
    public GenerationResult Run(GenerationRequest request)
    {
        var result = new GenerationResult();

        // 1. Split the template body into literal / placeholder / script / inline-expression segments.
        List<Segment> segments;
        try
        {
            segments = TemplateParser.Parse(request.TemplateBody);
        }
        catch (ScriptException ex)
        {
            result.Diagnostics.Add(ex.Diagnostic);
            result.Success = false;
            return result;
        }

        // 2. Seed variables (defaults overlaid by child values) and split out '='-formula sources.
        var (variables, formulaSources) = FormulaParser.Seed(request.TemplateDefaults, request.ChildValues);

        var now = request.Clock ?? DateTime.Now;
        var culture = request.FormatCulture ?? CultureInfo.CurrentCulture;
        var scope = new ScriptScope(variables, new SystemBuiltins(now, request.Doc), culture,
            BuildInputFallback(request.PreviewInputs, variables));
        var interpreter = new Interpreter(scope);

        bool isPreview = request.Mode == GenerationMode.Preview;
        if (isPreview) LintScriptLikeText(request.TemplateBody, result.Diagnostics);
        bool hasFailed = false;
        var output = new StringBuilder();
        // Each undefined content placeholder is reported ONCE, however many times it appears in the
        // body, so the diagnostics read as a clean list of what is missing. The inline ⟦{name}⟧
        // marker below is still emitted at every occurrence, so the preview shows each spot.
        var reportedUndefined = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 3. No-code tier: Excel-style formulas run once, in dependency order, BEFORE any script —
        // scripts see the results and may still SET over them (script stays the top tier).
        if (formulaSources.Count > 0 && !FormulaParser.EvaluateAll(formulaSources, variables, interpreter, result, isPreview))
        {
            hasFailed = true;
            goto done;
        }

        // 4. Walk the segments in order, executing scripts and substituting placeholders / expressions.
        foreach (var segment in segments)
        {
            switch (segment)
            {
                case ScriptSegment scriptSegment:
                    try
                    {
                        var statements = Parser.ParseScript(scriptSegment.Source, scriptSegment.Line);
                        interpreter.Execute(statements);
                    }
                    catch (ScriptException ex)
                    {
                        result.Diagnostics.Add(ex.Diagnostic);
                        if (!isPreview) { hasFailed = true; goto done; }
                        output.Append('⟦').Append(Marker(ex.Diagnostic.Code)).Append(": ").Append(ex.Diagnostic.Message).Append('⟧');
                    }
                    catch (Exception ex)
                    {
                        var diagnostic = new Diagnostic(DiagnosticCode.SyntaxError, DiagnosticSeverity.Error,
                            "internal error in script: " + ex.Message, scriptSegment.Line, 1);
                        result.Diagnostics.Add(diagnostic);
                        if (!isPreview) { hasFailed = true; goto done; }
                        output.Append('⟦').Append(Marker(diagnostic.Code)).Append(": ").Append(diagnostic.Message).Append('⟧');
                    }
                    break;

                case LiteralSegment literalSegment:
                    output.Append(literalSegment.Text);
                    break;

                case PlaceholderSegment placeholder when placeholder.IsBuiltin:
                {
                    var builtinValue = scope.Builtins.Lookup(placeholder.Namespace, placeholder.Member);
                    if (builtinValue is null)
                    {
                        var diagnostic = new Diagnostic(DiagnosticCode.UnknownBuiltin, DiagnosticSeverity.Error,
                            $"unknown built-in {placeholder.Namespace}.{placeholder.Member}", placeholder.Line, placeholder.Column);
                        result.Diagnostics.Add(diagnostic);
                        if (!isPreview) { hasFailed = true; goto done; }
                        output.Append('⟦').Append(Marker(diagnostic.Code)).Append(": ").Append(diagnostic.Message).Append('⟧');
                    }
                    else
                    {
                        output.Append(builtinValue.Render());
                    }
                    break;
                }

                case PlaceholderSegment placeholder:
                    if (variables.TryGetValue(placeholder.Name, out var value))
                    {
                        output.Append(value.Render());
                    }
                    else
                    {
                        if (reportedUndefined.Add(placeholder.Name))
                        {
                            var severity = !isPreview && request.Policy == MissingVariablePolicy.ErrorOnExport
                                ? DiagnosticSeverity.Error
                                : DiagnosticSeverity.Warning;
                            result.Diagnostics.Add(new Diagnostic(DiagnosticCode.UndefinedVariable, severity,
                                $"variable {{{placeholder.Name}}} has no value", placeholder.Line, placeholder.Column));
                        }
                        if (isPreview)
                        {
                            output.Append('⟦').Append(ScriptingConstants.Template.PlaceholderOpen).Append(placeholder.Name).Append(ScriptingConstants.Template.PlaceholderClose).Append('⟧');
                        }
                        else
                        {
                            switch (request.Policy)
                            {
                                case MissingVariablePolicy.ErrorOnExport:
                                    hasFailed = true;
                                    goto done;
                                case MissingVariablePolicy.LeavePlaceholder:
                                    output.Append(ScriptingConstants.Template.PlaceholderOpen).Append(placeholder.Name).Append(ScriptingConstants.Template.PlaceholderClose);
                                    break;
                                case MissingVariablePolicy.EmptyString:
                                    break;
                            }
                        }
                    }
                    break;

                case InlineExpressionSegment inlineExpr:
                    try
                    {
                        var expression = Parser.ParseExpressionOnly(inlineExpr.Source, inlineExpr.Line, inlineExpr.Column + 2);
                        output.Append(interpreter.Eval(expression).Render());
                    }
                    catch (ScriptException ex)
                    {
                        result.Diagnostics.Add(ex.Diagnostic);
                        if (!isPreview) { hasFailed = true; goto done; }
                        output.Append('⟦').Append(Marker(ex.Diagnostic.Code)).Append(": ").Append(ex.Diagnostic.Message).Append('⟧');
                    }
                    catch (Exception ex)
                    {
                        var diagnostic = new Diagnostic(DiagnosticCode.SyntaxError, DiagnosticSeverity.Error,
                            "internal error in expression: " + ex.Message, inlineExpr.Line, inlineExpr.Column);
                        result.Diagnostics.Add(diagnostic);
                        if (!isPreview) { hasFailed = true; goto done; }
                        output.Append('⟦').Append(Marker(diagnostic.Code)).Append(": ").Append(diagnostic.Message).Append('⟧');
                    }
                    break;
            }
        }

        // 5. Advisory typed-variable validation of FINAL values — after formulas AND scripts, so a
        // script may legitimately normalize a raw value into its declared type. Warnings only.
        if (request.VariableTypes is { Count: > 0 })
            FormulaParser.ValidateDeclaredTypes(request.VariableTypes, variables, result.Diagnostics);

    done:
        result.Text = output.ToString();
        result.Success = !hasFailed;
        return result;
    }

    /// <summary>
    /// Single-item lifecycle: resolves a library item to a generated document. The UI-free counterpart
    /// of the batch service, so "how a library item becomes a document" has one home. File access goes
    /// through the <see cref="ILibrary"/> port (passed in, so this service stays dependency-free).
    /// </summary>
    public (GenerationResult Gen, FrontMatter TemplateFm, string Name) GenerateItem(
        ILibrary library, LibraryItem item, GenerationMode mode, MissingVariablePolicy policy, CultureInfo culture
    )
    {
        // Templates are abstract — they cannot be instantiated or exported directly.
        if (item.Kind == LibraryItemKind.Template)
        {
            var template = library.LoadTemplate(item.FilePath);
            return (GenerationResult.Fail(DiagnosticCode.SyntaxError,
                    "templates are abstract and cannot be exported — generate a child document first"),
                template.Fm, template.DisplayName);
        }

        var child = library.LoadChild(item.FilePath);
        var (createdAt, modifiedAt) = library.GetTimestamps(item.FilePath);

        // Baked child: the body is already-generated content, so the pipeline only unescapes the
        // {{ }} braces — no template, variables, or includes are involved.
        if (child.IsBaked)
        {
            var bakedResult = Run(new GenerationRequest
            {
                TemplateBody = child.Body,
                Doc = new DocInfo(child.DisplayName, item.TemplateRef ?? "", createdAt, modifiedAt),
                Mode = mode,
                Policy = policy,
                FormatCulture = culture
            });
            return (bakedResult, child.Fm, child.DisplayName);
        }

        // Live child: it must be generated against its linked template.
        if (child.ResolvedTemplatePath is null)
            return (GenerationResult.Fail(DiagnosticCode.SyntaxError,
                    "the linked template could not be found — fix the 'template:' path in this child"),
                new FrontMatter(), child.DisplayName);

        var linkedTemplate = library.LoadTemplate(child.ResolvedTemplatePath);
        var (templateScript, templateContent) = BodySplitter.Split(linkedTemplate.Body);
        var (contentWithIncludes, includeWarnings) = library.ExpandIncludes(templateContent);

        var generationResult = Run(new GenerationRequest
        {
            TemplateBody = BodySplitter.Join(templateScript, contentWithIncludes),
            TemplateDefaults = linkedTemplate.Fm.Variables,
            ChildValues = child.Fm.Values,
            Doc = new DocInfo(child.DisplayName, linkedTemplate.DisplayName, createdAt, modifiedAt),
            Mode = mode,
            Policy = policy,
            FormatCulture = culture,
            VariableTypes = linkedTemplate.Fm.Types
        });
        generationResult.Diagnostics.InsertRange(0, includeWarnings);
        return (generationResult, linkedTemplate.Fm, child.DisplayName);
    }

    // Declared inputs a preview lets the script READ as empty (they have no value yet), minus any
    // that already carry a real value. Content substitution never consults this map, so undefined
    // content placeholders still surface — it only keeps script reads from aborting the preview.
    private static Dictionary<string, Value>? BuildInputFallback(
        IReadOnlyCollection<string>? previewInputs, Dictionary<string, Value> variables)
    {
        if (previewInputs is not { Count: > 0 }) return null;
        var fallback = new Dictionary<string, Value>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in previewInputs)
            if (!variables.ContainsKey(name)) fallback[name] = Value.Str("");
        return fallback.Count > 0 ? fallback : null;
    }

    // Inline preview markers show the short code (E101), not the enum member name. Appending the
    // DiagnosticCode enum directly binds StringBuilder.Append(object?), which boxes and prints the
    // member name ("TypeMismatch"); the diagnostics pane's own ToString() is separate from this.
    private static string Marker(DiagnosticCode code) => "E" + (int)code;

    // Deliberately narrow: only unmistakable Python statement lines (if/elif/else ending in ':').
    // Bare assignments are NOT flagged — "Total = 5" is perfectly normal prose in a document.
    private static readonly Regex ScriptLikeLine = new(
        @"^\s*(?:if\b[^:\n]*|elif\b[^:\n]*|else\s*|match\b[^:\n]*):\s*(?:#.*)?$",
        RegexOptions.Compiled);

    /// <summary>Preview-only lint: script-looking lines OUTSIDE #SCRIPT blocks print as plain text (W002).</summary>
    private static void LintScriptLikeText(string body, List<Diagnostic> diagnostics)
    {
        var lines = (body ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        bool inScript = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].Trim();
            if (trimmed == ScriptingConstants.Template.ScriptOpen) { inScript = true; continue; }
            if (trimmed == ScriptingConstants.Template.ScriptClose) { inScript = false; continue; }
            if (!inScript && ScriptLikeLine.IsMatch(lines[i]))
                diagnostics.Add(new Diagnostic(DiagnosticCode.ScriptLikeText, DiagnosticSeverity.Warning,
                    "this line looks like script but sits in the content, so it will print as plain text — move it into the Script pane (a #SCRIPT … #ENDSCRIPT block)",
                    i + 1, 1));
        }
    }
}