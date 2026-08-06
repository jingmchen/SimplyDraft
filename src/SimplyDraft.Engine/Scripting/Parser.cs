// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Core.Domains.Scripting.Expressions;
using SimplyDraft.Core.Domains.Scripting.Statements;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Exceptions;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Scripting;

public sealed class Parser
{
    public const int MaxParseDepth = 128;
    private readonly List<Token> _tokens;
    private int _position;
    private int _depth;

    // Token cursor
    private Token Current => _tokens[_position];
    private Token Lookahead => _position + 1 < _tokens.Count ? _tokens[_position + 1] : _tokens[^1];

    public Parser(List<Token> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (tokens.Count == 0)
            throw new ArgumentException("The token list must at least contain the end-of-file token.", nameof(tokens));
        
        _tokens = tokens;
    }

    public List<Statement> ParseProgram()
    {
        var statements = new List<Statement>();
        SkipNewlines();

        while (Current.Kind != TokenKind.EndOfLine)
        {
            if (Current.Kind == TokenKind.Indent)
                throw SyntaxError("unexpected indent");
            
            statements.Add(ParseStatement());
            SkipNewlines();
        }
        return statements;
    }

    public static List<Statement> ParseScript(string source, int startLine = 1)
        => new Parser(Lexer.LexScript(source, startLine)).ParseProgram();

    public Expression ParseExpression()
    {
        if (_depth >= MaxParseDepth)
            throw SyntaxError($"expression nested too deeply (limit {MaxParseDepth})");
        
        _depth++;
        try
        {
            return ParseConditional();
        }
        finally
        {
            _depth--;
        }
    }

    public static Expression ParseExpressionOnly(string source, int startLine = 1, int startCol = 1)
    {
        var parser = new Parser(Lexer.LexExpression(source, startLine, startCol));
        var expression = parser.ParseExpression();

        if (parser.Current.Kind != TokenKind.EndOfLine)
            throw parser.SyntaxError($"unexpected {Describe(parser.Current)} after the expression");
        
        return expression;
    }

    private Token Consume()
    {
        var token = _tokens[_position];
        
        if (_position < _tokens.Count - 1)
            _position++;
        
        return token;
    }

    private ScriptException SyntaxError(string message)
        => ScriptException.Error(DiagnosticCode.SyntaxError, message, Current.Line, Current.Column);

    private static ScriptException SyntaxErrorAt(Token token, string message)
        => ScriptException.Error(DiagnosticCode.SyntaxError, message, token.Line, token.Column);

    private static string Describe(Token token)
        => token.Kind switch
        {
            TokenKind.NewLine => "end of line",
            TokenKind.Indent => "an indented block",
            TokenKind.Dedent => "end of block",
            TokenKind.EndOfLine => "end of script",
            TokenKind.Str => $"\"{token.Text}\"",
            _ => $"'{token.Text}'"
        };

    private static bool IsBuiltinNamespace(string name)
        => name.Equals(ScriptingConstants.Builtins.System, StringComparison.OrdinalIgnoreCase)
        || name.Equals(ScriptingConstants.Builtins.Doc, StringComparison.OrdinalIgnoreCase);

    private void SkipNewlines()
    {
        while (Current.Kind == TokenKind.NewLine)
            Consume();
    }

    private void ExpectEndOfLine()
    {
        if (Current.Kind == TokenKind.NewLine)
        {
            Consume();
            return;
        }

        if (Current.Kind == TokenKind.EndOfLine)
            return;
        
        throw SyntaxError($"expected end of line, found {Describe(Current)}");
    }

    private void ExpectColon(string context)
    {
        if (Current.Kind != TokenKind.Colon)
            throw SyntaxError($"expected ':' {context}, found {Describe(Current)}");
        Consume();
    }

    private Statement ParseStatement()
    {
        if (Current.Kind == TokenKind.If)
            return ParseIfStatement();
        
        if (Current.Kind == TokenKind.ElseIf)
            throw SyntaxError("'elif' without a matching 'if'");
        
        if (Current.Kind == TokenKind.Else)
            throw SyntaxError("'else' without a matching 'if'");
        
        if (IsSoftKeyword(ScriptingConstants.Keywords.Match) &&
            Lookahead.Kind is not (TokenKind.Assign or TokenKind.Dot or TokenKind.LeftParen or TokenKind.LeftBracket))
                return ParseMatchStatement();
        
        return ParseAssignment();
    }

    private bool IsSoftKeyword(string keyword)
        => Current.Kind == TokenKind.Ident && Current.Text == keyword;

