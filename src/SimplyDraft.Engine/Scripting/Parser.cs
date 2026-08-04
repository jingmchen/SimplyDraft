using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Core.Domains.Scripting.Expressions;
using SimplyDraft.Core.Domains.Scripting.Statements;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Exceptions;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Scripting;

public sealed class Parser
{
    private readonly List<Token> _t;
    private int _i;
    private int _depth;
    public const int MaxParseDepth = 128;

    public Parser(List<Token> tokens) { _t = tokens; }

    public static List<Statement> ParseScript(string source, int startLine = 1)
        => new Parser(Lexer.LexScript(source, startLine)).ParseProgram();

    public static Expression ParseExpressionOnly(string source, int startLine = 1, int startCol = 1)
    {
        var p = new Parser(Lexer.LexExpression(source, startLine, startCol));
        var e = p.ParseExpr();
        if (p.Cur.Kind != TokenKind.EndOfLine)
            throw p.Err($"unexpected {Describe(p.Cur)} after the expression");
        return e;
    }

    private Token Cur => _t[_i];
    private Token Peek => _i + 1 < _t.Count ? _t[_i + 1] : _t[^1];
    private Token Next() { var t = _t[_i]; if (_i < _t.Count - 1) _i++; return t; }

    private ScriptException Err(string msg)
        => ScriptException.Error(DiagnosticCode.SyntaxError, msg, Cur.Line, Cur.Column);

    private static ScriptException ErrAt(Token t, string msg)
        => ScriptException.Error(DiagnosticCode.SyntaxError, msg, t.Line, t.Column);

    private static string Describe(Token t) => t.Kind switch
    {
        TokenKind.NewLine => "end of line",
        TokenKind.Indent => "an indented block",
        TokenKind.Dedent => "end of block",
        TokenKind.EndOfLine => "end of script",
        TokenKind.Str => $"\"{t.Text}\"",
        _ => $"'{t.Text}'"
    };

    private static bool IsBuiltinNamespace(string name)
        => name.Equals(ScriptingConstants.Builtins.System, StringComparison.OrdinalIgnoreCase)
        || name.Equals(ScriptingConstants.Builtins.Doc, StringComparison.OrdinalIgnoreCase);

    private void SkipNewlines() { while (Cur.Kind == TokenKind.NewLine) Next(); }

    private void ExpectEndOfLine()
    {
        if (Cur.Kind == TokenKind.NewLine) { Next(); return; }
        if (Cur.Kind == TokenKind.EndOfLine) return;
        throw Err($"expected end of line, found {Describe(Cur)}");
    }

    public List<Statement> ParseProgram()
    {
        var stmts = new List<Statement>();
        SkipNewlines();
        while (Cur.Kind != TokenKind.EndOfLine)
        {
            if (Cur.Kind == TokenKind.Indent) throw Err("unexpected indent");
            stmts.Add(ParseStatement());
            SkipNewlines();
        }
        return stmts;
    }

    private Statement ParseStatement()
    {
        if (Cur.Kind == TokenKind.If) return ParseIf();
        if (Cur.Kind == TokenKind.ElseIf) throw Err("'elif' without a matching 'if'");
        if (Cur.Kind == TokenKind.Else) throw Err("'else' without a matching 'if'");
        // 'match' is a soft keyword (like Python): it starts a match statement unless the next
        // token makes it an ordinary expression/assignment ('match = 1', 'match.x', 'match(…)').
        if (IsSoftKeyword(ScriptingConstants.Keywords.Match) &&
            Peek.Kind is not (TokenKind.Assign or TokenKind.Dot or TokenKind.LeftParen or TokenKind.LeftBracket))
            return ParseMatch();
        return ParseSimpleStatement();
    }

    private bool IsSoftKeyword(string keyword)
        => Cur.Kind == TokenKind.Ident && Cur.Text == keyword; // case-sensitive, like Python

