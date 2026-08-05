// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Markup;
using SimplyDraft.Core.Domains.Markup.Blocks;
using SimplyDraft.Core.Domains.Markup.Entries;
using SimplyDraft.Core.Domains.Markup.Inlines;
using SimplyDraft.Core.Enums;
using SimplyDraft.Engine.Constants;
using SimplyDraft.Engine.Utils;

namespace SimplyDraft.Engine.Markup;

internal sealed class MarkupParser
{
    private readonly MarkupDocument _document = new();
    private readonly InlineParser _inlines;

    // Injected clock + culture for the date commands (\today, \maketitle) — see MarkupEngine.Parse.
    public DateTime Today {get;}
    public CultureInfo Culture {get;}

    // Open-environment state
    private bool _insideVerbatim;
    private readonly List<(ListKind Kind, int Counter)> _openLists = [];
    private int _quoteDepth;
    private int _centerDepth;
    private ParagraphBlock? _lastListItem;
    private TableBlock? _openTable;
    private ImageBlock? _openFigure;
    private bool _openFigureHasCaption;

    // Numbering / cross-references
    private int _heading1Count, _heading2Count, _heading3Count, _figureCount;
    private string _currentAnchor = "";
    private readonly Dictionary<string, string> _labels = new(StringComparer.Ordinal);
    private readonly List<(int Level, ParagraphBlock Paragraph)> _numberedHeadings = [];
    private readonly List<ImageBlock> _numberedFigures = [];

    // Title block (\title/\author/\date … \maketitle)
    private List<Inline>? _titleInlines, _authorInlines, _dateInlines;
    private bool _dateGiven;

    public MarkupParser(DateTime today, CultureInfo culture)
    {
        Today = today;
        Culture = culture ?? throw new ArgumentNullException(nameof(culture));
        _inlines = new InlineParser(this);
    }

    public MarkupDocument Parse(string text)
    {
        var lines = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (int index = 0; index < lines.Length; index++)
        {
            int lineNumber = index + 1;
            string rawLine = lines[index];

            if (_insideVerbatim) { TakeVerbatimLine(rawLine); continue; }

            string line = MarkupHelper.StripComment(rawLine);
            string trimmed = line.Trim();

            if (_openTable != null) { TakeTableLine(trimmed, lineNumber); continue; }
            if (_openFigure != null) { TakeFigureLine(trimmed, lineNumber); continue; }

            if (TryWholeLineCommand(trimmed, lineNumber)) continue;
            if (trimmed.StartsWith(MarkupConstants.Tokens.BeginTabular, StringComparison.Ordinal))
            {
                OpenTable(trimmed, lineNumber);
                continue;
            }
            if (TryHeading(trimmed, lineNumber)) continue;
            if (TryRunInHeading(trimmed, lineNumber)) continue;
            if (TryTitleMeta(trimmed, lineNumber)) continue;
            if (TryLabelOnlyLine(trimmed, lineNumber)) continue;
            if (TryStandaloneImage(trimmed)) continue;
            if (TryMisplacedCaption(trimmed, lineNumber)) continue;
            if (TryListItem(trimmed, lineNumber)) continue;

            if (trimmed.Length == 0) { TakeBlankLine(); continue; }
            if (TryContinueListItem(line, lineNumber)) continue;

            AppendParagraph(line, lineNumber);
        }

        Finish(lines.Length);
        return _document;
    }

    // Line dispatch

    private void TakeVerbatimLine(string rawLine)
    {
        if (rawLine.Trim() == MarkupConstants.Tokens.EndVerbatim)
        {
            _insideVerbatim = false;
            return;
        }
        var verbatim = new ParagraphBlock(ParagraphKind.Verbatim);
        verbatim.Inlines.Add(new TextRun(rawLine, Mono: true));
        _document.Blocks.Add(verbatim);
    }

