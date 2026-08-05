// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;
using SimplyDraft.Core.Domains.Markup.Inlines;
using SimplyDraft.Engine.Constants;
using SimplyDraft.Engine.Utils;

namespace SimplyDraft.Engine.Markup;

internal sealed class InlineParser
{
    private const int MaxInlineDepth = 64; // Bound nesting depth to prevent overflowing the stack crash

    private readonly MarkupParser _host;

    public InlineParser(MarkupParser host)
        => _host = host ?? throw new ArgumentNullException(nameof(host));

    public void Parse(string source, List<Inline> output, int lineNumber)
        => ParseRun(source, 0, source.Length, InlineStyle.None, output, lineNumber, depth: 0);

    private void ParseRun(
        string source, int start, int end, InlineStyle style, List<Inline> output, int lineNumber, int depth
    )
    {
        if (depth > MaxInlineDepth)
        {
            _host.Warn($"formatting nested deeper than {MaxInlineDepth} levels — kept as plain text", lineNumber);
            if (end > start) output.Add(MakeTextRun(source[start..end], style));
            return;
        }

        var pendingText = new StringBuilder();

        void Flush()
        {
            if (pendingText.Length > 0)
            {
                output.Add(MakeTextRun(pendingText.ToString(), style));
                pendingText.Clear();
            }
        }

        int position = start;

        while (position < end)
        {
            char current = source[position];

            if (current != MarkupConstants.Delimiters.SyntaxStart)
            {
                pendingText.Append(current);
                position++;
                continue;
            }

            if (position + 1 >= end)
            {
                pendingText.Append(MarkupConstants.Delimiters.SyntaxStart);
                position++;
                continue;
            }

            char next = source[position + 1];

            if (next == MarkupConstants.Delimiters.SyntaxStart) // '\\' — soft line break
            {
                Flush();
                output.Add(new LineBreak());
                position += 2;
                continue;
            }

            if (next is MarkupConstants.Delimiters.Comment
                or MarkupConstants.Delimiters.GroupOpen
                or MarkupConstants.Delimiters.GroupClose
                or MarkupConstants.Delimiters.CellSeparator
                or MarkupConstants.EscapableChars.Hash
                or MarkupConstants.EscapableChars.Dollar
                or MarkupConstants.EscapableChars.Underscore
            )
            {
                pendingText.Append(next); // '\% \{ \} \& \# \$ \_' — unescape
                position += 2;
                continue;
            }

            if (next is ' ' or ',')
            {
                pendingText.Append(' ');
                position += 2;
                continue;
            }

            if (!char.IsLetter(next)) // unknown escape: keep both characters
            {
                pendingText.Append(MarkupConstants.Delimiters.SyntaxStart).Append(next);
                position += 2;
                continue;
            }

            // A \command — read its letters and dispatch.
            int commandStart = position + 1;
            int commandEnd = commandStart;

            while (commandEnd < end && char.IsLetter(source[commandEnd])) commandEnd++;

            string command = source[commandStart..commandEnd];

            position = DispatchCommand(
                source, end, command, position, commandEnd, style, output, pendingText, Flush, lineNumber, depth
            );
        }
        Flush();
    }

    private int DispatchCommand(
        string source, int end, string command, int commandPosition, int commandEnd,InlineStyle style,
        List<Inline> output, StringBuilder pendingText, Action flush, int lineNumber, int depth
    )
    {
        switch (command)
        {
            case MarkupConstants.Commands.NewLine:
                flush();
                output.Add(new LineBreak());
                return commandEnd;

            case MarkupConstants.Commands.TextBackslash:
                pendingText.Append(MarkupConstants.Delimiters.SyntaxStart);
                return commandEnd;

            case MarkupConstants.Commands.Today:
                pendingText.Append(_host.Today.ToString(MarkupConstants.Formats.DateFormat, _host.Culture));
                return commandEnd;

            case MarkupConstants.Commands.Ldots:
            case MarkupConstants.Commands.Dots:
                pendingText.Append('…');
                return commandEnd;

            case MarkupConstants.Commands.LaTeX:
                pendingText.Append("LaTeX");
                return commandEnd;

            case MarkupConstants.Commands.TeX:
                pendingText.Append("TeX");
                return commandEnd;

            case MarkupConstants.Commands.Quad:
                pendingText.Append('\u2003'); // em space
                return commandEnd;

            case MarkupConstants.Commands.QQuad:
                pendingText.Append('\u2003').Append('\u2003');
                return commandEnd;

            case MarkupConstants.Commands.NoIndent:
                return commandEnd; // accepted, no effect (paragraphs are not indented)

            case MarkupConstants.Commands.BigSkip:
            case MarkupConstants.Commands.MedSkip:
            case MarkupConstants.Commands.SmallSkip:
                flush();
                output.Add(new LineBreak());
                return commandEnd;

            case MarkupConstants.Commands.VSpace:
            case MarkupConstants.Commands.HSpace:
                return ConsumeSpaceCommand(source, end, command, commandEnd, output, pendingText, flush);

            case MarkupConstants.Commands.TextBold:
            case MarkupConstants.Commands.TextItalic:
            case MarkupConstants.Commands.Emph:
            case MarkupConstants.Commands.TextSlanted:
            case MarkupConstants.Commands.TextSmallCaps:
            case MarkupConstants.Commands.Underline:
            case MarkupConstants.Commands.TextTypewriter:
                return ConsumeStyleCommand(
                    source, end, command, commandEnd, style, output, pendingText, flush, lineNumber, depth
                );

            case MarkupConstants.Commands.Ref:
                return ConsumeRefCommand(source, end, commandEnd, style, output, pendingText, flush, lineNumber);

            case MarkupConstants.Commands.Label:
                return ConsumeLabelCommand(source, end, commandEnd, pendingText, lineNumber);
            
            default:
                _host.Warn(LineOnlyCommands.Contains(command)
                    ? $"\\{command} must start its own line (kept as text)"
                    : $"unknown markup command \\{command} (kept as text)", lineNumber);
                pendingText.Append(MarkupConstants.Delimiters.SyntaxStart).Append(command);
                return commandEnd;
        }
    }

