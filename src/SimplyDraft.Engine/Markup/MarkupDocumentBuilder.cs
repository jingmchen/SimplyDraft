using System.Globalization;
using System.Text;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Markup;
using SimplyDraft.Core.Domains.Markup.Blocks;
using SimplyDraft.Core.Domains.Markup.Entries;
using SimplyDraft.Core.Domains.Markup.Inlines;
using SimplyDraft.Core.Enums;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Markup;

public sealed class MarkupDocumentBuilder
{
    private readonly MarkupDocument _doc = new();

    // environment state
    private bool _verbatim;
    private readonly List<(ListKind Kind, int Counter)> _lists = [];
    private int _quote, _center;
    private ParagraphBlock? _lastItem;
    private TableBlock? _table;
    private ImageBlock? _figure;
    private bool _figureCaptioned;

    // numbering / cross-references
    private int _c1, _c2, _c3, _figCounter;
    private string _anchor = "";
    private readonly Dictionary<string, string> _labels = new(StringComparer.Ordinal);
    private readonly List<(int Level, ParagraphBlock P)> _headings = [];
    private readonly List<ImageBlock> _figures = [];

    // title block (\title/\author/\date … \maketitle)
    private List<Inline>? _titleInl, _authorInl, _dateInl;
    private bool _dateGiven;

    // Injected clock + culture for the date commands — see MarkupEngine.Parse.
    private readonly DateTime _today;
    private readonly CultureInfo _culture;

    public MarkupDocumentBuilder(DateTime today, CultureInfo culture)
    {
        _today = today;
        _culture = culture;
    }

    public MarkupDocument Run(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (int idx = 0; idx < lines.Length; idx++)
        {
            int lineNo = idx + 1;
            string raw = lines[idx];

            if (_verbatim)
            {
                if (raw.Trim() == MarkupConstants.Tokens.EndVerbatim) { _verbatim = false; continue; }
                
                var vp = new ParagraphBlock(ParagraphKind.Verbatim);
                
                vp.Inlines.Add(new TextRun(raw, Mono: true));
                _doc.Blocks.Add(vp);

                continue;
            }

            string line = MarkupHelper.StripComment(raw);
            string t = line.Trim();

            if (_table != null)
            {
                TableLine(t, lineNo);
                continue;
            }

            if (_figure != null)
            {
                FigureLine(t, lineNo);
                continue;
            }

            if (ExactLine(t, lineNo))
                continue;

            if (t.StartsWith(MarkupConstants.Tokens.BeginTabular, StringComparison.Ordinal))
            {
                OpenTable(t, lineNo);
                continue;
            }

            if (TryHeading(t, lineNo))
                continue;
            
            if (TryRunInHeading(t, lineNo))
                continue;
            
            if (TryTitleMeta(t, lineNo)) continue;

            if (TrailingLabels(t, 0) is { Count: > 0 } soloLabels)
            {
                foreach (var key in soloLabels) BindLabel(key, _anchor, lineNo);
                continue;
            }

            if (TryIncludeGraphics(t, out var imgPath))
            {
                _doc.Blocks.Add(new ImageBlock { Path = imgPath, Centered = _center > 0 });
                _lastItem = null;
                continue;
            }

            if (TryCaption(t, out _, out _))
            {
                Warn("\\caption belongs inside \\begin{figure} … \\end{figure}", lineNo);
                continue;
            }

            if (_lists.Count > 0 && t.StartsWith(MarkupConstants.Tokens.Item, StringComparison.Ordinal)
                && (t.Length == MarkupConstants.Tokens.Item.Length || !char.IsLetter(t[MarkupConstants.Tokens.Item.Length]))
            )
            {
                Item(t, lineNo);
                continue;
            }

            if (t.Length == 0)
            {
                if (_lists.Count == 0)
                    _doc.Blocks.Add(new ParagraphBlock(_quote > 0 ? ParagraphKind.Quote : ParagraphKind.Normal));
                _lastItem = null;
                continue;
            }

            if (_lists.Count > 0 && _lastItem != null)
            {
                _lastItem.Inlines.Add(new LineBreak());
                ParseInlines(line.TrimStart(), _lastItem.Inlines, lineNo);
                continue;
            }

            var para = new ParagraphBlock(_quote > 0 ? ParagraphKind.Quote : ParagraphKind.Normal, centered: _center > 0);
            ParseInlines(line, para.Inlines, lineNo);
            _doc.Blocks.Add(para);
            _lastItem = null;
        }

        Finish(lines.Length);
        return _doc;
    }

