using System.Text;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Common;
using SimplyDraft.Core.Domains.Batch;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Enums;
using SimplyDraft.Engine.Constants;
using SimplyDraft.Engine.Parsing;

namespace SimplyDraft.Engine.Services;

public sealed class BatchGenerator : IBatchGenerator
{
    private readonly IScriptingEngine _scripting;
    private readonly IFileSystem _fileSystem;

    public BatchGenerator(IScriptingEngine scripting, IFileSystem fileSystem)
    {
        _scripting = scripting;
        _fileSystem = fileSystem;
    }

    public async Task<BatchResult> RunAsync(
        BatchRequest request,
        IProgress<(int Done, int Total)>? progress,
        CancellationToken cancellationToken
    )
    {
        var batchResult = new BatchResult();

        // Detect the delimiter from the header row (tab-separated or comma-separated), then parse.
        string csvText = await _fileSystem.ReadAllTextAsync(request.CsvPath, cancellationToken).ConfigureAwait(false);
        int firstNewline = csvText.IndexOf('\n');
        string firstLine = firstNewline < 0 ? csvText : csvText[..firstNewline];
        char delimiter = firstLine.Contains('\t') ? '\t' : ',';
        var csvRows = CsvParser.Parse(csvText, delimiter);

        if (csvRows.Count < 2)
        {
            batchResult.Rows.Add(new BatchRowResult(0, "", false, "The CSV needs a header row and at least one data row."));
            return batchResult;
        }

        var headers = csvRows[0].Select(header => header.Trim()).ToArray();

        _fileSystem.CreateDirectory(request.OutputDir);
        
        var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        string fileNamePattern = string.IsNullOrWhiteSpace(request.FileNamePattern)
            ? (headers.Length > 0 && headers[0].Length > 0
                ? ScriptingConstants.Template.PlaceholderOpen + headers[0] + ScriptingConstants.Template.PlaceholderClose
                : "document")
            : request.FileNamePattern;
        
        string extension = "." + request.Exporter.FileExtension;
        
        int totalRows = csvRows.Count - 1;

        for (int rowIndex = 1; rowIndex < csvRows.Count; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Map this row's cells onto the header names.
            var cells = csvRows[rowIndex];
            var rowValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int columnIndex = 0; columnIndex < headers.Length; columnIndex++)
                if (headers[columnIndex].Length > 0)
                    rowValues[headers[columnIndex]] = columnIndex < cells.Length
                        ? cells[columnIndex]
                        : "";

            // Derive a unique, filesystem-safe output name from the pattern.
            string baseFileName = FileNameSanitizer.Sanitize(RenderPattern(fileNamePattern, rowValues, request.Template.Fm.Variables));
            string candidateName = baseFileName;
            int duplicateSuffix = 2;
            while (usedFileNames.Contains(candidateName) ||
                   _fileSystem.FileExists(Path.Combine(request.OutputDir, candidateName + extension)))
                candidateName = $"{baseFileName} ({duplicateSuffix++})";
            usedFileNames.Add(candidateName);
            string outputPath = Path.Combine(request.OutputDir, candidateName + extension);

            GenerationResult generationResult;
            try
            {
                generationResult = _scripting.Run(new GenerationRequest
                {
                    TemplateBody = request.Template.Body,
                    TemplateDefaults = request.Template.Fm.Variables,
                    ChildValues = rowValues,
                    Doc = new DocInfo(candidateName, request.Template.DisplayName, DateTime.Now, DateTime.Now),
                    VariableTypes = request.Template.Fm.Types,
                    Policy = request.Policy,
                    Mode = GenerationMode.Export,
                    FormatCulture = request.FormatCulture
                });
            }
            catch (Exception ex)
            {
                batchResult.Rows.Add(new BatchRowResult(rowIndex + 1, candidateName + extension, false, "generation error: " + ex.Message));
                progress?.Report((rowIndex, totalRows));
                continue;
            }

            if (!generationResult.Success)
            {
                batchResult.Rows.Add(new BatchRowResult(rowIndex + 1, candidateName + extension, false,
                    string.Join("; ", generationResult.Diagnostics.Select(diagnostic => diagnostic.ToString()))));
                progress?.Report((rowIndex, totalRows));
                continue;
            }

            try
            {
                await request.Exporter.ExportAsync(
                    doc: GeneratedDocument.From(generationResult.Text, request.Template.Fm, Path.GetDirectoryName(request.Template.FilePath)),
                    options: request.Options,
                    outputPath: outputPath,
                    ct: cancellationToken).ConfigureAwait(false);

                string warningSuffix = generationResult.Diagnostics.Count > 0 ? $" ({generationResult.Diagnostics.Count} warning(s))" : "";
                batchResult.Rows.Add(new BatchRowResult(rowIndex + 1, candidateName + extension, true, "ok" + warningSuffix));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                batchResult.Rows.Add(new BatchRowResult(rowIndex + 1, candidateName + extension, false, ex.Message));
            }
            progress?.Report((rowIndex, totalRows));
        }

        // Write a machine-readable per-row report alongside the outputs.
        var report = new StringBuilder("row,file,status,message\n");
        foreach (var rowResult in batchResult.Rows)
            report.Append(rowResult.RowNumber).Append(',')
                  .Append(CsvQuote(rowResult.FileName)).Append(',')
                  .Append(rowResult.Ok ? "ok" : "error").Append(',')
                  .Append(CsvQuote(rowResult.Message)).Append('\n');
        string reportPath = Path.Combine(request.OutputDir, $"batch-report-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        await _fileSystem.WriteAllTextAsync(reportPath, report.ToString(), cancellationToken).ConfigureAwait(false);
        batchResult.ReportPath = reportPath;
        return batchResult;
    }

    private static string CsvQuote(string field) => "\"" + field.Replace("\"", "\"\"") + "\"";

    internal static string RenderPattern(string pattern, IReadOnlyDictionary<string, string> values, IReadOnlyDictionary<string, string> defaults)
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