using System.Text.RegularExpressions;
using Mammoth;
using OpenXmlWord = DocumentFormat.OpenXml.Packaging.WordprocessingDocument;
using OpenXmlParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using OpenXmlTable = DocumentFormat.OpenXml.Wordprocessing.Table;
using OpenXmlTableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using OpenXmlTableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using OpenXmlText = DocumentFormat.OpenXml.Wordprocessing.Text;
using OpenXmlRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using OpenXmlSpaceProcessingModeValues = DocumentFormat.OpenXml.SpaceProcessingModeValues;
using SimplyDraft.Core.Configuration;
using SimplyDraft.Core.Models;

namespace SimplyDraft.Core.Services;

public sealed class DocxService : IDocumentService
{
    public string ConvertToHtml(string filePath)
    {
        var converter = new DocumentConverter();
        var result = converter.ConvertToHtml(filePath);
        return result.Value;
    }

    // ─── EXPOSED METHODS ───────────────────────
    public List<(int Index, string Text)> ExtractParagraphs(string filePath)
        => ExtractBlocks(filePath)
            .Where(b => b.Type != DocumentBlockType.Table)
            .Select(b => (b.Index, b.Text))
            .ToList();
    
    public Dictionary<string, int> FindExistingVariables(string filePath)
    {
        var counts = new Dictionary<string, int>();
        using var doc = OpenXmlWord.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) {return counts;}

        var rx = new Regex(@"\{\{[^}]+\}\}");
        foreach(var para in body.Descendants<OpenXmlParagraph>())
        {
            var text = string.Concat(para.Descendants<OpenXmlText>().Select(t => t.Text));

            foreach (Match m in rx.Matches(text))
            {
                counts[m.Value] = counts.TryGetValue(m.Value, out var c) ? c + 1 : 1;
            }
        }