    // Line dispatch

    /// <summary>Whole-line commands and environment delimiters (exact match after trimming).</summary>
    private bool ExactLine(string t, int lineNo)
    {
        switch (t)
        {
            case MarkupConstants.Tokens.BeginVerbatim: _verbatim = true; _lastItem = null; return true;
            case MarkupConstants.Tokens.BeginItemize: _lists.Add((ListKind.Bullet, 0)); _lastItem = null; return true;
            case MarkupConstants.Tokens.BeginEnumerate: _lists.Add((ListKind.Number, 0)); _lastItem = null; return true;
            case MarkupConstants.Tokens.BeginDescription: _lists.Add((ListKind.Description, 0)); _lastItem = null; return true;
            case MarkupConstants.Tokens.EndItemize:
            case MarkupConstants.Tokens.EndEnumerate:
            case MarkupConstants.Tokens.EndDescription:
                if (_lists.Count > 0) _lists.RemoveAt(_lists.Count - 1);
                else Warn($"unmatched {t}", lineNo);
                _lastItem = null; return true;
            case MarkupConstants.Tokens.BeginQuote: _quote++; _lastItem = null; return true;
            case MarkupConstants.Tokens.EndQuote:
                if (_quote > 0) _quote--;
                else Warn("unmatched \\end{quote}", lineNo);
                _lastItem = null; return true;
            case MarkupConstants.Tokens.BeginCenter: _center++; _lastItem = null; return true;
            case MarkupConstants.Tokens.EndCenter:
                if (_center > 0) _center--;
                else Warn("unmatched \\end{center}", lineNo);
                _lastItem = null; return true;
            case MarkupConstants.Tokens.BeginFigure:
                _figure = new ImageBlock { Centered = _center > 0 };
                _figureCaptioned = false;
                _lastItem = null; return true;
            case MarkupConstants.Tokens.EndFigure: Warn("unmatched \\end{figure}", lineNo); return true;
            case MarkupConstants.Tokens.EndTabular: Warn("unmatched \\end{tabular}", lineNo); return true;
            case MarkupConstants.Tokens.Centering:
                Warn("\\centering only applies inside a figure — use \\begin{center} for text", lineNo);
                return true;
            case MarkupConstants.Tokens.PageBreak:
            case MarkupConstants.Tokens.NewPage:
            case MarkupConstants.Tokens.ClearPage: _doc.Blocks.Add(new PageBreakBlock()); _lastItem = null; return true;
            case MarkupConstants.Tokens.HRule:
            case MarkupConstants.Tokens.HLine: _doc.Blocks.Add(new RuleBlock()); _lastItem = null; return true;   // stray \hline ≈ rule
            case MarkupConstants.Tokens.TableOfContents: _doc.Blocks.Add(new TableOfContentsBlock()); _lastItem = null; return true;
            case MarkupConstants.Tokens.ListOfFigures: _doc.Blocks.Add(new ListOfFiguresBlock()); _lastItem = null; return true;
            case MarkupConstants.Tokens.MakeTitle: EmitTitleBlock(lineNo); return true;
            case MarkupConstants.Tokens.BigSkip:
            case MarkupConstants.Tokens.MedSkip:
            case MarkupConstants.Tokens.SmallSkip:
                _doc.Blocks.Add(new ParagraphBlock(ParagraphKind.Normal));   // approximated as a blank line
                _lastItem = null; return true;
            case MarkupConstants.Tokens.NoIndent: return true;   // paragraphs are not indented — accepted, no effect
            default: return false;
        }
    }