    /// <summary>Whole-line commands and environment delimiters (exact match after trimming).</summary>
    private bool TryWholeLineCommand(string trimmed, int lineNumber)
    {
        switch (trimmed)
        {
            case MarkupConstants.Tokens.BeginVerbatim: _insideVerbatim = true; _lastListItem = null; return true;
            case MarkupConstants.Tokens.BeginItemize: _openLists.Add((ListKind.Bullet, 0)); _lastListItem = null; return true;
            case MarkupConstants.Tokens.BeginEnumerate: _openLists.Add((ListKind.Number, 0)); _lastListItem = null; return true;
            case MarkupConstants.Tokens.BeginDescription: _openLists.Add((ListKind.Description, 0)); _lastListItem = null; return true;
            case MarkupConstants.Tokens.EndItemize:
            case MarkupConstants.Tokens.EndEnumerate:
            case MarkupConstants.Tokens.EndDescription:
                if (_openLists.Count > 0) _openLists.RemoveAt(_openLists.Count - 1);
                else Warn($"unmatched {trimmed}", lineNumber);
                _lastListItem = null; return true;
            case MarkupConstants.Tokens.BeginQuote: _quoteDepth++; _lastListItem = null; return true;
            case MarkupConstants.Tokens.EndQuote:
                if (_quoteDepth > 0) _quoteDepth--;
                else Warn("unmatched \\end{quote}", lineNumber);
                _lastListItem = null; return true;
            case MarkupConstants.Tokens.BeginCenter: _centerDepth++; _lastListItem = null; return true;
            case MarkupConstants.Tokens.EndCenter:
                if (_centerDepth > 0) _centerDepth--;
                else Warn("unmatched \\end{center}", lineNumber);
                _lastListItem = null; return true;
            case MarkupConstants.Tokens.BeginFigure:
                _openFigure = new ImageBlock { Centered = _centerDepth > 0 };
                _openFigureHasCaption = false;
                _lastListItem = null; return true;
            case MarkupConstants.Tokens.EndFigure: Warn("unmatched \\end{figure}", lineNumber); return true;
            case MarkupConstants.Tokens.EndTabular: Warn("unmatched \\end{tabular}", lineNumber); return true;
            case MarkupConstants.Tokens.Centering:
                Warn("\\centering only applies inside a figure — use \\begin{center} for text", lineNumber);
                return true;
            case MarkupConstants.Tokens.PageBreak:
            case MarkupConstants.Tokens.NewPage:
            case MarkupConstants.Tokens.ClearPage:
                _document.Blocks.Add(new PageBreakBlock()); _lastListItem = null; return true;
            case MarkupConstants.Tokens.HRule:
            case MarkupConstants.Tokens.HLine:   // stray \hline ≈ rule
                _document.Blocks.Add(new RuleBlock()); _lastListItem = null; return true;
            case MarkupConstants.Tokens.TableOfContents:
                _document.Blocks.Add(new TableOfContentsBlock()); _lastListItem = null; return true;
            case MarkupConstants.Tokens.ListOfFigures:
                _document.Blocks.Add(new ListOfFiguresBlock()); _lastListItem = null; return true;
            case MarkupConstants.Tokens.MakeTitle: EmitTitleBlock(lineNumber); return true;
            case MarkupConstants.Tokens.BigSkip:
            case MarkupConstants.Tokens.MedSkip:
            case MarkupConstants.Tokens.SmallSkip:
                _document.Blocks.Add(new ParagraphBlock(ParagraphKind.Normal));   // approximated as a blank line
                _lastListItem = null; return true;
            case MarkupConstants.Tokens.NoIndent: return true;   // paragraphs are not indented — accepted, no effect
            default: return false;
        }
    }

    // Headings & title

    private static readonly (string Command, int Level)[] HeadingCommands =
    {
        (MarkupConstants.Tokens.Subsubsection, 3),
        (MarkupConstants.Tokens.Subsection, 2),
        (MarkupConstants.Tokens.Section, 1)
    };

