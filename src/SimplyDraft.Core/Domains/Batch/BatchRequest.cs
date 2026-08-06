// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Exporting;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Batch;

public sealed class BatchRequest
{
    public required TemplateDocument Template {get; init;}
    public required string CsvPath {get; init;}
    public required string OutputDir {get; init;}
    
    public required IDocumentExporter Exporter {get; init;}
    public ExportOptions Options {get; init;} = new();

    public string FileNamePattern {get; init;} = "";
    public MissingVariablePolicy Policy {get; init;} = MissingVariablePolicy.ErrorOnExport;
    public CultureInfo? FormatCulture {get; init;}
}