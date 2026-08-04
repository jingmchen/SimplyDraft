using System.Text;
using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Exceptions;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Scripting;

public sealed class Lexer
{
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

    private readonly string _src;
    private readonly bool _scriptMode;
    private readonly List<int> _indents = [0];
    private int _pos;
    private int _line;
    private int _col;
    private int _groupDepth;
    private bool _atLineStart;

    private Lexer(string source, bool scriptMode, int startLine, int startCol)
    {
        _src = (source ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        _scriptMode = scriptMode;
        _line = startLine;
        _col = startCol;
        _atLineStart = scriptMode;
    }

    public static List<Token> LexScript(string source, int startLine = 1)
        => new Lexer(source, scriptMode: true, startLine, 1).Lex();

    public static List<Token> LexExpression(string source, int startLine = 1, int startCol = 1)
        => new Lexer(source, scriptMode: false, startLine, startCol).Lex();

    private List<Token> Lex()
    {
        var tokens = new List<Token>();
        while (true)
        {
            if (_scriptMode && _atLineStart && _groupDepth == 0)
            {
                if (!StartLine(tokens)) break; // EOF
                continue;
            }

            SkipSpacesAndComment();
            if (_pos >= _src.Length) break;

            char c = _src[_pos];
            int line = _line, col = _col;

            if (c == '\n')
            {
                Advance();
                if (_groupDepth > 0) continue; // implicit line joining inside (…) / […]
                if (_scriptMode)
                {
                    tokens.Add(new Token(TokenKind.NewLine, "\n", 0, line, col));
                    _atLineStart = true;
                }
                continue; // expression mode: a newline is just whitespace
            }
            if (c == ScriptingConstants.Lexical.DoubleQuote || c == ScriptingConstants.Lexical.SingleQuote) { tokens.Add(LexString()); continue; }
            if (char.IsAsciiDigit(c)) { tokens.Add(LexNumber()); continue; }
            if (c == '_' || char.IsLetter(c)) { tokens.Add(LexIdentOrKeyword()); continue; }
            tokens.Add(LexSymbol());
        }

        if (_scriptMode)
        {
            if (!_atLineStart)
                tokens.Add(new Token(TokenKind.NewLine, "\n", 0, _line, _col));
            while (_indents[^1] > 0)
            {
                _indents.RemoveAt(_indents.Count - 1);
                tokens.Add(new Token(TokenKind.Dedent, "", 0, _line, _col));
            }
        }
        tokens.Add(new Token(TokenKind.EndOfLine, "", 0, _line, _col));
        return tokens;
    }

    /// <summary>
    /// Handles the start of a logical line: skips blank/comment-only lines, measures the
    /// indentation of the first real line and emits Indent/Dedent tokens. Returns false at EOF.
    /// </summary>
    private bool StartLine(List<Token> tokens)
    {
        while (true)
        {
            if (_pos >= _src.Length) return false;

            int j = _pos;
            bool sawTab = false;
            while (j < _src.Length && (_src[j] == ' ' || _src[j] == '\t'))
            {
                if (_src[j] == '\t') sawTab = true;
                j++;
            }
            bool blank = j >= _src.Length || _src[j] == '\n' || _src[j] == ScriptingConstants.Lexical.Comment;
            if (blank)
            {
                while (_pos < _src.Length && _src[_pos] != '\n') Advance();
                if (_pos < _src.Length) Advance(); // consume the newline
                continue;
            }
            if (sawTab)
                throw ScriptException.Error(DiagnosticCode.SyntaxError,
                    "tab characters are not allowed in indentation — use spaces", _line, 1);

            int width = j - _pos;
            while (_pos < j) Advance();
            EmitIndentTokens(tokens, width);
            _atLineStart = false;
            return true;
        }
    }

    private void EmitIndentTokens(List<Token> tokens, int width)
    {
        if (width > _indents[^1])
        {
            _indents.Add(width);
            tokens.Add(new Token(TokenKind.Indent, "", 0, _line, _col));
            return;
        }
        while (width < _indents[^1])
        {
            _indents.RemoveAt(_indents.Count - 1);
            tokens.Add(new Token(TokenKind.Dedent, "", 0, _line, _col));
        }
        if (width != _indents[^1])
            throw ScriptException.Error(DiagnosticCode.SyntaxError,
                "unindent does not match any outer indentation level", _line, 1);
    }

    private void SkipSpacesAndComment()
    {
        while (_pos < _src.Length)
        {
            char c = _src[_pos];
            if (c == ' ' || c == '\t') { Advance(); continue; }
            if (c == ScriptingConstants.Lexical.Comment)
            {
                while (_pos < _src.Length && _src[_pos] != '\n') Advance();
                continue;
            }
            break;
        }
    }

    private void Advance()
    {
        if (_src[_pos] == '\n') { _line++; _col = 1; } else { _col++; }
        _pos++;
    }

    private Token LexString()
    {
        int line = _line, col = _col;
        char quote = _src[_pos];
        Advance(); // opening quote
        var sb = new StringBuilder();
        while (true)
        {
            if (_pos >= _src.Length || _src[_pos] == '\n')
                throw ScriptException.Error(DiagnosticCode.SyntaxError,
                    "unterminated string literal (strings must close on the same line)", line, col);
            char c = _src[_pos];
            if (c == quote)
            {
                Advance();
                return new Token(TokenKind.Str, sb.ToString(), 0, line, col);
            }
            if (c == ScriptingConstants.Lexical.StringEscape)
            {
                Advance();
                if (_pos >= _src.Length || _src[_pos] == '\n')
                    throw ScriptException.Error(DiagnosticCode.SyntaxError,
                        "unterminated string literal (strings must close on the same line)", line, col);
                char e = _src[_pos];
                sb.Append(e switch
                {
                    'n' => "\n",
                    't' => "\t",
                    'r' => "\r",
                    '\\' => "\\",
                    '\'' => "'",
                    '"' => "\"",
                    _ => "\\" + e // unknown escape: keep both characters, like Python
                });
                Advance();
                continue;
            }
            sb.Append(c);
            Advance();
        }
    }

    private Token LexNumber()
    {
        int line = _line, col = _col;
        int start = _pos;
        while (_pos < _src.Length && char.IsAsciiDigit(_src[_pos])) Advance();
        if (_pos + 1 < _src.Length && _src[_pos] == '.' && char.IsAsciiDigit(_src[_pos + 1]))
        {
            Advance();
            while (_pos < _src.Length && char.IsAsciiDigit(_src[_pos])) Advance();
        }
        string text = _src[start.._pos];
        double val = double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        return new Token(TokenKind.Num, text, val, line, col);
    }

    private Token LexIdentOrKeyword()
    {
        int line = _line, col = _col;
        int start = _pos;
        while (_pos < _src.Length && (_src[_pos] == '_' || char.IsLetterOrDigit(_src[_pos]))) Advance();
        string text = _src[start.._pos];
        if (Keywords.TryGetValue(text, out var kw))
            return new Token(kw, text, 0, line, col);
        if (ScriptingConstants.ReservedWords.Contains(text))
            throw ScriptException.Error(DiagnosticCode.SyntaxError,
                $"the keyword '{text}' is not supported in MiniLatex scripts", line, col);
        return new Token(TokenKind.Ident, text, 0, line, col);
    }

    private Token LexSymbol()
    {
        int line = _line, col = _col;
        char c = _src[_pos];
        Advance();
        char next = _pos < _src.Length ? _src[_pos] : '\0';
        switch (c)
        {
            case '=':
                if (next == '=') { Advance(); return new Token(TokenKind.Equal, "==", 0, line, col); }
                return new Token(TokenKind.Assign, "=", 0, line, col);
            case '!':
                if (next == '=') { Advance(); return new Token(TokenKind.NotEqual, "!=", 0, line, col); }
                throw ScriptException.Error(DiagnosticCode.SyntaxError,
                    "unexpected '!' — negation is written 'not'", line, col);
            case '<':
                if (next == '=') { Advance(); return new Token(TokenKind.LessOrEqual, "<=", 0, line, col); }
                if (next == '>')
                    throw ScriptException.Error(DiagnosticCode.SyntaxError,
                        "'<>' is not valid — not-equal is written '!='", line, col);
                return new Token(TokenKind.Less, "<", 0, line, col);
            case '>':
                if (next == '=') { Advance(); return new Token(TokenKind.GreaterOrEqual, ">=", 0, line, col); }
                return new Token(TokenKind.Greater, ">", 0, line, col);
            case '+': return new Token(TokenKind.Plus, "+", 0, line, col);
            case '-': return new Token(TokenKind.Minus, "-", 0, line, col);
            case '*': return new Token(TokenKind.Star, "*", 0, line, col);
            case '/': return new Token(TokenKind.Slash, "/", 0, line, col);
            case '%': return new Token(TokenKind.Percent, "%", 0, line, col);
            case '(': _groupDepth++; return new Token(TokenKind.LeftParen, "(", 0, line, col);
            case ')': if (_groupDepth > 0) _groupDepth--; return new Token(TokenKind.RightParen, ")", 0, line, col);
            case '[': _groupDepth++; return new Token(TokenKind.LeftBracket, "[", 0, line, col);
            case ']': if (_groupDepth > 0) _groupDepth--; return new Token(TokenKind.RightBracket, "]", 0, line, col);
            case ':': return new Token(TokenKind.Colon, ":", 0, line, col);
            case ',': return new Token(TokenKind.Comma, ",", 0, line, col);
            case '.': return new Token(TokenKind.Dot, ".", 0, line, col);
            case '&':
                throw ScriptException.Error(DiagnosticCode.SyntaxError,
                    "'&' is not used in this language — join text with '+' (wrap non-text values with str(…))", line, col);
            case '{':
                throw ScriptException.Error(DiagnosticCode.SyntaxError,
                    "braces are not used in scripts — write the variable name directly (name, not {name})", line, col);
            case ';':
                throw ScriptException.Error(DiagnosticCode.SyntaxError,
                    "';' is not used — write one statement per line", line, col);
            default:
                throw ScriptException.Error(DiagnosticCode.SyntaxError, $"unexpected character '{c}'", line, col);
        }
    }
}