    private AssignmentStatement ParseAssignment()
    {
        var firstToken = Current;
        var target = ParseExpression();

        if (Current.Kind != TokenKind.Assign)
            throw SyntaxErrorAt(firstToken, "this line has no effect — write an assignment (name = …) or an if-statement");
        
        Consume();
        var value = ParseExpression();
        ExpectEndOfLine();
        
        return target switch
        {
            NameExpression name when IsBuiltinNamespace(name.Name)
                => throw ScriptException.Error(
                    DiagnosticCode.AssignToBuiltin,
                    $"'{name.Name.ToLowerInvariant()}' is reserved for built-in values and cannot be assigned",
                    firstToken.Line, firstToken.Column
                ),
            
            NameExpression name
                => new AssignmentStatement(name.Name, value, firstToken.Line, firstToken.Column),
            
            BuiltinRefExpression builtinRef
                => throw ScriptException.Error(
                    DiagnosticCode.AssignToBuiltin,
                    $"{builtinRef.Namespace}.{builtinRef.Member} is read-only and cannot be assigned",
                    firstToken.Line, firstToken.Column
                ),
            
            _ => throw SyntaxErrorAt(firstToken, "invalid assignment target — assign to a plain name, e.g. result = …")
        };
    }

    private IfStatement ParseIfStatement()
    {
        var ifToken = Consume(); // 'if'
        var statement = new IfStatement(ifToken.Line, ifToken.Column);
        var condition = ParseExpression();
        ExpectColon("after the condition");
        statement.Branches.Add((condition, ParseBlock()));

        while (Current.Kind == TokenKind.ElseIf)
        {
            Consume();
            var elifCondition = ParseExpression();
            ExpectColon("after the condition");
            statement.Branches.Add((elifCondition, ParseBlock()));
        }

        if (Current.Kind == TokenKind.Else)
        {
            Consume();
            ExpectColon("after 'else'");
            statement.Branches.Add((null, ParseBlock()));
        }

        return statement;
    }

    private MatchStatement ParseMatchStatement()
    {
        var matchToken = Consume(); // 'match'
        var subject = ParseExpression();
        ExpectColon("after the match subject");

        if (Current.Kind != TokenKind.NewLine)
            throw SyntaxError("write the case clauses on indented lines below 'match …:'");
        
        Consume();
        if (Current.Kind != TokenKind.Indent)
            throw SyntaxError("expected an indented block of case clauses");
        
        Consume();
        var cases = new List<MatchCase>();

        while (Current.Kind != TokenKind.Dedent)
        {
            if (Current.Kind == TokenKind.EndOfLine)
                throw SyntaxError("unexpected end of script inside match");
            
            if (!IsSoftKeyword(ScriptingConstants.Keywords.Case))
                throw SyntaxError($"expected 'case', found {Describe(Current)}");
            
            var caseToken = Consume();
            var literal = ParseCasePattern();
            ExpectColon("after the case pattern");
            cases.Add(new MatchCase(literal, ParseBlock(), caseToken.Line, caseToken.Column));
            SkipNewlines();
        }

        Consume(); // dedent
        if (cases.Count == 0)
            throw SyntaxErrorAt(matchToken, "match needs at least one case clause");
        
        return new MatchStatement(subject, cases, matchToken.Line, matchToken.Column);
    }

    private Value? ParseCasePattern()
    {
        var token = Current;

        switch (token.Kind)
        {
            case TokenKind.Str:
                Consume();
                return Value.Str(token.Text);
            
            case TokenKind.Num:
                Consume();
                return Value.Num(token.NumberValue);
            
            case TokenKind.True:
                Consume();
                return Value.Bool(true);
            
            case TokenKind.False:
                Consume();
                return Value.Bool(false);
            
            case TokenKind.Ident when token.Text == ScriptingConstants.Keywords.Discard:
                Consume();
                return null;
            
            case TokenKind.Minus:
                Consume();
                if (Current.Kind != TokenKind.Num)
                    throw SyntaxError("expected a number after '-'");
                var number = Consume();
                return Value.Num(-number.NumberValue);

            default:
                throw SyntaxError($"case patterns are literals (\"text\", numbers, True/False) or '_', found {Describe(token)}");
        }
    }