        return counts;
    }

    public bool InjectVariable(
        string filePath, string selectedText, string variableKey,
        VariableType type = Models.VariableType.Instance, int paragraphIndex = -1
    )
    {
        bool replaced = false;
        using var doc = OpenXmlWord.Open(filePath, true);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) {return false;}

        int containerIndex = 0;
        foreach (var bodyChild in body.ChildElements)
        {
            if (bodyChild is OpenXmlParagraph topPara)
            {
                if (TryInjectIntoContainer(topPara, selectedText, variableKey,
                        type, paragraphIndex, containerIndex, ref replaced))
                    if (type == VariableType.Instance) goto done;
                containerIndex++;
            }
            else if (bodyChild is OpenXmlTable table)
            {
                int tableIndex = containerIndex++;
                foreach (var row  in table.ChildElements.OfType<OpenXmlTableRow>())
                foreach (var cell in row.ChildElements.OfType<OpenXmlTableCell>())
                foreach (var cp   in cell.ChildElements.OfType<OpenXmlParagraph>())
                {
                    if (TryInjectIntoContainer(cp, selectedText, variableKey,
                            type, paragraphIndex, tableIndex, ref replaced))
                        if (type == VariableType.Instance) goto done;
                }
            }
        }
 
        done:
        doc.MainDocumentPart?.Document?.Save();
        return replaced;
    }

    public void ExportToChild(string filePath, string outputPath, List<Variable> variables)
    {
        File.Copy(filePath, outputPath, overwrite:true);
        using var doc = OpenXmlWord.Open(outputPath, true);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) {return;}

        var subs = BuildSubstitutions(variables);

        foreach (var para in body.Descendants<OpenXmlParagraph>())
        {
            var runs = para.Descendants<OpenXmlRun>().ToList();
            if (runs.Count == 0) continue;
            var fullText = string.Concat(runs.SelectMany(r => r.Descendants<OpenXmlText>().Select(t => t.Text)));
            string newText = fullText;
            foreach (var (key, value) in subs)
                newText = newText.Replace(key, value);
            if (newText != fullText)
                RebuildRuns(para, runs, newText);
        }
 
        doc.MainDocumentPart?.Document?.Save();
    }

    // ─── INTERNAL METHODS (DOCUMENT) ───────────
    private List<DocumentBlock> ExtractBlocks(string filePath)
    {
        var blocks = new List<DocumentBlock>();
        int index = 0;

        using var doc = OpenXmlWord.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) {return blocks;}

        foreach (var child in body.ChildElements)
        {
            if (child is OpenXmlParagraph para)
            {
                var block = ParseParagraph(para, index++);
                if (block != null) {blocks.Add(block);}
            }
            else if (child is OpenXmlTable table)
            {
                var block = ParseTable(table, index++);
                if (block != null) {blocks.Add(block);}
            }
        }

        return blocks;
    }

    private static DocumentBlock? ParseParagraph(OpenXmlParagraph para, int index)
    {
        var text = string.Concat(para.Descendants<OpenXmlText>().Select(t => t.Text));
        var styleID = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? string.Empty;
        var styleIDLower = styleID.ToLowerInvariant();

        DocumentBlockType blockType;
        if (IsHeadingStyle(styleIDLower, out int headingLevel))
        {
            blockType = headingLevel switch
            {
                1 => DocumentBlockType.Heading1,
                2 => DocumentBlockType.Heading2,
                3 => DocumentBlockType.Heading3,
                _ => DocumentBlockType.Heading4
            };
        }
        else if (styleIDLower.Contains("caption")) {blockType = DocumentBlockType.Caption;}
        else if (styleIDLower.Contains("code")) {blockType = DocumentBlockType.CodeBlock;}
        else {blockType = DocumentBlockType.Paragraph;}

        var numPr = para.ParagraphProperties?.NumberingProperties;
        bool isList = numPr?.NumberingId?.Val?.HasValue == true && numPr.NumberingId.Val.Value > 0;
        int listLevel = numPr?.NumberingLevelReference?.Val?.Value ?? 0;
        
        if (isList) {blockType = DocumentBlockType.ListItem;}

        var firstRun = para.Descendants<OpenXmlRun>().FirstOrDefault();

        return new DocumentBlock
        {
            Index = index,
            Type = blockType,
            Text = text,
            ListLevel = listLevel,
            IsBold = firstRun?.RunProperties?.Bold != null,
            IsItalic = firstRun?.RunProperties?.Italic != null,
            IsAllCaps = firstRun?.RunProperties?.Caps != null
        };
    }

    private static DocumentBlock? ParseTable(OpenXmlTable table, int index)
    {
        var rows = new List<TableRow>();
        bool firstRow = true;

        foreach (var child in table.ChildElements)
        {
            if (child is not OpenXmlTableRow row) continue;
            var cells = new List<TableCell>();
            
            foreach (var subchild in row.ChildElements)
            {
                if (subchild is not OpenXmlTableCell cell) continue;
                var cellText = string.Concat(cell.Descendants<OpenXmlText>().Select(t => t.Text));
                cells.Add(new TableCell(cellText, firstRow));
            }

            if (cells.Count > 0) {rows.Add(new TableRow(cells));}
            firstRow = false;
        }

        if (rows.Count == 0) {return null;}
        
        return new DocumentBlock
        {
            Index = index,
            Type = DocumentBlockType.Table,
            Rows = rows,
            Text = string.Empty
        };
    }

    private static bool IsHeadingStyle(string styleIDLower, out int level)
    {
        level = 0;
        if (string.IsNullOrEmpty(styleIDLower)) return false;

        string[] prefixes =
        {
            TranslationKeys.Text.Heading.EnglishKey,
            TranslationKeys.Text.Heading.FrenchKey,
            TranslationKeys.Text.Heading.GermanKey,
            TranslationKeys.Text.Heading.SwedishKey,
            TranslationKeys.Text.Heading.ItalianKey,
            TranslationKeys.Text.Heading.DutchKey,
            TranslationKeys.Text.Heading.PolishKey
        };

        foreach (var p in prefixes)
        {
            if (styleIDLower.StartsWith(p) && styleIDLower.Length > p.Length &&
                int.TryParse(styleIDLower[p.Length..], out int n) && n >=1 && n <= 4)
            {
                level = n; return true;
            }
        }

        if (styleIDLower.Length == 1 && int.TryParse(styleIDLower, out int d) && d >= 1 && d <= 4)
        {
            level = d; return true;
        }

        return false;
    }

    // ─── INTERNAL METHODS (VARIABLE) ───────────
    private static bool TryInjectIntoContainer(
        OpenXmlParagraph para, string selectedText, string variableKey,
        VariableType type, int paragraphIndex, int currentIndex, ref bool replaced
    )
    {
        if (type == VariableType.Instance && paragraphIndex >= 0 && currentIndex != paragraphIndex)
        {
            return false;
        }

        var runs = para.Descendants<OpenXmlRun>().ToList();
        if (runs.Count == 0) {return false;}

        var fullText = string.Concat(runs.SelectMany(r => r.Descendants<OpenXmlText>().Select(t => t.Text)));
        if (!fullText.Contains(selectedText, StringComparison.Ordinal)) {return false;}

        bool made = false;
        int searchFrom = 0;

        while (true)
        {
            int pos = fullText.IndexOf(selectedText, searchFrom, StringComparison.Ordinal);
            if (pos < 0) break;
            string newFull = fullText[..pos] + variableKey + fullText[(pos + selectedText.Length)..];
            RebuildRuns(para, runs, newFull);

            runs = para.Descendants<OpenXmlRun>().ToList();
            fullText = string.Concat(runs.SelectMany(r => r.Descendants<OpenXmlText>().Select(t => t.Text)));

            made = replaced = true;
            if (type == VariableType.Instance) {break;}

            searchFrom = pos + variableKey.Length;
            if (searchFrom >= fullText.Length) {break;}
        }

        return made;
    }

    private static void RebuildRuns(OpenXmlParagraph para, List<OpenXmlRun> runs, string newFullText)
    {
        if (runs.Count == 0) {return;}

        var firstRun = runs[0];
        var propsClone = firstRun.RunProperties?.CloneNode(true) as DocumentFormat.OpenXml.Wordprocessing.RunProperties;

        var textEl = firstRun.GetFirstChild<OpenXmlText>();
        if (textEl == null) {textEl = new OpenXmlText(); firstRun.AppendChild(textEl);}
        foreach (var extra in firstRun.Descendants<OpenXmlText>().Skip(1).ToList()) {extra.Remove();}

        textEl.Text = newFullText;
        textEl.Space = OpenXmlSpaceProcessingModeValues.Preserve;

        if (propsClone != null && firstRun.RunProperties == null)
        {
            firstRun.InsertAt(propsClone, 0);
        }

        for (int i = 1; i < runs.Count; i++) runs[i].Remove();
    }

    // ─── INTERNAL METHODS (EXPORTING) ──────────
    private static Dictionary<string, string> BuildSubstitutions(List<Variable> variables)
    {
        var map = new Dictionary<string, string>();
        foreach (var v in variables)
        {
            if (v is FixedVariable fixedVariable) continue;
            string resolved = v.RuntimeValue ?? string.Empty;
            if (v is ConditionalVariable conditionalVariable)
            {
                var rule = conditionalVariable.ConditionalRules.FirstOrDefault(r =>
                    r.WhenValue.Equals(v.RuntimeValue, StringComparison.OrdinalIgnoreCase));
                resolved = rule?.ThenSubstitute ?? resolved;
            }
            map[v.Key] = resolved;
        }
        return map;
    }
}