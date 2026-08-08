// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;
using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Exceptions;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Scripting;

public sealed class Lexer
{
    private readonly string _source;
    private readonly bool _scriptMode;
    private int _position;
    private int _line;
    private int _column;
    private int _openGroupDepth;
    private bool _atLineStart;
    private readonly List<int> _indentWidths = [0];
    private static readonly Dictionary<string, TokenKind> Keywords = new(StringComparer.Ordinal)
    {
        [ScriptingConstants.Keywords.If] = TokenKind.If,
        [ScriptingConstants.Keywords.Elif] = TokenKind.ElseIf,
        [ScriptingConstants.Keywords.Else] = TokenKind.Else,
        [ScriptingConstants.Keywords.And] = TokenKind.And,
        [ScriptingConstants.Keywords.Or] = TokenKind.Or,
        [ScriptingConstants.Keywords.Not] = TokenKind.Not,
        [ScriptingConstants.Keywords.In] = TokenKind.In,
        [ScriptingConstants.Keywords.True] = TokenKind.True,
        [ScriptingConstants.Keywords.False] = TokenKind.False,
    };

    private Lexer(string source, bool scriptMode, int startLine, int startColumn)
    {
        _source = (source ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        _scriptMode = scriptMode;
        _line = startLine;
        _column = startColumn;
        _atLineStart = scriptMode;
    }

    public static List<Token> LexScript(string source, int startLine = 1)
        => new Lexer(source, scriptMode: true, startLine, 1).Tokenize();

    public static List<Token> LexExpression(string source, int startLine = 1, int startColumn = 1)
        => new Lexer(source, scriptMode: false, startLine, startColumn).Tokenize();

    private List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (true)
        {
            if (_scriptMode && _atLineStart && _openGroupDepth == 0)
            {
                if (!StartLogicalLine(tokens))
                    break; // EOF
                
                continue;
            }

            SkipSpacesAndComment();

            if (_position >= _source.Length)
                break;

            char current = _source[_position];
            int line = _line, column = _column;

            if (current == '\n')
            {
                Advance();
                
                if (_openGroupDepth > 0)
                    continue; // implicit line joining inside (…) / […]
                
                if (_scriptMode)
                {
                    tokens.Add(new Token(TokenKind.NewLine, "\n", 0, line, column));
                    _atLineStart = true;
                }

                continue; // expression mode: a newline is just whitespace
            }
            if (current == ScriptingConstants.Lexical.DoubleQuote || current == ScriptingConstants.Lexical.SingleQuote)
            {
                tokens.Add(ReadString());
                continue;
            }

            if (char.IsAsciiDigit(current))
            {
                tokens.Add(ReadNumber());
                continue;
            }

            if (current == '_' || char.IsLetter(current)) 
            {
                tokens.Add(ReadIdentifierOrKeyword());
                continue;
            }

            tokens.Add(ReadSymbol());
        }

        if (_scriptMode)
        {
            if (!_atLineStart)
                tokens.Add(new Token(TokenKind.NewLine, "\n", 0, _line, _column));
            
            while (_indentWidths[^1] > 0)
            {
                _indentWidths.RemoveAt(_indentWidths.Count - 1);
                tokens.Add(new Token(TokenKind.Dedent, "", 0, _line, _column));
            }
        }
        tokens.Add(new Token(TokenKind.EndOfLine, "", 0, _line, _column));
        return tokens;
    }