    private bool TryHeading(string trimmed, int lineNumber)
    {
        foreach (var (command, level) in HeadingCommands)
        {
            if (!trimmed.StartsWith(command, StringComparison.Ordinal)) continue;

            int position = command.Length;
            bool starred = position < trimmed.Length && trimmed[position] == MarkupConstants.Delimiters.StarredSuffix;
            
            if (starred) position++;

            if (position >= trimmed.Length || trimmed[position] != MarkupConstants.Delimiters.GroupOpen) continue;
            
            int close = MarkupHelper.MatchBrace(trimmed, position, trimmed.Length);
            
            if (close < 0) continue;

            var (labels, afterLabels) = ReadLeadingLabels(trimmed, close + 1);
            string trailingText = afterLabels < trimmed.Length ? trimmed[afterLabels..].Trim() : "";

            string headingNumber = starred ? "" : BumpHeadingNumber(level);
            var heading = new ParagraphBlock(level switch
            {
                1 => ParagraphKind.Heading1,
                2 => ParagraphKind.Heading2,
                _ => ParagraphKind.Heading3
            }, centered: _centerDepth > 0, headingNumber: headingNumber);
            
            ParseInlines(trimmed[(position + 1)..close].Trim(), heading.Inlines, lineNumber);
            _document.Blocks.Add(heading);
            
            if (!starred)
            {
                _currentAnchor = headingNumber;
                _numberedHeadings.Add((level, heading));
            }
            
            foreach (var key in labels) BindLabel(key, starred ? _currentAnchor : headingNumber, lineNumber);
            
            if (trailingText.Length > 0)
            {
                var tail = new ParagraphBlock(_quoteDepth > 0 ? ParagraphKind.Quote : ParagraphKind.Normal,
                    centered: _centerDepth > 0);
                ParseInlines(trailingText, tail.Inlines, lineNumber);   // an inline \label here binds to this heading
                _document.Blocks.Add(tail);
            }
            
            _lastListItem = null;
            
            return true;
        }
        return false;
    }

    private static (List<string> Labels, int After) ReadLeadingLabels(string trimmed, int from)
    {
        var labels = new List<string>();
        int scan = from;

        while (true)
        {
            while (scan < trimmed.Length && char.IsWhiteSpace(trimmed[scan])) scan++;

            if (scan >= trimmed.Length) break;
            
            if (!trimmed.AsSpan(scan).StartsWith(MarkupConstants.Tokens.Label, StringComparison.Ordinal)) break;
            
            int braceOpen = scan + MarkupConstants.Tokens.Label.Length;
            
            if (braceOpen >= trimmed.Length || trimmed[braceOpen] != MarkupConstants.Delimiters.GroupOpen) break;
            
            int braceClose = MarkupHelper.MatchBrace(trimmed, braceOpen, trimmed.Length);
            
            if (braceClose < 0) break;
            
            labels.Add(trimmed[(braceOpen + 1)..braceClose].Trim());
            scan = braceClose + 1;
        }
        return (labels, scan);
    }

    /// <summary>\paragraph{X} / \subparagraph{X} — LaTeX run-in headings: bold lead-in, text continues.</summary>
    private bool TryRunInHeading(string trimmed, int lineNumber)
    {
        foreach (var command in new[] { MarkupConstants.Tokens.Subparagraph, MarkupConstants.Tokens.Paragraph })
        {
            if (!trimmed.StartsWith(command, StringComparison.Ordinal)) continue;
            
            int position = command.Length;
            
            if (position < trimmed.Length && trimmed[position] == MarkupConstants.Delimiters.StarredSuffix) position++;
            
            if (position >= trimmed.Length || trimmed[position] != MarkupConstants.Delimiters.GroupOpen) continue;
            
            int close = MarkupHelper.MatchBrace(trimmed, position, trimmed.Length);
            
            if (close < 0) continue;

            var paragraph = new ParagraphBlock(ParagraphKind.Normal, centered: _centerDepth > 0);
            var leadIn = new List<Inline>();
            ParseInlines(trimmed[(position + 1)..close].Trim(), leadIn, lineNumber);
            
            foreach (var inline in leadIn)
                paragraph.Inlines.Add(inline is TextRun run ? run with { Bold = true } : inline);
            
            string rest = trimmed[(close + 1)..].TrimStart();
            
            if (rest.Length > 0)
            {
                paragraph.Inlines.Add(new TextRun("  "));
                ParseInlines(rest, paragraph.Inlines, lineNumber);
            }

            _document.Blocks.Add(paragraph);
            _lastListItem = null;
            
            return true;
        }
        return false;
    }