    private List<Statement> ParseBlock()
    {
        if (Current.Kind != TokenKind.NewLine)
        {
            if (Current.Kind == TokenKind.If)
                throw SyntaxError("write a nested if-statement on its own indented line");
            return [ParseAssignment()];
        }
        Consume(); // newline

        if (Current.Kind != TokenKind.Indent)
            throw SyntaxError("expected an indented block after ':'");
        
        Consume();
        var body = new List<Statement>();

        while (Current.Kind != TokenKind.Dedent)
        {
            if (Current.Kind == TokenKind.EndOfLine)
                throw SyntaxError("unexpected end of script inside an indented block");
            
            body.Add(ParseStatement());
            SkipNewlines();
        }
        Consume(); // dedent
        return body;
    }

    private Expression Descend(Func<Expression> production)
    {
        if (_depth >= MaxParseDepth)
            throw SyntaxError($"expression nested too deeply (limit {MaxParseDepth})");
        
        _depth++;
        try
        {
            return production();
        }
        finally
        {
            _depth--;
        }
    }

    private Expression ParseConditional()
    {
        var thenValue = ParseOr();

        if (Current.Kind != TokenKind.If)
            return thenValue;
        
        var ifToken = Consume();
        var condition = ParseOr();
        
        if (Current.Kind != TokenKind.Else)
            throw SyntaxError("expected 'else' to complete 'value if condition else other'");
        
        Consume();
        var elseValue = Descend(ParseConditional);
        
        return new ConditionalExpression(condition, thenValue, elseValue, ifToken.Line, ifToken.Column);
    }

    private Expression ParseOr()
    {
        var left = ParseAnd();

        while (Current.Kind == TokenKind.Or)
        {
            var orToken = Consume();
            left = new BinaryExpression(BinaryOperator.Or, left, ParseAnd(), orToken.Line, orToken.Column);
        }

        return left;
    }

    private Expression ParseAnd()
    {
        var left = ParseNot();

        while (Current.Kind == TokenKind.And)
        {
            var andToken = Consume();
            left = new BinaryExpression(BinaryOperator.And, left, ParseNot(), andToken.Line, andToken.Column);
        }
        
        return left;
    }

    private Expression ParseNot()
    {
        if (Current.Kind == TokenKind.Not)
        {
            var notToken = Consume();
            return new UnaryExpression(UnaryOperator.Not, Descend(ParseNot), notToken.Line, notToken.Column);
        }

        return ParseComparison();
    }

    private Expression ParseComparison()
    {
        var left = ParseAdditive();
        BinaryOperator op;
        Token opToken;

        switch (Current.Kind)
        {
            case TokenKind.Equal:
                op = BinaryOperator.Equal;
                opToken = Consume();
                break;
            
            case TokenKind.NotEqual:
                op = BinaryOperator.NotEqual;
                opToken = Consume();
                break;
            
            case TokenKind.Less:
                op = BinaryOperator.Less;
                opToken = Consume();
                break;
            
            case TokenKind.LessOrEqual:
                op = BinaryOperator.LessOrEqual;
                opToken = Consume();
                break;
            
            case TokenKind.Greater:
                op = BinaryOperator.Greater;
                opToken = Consume();
                break;
            
            case TokenKind.GreaterOrEqual:
                op = BinaryOperator.GreaterOrEqual;
                opToken = Consume();
                break;
            
            case TokenKind.In:
                op = BinaryOperator.In;
                opToken = Consume();
                break;
            
            case TokenKind.Not when Lookahead.Kind == TokenKind.In:
                opToken = Consume();
                Consume();
                op = BinaryOperator.NotIn;
                break;
            
            default:
                return left;
        }

        var right = ParseAdditive();

        if (Current.Kind is TokenKind.Equal or TokenKind.NotEqual or TokenKind.Less or TokenKind.LessOrEqual
            or TokenKind.Greater or TokenKind.GreaterOrEqual or TokenKind.In)
                throw SyntaxError("comparison chaining (a < b < c) is not supported — combine two comparisons with 'and'");
        
        return new BinaryExpression(op, left, right, opToken.Line, opToken.Column);
    }

    private Expression ParseAdditive()
    {
        var left = ParseMultiplicative();

        while (Current.Kind is TokenKind.Plus or TokenKind.Minus)
        {
            var opToken = Consume();
            var op = opToken.Kind == TokenKind.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            left = new BinaryExpression(op, left, ParseMultiplicative(), opToken.Line, opToken.Column);
        }

        return left;
    }

    private Expression ParseMultiplicative()
    {
        var left = ParseUnary();

        while (Current.Kind is TokenKind.Star or TokenKind.Slash or TokenKind.Percent)
        {
            var opToken = Consume();

            var op = opToken.Kind switch
            {
                TokenKind.Star => BinaryOperator.Multiply,
                TokenKind.Slash => BinaryOperator.Divide,
                _ => BinaryOperator.Modulo
            };

            left = new BinaryExpression(op, left, ParseUnary(), opToken.Line, opToken.Column);
        }
        return left;
    }