    private bool StartLogicalLine(List<Token> tokens)
    {
        while (true)
        {
            if (_position >= _source.Length)
                return false;

            int scan = _position;
            bool sawTab = false;

            while (scan < _source.Length && (_source[scan] == ' ' || _source[scan] == '\t'))
            {
                if (_source[scan] == '\t')
                    sawTab = true;
                
                scan++;
            }
            bool isBlankOrComment = scan >= _source.Length || _source[scan] == '\n' || _source[scan] == ScriptingConstants.Lexical.Comment;
            
            if (isBlankOrComment)
            {
                while (_position < _source.Length && _source[_position] != '\n')
                    Advance();
                
                if (_position < _source.Length)
                    Advance(); // consume the newline
                
                continue;
            }
            if (sawTab)
                throw ScriptException.Error(
                    DiagnosticCode.SyntaxError,
                    "tab characters are not allowed in indentation — use spaces",
                    _line, 1);

            int indentWidth = scan - _position;

            while (_position < scan)
                Advance();
            
            EmitIndentOrDedents(tokens, indentWidth);
            _atLineStart = false;
            return true;
        }
    }

    private void EmitIndentOrDedents(List<Token> tokens, int indentWidth)
    {
        if (indentWidth > _indentWidths[^1])
        {
            _indentWidths.Add(indentWidth);
            tokens.Add(new Token(TokenKind.Indent, "", 0, _line, _column));
            return;
        }
        while (indentWidth < _indentWidths[^1])
        {
            _indentWidths.RemoveAt(_indentWidths.Count - 1);
            tokens.Add(new Token(TokenKind.Dedent, "", 0, _line, _column));
        }
        if (indentWidth != _indentWidths[^1])
            throw ScriptException.Error(
                DiagnosticCode.SyntaxError,
                "unindent does not match any outer indentation level",
                _line, 1);
    }

    private void SkipSpacesAndComment()
    {
        while (_position < _source.Length)
        {
            char current = _source[_position];

            if (current == ' ' || current == '\t')
            {
                Advance();
                continue;
            }

            if (current == ScriptingConstants.Lexical.Comment)
            {
                while (_position < _source.Length && _source[_position] != '\n')
                    Advance();
                
                continue;
            }
            break;
        }
    }

