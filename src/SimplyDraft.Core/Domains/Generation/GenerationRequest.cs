// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Domains.Documents;

namespace SimplyDraft.Core.Domains.Generation;

public sealed class GenerationRequest
{
    public required string TemplateBody {get; init;}
    public required DocInfo Doc {get; init;}
    public MissingVariablePolicy Policy {get; init;} = MissingVariablePolicy.ErrorOnExport;
    public GenerationMode Mode {get; init;} = GenerationMode.Export;
    public DateTime? Clock {get; init;}
    public CultureInfo? FormatCulture {get; init;}
    public IReadOnlyDictionary<string, string> TemplateDefaults {get; init;} = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string>? ChildValues {get; init;}
    public IReadOnlyDictionary<string, string>? VariableTypes {get; init;}
    public IReadOnlyCollection<string>? PreviewInputs {get; init;}
}