    /// <summary>\title{…} / \author{…} / \date{…} — stored for \maketitle.</summary>
    private bool TryTitleMeta(string trimmed, int lineNumber)
    {
        foreach (var command in new[] { MarkupConstants.Tokens.Title, MarkupConstants.Tokens.Author, MarkupConstants.Tokens.Date })
        {
            if (!trimmed.StartsWith(command + MarkupConstants.Delimiters.GroupOpen, StringComparison.Ordinal)) continue;

            int close = MarkupHelper.MatchBrace(trimmed, command.Length, trimmed.Length);
            
            if (close < 0 || trimmed[(close + 1)..].Trim().Length != 0) continue;
            
            var inlines = new List<Inline>();
            
            ParseInlines(trimmed[(command.Length + 1)..close].Trim(), inlines, lineNumber);
            
            switch (command)
            {
                case MarkupConstants.Tokens.Title: _titleInlines = inlines; break;
                case MarkupConstants.Tokens.Author: _authorInlines = inlines; break;
                default: _dateInlines = inlines; _dateGiven = true; break;   // \date{} suppresses the date
            }

            _lastListItem = null;

            return true;
        }
        return false;
    }

    private void EmitTitleBlock(int lineNumber)
    {
        _lastListItem = null;

        if (_titleInlines is null && _authorInlines is null && !_dateGiven)
        {
            Warn("\\maketitle: no \\title{…} was given above it", lineNumber);
            return;
        }

        if (_titleInlines is { Count: > 0 })
        {
            var title = new ParagraphBlock(ParagraphKind.Heading1, centered: true);   // unnumbered, not in the TOC
            title.Inlines.AddRange(_titleInlines);
            _document.Blocks.Add(title);
        }

        if (_authorInlines is { Count: > 0 })
        {
            var author = new ParagraphBlock(ParagraphKind.Normal, centered: true);
            author.Inlines.AddRange(_authorInlines);
            _document.Blocks.Add(author);
        }

        if (_dateGiven && _dateInlines is { Count: > 0 })
        {
            var date = new ParagraphBlock(ParagraphKind.Normal, centered: true);
            date.Inlines.AddRange(_dateInlines);
            _document.Blocks.Add(date);
        }
        else if (!_dateGiven)
        {
            var date = new ParagraphBlock(ParagraphKind.Normal, centered: true);
            date.Inlines.Add(new TextRun(Today.ToString(MarkupConstants.Formats.DateFormat, Culture)));
            _document.Blocks.Add(date);
        }
    }

    private string BumpHeadingNumber(int level)
    {
        switch (level)
        {
            case 1:
                _heading1Count++; _heading2Count = 0; _heading3Count = 0; return _heading1Count.ToString(CultureInfo.InvariantCulture);
            case 2:
                _heading2Count++; _heading3Count = 0; return $"{_heading1Count}.{_heading2Count}";
            default:
                _heading3Count++; return $"{_heading1Count}.{_heading2Count}.{_heading3Count}";
        }
    }

    // ---------- lists & paragraphs ----------