    private MatchStatement ParseMatch()
    {
        var kw = Next(); // 'match'
        var subject = ParseExpr();
        ExpectColon("after the match subject");
        if (Cur.Kind != TokenKind.NewLine)
            throw Err("write the case clauses on indented lines below 'match …:'");
        Next();
        if (Cur.Kind != TokenKind.Indent)
            throw Err("expected an indented block of case clauses");
        Next();
        var cases = new List<MatchCase>();
        while (Cur.Kind != TokenKind.Dedent)
        {
            if (Cur.Kind == TokenKind.EndOfLine)
                throw Err("unexpected end of script inside match");
            if (!IsSoftKeyword(ScriptingConstants.Keywords.Case))
                throw Err($"expected 'case', found {Describe(Cur)}");
            var caseTok = Next();
            Value? literal;
            var t = Cur;
            switch (t.Kind)
            {
                case TokenKind.Str: Next(); literal = Value.Str(t.Text); break;
                case TokenKind.Num: Next(); literal = Value.Num(t.NumberValue); break;
                case TokenKind.True: Next(); literal = Value.Bool(true); break;
                case TokenKind.False: Next(); literal = Value.Bool(false); break;
                case TokenKind.Ident when t.Text == ScriptingConstants.Keywords.Discard: Next(); literal = null; break;
                case TokenKind.Minus:
                    Next();
                    if (Cur.Kind != TokenKind.Num) throw Err("expected a number after '-'");
                    var n = Next();
                    literal = Value.Num(-n.NumberValue);
                    break;
                default:
                    throw Err($"case patterns are literals (\"text\", numbers, True/False) or '_', found {Describe(t)}");
            }
            ExpectColon("after the case pattern");
            cases.Add(new MatchCase(literal, ParseBlock(), caseTok.Line, caseTok.Column));
            SkipNewlines();
        }
        Next(); // dedent
        if (cases.Count == 0)
            throw ErrAt(kw, "match needs at least one case clause");
        
        return new MatchStatement(
            line: kw.Line,
            column: kw.Column,
            subject: subject,
            cases: cases
        );
    }

    private AssignmentStatement ParseSimpleStatement()
    {
        var first = Cur;
        var target = ParseExpr();
        if (Cur.Kind != TokenKind.Assign)
            throw ErrAt(first, "this line has no effect — write an assignment (name = …) or an if-statement");
        Next();
        var value = ParseExpr();
        ExpectEndOfLine();
        return target switch
        {
            NameExpression n when IsBuiltinNamespace(n.Name)
                => throw ScriptException.Error(DiagnosticCode.AssignToBuiltin,
                    $"'{n.Name.ToLowerInvariant()}' is reserved for built-in values and cannot be assigned", first.Line, first.Column),
            
            NameExpression n => new AssignmentStatement(
                line: first.Line,
                column: first.Column,
                name: n.Name,
                value: value
            ),
            
            BuiltinRefExpression b
                => throw ScriptException.Error(DiagnosticCode.AssignToBuiltin,
                    $"{b.Namespace}.{b.Member} is read-only and cannot be assigned", first.Line, first.Column),
            _ => throw ErrAt(first, "invalid assignment target — assign to a plain name, e.g. result = …")
        };
    }

    private IfStatement ParseIf()
    {
        var ifTok = Next(); // 'if'
        var stmt = new IfStatement(ifTok.Line, ifTok.Column);
        var cond = ParseExpr();
        ExpectColon("after the condition");
        stmt.Branches.Add((cond, ParseBlock()));
        while (Cur.Kind == TokenKind.ElseIf)
        {
            Next();
            var c2 = ParseExpr();
            ExpectColon("after the condition");
            stmt.Branches.Add((c2, ParseBlock()));
        }
        if (Cur.Kind == TokenKind.Else)
        {
            Next();
            ExpectColon("after 'else'");
            stmt.Branches.Add((null, ParseBlock()));
        }
        return stmt;
    }

    private void ExpectColon(string context)
    {
        if (Cur.Kind != TokenKind.Colon)
            throw Err($"expected ':' {context}, found {Describe(Cur)}");
        Next();
    }

    private List<Statement> ParseBlock()
    {
        if (Cur.Kind != TokenKind.NewLine)
        {
            // single-line suite:  if cond: name = value
            if (Cur.Kind == TokenKind.If)
                throw Err("write a nested if-statement on its own indented line");
            return new List<Statement> { ParseSimpleStatement() };
        }
        Next(); // newline
        if (Cur.Kind != TokenKind.Indent)
            throw Err("expected an indented block after ':'");
        Next();
        var body = new List<Statement>();
        while (Cur.Kind != TokenKind.Dedent)
        {
            if (Cur.Kind == TokenKind.EndOfLine)
                throw Err("unexpected end of script inside an indented block");
            body.Add(ParseStatement());
            SkipNewlines();
        }
        Next(); // dedent
        return body;
    }

    // ---------- expressions ----------

    public Expression ParseExpr()
    {
        // Bound recursion depth: deeply nested parens/subscripts recurse the descent per level and
        // would otherwise overflow the stack (uncatchable). A catchable SyntaxError is far better.
        if (_depth >= MaxParseDepth)
            throw Err($"expression nested too deeply (limit {MaxParseDepth})");
        _depth++;
        try { return ParseConditional(); }
        finally { _depth--; }
    }

