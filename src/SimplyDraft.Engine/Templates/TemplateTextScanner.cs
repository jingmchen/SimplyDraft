// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;
using System.Text.RegularExpressions;
using SimplyDraft.Core.Domains.Document.Segments;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Exceptions;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Templates;

internal sealed class TemplateTextScanner
{
    private readonly string _text;
    private readonly List<Segment> _segments;
    private readonly StringBuilder _pendingLiteral = new();

    private int _position;
    private int _line;
    private int _column = 1;
    private int _literalStartLine;
    private int _literalStartColumn = 1;

    // Markup-argument tracking (see the class summary).
    private int _commandArgumentDepth;
    private bool _justClosedCommandArgument;

    public TemplateTextScanner(string text, int startLine, List<Segment> segments)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _segments = segments ?? throw new ArgumentNullException(nameof(segments));
        _line = startLine;
        _literalStartLine = startLine;
    }

    public void Scan()
    {
        while (_position < _text.Length)
        {
            char current = _text[_position];

            if (current == ScriptingConstants.Template.PlaceholderOpen)
            {
                ScanOpenBrace();
                continue;
            }

            if (current == ScriptingConstants.Template.PlaceholderClose)
            {
                ScanCloseBrace();
                continue;
            }

            AppendLiteral(current);
        }
        FlushLiteral();
    }

    private void ScanOpenBrace()
    {
        // literal escape {{
        if (NextCharIs(ScriptingConstants.Template.PlaceholderOpen))
        {
            _pendingLiteral.Append(ScriptingConstants.Template.PlaceholderOpen);
            _position += 2;
            _column += 2;
            _justClosedCommandArgument = false;
            return;
        }

        // literal command-argument brace (see the class summary)
        if (_justClosedCommandArgument || PendingLiteralEndsWithCommand())
        {
            _pendingLiteral.Append(ScriptingConstants.Template.PlaceholderOpen);
            _commandArgumentDepth++;
            _justClosedCommandArgument = false;
            _position++;
            _column++;
            return;
        }

        int segmentLine = _line, segmentColumn = _column;

        if (NextCharIs(ScriptingConstants.Template.ExpressionMarker))
        {
            ScanInlineExpression(segmentLine, segmentColumn);
            return;
        }

        ScanPlaceholder(segmentLine, segmentColumn);
    }

    private void ScanInlineExpression(int segmentLine, int segmentColumn)
    {
        FlushLiteral();
        int scanIndex = _position + 2; // past "{="
        int braceDepth = 1;
        char openQuote = '\0'; // '\0' = outside a string; otherwise the open quote char
        var expressionSource = new StringBuilder();

        while (scanIndex < _text.Length)
        {
            char current = _text[scanIndex];

            if (openQuote != '\0')
            {
                if (current == ScriptingConstants.Lexical.StringEscape && scanIndex + 1 < _text.Length)
                {
                    expressionSource.Append(current).Append(_text[scanIndex + 1]);
                    scanIndex += 2;
                    continue;
                }

                if (current == openQuote)
                    openQuote = '\0';
                
                expressionSource.Append(current);
                scanIndex++;
                continue;
            }
            if (current == ScriptingConstants.Lexical.DoubleQuote || current == ScriptingConstants.Lexical.SingleQuote)
            {
                openQuote = current;
                expressionSource.Append(current);
                scanIndex++;
                continue;
            }

            if (current == ScriptingConstants.Template.PlaceholderOpen)
            {
                braceDepth++;
                expressionSource.Append(current);
                scanIndex++;
                continue;
            }

            if (current == ScriptingConstants.Template.PlaceholderClose)
            {
                braceDepth--;
                if (braceDepth == 0) break;
                expressionSource.Append(current);
                scanIndex++;
                continue;
            }

            expressionSource.Append(current);
            scanIndex++;
        }
        if (braceDepth != 0)
            throw ScriptException.Error(
                DiagnosticCode.SyntaxError, "unterminated {= expression", segmentLine, segmentColumn);

        _segments.Add(new InlineExpressionSegment(expressionSource.ToString(), segmentLine, segmentColumn));
        AdvanceThrough(scanIndex); // consume "{=", the expression, and the closing '}'
        AnchorLiteralStartHere();
        _justClosedCommandArgument = false;
    }

    private void ScanPlaceholder(int segmentLine, int segmentColumn)
    {
        int nameScan = _position + 1;
        
        if (nameScan >= _text.Length || !(char.IsLetter(_text[nameScan]) || _text[nameScan] == '_'))
            throw ScriptException.Error(
                DiagnosticCode.SyntaxError,
                "invalid placeholder — names start with a letter or '_' (write '{{' for a literal brace)",
                segmentLine, segmentColumn);

        int nameStart = nameScan;
        nameScan++;

        while (nameScan < _text.Length && (char.IsLetterOrDigit(_text[nameScan]) || _text[nameScan] == '_'))
            nameScan++;
        
        string firstPart = _text[nameStart..nameScan];
        string? member = null;

        if (nameScan < _text.Length && _text[nameScan] == ScriptingConstants.Template.MemberSeparator)
        {
            nameScan++;
            int memberStart = nameScan;

            while (nameScan < _text.Length && (char.IsLetterOrDigit(_text[nameScan]) || _text[nameScan] == '_'))
                nameScan++;
            
            member = _text[memberStart..nameScan];
        }

        if (nameScan >= _text.Length || _text[nameScan] != ScriptingConstants.Template.PlaceholderClose)
            throw ScriptException.Error(
                DiagnosticCode.SyntaxError,
                $"unterminated placeholder '{{{firstPart}…' — expected '}}'",
                segmentLine, segmentColumn);

        FlushLiteral();

        if (member != null)
        {
            bool isValidBuiltin = member.Length > 0 &&
                (firstPart.Equals(ScriptingConstants.Builtins.System, StringComparison.OrdinalIgnoreCase) ||
                 firstPart.Equals(ScriptingConstants.Builtins.Doc, StringComparison.OrdinalIgnoreCase));
            
            if (!isValidBuiltin)
                throw ScriptException.Error(
                    DiagnosticCode.SyntaxError,
                    $"invalid placeholder {{{firstPart}.{member}}} — dotted names are reserved for system.* and doc.*",
                    segmentLine, segmentColumn);
            
            _segments.Add(new PlaceholderSegment(firstPart.ToLowerInvariant(), member.ToLowerInvariant(), segmentLine, segmentColumn));
        }
        else
        {
            _segments.Add(new PlaceholderSegment(firstPart, segmentLine, segmentColumn));
        }

        int consumed = (nameScan - _position) + 1; // includes both braces; placeholders never span lines
        _column += consumed;
        _position = nameScan + 1;
        AnchorLiteralStartHere();
        _justClosedCommandArgument = false;
    }

    private void ScanCloseBrace()
    {
        // literal escape }}
        if (NextCharIs(ScriptingConstants.Template.PlaceholderClose))
        {
            _pendingLiteral.Append(ScriptingConstants.Template.PlaceholderClose);
            _position += 2;
            _column += 2;
            _justClosedCommandArgument = false;
            return;
        }

        // a lone '}' is literal text
        _pendingLiteral.Append(ScriptingConstants.Template.PlaceholderClose);
        _position++;
        _column++;

        if (_commandArgumentDepth > 0)
        {
            _commandArgumentDepth--;
            _justClosedCommandArgument = true;
        }
        else
        {
            _justClosedCommandArgument = false;
        }
    }

    private bool NextCharIs(char expected)
        => _position + 1 < _text.Length && _text[_position + 1] == expected;

    private void AppendLiteral(char current)
    {
        _pendingLiteral.Append(current);

        if (current == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        _position++;
        _justClosedCommandArgument = false;
    }

    private void FlushLiteral()
    {
        if (_pendingLiteral.Length > 0)
        {
            _segments.Add(new LiteralSegment(_pendingLiteral.ToString(), _literalStartLine, _literalStartColumn));
            _pendingLiteral.Clear();
        }
        AnchorLiteralStartHere();
    }

    private void AnchorLiteralStartHere()
    {
        _literalStartLine = _line;
        _literalStartColumn = _column;
    }

    private void AdvanceThrough(int lastIndex)
    {
        for (int index = _position; index <= lastIndex; index++)
        {
            if (_text[index] == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
        }
        _position = lastIndex + 1;
    }

    private bool PendingLiteralEndsWithCommand()
    {
        if (_pendingLiteral.Length == 0)
            return false;
        
        int tailLength = Math.Min(_pendingLiteral.Length, 96);
        
        return CommandTail.IsMatch(_pendingLiteral.ToString(_pendingLiteral.Length - tailLength, tailLength));
    }

    private static readonly Regex CommandTail =
        new(@"\\[A-Za-z]+\*?(\[[^\[\]\n]*\])?$", RegexOptions.Compiled);
}