    private void Advance()
    {
        if (_source[_position] == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        _position++;
    }

    private Token ReadString()
    {
        int line = _line, column = _column;
        char quote = _source[_position];

        Advance(); // opening quote

        var text = new StringBuilder();

        while (true)
        {
            if (_position >= _source.Length || _source[_position] == '\n')
                throw ScriptException.Error(
                    DiagnosticCode.SyntaxError,
                    "unterminated string literal (strings must close on the same line)",
                    line, column);
            
            char current = _source[_position];

            if (current == quote)
            {
                Advance();
                return new Token(TokenKind.Str, text.ToString(), 0, line, column);
            }

            if (current == ScriptingConstants.Lexical.StringEscape)
            {
                Advance();

                if (_position >= _source.Length || _source[_position] == '\n')
                    throw ScriptException.Error(
                        DiagnosticCode.SyntaxError,
                        "unterminated string literal (strings must close on the same line)",
                        line, column);
                
                char escaped = _source[_position];
                
                text.Append(escaped switch
                {
                    'n' => "\n",
                    't' => "\t",
                    'r' => "\r",
                    '\\' => "\\",
                    '\'' => "'",
                    '"' => "\"",
                    _ => "\\" + escaped // unknown escape: keep both characters, like Python
                });

                Advance();
                continue;
            }
            text.Append(current);
            Advance();
        }
    }

    private Token ReadNumber()
    {
        int line = _line, column = _column;
        int start = _position;

        while (_position < _source.Length && char.IsAsciiDigit(_source[_position]))
            Advance();
        
        if (_position + 1 < _source.Length && _source[_position] == '.' && char.IsAsciiDigit(_source[_position + 1]))
        {
            Advance();
            while (_position < _source.Length && char.IsAsciiDigit(_source[_position])) Advance();
        }

        string text = _source[start.._position];
        double value = double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        return new Token(TokenKind.Num, text, value, line, column);
    }

    private Token ReadIdentifierOrKeyword()
    {
        int line = _line, column = _column;
        int start = _position;

        while (_position < _source.Length && (_source[_position] == '_' || char.IsLetterOrDigit(_source[_position])))
            Advance();
        
        string text = _source[start.._position];
        
        if (Keywords.TryGetValue(text, out var keyword))
            return new Token(keyword, text, 0, line, column);
        
        if (ScriptingConstants.ReservedWords.Contains(text))
            throw ScriptException.Error(
                DiagnosticCode.SyntaxError,
                $"the keyword '{text}' is not supported in SimplyDraft scripts",
                line, column);
        
        return new Token(TokenKind.Ident, text, 0, line, column);
    }

    private Token ReadSymbol()
    {
        int line = _line, column = _column;
        char symbol = _source[_position];

        Advance();
        
        char next = _position < _source.Length ? _source[_position] : '\0';
        
        switch (symbol)
        {
            case '=':
                if (next == '=')
                {
                    Advance();
                    return new Token(TokenKind.Equal, "==", 0, line, column);
                }
                return new Token(TokenKind.Assign, "=", 0, line, column);

            case '!':
                if (next == '=')
                {
                    Advance();
                    return new Token(TokenKind.NotEqual, "!=", 0, line, column);
                }
                throw ScriptException.Error(
                    DiagnosticCode.SyntaxError,
                    "unexpected '!' — negation is written 'not'",
                    line, column);

            case '<':
                if (next == '=')
                {
                    Advance();
                    return new Token(TokenKind.LessOrEqual, "<=", 0, line, column);
                }
                if (next == '>')
                    throw ScriptException.Error(
                        DiagnosticCode.SyntaxError,
                        "'<>' is not valid — not-equal is written '!='",
                        line, column);
                return new Token(TokenKind.Less, "<", 0, line, column);

            case '>':
                if (next == '=')
                {
                    Advance();
                    return new Token(TokenKind.GreaterOrEqual, ">=", 0, line, column);
                }
                return new Token(TokenKind.Greater, ">", 0, line, column);

            case '+':
                return new Token(TokenKind.Plus, "+", 0, line, column);
            
            case '-':
                return new Token(TokenKind.Minus, "-", 0, line, column);
            
            case '*':
                return new Token(TokenKind.Star, "*", 0, line, column);
            
            case '/':
                return new Token(TokenKind.Slash, "/", 0, line, column);
            
            case '%':
                return new Token(TokenKind.Percent, "%", 0, line, column);
            
            case '(':
                _openGroupDepth++;
                return new Token(TokenKind.LeftParen, "(", 0, line, column);
            
            case ')':
                if (_openGroupDepth > 0)
                    _openGroupDepth--;
                return new Token(TokenKind.RightParen, ")", 0, line, column);
            
            case '[':
                _openGroupDepth++;
                return new Token(TokenKind.LeftBracket, "[", 0, line, column);
            
            case ']':
                if (_openGroupDepth > 0)
                    _openGroupDepth--;
                return new Token(TokenKind.RightBracket, "]", 0, line, column);
            
            case ':':
                return new Token(TokenKind.Colon, ":", 0, line, column);
            
            case ',':
                return new Token(TokenKind.Comma, ",", 0, line, column);
            
            case '.':
                return new Token(TokenKind.Dot, ".", 0, line, column);
            
            case '&':
                throw ScriptException.Error(
                    DiagnosticCode.SyntaxError,
                    "'&' is not used in this language — join text with '+' (wrap non-text values with str(…))",
                    line, column);
            
            case '{':
                throw ScriptException.Error(
                    DiagnosticCode.SyntaxError,
                    "braces are not used in scripts — write the variable name directly (name, not {name})",
                    line, column);
            
            case ';':
                throw ScriptException.Error(
                    DiagnosticCode.SyntaxError,
                    "';' is not used — write one statement per line",
                    line, column);
            
            default:
                throw ScriptException.Error(
                    DiagnosticCode.SyntaxError,
                    $"unexpected character '{symbol}'",
                    line, column);
        }
    }
}