    // Shared recursion budget for the prefix/ternary productions. 'not'/'-' chains and ternary
    // else-chains recurse into themselves WITHOUT passing through ParseExpr, so before this they
    // bypassed the depth guard entirely: `not not … x`, `- - … x`, or a long `a if b else …` chain
    // overflowed the native stack (an uncatchable crash). Routing their recursive call through here
    // makes them share ParseExpr's budget and raise a catchable SyntaxError instead.
    private Expression Descend(Func<Expression> production)
    {
        if (_depth >= MaxParseDepth)
            throw Err($"expression nested too deeply (limit {MaxParseDepth})");
        _depth++;
        try { return production(); }
        finally { _depth--; }
    }

    private Expression ParseConditional()
    {
        var then = ParseOr();
        if (Cur.Kind != TokenKind.If) return then;
        var t = Next();
        var cond = ParseOr();
        if (Cur.Kind != TokenKind.Else)
            throw Err("expected 'else' to complete 'value if condition else other'");
        Next();
        var els = Descend(ParseConditional);

        return new ConditionalExpression(
            line: t.Line,
            column: t.Column,
            condition: cond,
            then: then,
            el: els
        );
    }

    private Expression ParseOr()
    {
        var left = ParseAnd();
        while (Cur.Kind == TokenKind.Or)
        {
            var t = Next();

            left = new BinaryExpression(
                line: t.Line,
                column: t.Column,
                op: BinaryOperator.Or,
                left: left,
                right: ParseAnd()
            );
        }
        return left;
    }

    private Expression ParseAnd()
    {
        var left = ParseNot();
        while (Cur.Kind == TokenKind.And)
        {
            var t = Next();

            left = new BinaryExpression(
                line: t.Line,
                column: t.Column,
                op: BinaryOperator.And,
                left: left,
                right: ParseNot()
            );
        }
        return left;
    }

    private Expression ParseNot()
    {
        if (Cur.Kind == TokenKind.Not)
        {
            var t = Next();

            return new UnaryExpression(
                line: t.Line,
                column: t.Column,
                op: UnaryOperator.Not,
                operand: Descend(ParseNot)
            );
        }
        return ParseComparison();
    }

    private Expression ParseComparison()
    {
        var left = ParseAdditive();
        BinaryOperator op;
        Token t;
        switch (Cur.Kind)
        {
            case TokenKind.Equal: op = BinaryOperator.Equal; t = Next(); break;
            case TokenKind.NotEqual: op = BinaryOperator.NotEqual; t = Next(); break;
            case TokenKind.Less: op = BinaryOperator.Less; t = Next(); break;
            case TokenKind.LessOrEqual: op = BinaryOperator.LessOrEqual; t = Next(); break;
            case TokenKind.Greater: op = BinaryOperator.Greater; t = Next(); break;
            case TokenKind.GreaterOrEqual: op = BinaryOperator.GreaterOrEqual; t = Next(); break;
            case TokenKind.In: op = BinaryOperator.In; t = Next(); break;
            case TokenKind.Not when Peek.Kind == TokenKind.In:
                t = Next(); Next(); op = BinaryOperator.NotIn; break;
            default:
                return left;
        }
        var right = ParseAdditive();
        if (
            Cur.Kind is TokenKind.Equal or TokenKind.NotEqual or TokenKind.Less or TokenKind.LessOrEqual
            or TokenKind.Greater or TokenKind.GreaterOrEqual or TokenKind.In
        )
            throw Err("comparison chaining (a < b < c) is not supported — combine two comparisons with 'and'");
        
        return new BinaryExpression(
            line: t.Line,
            column: t.Column,
            op: op,
            left: left,
            right: right
        );
    }

    private Expression ParseAdditive()
    {
        var left = ParseTerm();
        while (Cur.Kind is TokenKind.Plus or TokenKind.Minus)
        {
            var t = Next();
            var op = t.Kind == TokenKind.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;

            left = new BinaryExpression(
                line: t.Line,
                column: t.Column,
                op: op,
                left: left,
                right: ParseTerm()
            );
        }
        return left;
    }

    private Expression ParseTerm()
    {
        var left = ParseUnary();
        while (Cur.Kind is TokenKind.Star or TokenKind.Slash or TokenKind.Percent)
        {
            var t = Next();
            var op = t.Kind switch
            {
                TokenKind.Star => BinaryOperator.Multiply,
                TokenKind.Slash => BinaryOperator.Divide,
                _ => BinaryOperator.Modulo
            };

            left = new BinaryExpression(
                line: t.Line,
                column: t.Column,
                op: op,
                left: left,
                right: ParseUnary()
            );
        }
        return left;
    }