    private bool TryListItem(string trimmed, int lineNumber)
    {
        bool isItemLine = _openLists.Count > 0
            && trimmed.StartsWith(MarkupConstants.Tokens.Item, StringComparison.Ordinal)
            && (trimmed.Length == MarkupConstants.Tokens.Item.Length
                || !char.IsLetter(trimmed[MarkupConstants.Tokens.Item.Length]));
        
        if (!isItemLine) return false;
        
        TakeListItem(trimmed, lineNumber);
        
        return true;
    }

    private void TakeListItem(string trimmed, int lineNumber)
    {
        var (kind, counter) = _openLists[^1];
        string rest = trimmed.Length > MarkupConstants.Tokens.Item.Length
            ? trimmed[MarkupConstants.Tokens.Item.Length..].TrimStart()
            : "";
        
        ParagraphBlock item;
        
        if (kind == ListKind.Description)
        {
            item = new ParagraphBlock(ParagraphKind.DescriptionItem, _openLists.Count, centered: _centerDepth > 0);
            if (rest.StartsWith(MarkupConstants.Delimiters.OptionalArgOpen))
            {
                int closeBracket = rest.IndexOf(MarkupConstants.Delimiters.OptionalArgClose);
                if (closeBracket > 0)
                {
                    ParseInlines(rest[1..closeBracket].Trim(), item.Term, lineNumber);
                    rest = rest[(closeBracket + 1)..].TrimStart();
                }
                else
                {
                    Warn("\\item[…] is missing its closing ']'", lineNumber);
                }
            }
            ParseInlines(rest, item.Inlines, lineNumber);
        }
        else
        {
            counter++;
            _openLists[^1] = (kind, counter);
            item = new ParagraphBlock(
                kind == ListKind.Number ? ParagraphKind.NumberItem : ParagraphKind.BulletItem,
                _openLists.Count, counter, centered: _centerDepth > 0);
            ParseInlines(rest, item.Inlines, lineNumber);
        }
        _document.Blocks.Add(item);
        _lastListItem = item;
    }

    private void TakeBlankLine()
    {
        if (_openLists.Count == 0)
            _document.Blocks.Add(new ParagraphBlock(_quoteDepth > 0 ? ParagraphKind.Quote : ParagraphKind.Normal));
        _lastListItem = null;
    }

    /// <summary>Inside a list, a plain line continues the previous \item on a soft line break.</summary>
    private bool TryContinueListItem(string line, int lineNumber)
    {
        if (_openLists.Count == 0 || _lastListItem is null) return false;
        _lastListItem.Inlines.Add(new LineBreak());
        ParseInlines(line.TrimStart(), _lastListItem.Inlines, lineNumber);
        return true;
    }

    private void AppendParagraph(string line, int lineNumber)
    {
        var paragraph = new ParagraphBlock(_quoteDepth > 0 ? ParagraphKind.Quote : ParagraphKind.Normal,
            centered: _centerDepth > 0);
        ParseInlines(line, paragraph.Inlines, lineNumber);
        _document.Blocks.Add(paragraph);
        _lastListItem = null;
    }

    // Tabular

    private void OpenTable(string trimmed, int lineNumber)
    {
        _openTable = new TableBlock();
        _lastListItem = null;
        string rest = trimmed[MarkupConstants.Tokens.BeginTabular.Length..].Trim();

        if (rest.StartsWith(MarkupConstants.Delimiters.GroupOpen))
        {
            int close = MarkupHelper.MatchBrace(rest, 0, rest.Length);

            if (close > 0)
            {
                foreach (char specifier in rest[1..close])
                    if (specifier is MarkupConstants.ColumnAlignment.AlignLeft
                        or MarkupConstants.ColumnAlignment.AlignCenter
                        or MarkupConstants.ColumnAlignment.AlignRight)
                        _openTable.Alignments.Add(specifier);
                rest = rest[(close + 1)..].Trim();
            }
        }
        if (rest.Length > 0)
            Warn("text after \\begin{tabular}{…} is ignored", lineNumber);
    }

