// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Domains.Markup;
using SimplyDraft.Core.Domains.Markup.Blocks;
using SimplyDraft.Core.Domains.Markup.Inlines;
using SimplyDraft.Core.Enums;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Services;

public sealed class RenderEngine : IRenderEngine
{
    private const int Width = 78; // Text width of the .txt projection

    public string Render(MarkupDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Render(document.Blocks, wrap: true);
    }

    public string Render(MarkupDocument document, bool wrap)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Render(document.Blocks, wrap);
    }

    public string Render(IReadOnlyList<Block> blocks, bool wrap)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        var output = new StringBuilder();
        foreach (var block in blocks)
        {
            switch (block)
            {
                case PageBreakBlock:
                    output.Append('\n');
                    break;
                
                case RuleBlock:
                    output.Append(new string('-', Width)).Append('\n');
                    break;
                
                case TableBlock table:
                    RenderTable(output, table);
                    break;
                
                case TableOfContentsBlock toc:
                    RenderTableOfContents(output, toc);
                    break;
                
                case ListOfFiguresBlock listOfFigures:
                    RenderListOfFigures(output, listOfFigures);
                    break;
                
                case ImageBlock image:
                    RenderImage(output, image);
                    break;
                
                case ParagraphBlock paragraph:
                    RenderParagraph(output, paragraph, wrap);
                    break;
            }
        }
        return output.ToString();
    }

    private static void RenderParagraph(StringBuilder output, ParagraphBlock paragraph, bool wrap)
    {
        string text = RenderInlines(paragraph.Inlines);

        switch (paragraph.Kind)
        {
            case ParagraphKind.Heading1:
            case ParagraphKind.Heading2:
            case ParagraphKind.Heading3:
                RenderHeading(output, paragraph, text, wrap);
                break;

            case ParagraphKind.BulletItem:
                string bulletPrefix = Indent(paragraph.ListLevel) + "• ";
                EmitWrapped(output, bulletPrefix, new string(' ', bulletPrefix.Length), text, paragraph.Centered, wrap);
                break;
                
            case ParagraphKind.NumberItem:
                string numberPrefix = Indent(paragraph.ListLevel) + paragraph.Number + ". ";
                EmitWrapped(output, numberPrefix, new string(' ', numberPrefix.Length), text, paragraph.Centered, wrap);
                break;
            
            case ParagraphKind.DescriptionItem:
                string term = RenderInlines(paragraph.Term).Replace('\n', ' ');
                string descPrefix = Indent(paragraph.ListLevel) + (term.Length > 0 ? term + "  " : "");
                if (text.Length == 0) Emit(output, descPrefix.TrimEnd(), paragraph.Centered);
                else EmitWrapped(output, descPrefix, new string(' ', descPrefix.Length), text, paragraph.Centered, wrap);
                break;
            
            case ParagraphKind.Quote:
                EmitWrapped(output, "> ", "> ", text, centered: false, wrap);
                break;
            
            case ParagraphKind.Verbatim:
                output.Append(text).Append('\n'); // exact — never wrapped
                break;
            
            default:
                EmitWrapped(output, "", "", text, paragraph.Centered, wrap);
                break;
        }
    }

    private static void RenderHeading(StringBuilder output, ParagraphBlock heading, string text, bool wrap)
    {
        if (heading.HeadingNumber.Length > 0)
            text = heading.HeadingNumber + " " + text;
        
        char underline = heading.Kind switch
        {
            ParagraphKind.Heading1 => '=',
            ParagraphKind.Heading2 => '-',
            _ => '~'
        };

        var lines = (wrap ? WrapSegments(text, Width) : text.Split('\n')).ToList();
        int underlineLength = Math.Max(3, Math.Min(lines.Max(line => line.Length), Width));
        
        foreach (var line in lines)
            Emit(output, line, heading.Centered);
        
        Emit(output, new string(underline, underlineLength), heading.Centered);
    }

    private static string Indent(int listLevel)
        => new(' ', 2 * Math.Max(0, listLevel - 1));

    private static void EmitWrapped(
        StringBuilder output, string firstPrefix, string continuationPrefix, string text, bool centered, bool wrap)
    {
        int available = Math.Max(20, Width - Math.Max(firstPrefix.Length, continuationPrefix.Length));
        var lines = wrap ? WrapSegments(text, available) : text.Split('\n');
        bool isFirst = true;
        
        foreach (var line in lines)
        {
            Emit(output, (isFirst ? firstPrefix : continuationPrefix) + line, centered);
            isFirst = false;
        }
    }

    private static IEnumerable<string> WrapSegments(string text, int width)
    {
        foreach (var segment in text.Split('\n'))
        {
            if (segment.Length <= width)
            {
                yield return segment;
                continue;
            }

            int position = 0;

            while (position < segment.Length)
            {
                if (segment.Length - position <= width)
                {
                    yield return segment[position..];
                    break;
                }

                int cut = segment.LastIndexOf(' ', position + width, width + 1);
                
                if (cut <= position)
                    cut = position + width;
                
                yield return segment[position..cut];
                position = cut;
                
                while (position < segment.Length && segment[position] == ' ')
                    position++;
            }
        }
    }

    private static void Emit(StringBuilder output, string text, bool centered)
    {
        foreach (var line in text.Split('\n'))
        {
            if (centered && line.Length > 0 && line.Length < Width)
                output.Append(new string(' ', (Width - line.Length) / 2));
            output.Append(line).Append('\n');
        }
    }

    private static void RenderTableOfContents(StringBuilder output, TableOfContentsBlock toc)
    {
        output.Append("Contents\n========\n");

        foreach (var entry in toc.Entries)
        {
            output.Append(new string(' ', 2 * Math.Max(0, entry.Level - 1)));

            if (entry.Number.Length > 0)
                output.Append(entry.Number).Append(' ');
            
            output.Append(entry.Text).Append('\n');
        }

        output.Append('\n');
    }

    private static void RenderListOfFigures(StringBuilder output, ListOfFiguresBlock listOfFigures)
    {
        output.Append("List of Figures\n===============\n");

        foreach (var entry in listOfFigures.Entries)
            output.Append("Figure ").Append(entry.Number).Append(": ").Append(entry.Text).Append('\n');
        
        output.Append('\n');
    }

    private static void RenderImage(StringBuilder output, ImageBlock image)
    {
        if (image.Path.Length > 0)
            Emit(output, "[image: " + image.Path + "]", image.Centered);
        
        if (image.FigureNumber > 0)
            Emit(output, "Figure " + image.FigureNumber + ": " + RenderInlines(image.Caption).Replace('\n', ' '), image.Centered);
    }

    private static void RenderTable(StringBuilder output, TableBlock table)
    {
        int columnCount = table.ColumnCount;

        if (columnCount == 0)
            return;

        var cells = new List<string[]>();

        foreach (var row in table.Rows)
        {
            var rowCells = new string[columnCount];
            for (int column = 0; column < columnCount; column++)
                rowCells[column] = column < row.Cells.Count
                    ? RenderInlines(row.Cells[column]).Replace('\n', ' ')
                    : "";
            
            cells.Add(rowCells);
        }

        var columnWidths = new int[columnCount];

        foreach (var row in cells)
            for (int column = 0; column < columnCount; column++)
                columnWidths[column] = Math.Max(columnWidths[column], row[column].Length);

        string Border(char fill)
            => "+" + string.Join("+", columnWidths.Select(width => new string(fill, width + 2))) + "+";

        string Pad(string cell, int column)
        {
            int width = columnWidths[column];

            char alignment = column < table.Alignments.Count
                ? table.Alignments[column]
                : MarkupConstants.ColumnAlignment.AlignLeft;
            
            return alignment switch
            {
                MarkupConstants.ColumnAlignment.AlignRight => cell.PadLeft(width),
                MarkupConstants.ColumnAlignment.AlignCenter => cell.PadLeft(cell.Length + (width - cell.Length) / 2).PadRight(width),
                _ => cell.PadRight(width)
            };
        }

        output.Append(Border('-')).Append('\n');

        for (int rowIndex = 0; rowIndex < cells.Count; rowIndex++)
        {
            output.Append('|');

            for (int column = 0; column < columnCount; column++)
                output.Append(' ').Append(Pad(cells[rowIndex][column], column)).Append(" |");
            
            output.Append('\n');
            
            if (rowIndex == 0 && cells.Count > 1)
                output.Append(Border('=')).Append('\n');
        }

        output.Append(Border('-')).Append('\n');
    }

    private static string RenderInlines(IEnumerable<Inline> inlines)
    {
        var text = new StringBuilder();
        
        foreach (var inline in inlines)
        {
            if (inline is LineBreak)
                text.Append('\n');
            else if (inline is TextRun run)
                text.Append(run.SmallCaps ? run.Text.ToUpperInvariant() : run.Text);
            else if (inline is RefRun reference)
                text.Append('?').Append(reference.Key).Append('?');
        }

        return text.ToString();
    }
}