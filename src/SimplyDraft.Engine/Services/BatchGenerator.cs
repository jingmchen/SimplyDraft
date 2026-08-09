// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Common;
using SimplyDraft.Core.Domains.Batch;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Enums;
using SimplyDraft.Engine.Constants;
using SimplyDraft.Engine.Templates;

namespace SimplyDraft.Engine.Services;

public sealed class BatchGenerator : IBatchGenerator
{
    private readonly IScriptingEngine _scripting;
    private readonly IFileSystem _fileSystem;
    private static string CsvQuote(string field) => "\"" + field.Replace("\"", "\"\"") + "\"";

    public BatchGenerator(IScriptingEngine scripting, IFileSystem fileSystem)
    {
        _scripting = scripting ?? throw new ArgumentNullException(nameof(scripting));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public async Task<BatchResult> RunAsync(
        BatchRequest request,
        IProgress<(int Done, int Total)>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var batchResult = new BatchResult();
        var csvRows = await ReadCsvAsync(request.CsvPath, ct).ConfigureAwait(false);

        if (csvRows.Count < 2)
        {
            batchResult.Rows.Add(new BatchRowResult(0, "", false, "The CSV needs a header row and at least one data row."));
            return batchResult;
        }

        var headers = csvRows[0].Select(header => header.Trim()).ToArray();
        string fileNamePattern = ResolveFileNamePattern(request.FileNamePattern, headers);
        string extension = "." + request.Exporter.FileExtension;
        var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int totalRows = csvRows.Count - 1;

        Directory.CreateDirectory(request.OutputDir);

        for (int rowIndex = 1; rowIndex < csvRows.Count; rowIndex++)
        {
            ct.ThrowIfCancellationRequested();

            var rowValues = MapCellsToHeaders(headers, csvRows[rowIndex]);
            string fileName = ReserveUniqueFileName(fileNamePattern, rowValues, request, usedFileNames, extension);
            string outputPath = Path.Combine(request.OutputDir, fileName + extension);

            var rowResult = await GenerateAndExportRowAsync(
                request, rowValues, fileName, extension, outputPath, rowIndex, ct).ConfigureAwait(false);
            
            batchResult.Rows.Add(rowResult);
            progress?.Report((rowIndex, totalRows));
        }

        batchResult.ReportPath = await WriteReportAsync(batchResult, request.OutputDir, ct).ConfigureAwait(false);
        return batchResult;
    }

    private async Task<List<string[]>> ReadCsvAsync(string csvPath, CancellationToken cancellationToken)
    {
        string csvText = await _fileSystem.ReadAllTextAsync(csvPath, cancellationToken).ConfigureAwait(false);
        int firstNewline = csvText.IndexOf('\n');
        string headerLine = firstNewline < 0 ? csvText : csvText[..firstNewline];
        char delimiter = headerLine.Contains('\t') ? '\t' : ',';

        return CSVParser.Parse(csvText, delimiter);
    }

    private static string ResolveFileNamePattern(string requestedPattern, string[] headers)
    {
        if (!string.IsNullOrWhiteSpace(requestedPattern))
            return requestedPattern;
        
        return headers.Length > 0 && headers[0].Length > 0
            ? ScriptingConstants.Template.PlaceholderOpen + headers[0] + ScriptingConstants.Template.PlaceholderClose
            : "document";
    }

    private static Dictionary<string, string> MapCellsToHeaders(string[] headers, string[] cells)
    {
        var rowValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        for (int columnIndex = 0; columnIndex < headers.Length; columnIndex++)
            if (headers[columnIndex].Length > 0)
                rowValues[headers[columnIndex]] = columnIndex < cells.Length ? cells[columnIndex] : "";
        
        return rowValues;
    }

    private static string ReserveUniqueFileName(string fileNamePattern, Dictionary<string, string> rowValues,
        BatchRequest request, HashSet<string> usedFileNames, string extension)
    {
        string baseFileName = FileNameSanitizer.Sanitize(RenderPattern(fileNamePattern, rowValues, request.Template.Fm.Variables));
        string candidate = baseFileName;
        int duplicateSuffix = 2;

        while (usedFileNames.Contains(candidate) || File.Exists(Path.Combine(request.OutputDir, candidate + extension)))
            candidate = $"{baseFileName} ({duplicateSuffix++})";
        
        usedFileNames.Add(candidate);
        return candidate;
    }

    private async Task<BatchRowResult> GenerateAndExportRowAsync(
        BatchRequest request,
        Dictionary<string, string> rowValues,
        string fileName,
        string extension,
        string outputPath,
        int rowIndex,
        CancellationToken ct)
    {
        int csvRowNumber = rowIndex + 1;
        string reportedFileName = fileName + extension;

        GenerationResult generation;
        try
        {
            generation = _scripting.Run(new GenerationRequest
            {
                TemplateBody = request.Template.Body,
                TemplateDefaults = request.Template.Fm.Variables,
                ChildValues = rowValues,
                Doc = new DocInfo(fileName, request.Template.DisplayName, DateTime.Now, DateTime.Now),
                VariableTypes = request.Template.Fm.Types,
                Policy = request.Policy,
                Mode = GenerationMode.Export,
                FormatCulture = request.FormatCulture
            });
        }
        catch (Exception ex)
        {
            return new BatchRowResult(csvRowNumber, reportedFileName, false, "generation error: " + ex.Message);
        }

        if (!generation.Success)
            return new BatchRowResult(
                csvRowNumber, reportedFileName, false, string.Join("; ", generation.Diagnostics.Select(diagnostic => diagnostic.ToString())));
        
        try
        {
            await request.Exporter.ExportAsync(
                GeneratedDocument.From(generation.Text, request.Template.Fm, Path.GetDirectoryName(request.Template.FilePath)),
                outputPath, request.Options, ct).ConfigureAwait(false);

            string warningSuffix = generation.Diagnostics.Count > 0
                ? $" ({generation.Diagnostics.Count} warning(s))"
                : "";
            
            return new BatchRowResult(csvRowNumber, reportedFileName, true, "ok" + warningSuffix);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new BatchRowResult(csvRowNumber, reportedFileName, false, ex.Message);
        }
    }

    private async Task<string> WriteReportAsync(
        BatchResult batchResult, string outputDir, CancellationToken ct)
    {
        var report = new StringBuilder("row,file,status,message\n");
        
        foreach (var row in batchResult.Rows)
            report.Append(row.RowNumber).Append(',')
                  .Append(CsvQuote(row.FileName)).Append(',')
                  .Append(row.Ok ? "ok" : "error").Append(',')
                  .Append(CsvQuote(row.Message)).Append('\n');
        
        string reportPath = Path.Combine(outputDir, $"batch-report-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        await _fileSystem.WriteAllTextAsync(reportPath, report.ToString(), ct).ConfigureAwait(false);
        return reportPath;
    }

    internal static string RenderPattern(
        string pattern, IReadOnlyDictionary<string, string> values, IReadOnlyDictionary<string, string> defaults)
    {
        var builder = new StringBuilder();
        int position = 0;

        while (position < pattern.Length)
        {
            char current = pattern[position];

            if (current == ScriptingConstants.Template.PlaceholderOpen)
            {
                int closeBrace = pattern.IndexOf(ScriptingConstants.Template.PlaceholderClose, position + 1);

                if (closeBrace > position + 1)
                {
                    string variableName = pattern[(position + 1)..closeBrace];

                    if (values.TryGetValue(variableName, out var value) || defaults.TryGetValue(variableName, out value))
                        builder.Append(value);
                    
                    position = closeBrace + 1;
                    continue;
                }
            }
            builder.Append(current);
            position++;
        }
        return builder.ToString();
    }
}