    private void TakeTableLine(string trimmed, int lineNumber)
    {
        if (trimmed == MarkupConstants.Tokens.EndTabular) {CloseTable(lineNumber); return;}
        if (trimmed.Length == 0) return;

        while (trimmed.EndsWith(MarkupConstants.Tokens.HLine, StringComparison.Ordinal))
            trimmed = trimmed[..^MarkupConstants.Tokens.HLine.Length].TrimEnd();
        if (trimmed.EndsWith(MarkupConstants.Delimiters.LineBreak, StringComparison.Ordinal))
            trimmed = trimmed[..^MarkupConstants.Delimiters.LineBreak.Length].TrimEnd();
        if (trimmed.Length == 0 || trimmed == MarkupConstants.Tokens.HLine) return;

        var row = new RowBlock();

        foreach (var cell in SplitCells(trimmed))
        {
            var cellInlines = new List<Inline>();
            ParseInlines(cell.Trim(), cellInlines, lineNumber);
            row.Cells.Add(cellInlines);
        }
        _openTable!.Rows.Add(row);
    }

    private void CloseTable(int lineNumber)
    {
        if (_openTable!.Rows.Count > 0) _document.Blocks.Add(_openTable);
        else Warn("empty tabular environment", lineNumber);
        _openTable = null;
    }

    /// <summary>Splits a row on unescaped '&' (\& stays in the cell for the inline parser).</summary>
    private static List<string> SplitCells(string row)
    {
        var cells = new List<string>();
        var cell = new System.Text.StringBuilder();

        for (int index = 0; index < row.Length; index++)
        {
            char current = row[index];
            if (current == MarkupConstants.Delimiters.SyntaxStart && index + 1 < row.Length)
            {
                cell.Append(current).Append(row[index + 1]);
                index++;
                continue;
            }
            if (current == MarkupConstants.Delimiters.CellSeparator)
            {
                cells.Add(cell.ToString());
                cell.Clear();
                continue;
            }
            cell.Append(current);
        }
        cells.Add(cell.ToString());
        return cells;
    }

    // ---------- figure ----------

    private void TakeFigureLine(string trimmed, int lineNumber)
    {
        if (trimmed == MarkupConstants.Tokens.EndFigure) { EmitFigure(lineNumber); return; }
        if (trimmed.Length == 0) return;
        if (trimmed == MarkupConstants.Tokens.Centering) { _openFigure!.Centered = true; return; }

        if (TryReadIncludeGraphics(trimmed, out var imagePath))
        {
            if (_openFigure!.Path.Length > 0) Warn("second \\includegraphics in the same figure is ignored", lineNumber);
            else _openFigure.Path = imagePath;
            return;
        }

        if (TryReadCaption(trimmed, out var captionText, out var captionLabels))
        {
            if (_openFigureHasCaption) Warn("second \\caption in the same figure is ignored", lineNumber);
            else
            {
                _openFigureHasCaption = true;
                _openFigure!.FigureNumber = ++_figureCount;
                _numberedFigures.Add(_openFigure);
                ParseInlines(captionText.Trim(), _openFigure.Caption, lineNumber);
            }
            foreach (var key in captionLabels) BindLabel(key, CurrentLabelTarget, lineNumber);
            return;
        }

        if (TrailingLabels(trimmed, 0) is { Count: > 0 } labels)
        {
            foreach (var key in labels) BindLabel(key, CurrentLabelTarget, lineNumber);
            return;
        }

        Warn($"line ignored inside figure: {trimmed}", lineNumber);
    }

    private static bool TryReadCaption(string trimmed, out string captionText, out List<string> labels)
    {
        captionText = "";
        labels = [];
        
        if (!trimmed.StartsWith(MarkupConstants.Tokens.CaptionOpen, StringComparison.Ordinal)) return false;
        
        int open = MarkupConstants.Tokens.Caption.Length;
        int close = MarkupHelper.MatchBrace(trimmed, open, trimmed.Length);
        
        if (close < 0) return false;
        
        var trailing = TrailingLabels(trimmed, close + 1);
        
        if (trailing is null) return false;
        
        captionText = trimmed[(open + 1)..close];
        labels = trailing;

        return true;
    }