    private bool TryHeading(string t, int lineNo)
    {
        foreach (var (cmd, level) in HeadingCmds)
        {
            if (!t.StartsWith(cmd, StringComparison.Ordinal))
                continue;
            
            int pos = cmd.Length;
            
            bool starred = pos < t.Length && t[pos] == MarkupConstants.Delimiters.StarredSuffix;
            
            if (starred)
                pos++;
            
            if (pos >= t.Length || t[pos] != MarkupConstants.Delimiters.GroupOpen)
                continue;
            
            int close = MarkupHelper.MatchBrace(t, pos, t.Length);
            
            if (close < 0)
                continue;

            // Leading \label{…} tokens bind to the heading; anything after them becomes an
            // ordinary paragraph on the next block — like real LaTeX, where text may follow
            // a sectioning command on the same source line.
            int q = close + 1;
            var labels = new List<string>();

            while (true)
            {
                while (q < t.Length && char.IsWhiteSpace(t[q]))
                    q++;
                
                if (q >= t.Length)
                    break;
                
                if (!t.AsSpan(q).StartsWith(MarkupConstants.Tokens.Label, StringComparison.Ordinal))
                    break;
                
                int lb = q + MarkupConstants.Tokens.Label.Length;
                
                if (lb >= t.Length || t[lb] != MarkupConstants.Delimiters.GroupOpen)
                    break;
                
                int lc = MarkupHelper.MatchBrace(t, lb, t.Length);
                
                if (lc < 0)
                    break;
                
                labels.Add(t[(lb + 1)..lc].Trim());
                
                q = lc + 1;
            }
            string trailing = q < t.Length ? t[q..].Trim() : "";

            string number = starred ? "" : Bump(level);
            var p = new ParagraphBlock(level switch
            {
                1 => ParagraphKind.Heading1,
                2 => ParagraphKind.Heading2,
                _ => ParagraphKind.Heading3
            }, centered: _center > 0, headingNumber: number);

            ParseInlines(t[(pos + 1)..close].Trim(), p.Inlines, lineNo);
            
            _doc.Blocks.Add(p);
            
            if (!starred)
            {
                _anchor = number;
                _headings.Add((level, p));
            }

            foreach (var key in labels)
                BindLabel(key, starred ? _anchor : number, lineNo);
            
            if (trailing.Length > 0)
            {
                var tail = new ParagraphBlock(_quote > 0 ? ParagraphKind.Quote : ParagraphKind.Normal, centered: _center > 0);
                ParseInlines(trailing, tail.Inlines, lineNo);   // an inline \label here binds to this heading
                _doc.Blocks.Add(tail);
            }

            _lastItem = null;
            
            return true;
        }

        return false;
    }

    private static readonly (string Cmd, int Level)[] HeadingCmds =
    {
        (MarkupConstants.Tokens.Subsubsection, 3), (MarkupConstants.Tokens.Subsection, 2), (MarkupConstants.Tokens.Section, 1)
    };

    /// <summary>\paragraph{X} / \subparagraph{X} — LaTeX run-in headings: bold lead-in, text continues.</summary>
    private bool TryRunInHeading(string t, int lineNo)
    {
        foreach (var cmd in new[] { MarkupConstants.Tokens.Subparagraph, MarkupConstants.Tokens.Paragraph })
        {
            if (!t.StartsWith(cmd, StringComparison.Ordinal)) continue;
            int pos = cmd.Length;
            if (pos < t.Length && t[pos] == MarkupConstants.Delimiters.StarredSuffix) pos++;
            if (pos >= t.Length || t[pos] != MarkupConstants.Delimiters.GroupOpen) continue;
            int close = MarkupHelper.MatchBrace(t, pos, t.Length);
            if (close < 0) continue;

            var p = new ParagraphBlock(ParagraphKind.Normal, centered: _center > 0);
            var lead = new List<Inline>();
            ParseInlines(t[(pos + 1)..close].Trim(), lead, lineNo);
            foreach (var inl in lead)
                p.Inlines.Add(inl is TextRun r ? r with { Bold = true } : inl);
            string rest = t[(close + 1)..].TrimStart();
            if (rest.Length > 0)
            {
                p.Inlines.Add(new TextRun("  "));
                ParseInlines(rest, p.Inlines, lineNo);
            }
            _doc.Blocks.Add(p);
            _lastItem = null;
            return true;
        }
        return false;
    }

    /// <summary>\title{…} / \author{…} / \date{…} — stored for \maketitle.</summary>
    private bool TryTitleMeta(string t, int lineNo)
    {
        foreach (var cmd in new[] { MarkupConstants.Tokens.Title, MarkupConstants.Tokens.Author, MarkupConstants.Tokens.Date })
        {
            if (!t.StartsWith(cmd + MarkupConstants.Delimiters.GroupOpen, StringComparison.Ordinal)) continue;
            int close = MarkupHelper.MatchBrace(t, cmd.Length, t.Length);
            if (close < 0 || t[(close + 1)..].Trim().Length != 0) continue;
            var inl = new List<Inline>();
            ParseInlines(t[(cmd.Length + 1)..close].Trim(), inl, lineNo);
            switch (cmd)
            {
                case MarkupConstants.Tokens.Title: _titleInl = inl; break;
                case MarkupConstants.Tokens.Author: _authorInl = inl; break;
                default: _dateInl = inl; _dateGiven = true; break;   // \date{} suppresses the date
            }
            _lastItem = null;
            return true;
        }
        return false;
    }