    private Expression ParseUnary()
    {
        if (Cur.Kind == TokenKind.Minus)
        {
            var t = Next();

            return new UnaryExpression(
                line: t.Line,
                column: t.Column,
                op: UnaryOperator.Negate,
                operand: Descend(ParseUnary)
            );
        }
        return ParsePostfix();
    }

    private Expression ParsePostfix()
    {
        var e = ParsePrimary();
        while (true)
        {
            if (Cur.Kind == TokenKind.LeftParen)
            {
                var t = Next();
                var args = ParseArgs();
                if (e is NameExpression fn)
                {
                    e = new CallExpression(
                        line: fn.Line, column: fn.Column, name: fn.Name, args: args
                    );

                    continue;
                }
                throw ErrAt(t, "this value cannot be called — only functions like len(…) and methods like s.upper() are callable");
            }
            if (Cur.Kind == TokenKind.Dot)
            {
                var dot = Next();
                if (Cur.Kind != TokenKind.Ident)
                    throw Err($"expected a name after '.', found {Describe(Cur)}");
                var member = Next();
                if (Cur.Kind == TokenKind.LeftParen)
                {
                    Next();
                    
                    var args = ParseArgs();
                    
                    e = new MethodCallExpression(
                        line: dot.Line, column: dot.Column, name: member.Text, receiver: e, args: args
                    );

                    continue;
                }
                if (e is NameExpression ns && IsBuiltinNamespace(ns.Name))
                {
                    e = new BuiltinRefExpression(
                        line: ns.Line, column: ns.Column, ns: ns.Name.ToLowerInvariant(), member: member.Text.ToLowerInvariant()
                    );

                    continue;
                }
                throw ErrAt(dot,
                    $"'.{member.Text}' is not valid here — dots are for method calls (s.upper()) and the system.*/doc.* built-ins");
            }
            if (Cur.Kind == TokenKind.LeftBracket)
            {
                var t = Next();
                Expression? start = null, end = null;
                bool isSlice = false;
                if (Cur.Kind != TokenKind.Colon) start = ParseExpr();
                if (Cur.Kind == TokenKind.Colon)
                {
                    isSlice = true;
                    Next();
                    if (Cur.Kind != TokenKind.RightBracket) end = ParseExpr();
                }
                if (Cur.Kind != TokenKind.RightBracket)
                    throw Err($"expected ']', found {Describe(Cur)}");
                Next();
                if (isSlice)
                    e = new SliceExpression(line: t.Line, column: t.Column, target: e, start: start, end: end);
                else if (start != null)
                    e = new IndexExpression(line: t.Line, column: t.Column, target: e, index: start);
                else
                    throw ErrAt(t, "empty subscript — write s[i] or s[start:end]");
                continue;
            }
            return e;
        }
    }

    private List<Expression> ParseArgs()
    {
        var args = new List<Expression>();
        if (Cur.Kind != TokenKind.RightParen)
        {
            args.Add(ParseExpr());
            while (Cur.Kind == TokenKind.Comma)
            {
                Next();
                args.Add(ParseExpr());
            }
        }
        if (Cur.Kind != TokenKind.RightParen)
            throw Err($"expected ')', found {Describe(Cur)}");
        Next();
        return args;
    }

    private Expression ParsePrimary()
    {
        var t = Cur;
        switch (t.Kind)
        {
            case TokenKind.Num:
                Next();
                return new LiteralExpression(line: t.Line, column: t.Column, value: Value.Num(t.NumberValue));
            case TokenKind.Str:
                Next();
                return new LiteralExpression(line: t.Line, column: t.Column, value: Value.Str(t.Text));
            case TokenKind.True:
                Next();
                return new LiteralExpression(line: t.Line, column: t.Column, value: Value.Bool(true));
            case TokenKind.False:
                Next();
                return new LiteralExpression(line: t.Line, column: t.Column, value: Value.Bool(false));
            case TokenKind.Ident:
                Next();
                return new NameExpression(line: t.Line, column: t.Column, name: t.Text);
            case TokenKind.LeftParen:
            {
                Next();
                var e = ParseExpr();
                if (Cur.Kind != TokenKind.RightParen)
                    throw Err($"expected ')', found {Describe(Cur)}");
                Next();
                return e;
            }
            default:
                throw Err($"unexpected {Describe(t)}");
        }
    }
}