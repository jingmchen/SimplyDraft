// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using System.Text.RegularExpressions;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Document.Segments;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Exceptions;
using SimplyDraft.Engine.Constants;
using SimplyDraft.Engine.Generation;
using SimplyDraft.Engine.Templates;
using SimplyDraft.Engine.Utils;

namespace SimplyDraft.Engine.Services;

public sealed class ScriptingEngine : IScriptingEngine
{
    private readonly IBuiltinProvider _builtins;

    public ScriptingEngine(IBuiltinProvider builtins)
        => _builtins = builtins ?? throw new ArgumentNullException(nameof(builtins));

    public GenerationResult Run(GenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new GenerationResult();

        if (!TryParseTemplate(request.TemplateBody, result, out var segments))
            return result;

        var run = new GenerationRun(request, _builtins, result);

        if (run.IsPreview)
            WarnOnScriptLikeContent(request.TemplateBody, result.Diagnostics);

        bool completed = run.EvaluateFormulas() && run.WriteSegments(segments);

        if (completed)
            run.ValidateDeclaredTypes();

        result.Text = run.OutputText;
        result.Success = completed;
        return result;
    }

    private static bool TryParseTemplate(string templateBody, GenerationResult result, out List<Segment> segments)
    {
        try
        {
            segments = TemplateParser.Parse(templateBody);
            return true;
        }
        catch (ScriptException ex)
        {
            result.Diagnostics.Add(ex.Diagnostic);
            result.Success = false;
            segments = [];
            return false;
        }
    }

    public (GenerationResult Result, FrontMatter TemplateFm, string Name) GenerateItem(
        ILibrary library, LibraryItem item, GenerationMode mode, MissingVariablePolicy policy, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(culture);

        if (item.Kind == LibraryItemKind.Template)
        {
            var template = library.LoadTemplate(item.FilePath);
            return (GenerationResult.Fail(
                DiagnosticCode.SyntaxError,
                "templates are abstract and cannot be exported — generate a child document first"),
                template.Fm, template.DisplayName
            );
        }

        var child = library.LoadChild(item.FilePath);
        var (createdAt, modifiedAt) = library.GetTimestamps(item.FilePath);

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

        if (child.ResolvedTemplatePath is null)
            return (GenerationResult.Fail(
                DiagnosticCode.SyntaxError,
                "the linked template could not be found — fix the 'template:' path in this child"),
                new FrontMatter(),
                child.DisplayName
            );

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

    private static void WarnOnScriptLikeContent(string body, List<Diagnostic> diagnostics)
    {
        var lines = (body ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        bool insideScriptBlock = false;

        for (int index = 0; index < lines.Length; index++)
        {
            string trimmed = lines[index].Trim();

            if (trimmed == ScriptingConstants.Template.ScriptOpen)
            {
                insideScriptBlock = true;
                continue;
            }

            if (trimmed == ScriptingConstants.Template.ScriptClose)
            {
                insideScriptBlock = false;
                continue;
            }

            if (!insideScriptBlock && ScriptLikeLine.IsMatch(lines[index]))
                diagnostics.Add(new Diagnostic(
                    DiagnosticCode.ScriptLikeText,
                    DiagnosticSeverity.Warning,
                    "this line looks like script but sits in the content, so it will print as plain text — move it into the Script pane (a #SCRIPT … #ENDSCRIPT block)",
                    index + 1, 1
                ));
        }
    }

    private static readonly Regex ScriptLikeLine = new(
        @"^\s*(?:if\b[^:\n]*|elif\b[^:\n]*|else\s*|match\b[^:\n]*):\s*(?:#.*)?$",
        RegexOptions.Compiled);
}