    private void EmitTitleBlock(int lineNo)
    {
        _lastItem = null;
        if (_titleInl is null && _authorInl is null && !_dateGiven)
        {
            Warn("\\maketitle: no \\title{…} was given above it", lineNo);
            return;
        }
        if (_titleInl is { Count: > 0 })
        {
            var tp = new ParagraphBlock(ParagraphKind.Heading1, centered: true);   // unnumbered, not in the TOC
            tp.Inlines.AddRange(_titleInl);
            _doc.Blocks.Add(tp);
        }
        if (_authorInl is { Count: > 0 })
        {
            var ap = new ParagraphBlock(ParagraphKind.Normal, centered: true);
            ap.Inlines.AddRange(_authorInl);
            _doc.Blocks.Add(ap);
        }
        if (_dateGiven && _dateInl is { Count: > 0 })
        {
            var dp = new ParagraphBlock(ParagraphKind.Normal, centered: true);
            dp.Inlines.AddRange(_dateInl);
            _doc.Blocks.Add(dp);
        }
        else if (!_dateGiven)
        {
            var dp = new ParagraphBlock(ParagraphKind.Normal, centered: true);
            dp.Inlines.Add(new TextRun(_today.ToString(MarkupConstants.Formats.DateFormat, _culture)));
            _doc.Blocks.Add(dp);
        }
    }

    private string Bump(int level)
    {
        switch (level)
        {
            case 1: _c1++; _c2 = 0; _c3 = 0; return _c1.ToString(CultureInfo.InvariantCulture);
            case 2: _c2++; _c3 = 0; return $"{_c1}.{_c2}";
            default: _c3++; return $"{_c1}.{_c2}.{_c3}";
        }
    }

    private void Item(string t, int lineNo)
    {
        var (kind, counter) = _lists[^1];
        string rest = t.Length > MarkupConstants.Tokens.Item.Length ? t[MarkupConstants.Tokens.Item.Length..].TrimStart() : "";
        ParagraphBlock item;
        if (kind == ListKind.Description)
        {
            item = new ParagraphBlock(ParagraphKind.DescriptionItem, _lists.Count, centered: _center > 0);
            if (rest.StartsWith(MarkupConstants.Delimiters.OptionalArgOpen))
            {
                int rb = rest.IndexOf(MarkupConstants.Delimiters.OptionalArgClose);
                if (rb > 0)
                {
                    ParseInlines(rest[1..rb].Trim(), item.Term, lineNo);
                    rest = rest[(rb + 1)..].TrimStart();
                }
                else
                {
                    Warn("\\item[…] is missing its closing ']'", lineNo);
                }
            }
            ParseInlines(rest, item.Inlines, lineNo);
        }
        else
        {
            counter++;
            _lists[^1] = (kind, counter);
            item = new ParagraphBlock(
                kind == ListKind.Number ? ParagraphKind.NumberItem : ParagraphKind.BulletItem,
                _lists.Count, counter, centered: _center > 0);
            ParseInlines(rest, item.Inlines, lineNo);
        }
        _doc.Blocks.Add(item);
        _lastItem = item;
    }

    // ---------- tabular ----------

    private void OpenTable(string t, int lineNo)
    {
        _table = new TableBlock();
        _lastItem = null;
        string rest = t[MarkupConstants.Tokens.BeginTabular.Length..].Trim();
        if (rest.StartsWith(MarkupConstants.Delimiters.GroupOpen))
        {
            int close = MarkupHelper.MatchBrace(rest, 0, rest.Length);
            if (close > 0)
            {
                foreach (char c in rest[1..close])
                    if (c is MarkupConstants.ColumnAlignment.AlignLeft or MarkupConstants.ColumnAlignment.AlignCenter or MarkupConstants.ColumnAlignment.AlignRight) _table.Alignments.Add(c);
                rest = rest[(close + 1)..].Trim();
            }
        }
        if (rest.Length > 0)
            Warn("text after \\begin{tabular}{…} is ignored", lineNo);
    }