    private void EmitFigure(int lineNumber)
    {
        var figure = _openFigure!;
        _openFigure = null;
        _lastListItem = null;
        
        if (figure.Path.Length == 0 && figure.Caption.Count == 0)
        {
            Warn("empty figure environment", lineNumber);
            if (figure.FigureNumber > 0) _numberedFigures.Remove(figure);
            return;
        }

        _document.Blocks.Add(figure);
    }

    private bool TryStandaloneImage(string trimmed)
    {
        if (!TryReadIncludeGraphics(trimmed, out var imagePath)) return false;

        _document.Blocks.Add(new ImageBlock {Path = imagePath, Centered = _centerDepth > 0});
        _lastListItem = null;

        return true;
    }

    private bool TryMisplacedCaption(string trimmed, int lineNumber)
    {
        if (!TryReadCaption(trimmed, out _, out _)) return false;

        Warn("\\caption belongs inside \\begin{figure} … \\end{figure}", lineNumber);

        return true;
    }

    private static bool TryReadIncludeGraphics(string trimmed, out string path)
    {
        path = "";
        
        const string command = MarkupConstants.Tokens.IncludeGraphics;
        
        if (!trimmed.StartsWith(command, StringComparison.Ordinal)) return false;

        int position = command.Length;
        
        if (position < trimmed.Length && trimmed[position] == MarkupConstants.Delimiters.OptionalArgOpen)
        {
            int closeBracket = trimmed.IndexOf(MarkupConstants.Delimiters.OptionalArgClose, position);
            if (closeBracket < 0) return false;
            position = closeBracket + 1; // size options are accepted and ignored
        }
        
        while (position < trimmed.Length && char.IsWhiteSpace(trimmed[position])) position++;
        
        if (position >= trimmed.Length || trimmed[position] != MarkupConstants.Delimiters.GroupOpen) return false;
        
        int close = MarkupHelper.MatchBrace(trimmed, position, trimmed.Length);
        
        if (close < 0 || trimmed[(close + 1)..].Trim().Length != 0) return false;
        
        path = trimmed[(position + 1)..close].Trim();
        
        return path.Length > 0;
    }

    // Labels & references

    private bool TryLabelOnlyLine(string trimmed, int lineNumber)
    {
        if (TrailingLabels(trimmed, 0) is not { Count: > 0 } labels) return false;

        foreach (var key in labels) BindLabel(key, _currentAnchor, lineNumber);

        return true;
    }

    private static List<string>? TrailingLabels(string trimmed, int from)
    {
        var labels = new List<string>();
        int scan = from;

        while (true)
        {
            while (scan < trimmed.Length && char.IsWhiteSpace(trimmed[scan])) scan++;

            if (scan >= trimmed.Length) return labels;

            if (!trimmed.AsSpan(scan).StartsWith(MarkupConstants.Tokens.Label, StringComparison.Ordinal)) return null;

            int braceOpen = scan + MarkupConstants.Tokens.Label.Length;

            if (braceOpen >= trimmed.Length || trimmed[braceOpen] != MarkupConstants.Delimiters.GroupOpen) return null;

            int braceClose = MarkupHelper.MatchBrace(trimmed, braceOpen, trimmed.Length);

            if (braceClose < 0) return null;

            labels.Add(trimmed[(braceOpen + 1)..braceClose].Trim());

            scan = braceClose + 1;
        }
    }

    /// <summary>What a \label on the current line refers to: the captioned figure being built, else the current heading.</summary>
    internal string CurrentLabelTarget
        => _openFigure != null && _openFigureHasCaption
            ? _openFigure.FigureNumber.ToString(CultureInfo.InvariantCulture)
            : _currentAnchor;