    private Expression ParseUnary()
    {
        if (Current.Kind == TokenKind.Minus)
        {
            var minusToken = Consume();
            return new UnaryExpression(UnaryOperator.Negate, Descend(ParseUnary), minusToken.Line, minusToken.Column);
        }

        return ParsePostfix();
    }

    private Expression ParsePostfix()
    {
        var expression = ParsePrimary();

        while (true)
        {
            if (Current.Kind == TokenKind.LeftParen)
            {
                var parenToken = Consume();
                var arguments = ParseArguments();

                if (expression is NameExpression functionName)
                {
                    expression = new CallExpression(functionName.Name, arguments, functionName.Line, functionName.Column);
                    continue;
                }
                
                throw SyntaxErrorAt(
                    parenToken, "this value cannot be called — only functions like len(…) and methods like s.upper() are callable"
                );
            }

            if (Current.Kind == TokenKind.Dot)
            {
                var dotToken = Consume();

                if (Current.Kind != TokenKind.Ident)
                    throw SyntaxError($"expected a name after '.', found {Describe(Current)}");
                
                var member = Consume();

                if (Current.Kind == TokenKind.LeftParen)
                {
                    Consume();
                    var arguments = ParseArguments();
                    expression = new MethodCallExpression(member.Text, expression, arguments, dotToken.Line, dotToken.Column);
                    continue;
                }
                
                if (expression is NameExpression ns && IsBuiltinNamespace(ns.Name))
                {
                    expression = new BuiltinRefExpression(ns.Name.ToLowerInvariant(), member.Text.ToLowerInvariant(), ns.Line, ns.Column);
                    continue;
                }

                throw SyntaxErrorAt(
                    dotToken, $"'.{member.Text}' is not valid here — dots are for method calls (s.upper()) and the system.*/doc.* built-ins"
                );
            }

            if (Current.Kind == TokenKind.LeftBracket)
            {
                expression = ParseSubscript(expression);
                continue;
            }

            return expression;
        }
    }

    private Expression ParseSubscript(Expression target)
    {
        var bracketToken = Consume();
        Expression? start = null, end = null;
        bool isSlice = false;

        if (Current.Kind != TokenKind.Colon)
            start = ParseExpression();
        
        if (Current.Kind == TokenKind.Colon)
        {
            isSlice = true;
            Consume();
            
            if (Current.Kind != TokenKind.RightBracket)
                end = ParseExpression();
        }

        if (Current.Kind != TokenKind.RightBracket)
            throw SyntaxError($"expected ']', found {Describe(Current)}");
        
        Consume();
        
        if (isSlice)
            return new SliceExpression(target, start, end, bracketToken.Line, bracketToken.Column);
        
        if (start != null)
            return new IndexExpression(target, start, bracketToken.Line, bracketToken.Column);
        
        throw SyntaxErrorAt(bracketToken, "empty subscript — write s[i] or s[start:end]");
    }

    private List<Expression> ParseArguments()
    {
        var arguments = new List<Expression>();

        if (Current.Kind != TokenKind.RightParen)
        {
            arguments.Add(ParseExpression());

            while (Current.Kind == TokenKind.Comma)
            {
                Consume();
                arguments.Add(ParseExpression());
            }
        }

        if (Current.Kind != TokenKind.RightParen)
            throw SyntaxError($"expected ')', found {Describe(Current)}");
        
        Consume();
        return arguments;
    }

    private Expression ParsePrimary()
    {
        var token = Current;

        switch (token.Kind)
        {
            case TokenKind.Num:
                Consume();
                return new LiteralExpression(Value.Num(token.NumberValue), token.Line, token.Column);

            case TokenKind.Str:
                Consume();
                return new LiteralExpression(Value.Str(token.Text), token.Line, token.Column);

            case TokenKind.True:
                Consume();
                return new LiteralExpression(Value.Bool(true), token.Line, token.Column);

            case TokenKind.False:
                Consume();
                return new LiteralExpression(Value.Bool(false), token.Line, token.Column);

            case TokenKind.Ident:
                Consume();
                return new NameExpression(token.Text, token.Line, token.Column);

            case TokenKind.LeftParen:
                Consume();
                var inner = ParseExpression();
                if (Current.Kind != TokenKind.RightParen)
                    throw SyntaxError($"expected ')', found {Describe(Current)}");
                Consume();
                return inner;
            
            default:
                throw SyntaxError($"unexpected {Describe(token)}");
        }
    }
}