    private void TableLine(string t, int lineNo)
    {
        if (t == MarkupConstants.Tokens.EndTabular) { CloseTable(lineNo); return; }
        if (t.Length == 0) return;

        while (t.EndsWith(MarkupConstants.Tokens.HLine, StringComparison.Ordinal)) t = t[..^MarkupConstants.Tokens.HLine.Length].TrimEnd();
        if (t.EndsWith(MarkupConstants.Delimiters.LineBreak, StringComparison.Ordinal)) t = t[..^MarkupConstants.Delimiters.LineBreak.Length].TrimEnd();
        if (t.Length == 0 || t == MarkupConstants.Tokens.HLine) return;

        var row = new RowBlock();
        foreach (var cell in SplitCells(t))
        {
            var inl = new List<Inline>();
            ParseInlines(cell.Trim(), inl, lineNo);
            row.Cells.Add(inl);
        }
        _table!.Rows.Add(row);
    }

    private void CloseTable(int lineNo)
    {
        if (_table!.Rows.Count > 0) _doc.Blocks.Add(_table);
        else Warn("empty tabular environment", lineNo);
        _table = null;
    }

    /// <summary>Splits a row on unescaped '&' (\& stays in the cell for the inline parser).</summary>
    private static List<string> SplitCells(string s)
    {
        var cells = new List<string>();
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == MarkupConstants.Delimiters.SyntaxStart && i + 1 < s.Length) { sb.Append(c).Append(s[i + 1]); i++; continue; }
            if (c == MarkupConstants.Delimiters.CellSeparator) { cells.Add(sb.ToString()); sb.Clear(); continue; }
            sb.Append(c);
        }
        cells.Add(sb.ToString());
        return cells;
    }

    // ---------- figure ----------

    private void FigureLine(string t, int lineNo)
    {
        if (t == MarkupConstants.Tokens.EndFigure) { EmitFigure(lineNo); return; }
        if (t.Length == 0) return;
        if (t == MarkupConstants.Tokens.Centering) { _figure!.Centered = true; return; }

        if (TryIncludeGraphics(t, out var path))
        {
            if (_figure!.Path.Length > 0) Warn("second \\includegraphics in the same figure is ignored", lineNo);
            else _figure.Path = path;
            return;
        }

        if (TryCaption(t, out var cap, out var capLabels))
        {
            if (_figureCaptioned) Warn("second \\caption in the same figure is ignored", lineNo);
            else
            {
                _figureCaptioned = true;
                _figure!.FigureNumber = ++_figCounter;
                _figures.Add(_figure);
                ParseInlines(cap.Trim(), _figure.Caption, lineNo);
            }
            foreach (var key in capLabels) BindLabel(key, CurrentLabelTarget(), lineNo);
            return;
        }

        if (TrailingLabels(t, 0) is { Count: > 0 } labels)
        {
            foreach (var key in labels) BindLabel(key, CurrentLabelTarget(), lineNo);
            return;
        }

        Warn($"line ignored inside figure: {t}", lineNo);
    }

    private static bool TryCaption(string t, out string arg, out List<string> labels)
    {
        arg = "";
        labels = new List<string>();
        if (!t.StartsWith(MarkupConstants.Tokens.CaptionOpen, StringComparison.Ordinal)) return false;
        int open = MarkupConstants.Tokens.Caption.Length;
        int close = MarkupHelper.MatchBrace(t, open, t.Length);
        if (close < 0) return false;
        var trailing = TrailingLabels(t, close + 1);
        if (trailing is null) return false;
        arg = t[(open + 1)..close];
        labels = trailing;
        return true;
    }

    private void EmitFigure(int lineNo)
    {
        var f = _figure!;
        _figure = null;
        _lastItem = null;
        if (f.Path.Length == 0 && f.Caption.Count == 0)
        {
            Warn("empty figure environment", lineNo);
            if (f.FigureNumber > 0) _figures.Remove(f);
            return;
        }
        _doc.Blocks.Add(f);
    }

    private static bool TryIncludeGraphics(string t, out string path)
    {
        path = "";
        const string cmd = MarkupConstants.Tokens.IncludeGraphics;
        if (!t.StartsWith(cmd, StringComparison.Ordinal)) return false;
        int pos = cmd.Length;
        if (pos < t.Length && t[pos] == MarkupConstants.Delimiters.OptionalArgOpen)
        {
            int rb = t.IndexOf(MarkupConstants.Delimiters.OptionalArgClose, pos);
            if (rb < 0) return false;
            pos = rb + 1;                       // size options are accepted and ignored
        }
        while (pos < t.Length && char.IsWhiteSpace(t[pos])) pos++;
        if (pos >= t.Length || t[pos] != MarkupConstants.Delimiters.GroupOpen) return false;
        int close = MarkupHelper.MatchBrace(t, pos, t.Length);
        if (close < 0 || t[(close + 1)..].Trim().Length != 0) return false;
        path = t[(pos + 1)..close].Trim();
        return path.Length > 0;
    }

    // ---------- labels & references ----------

    /// <summary>
    /// Scans from <paramref name="from"/>: returns the keys if the remainder is only whitespace
    /// and \label{…} tokens (possibly none); null if anything else is present.
    /// </summary>
    private static List<string>? TrailingLabels(string t, int from)
    {
        var labels = new List<string>();
        int q = from;
        while (true)
        {
            while (q < t.Length && char.IsWhiteSpace(t[q])) q++;
            if (q >= t.Length) return labels;
            if (!t.AsSpan(q).StartsWith(MarkupConstants.Tokens.Label, StringComparison.Ordinal)) return null;
            int lb = q + MarkupConstants.Tokens.Label.Length;
            if (lb >= t.Length || t[lb] != MarkupConstants.Delimiters.GroupOpen) return null;
            int lc = MarkupHelper.MatchBrace(t, lb, t.Length);
            if (lc < 0) return null;
            labels.Add(t[(lb + 1)..lc].Trim());
            q = lc + 1;
        }
    }

    private string CurrentLabelTarget()
        => _figure != null && _figureCaptioned
            ? _figure.FigureNumber.ToString(CultureInfo.InvariantCulture)
            : _anchor;

    private void BindLabel(string key, string value, int lineNo)
    {
        if (key.Length == 0) { Warn("\\label needs a name, e.g. \\label{sec:intro}", lineNo); return; }
        if (!_labels.TryAdd(key, value))
            Warn($"duplicate label '{key}' — the first definition wins", lineNo);
    }

    // ---------- end of document ----------

    private void Finish(int lastLine)
    {
        if (_verbatim) Warn("\\begin{verbatim} is never closed", lastLine);
        if (_table != null) { Warn("\\begin{tabular} is never closed", lastLine); CloseTable(lastLine); }
        if (_figure != null) { Warn("\\begin{figure} is never closed", lastLine); EmitFigure(lastLine); }
        if (_lists.Count > 0) Warn("a list environment is never closed", lastLine);
        if (_quote > 0) Warn("\\begin{quote} is never closed", lastLine);
        if (_center > 0) Warn("\\begin{center} is never closed", lastLine);
        ResolveRefs();
        FillTocAndLof();
    }

    private void ResolveRefs()
    {
        foreach (var list in AllInlineLists())
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is not RefRun r) continue;
                if (_labels.TryGetValue(r.Key, out var v) && v.Length > 0)
                {
                    list[i] = new TextRun(v, r.Bold, r.Italic, r.Underline, r.Mono);
                }
                else
                {
                    Warn(_labels.ContainsKey(r.Key)
                        ? $"label '{r.Key}' is not attached to a numbered section or figure"
                        : $"undefined reference '{r.Key}'", r.Line);
                    list[i] = new TextRun("?" + r.Key + "?", r.Bold, r.Italic, r.Underline, r.Mono);
                }
            }
    }

    private IEnumerable<List<Inline>> AllInlineLists()
    {
        foreach (var b in _doc.Blocks)
        {
            if (b is ParagraphBlock p) { yield return p.Term; yield return p.Inlines; }
            else if (b is TableBlock t)
                foreach (var row in t.Rows)
                    foreach (var cell in row.Cells) yield return cell;
            else if (b is ImageBlock img) yield return img.Caption;
        }
    }

    private void FillTocAndLof()
    {
        List<TableOfContentsEntry>? toc = null;
        List<FigureEntry>? lof = null;
        foreach (var b in _doc.Blocks)
        {
            if (b is TableOfContentsBlock tb)
            {
                toc ??= _headings
                    .Select(h => new TableOfContentsEntry(h.Level, h.P.HeadingNumber, MarkupHelper.Flatten(h.P.Inlines)))
                    .ToList();
                tb.Entries.AddRange(toc);
            }
            else if (b is ListOfFiguresBlock lb)
            {
                lof ??= _figures
                    .Select(f => new FigureEntry(f.FigureNumber, MarkupHelper.Flatten(f.Caption)))
                    .ToList();
                lb.Entries.AddRange(lof);
            }
        }
    }

    private void Warn(string msg, int line)
        => _doc.Warnings.Add(new Diagnostic(
            Code: DiagnosticCode.MarkupWarning,
            Severity: DiagnosticSeverity.Warning,
            Message: msg,
            Line: line,
            Col: 1
        ));

    // ---------- inline parsing ----------

    private void ParseInlines(string s, List<Inline> output, int lineNo)
        => Walk(s, 0, s.Length, bold: false, italic: false, underline: false, mono: false, sc: false, output, lineNo, depth: 0);

    // Bound nesting depth. \textbf{\textbf{…}} nested tens of thousands deep (from a variable value or
    // pasted content) recursed one native frame per level and overflowed the stack — an uncatchable
    // crash, contrary to this parser's "markup never fails a document" contract. Past the limit, keep
    // the remainder as literal text (with the current run's formatting) and warn once.
    private const int MaxInlineDepth = 64;

    private void Walk(string s, int start, int end, bool bold, bool italic, bool underline, bool mono, bool sc,
        List<Inline> output, int lineNo, int depth)
    {
        if (depth > MaxInlineDepth)
        {
            Warn($"formatting nested deeper than {MaxInlineDepth} levels — kept as plain text", lineNo);
            if (end > start) output.Add(new TextRun(s[start..end], bold, italic, underline, mono, sc));
            return;
        }
        var sb = new StringBuilder();
        void Flush()
        {
            if (sb.Length > 0)
            {
                output.Add(new TextRun(sb.ToString(), bold, italic, underline, mono, sc));
                sb.Clear();
            }
        }

        int p = start;
        while (p < end)
        {
            char c = s[p];
            if (c != MarkupConstants.Delimiters.SyntaxStart) { sb.Append(c); p++; continue; }
            if (p + 1 >= end) { sb.Append(MarkupConstants.Delimiters.SyntaxStart); p++; continue; }
            char n = s[p + 1];
            if (n == MarkupConstants.Delimiters.SyntaxStart) { Flush(); output.Add(new LineBreak()); p += 2; continue; }
            if (n is MarkupConstants.Delimiters.Comment or MarkupConstants.Delimiters.GroupOpen or MarkupConstants.Delimiters.GroupClose
                or MarkupConstants.Delimiters.CellSeparator or MarkupConstants.EscapableChars.Hash or MarkupConstants.EscapableChars.Dollar or MarkupConstants.EscapableChars.Underscore)
            { sb.Append(n); p += 2; continue; }
            if (n is ' ' or ',') { sb.Append(' '); p += 2; continue; }   // \␣ and \, — explicit spaces
            if (!char.IsLetter(n)) { sb.Append(MarkupConstants.Delimiters.SyntaxStart).Append(n); p += 2; continue; }

            int cs = p + 1;
            int ce = cs;
            while (ce < end && char.IsLetter(s[ce])) ce++;
            string cmd = s[cs..ce];
            switch (cmd)
            {
                case MarkupConstants.Commands.NewLine:
                    Flush(); output.Add(new LineBreak()); p = ce; continue;
                case MarkupConstants.Commands.TextBackslash:
                    sb.Append(MarkupConstants.Delimiters.SyntaxStart); p = ce; continue;
                case MarkupConstants.Commands.Today:
                    sb.Append(_today.ToString(MarkupConstants.Formats.DateFormat, _culture));
                    p = ce; continue;
                case MarkupConstants.Commands.Ldots:
                case MarkupConstants.Commands.Dots:
                    sb.Append('…'); p = ce; continue;
                case MarkupConstants.Commands.LaTeX:
                    sb.Append("LaTeX"); p = ce; continue;
                case MarkupConstants.Commands.TeX:
                    sb.Append("TeX"); p = ce; continue;
                case MarkupConstants.Commands.Quad:
                    sb.Append(' '); p = ce; continue;
                case MarkupConstants.Commands.QQuad:
                    sb.Append(' ').Append(' '); p = ce; continue;
                case MarkupConstants.Commands.NoIndent:
                    p = ce; continue;   // accepted, no effect (paragraphs are not indented)
                case MarkupConstants.Commands.BigSkip:
                case MarkupConstants.Commands.MedSkip:
                case MarkupConstants.Commands.SmallSkip:
                    Flush(); output.Add(new LineBreak()); p = ce; continue;
                case MarkupConstants.Commands.VSpace:
                case MarkupConstants.Commands.HSpace:
                {
                    // \vspace*{1cm} → approximated: vertical = line break, horizontal = em space
                    int q2 = ce;
                    if (q2 < end && s[q2] == MarkupConstants.Delimiters.StarredSuffix) q2++;
                    if (q2 < end && s[q2] == MarkupConstants.Delimiters.GroupOpen)
                    {
                        int close = MarkupHelper.MatchBrace(s, q2, end);
                        if (close > q2) q2 = close + 1;
                    }
                    if (cmd == MarkupConstants.Commands.VSpace) { Flush(); output.Add(new LineBreak()); }
                    else sb.Append(' ');
                    p = q2;
                    continue;
                }
                case MarkupConstants.Commands.TextBold:
                case MarkupConstants.Commands.TextItalic:
                case MarkupConstants.Commands.Emph:
                case MarkupConstants.Commands.TextSlanted:
                case MarkupConstants.Commands.TextSmallCaps:
                case MarkupConstants.Commands.Underline:
                case MarkupConstants.Commands.TextTypewriter:
                {
                    if (ce < end && s[ce] == MarkupConstants.Delimiters.GroupOpen)
                    {
                        int close = MarkupHelper.MatchBrace(s, ce, end);
                        if (close > ce)
                        {
                            Flush();
                            Walk(s, ce + 1, close,
                                bold || cmd == MarkupConstants.Commands.TextBold,
                                italic || cmd is MarkupConstants.Commands.TextItalic or MarkupConstants.Commands.Emph or MarkupConstants.Commands.TextSlanted,
                                underline || cmd == MarkupConstants.Commands.Underline,
                                mono || cmd == MarkupConstants.Commands.TextTypewriter,
                                sc || cmd == MarkupConstants.Commands.TextSmallCaps,
                                output, lineNo, depth + 1);
                            p = close + 1;
                            continue;
                        }
                    }
                    Warn($"\\{cmd} expects an argument in braces", lineNo);
                    sb.Append(MarkupConstants.Delimiters.SyntaxStart).Append(cmd);
                    p = ce;
                    continue;
                }
                case MarkupConstants.Commands.Ref:
                {
                    if (ce < end && s[ce] == MarkupConstants.Delimiters.GroupOpen)
                    {
                        int close = MarkupHelper.MatchBrace(s, ce, end);
                        if (close > ce)
                        {
                            Flush();
                            output.Add(new RefRun(s[(ce + 1)..close].Trim(), lineNo, bold, italic, underline, mono));
                            p = close + 1;
                            continue;
                        }
                    }
                    Warn("\\ref expects a label name in braces", lineNo);
                    sb.Append(MarkupConstants.Tokens.Ref);
                    p = ce;
                    continue;
                }
                case MarkupConstants.Commands.Label:
                {
                    if (ce < end && s[ce] == MarkupConstants.Delimiters.GroupOpen)
                    {
                        int close = MarkupHelper.MatchBrace(s, ce, end);
                        if (close > ce)
                        {
                            BindLabel(s[(ce + 1)..close].Trim(), CurrentLabelTarget(), lineNo);
                            p = close + 1;   // emits nothing
                            continue;
                        }
                    }
                    Warn("\\label expects a name in braces", lineNo);
                    sb.Append(MarkupConstants.Tokens.Label);
                    p = ce;
                    continue;
                }
                default:
                    Warn(LineOnlyCmds.Contains(cmd)
                        ? $"\\{cmd} must start its own line (kept as text)"
                        : $"unknown markup command \\{cmd} (kept as text)", lineNo);
                    sb.Append(MarkupConstants.Delimiters.SyntaxStart).Append(cmd);
                    p = ce;
                    continue;
            }
        }
        Flush();
    }

    private static readonly HashSet<string> LineOnlyCmds = new(StringComparer.Ordinal)
    {
        MarkupConstants.Commands.Section, MarkupConstants.Commands.Subsection, MarkupConstants.Commands.Subsubsection,
        MarkupConstants.Commands.Paragraph, MarkupConstants.Commands.Subparagraph,
        MarkupConstants.Commands.Item, MarkupConstants.Commands.Caption, MarkupConstants.Commands.IncludeGraphics,
        MarkupConstants.Commands.Begin, MarkupConstants.Commands.End, MarkupConstants.Commands.Input,
        MarkupConstants.Commands.TableOfContents, MarkupConstants.Commands.ListOfFigures,
        MarkupConstants.Commands.HRule, MarkupConstants.Commands.HLine,
        MarkupConstants.Commands.PageBreak, MarkupConstants.Commands.NewPage, MarkupConstants.Commands.ClearPage,
        MarkupConstants.Commands.Centering,
        MarkupConstants.Commands.Title, MarkupConstants.Commands.Author, MarkupConstants.Commands.Date,
        MarkupConstants.Commands.MakeTitle
    };
}