    internal void BindLabel(string key, string target, int lineNumber)
    {
        if (key.Length == 0)
        {
            Warn("\\label needs a name, e.g. \\label{sec:intro}", lineNumber);
            return;
        }
        if (!_labels.TryAdd(key, target))
            Warn($"duplicate label '{key}' — the first definition wins", lineNumber);
    }

    // End of document

    private void Finish(int lastLineNumber)
    {
        if (_insideVerbatim) Warn("\\begin{verbatim} is never closed", lastLineNumber);
        if (_openTable != null) { Warn("\\begin{tabular} is never closed", lastLineNumber); CloseTable(lastLineNumber); }
        if (_openFigure != null) { Warn("\\begin{figure} is never closed", lastLineNumber); EmitFigure(lastLineNumber); }
        if (_openLists.Count > 0) Warn("a list environment is never closed", lastLineNumber);
        if (_quoteDepth > 0) Warn("\\begin{quote} is never closed", lastLineNumber);
        if (_centerDepth > 0) Warn("\\begin{center} is never closed", lastLineNumber);
        ResolveReferences();
        PopulateContentsTables();
    }

    /// <summary>Replaces every \ref run with its resolved number, or a visible ?key? with a warning.</summary>
    private void ResolveReferences()
    {
        foreach (var inlineList in AllInlineLists())
            for (int index = 0; index < inlineList.Count; index++)
            {
                if (inlineList[index] is not RefRun reference) continue;

                if (_labels.TryGetValue(reference.Key, out var target) && target.Length > 0)
                {
                    inlineList[index] = new TextRun(target,
                        reference.Bold, reference.Italic, reference.Underline, reference.Mono);
                }
                else
                {
                    Warn(_labels.ContainsKey(reference.Key)
                        ? $"label '{reference.Key}' is not attached to a numbered section or figure"
                        : $"undefined reference '{reference.Key}'", reference.Line);
                    inlineList[index] = new TextRun("?" + reference.Key + "?",
                        reference.Bold, reference.Italic, reference.Underline, reference.Mono);
                }
            }
    }

    private IEnumerable<List<Inline>> AllInlineLists()
    {
        foreach (var block in _document.Blocks)
        {
            if (block is ParagraphBlock paragraph)
            {
                yield return paragraph.Term;
                yield return paragraph.Inlines;
            }
            else if (block is TableBlock table)
            {
                foreach (var row in table.Rows)
                    foreach (var cell in row.Cells)
                        yield return cell;
            }
            else if (block is ImageBlock image)
            {
                yield return image.Caption;
            }
        }
    }

    /// <summary>Fills every \tableofcontents and \listoffigures block from the collected headings/figures.</summary>
    private void PopulateContentsTables()
    {
        List<TableOfContentsEntry>? tocEntries = null;
        List<FigureEntry>? figureEntries = null;

        foreach (var block in _document.Blocks)
        {
            if (block is TableOfContentsBlock toc)
            {
                tocEntries ??= _numberedHeadings
                    .Select(h => new TableOfContentsEntry(h.Level, h.Paragraph.HeadingNumber, MarkupHelper.Flatten(h.Paragraph.Inlines)))
                    .ToList();
                
                toc.Entries.AddRange(tocEntries);
            }
            else if (block is ListOfFiguresBlock listOfFigures)
            {
                figureEntries ??= _numberedFigures
                    .Select(f => new FigureEntry(f.FigureNumber, MarkupHelper.Flatten(f.Caption)))
                    .ToList();
                
                listOfFigures.Entries.AddRange(figureEntries);
            }
        }
    }

    // Shared plumbing

    internal void Warn(string message, int lineNumber)
        => _document.Warnings.Add(new Diagnostic(DiagnosticCode.MarkupWarning, DiagnosticSeverity.Warning, message, lineNumber, 1));

    private void ParseInlines(string source, List<Inline> output, int lineNumber)
        => _inlines.Parse(source, output, lineNumber);
}