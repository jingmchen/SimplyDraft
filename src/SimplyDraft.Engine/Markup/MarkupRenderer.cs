using System.Text;
using SimplyDraft.Core.Domains.Markup;
using SimplyDraft.Core.Domains.Markup.Blocks;
using SimplyDraft.Core.Domains.Markup.Inlines;
using SimplyDraft.Core.Enums;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Markup;

public static class MarkupRenderer
{
    /// <summary>Text width of the .txt projection; also the content editor's column guide.</summary>
    public const int Width = 78;

    public static string Render(MarkupDocument doc) => Render(doc.Blocks, wrap: true);

    /// <summary>
    /// wrap=true is the .txt projection (hard-wrapped at <see cref="Width"/>).
    /// wrap=false is the FLOW projection: one logical line per paragraph, no hard wraps — fed to
    /// the preview's Page view, whose own layout (docx font at real page width) then wraps it
    /// where Word will.
    /// </summary>
    public static string Render(MarkupDocument doc, bool wrap) => Render(doc.Blocks, wrap);

    /// <summary>
    /// Renders just the given blocks.
    /// The app's page-view preview uses this to render the prose runs BETWEEN tables as text,
    /// while drawing the tables themselves as native controls.
    /// </summary>
    public static string Render(IReadOnlyList<Block> blocks, bool wrap)
    {
        var sb = new StringBuilder();
        foreach (var block in blocks)
        {
            switch (block)
            {
                case PageBreakBlock:
                    sb.Append('\n');
                    break;
                case RuleBlock:
                    sb.Append(new string('-', Width)).Append('\n');
                    break;
                case TableBlock t:
                    RenderTable(sb, t);
                    break;
                case TableOfContentsBlock toc:
                    RenderToc(sb, toc);
                    break;
                case ListOfFiguresBlock lof:
                    RenderLof(sb, lof);
                    break;
                case ImageBlock img:
                    RenderImage(sb, img);
                    break;
                case ParagraphBlock p:
                    RenderParagraph(sb, p, wrap);
                    break;
            }
        }
        return sb.ToString();
    }

    private static void RenderParagraph(StringBuilder sb, ParagraphBlock p, bool wrap)
    {
        string text = RenderInlines(p.Inlines);
        switch (p.Kind)
        {
            case ParagraphKind.Heading1:
            case ParagraphKind.Heading2:
            case ParagraphKind.Heading3:
            {
                if (p.HeadingNumber.Length > 0) text = p.HeadingNumber + " " + text;
                char u = p.Kind switch
                {
                    ParagraphKind.Heading1 => '=',
                    ParagraphKind.Heading2 => '-',
                    _ => '~'
                };
                var lines = (wrap ? WrapSegments(text, Width) : text.Split('\n')).ToList();
                int len = Math.Max(3, Math.Min(lines.Max(l => l.Length), Width));
                foreach (var ln in lines) Emit(sb, ln, p.Centered);
                Emit(sb, new string(u, len), p.Centered);
                break;
            }
            case ParagraphKind.BulletItem:
            {
                string pre = Indent(p.ListLevel) + "• ";
                EmitWrapped(sb, pre, new string(' ', pre.Length), text, p.Centered, wrap);
                break;
            }
            case ParagraphKind.NumberItem:
            {
                string pre = Indent(p.ListLevel) + p.Number + ". ";
                EmitWrapped(sb, pre, new string(' ', pre.Length), text, p.Centered, wrap);
                break;
            }
            case ParagraphKind.DescriptionItem:
            {
                string term = RenderInlines(p.Term).Replace('\n', ' ');
                string pre = Indent(p.ListLevel) + (term.Length > 0 ? term + "  " : "");
                if (text.Length == 0) Emit(sb, pre.TrimEnd(), p.Centered);
                else EmitWrapped(sb, pre, new string(' ', pre.Length), text, p.Centered, wrap);
                break;
            }
            case ParagraphKind.Quote:
                EmitWrapped(sb, "> ", "> ", text, centered: false, wrap);
                break;
            case ParagraphKind.Verbatim:
                sb.Append(text).Append('\n');   // exact — never wrapped
                break;
            default:
                EmitWrapped(sb, "", "", text, p.Centered, wrap);
                break;
        }
    }

    private static string Indent(int listLevel)
        => new(' ', 2 * Math.Max(0, listLevel - 1));
    