    private static int ConsumeSpaceCommand(
        string source, int end, string command, int commandEnd,
        List<Inline> output, StringBuilder pendingText, Action flush
    )
    {
        int position = commandEnd;

        if (position < end && source[position] == MarkupConstants.Delimiters.StarredSuffix) position++;
        if (position < end && source[position] == MarkupConstants.Delimiters.GroupOpen)
        {
            int close = MarkupHelper.MatchBrace(source, position, end);
            if (close > position) position = close + 1;
        }

        if (command == MarkupConstants.Commands.VSpace)
        {
            flush();
            output.Add(new LineBreak());
        }
        else
        {
            pendingText.Append('\u2003'); // em space
        }
        return position;
    }

    private int ConsumeStyleCommand(
        string source, int end, string command, int commandEnd, InlineStyle style,
        List<Inline> output, StringBuilder pendingText, Action flush, int lineNumber, int depth
    )
    {
        if (commandEnd < end && source[commandEnd] == MarkupConstants.Delimiters.GroupOpen)
        {
            int close = MarkupHelper.MatchBrace(source, commandEnd, end);

            if (close > commandEnd)
            {
                flush();

                ParseRun(
                    source, commandEnd + 1, close, ApplyStyleCommand(style, command),
                    output, lineNumber, depth + 1
                );

                return close + 1;
            }
        }
        _host.Warn($"\\{command} expects an argument in braces", lineNumber);
        pendingText.Append(MarkupConstants.Delimiters.SyntaxStart).Append(command);
        return commandEnd;
    }

    private static InlineStyle ApplyStyleCommand(InlineStyle style, string command)
        => command switch
        {
            MarkupConstants.Commands.TextBold => style with {Bold = true},
            MarkupConstants.Commands.TextItalic or MarkupConstants.Commands.Emph
                or MarkupConstants.Commands.TextSlanted => style with {Italic = true},
            MarkupConstants.Commands.Underline => style with {Underline = true},
            MarkupConstants.Commands.TextTypewriter => style with {Mono = true},
            MarkupConstants.Commands.TextSmallCaps => style with {SmallCaps = true},
            _ => style
        };

    private int ConsumeRefCommand(
        string source, int end, int commandEnd, InlineStyle style,
        List<Inline> output, StringBuilder pendingText, Action flush, int lineNumber
    )
    {
        if (commandEnd < end && source[commandEnd] == MarkupConstants.Delimiters.GroupOpen)
        {
            int close = MarkupHelper.MatchBrace(source, commandEnd, end);
            if (close > commandEnd)
            {
                flush();
                output.Add(
                    new RefRun(
                        source[(commandEnd + 1)..close].Trim(), lineNumber,
                        style.Bold, style.Italic, style.Underline, style.Mono
                ));
                return close + 1;
            }
        }
        _host.Warn("\\ref expects a label name in braces", lineNumber);
        pendingText.Append(MarkupConstants.Tokens.Ref);
        return commandEnd;
    }

    private int ConsumeLabelCommand(
        string source, int end, int commandEnd, StringBuilder pendingText, int lineNumber
    )
    {
        if (commandEnd < end && source[commandEnd] == MarkupConstants.Delimiters.GroupOpen)
        {
            int close = MarkupHelper.MatchBrace(source, commandEnd, end);
            if (close > commandEnd)
            {
                _host.BindLabel(source[(commandEnd + 1)..close].Trim(), _host.CurrentLabelTarget, lineNumber);
                return close + 1;
            }
        }

        _host.Warn("\\label expects a name in braces", lineNumber);
        pendingText.Append(MarkupConstants.Tokens.Label);
        return commandEnd;
    }

    private static TextRun MakeTextRun(string text, InlineStyle style)
        => new(text, style.Bold, style.Italic, style.Underline, style.Mono, style.SmallCaps);

    private static readonly HashSet<string> LineOnlyCommands = new(StringComparer.Ordinal)
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