    private static void EmitWrapped(
        StringBuilder sb,
        string firstPrefix,
        string contPrefix,
        string text,
        bool centered,
        bool wrap
    )
    {
        int avail = Math.Max(20, Width - Math.Max(firstPrefix.Length, contPrefix.Length));
        var pieces = wrap ? WrapSegments(text, avail) : text.Split('\n');
        bool first = true;

        foreach (var line in pieces)
        {
            Emit(sb, (first ? firstPrefix : contPrefix) + line, centered);
            first = false;
        }
    }

    private static IEnumerable<string> WrapSegments(string text, int width)
    {
        foreach (var seg in text.Split('\n'))
        {
            if (seg.Length <= width) { yield return seg; continue; }
            int pos = 0;
            while (pos < seg.Length)
            {
                if (seg.Length - pos <= width) { yield return seg[pos..]; break; }
                int cut = seg.LastIndexOf(' ', pos + width, width + 1);
                if (cut <= pos) cut = pos + width;
                yield return seg[pos..cut];
                pos = cut;
                while (pos < seg.Length && seg[pos] == ' ') pos++;
            }
        }
    }
    
    private static void Emit(StringBuilder sb, string text, bool centered)
    {
        foreach (var line in text.Split('\n'))
        {
            if (centered && line.Length > 0 && line.Length < Width)
                sb.Append(new string(' ', (Width - line.Length) / 2));
            sb.Append(line).Append('\n');
        }
    }

    private static void RenderToc(StringBuilder sb, TableOfContentsBlock toc)
    {
        sb.Append("Contents\n========\n");
        foreach (var e in toc.Entries)
        {
            sb.Append(new string(' ', 2 * Math.Max(0, e.Level - 1)));
            if (e.Number.Length > 0) sb.Append(e.Number).Append(' ');
            sb.Append(e.Text).Append('\n');
        }
        sb.Append('\n');
    }

    private static void RenderLof(StringBuilder sb, ListOfFiguresBlock lof)
    {
        sb.Append("List of Figures\n===============\n");
        foreach (var e in lof.Entries)
            sb.Append("Figure ").Append(e.Number).Append(": ").Append(e.Text).Append('\n');
        sb.Append('\n');
    }

    private static void RenderImage(StringBuilder sb, ImageBlock img)
    {
        if (img.Path.Length > 0)
            Emit(sb, "[image: " + img.Path + "]", img.Centered);
        if (img.FigureNumber > 0)
            Emit(sb, "Figure " + img.FigureNumber + ": " + RenderInlines(img.Caption).Replace('\n', ' '),
                img.Centered);
    }

    private static void RenderTable(StringBuilder sb, TableBlock t)
    {
        int cols = t.ColumnCount;
        if (cols == 0) return;

        var cells = new List<string[]>();
        foreach (var row in t.Rows)
        {
            var arr = new string[cols];
            for (int c = 0; c < cols; c++)
                arr[c] = c < row.Cells.Count ? RenderInlines(row.Cells[c]).Replace('\n', ' ') : "";
            cells.Add(arr);
        }
        var widths = new int[cols];
        foreach (var row in cells)
            for (int c = 0; c < cols; c++)
                widths[c] = Math.Max(widths[c], row[c].Length);

        string Border(char fill)
            => "+" + string.Join("+", widths.Select(w => new string(fill, w + 2))) + "+";

        string Pad(string s, int c)
        {
            int w = widths[c];
            char a = c < t.Alignments.Count ? t.Alignments[c] : MarkupConstants.ColumnAlignment.AlignLeft;
            return a switch
            {
                MarkupConstants.ColumnAlignment.AlignRight => s.PadLeft(w),
                MarkupConstants.ColumnAlignment.AlignCenter => s.PadLeft(s.Length + (w - s.Length) / 2).PadRight(w),
                _ => s.PadRight(w)
            };
        }

        sb.Append(Border('-')).Append('\n');
        for (int r = 0; r < cells.Count; r++)
        {
            sb.Append('|');
            for (int c = 0; c < cols; c++)
                sb.Append(' ').Append(Pad(cells[r][c], c)).Append(" |");
            sb.Append('\n');
            if (r == 0 && cells.Count > 1) sb.Append(Border('=')).Append('\n');
        }
        sb.Append(Border('-')).Append('\n');
    }

    private static string RenderInlines(IEnumerable<Inline> inlines)
    {
        var sb = new StringBuilder();
        foreach (var i in inlines)
        {
            if (i is LineBreak) sb.Append('\n');
            else if (i is TextRun r) sb.Append(r.SmallCaps ? r.Text.ToUpperInvariant() : r.Text);
            else if (i is RefRun rr) sb.Append('?').Append(rr.Key).Append('?');
        }
        return sb.ToString();
    }
}