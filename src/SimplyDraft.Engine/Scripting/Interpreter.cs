// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Engine.Scripting;

public sealed class Interpreter
{
    public const int MaxStatements = 10_000;
    public static readonly TimeSpan MaxWall = TimeSpan.FromSeconds(2);
    public const int MaxDepth = 256;
    public const int MaxStringLength = 1_000_000;

    private readonly ScriptScope _scope;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private int _statementsExecuted;
    private int _evaluationDepth;

    public Interpreter(ScriptScope scope)
        => _scope = scope ?? throw new ArgumentNullException(nameof(scope));

    public void Execute(IReadOnlyList<Statement> statements)
    {
        ArgumentNullException.ThrowIfNull(statements);

        foreach (var statement in statements)
            ExecuteStatement(statement);
    }

    private void ExecuteStatement(Statement statement)
    {
        if (++_statementsExecuted > MaxStatements || _stopwatch.Elapsed > MaxWall)
            throw ScriptException.Error(
                DiagnosticCode.LimitExceeded,
                $"script execution limit exceeded ({MaxStatements} statements / {MaxWall.TotalSeconds:0} s)",
                statement.Line,
                statement.Column
            );
        
        switch (statement)
        {
            case AssignStatement assignment:
                _scope.Variables[assignment.Name] = Evaluate(assignment.Value);
                break;

            case IfStatement ifStatement:
                ExecuteIf(ifStatement);
                break;

            case MatchStatement matchStatement:
                ExecuteMatch(matchStatement);
                break;
        }
    }

    private void ExecuteIf(IfStatement ifStatement)
    {
        foreach (var (condition, body) in ifStatement.Branches)
        {
            if (condition is null)
            {
                Execute(body);
                return;
            }

            var conditionValue = Evaluate(condition);

            if (conditionValue.Kind != ValueKind.Boolean)
                throw ScriptException.Error(
                    DiagnosticCode.TypeMismatch,
                    $"if condition must be True or False, got {conditionValue.KindName} — compare explicitly (e.g. name != \"\")",
                    condition.Line,
                    condition.Column
                );
            
            if (conditionValue.AsBool)
            {
                Execute(body);
                return;
            }
        }
    }

    private void ExecuteMatch(MatchStatement matchStatement)
    {
        var subject = Evaluate(matchStatement.Subject);

        foreach (var matchCase in matchStatement.Cases)
        {
            if (matchCase.Literal is null || LiteralMatches(subject, matchCase.Literal))
            {
                Execute(matchCase.Body);
                return;
            }
        }
    }

    private static bool LiteralMatches(Value subject, Value literal)
        => (subject.Kind, literal.Kind) switch
        {
            (ValueKind.Str, ValueKind.Str) => string.Equals(subject.AsString, literal.AsString, StringComparison.Ordinal),
            (ValueKind.Num, ValueKind.Num) => subject.AsNumber.Equals(literal.AsNumber),
            (ValueKind.Bool, ValueKind.Bool) => subject.AsBool == literal.AsBool,
            _ => false
        };

    public Value Evaluate(Expression expression)
    {
        if (_evaluationDepth >= MaxDepth)
            throw ScriptException.Error(
                DiagnosticCode.LimitExceeded,
                $"expression nested too deeply (limit {MaxDepth})",
                expression.Line,
                expression.Column
            );
        
        _evaluationDepth++;

        try
        {
            return EvaluateCore(expression);
        }
        finally
        {
            _evaluationDepth--;
        }
    }

    private Value EvaluateCore(Expression expression)
        => expression switch
        {
            LiteralExpression literal => literal.Value,
            NameExpression name => EvaluateName(name),
            BuiltinRefExpression builtinRef => EvaluateBuiltinRef(builtinRef),
            CallExpression call => EvaluateFunctionCall(call),
            MethodCallExpression methodCall => EvaluateMethodCall(methodCall),
            IndexExpression index => EvaluateIndex(index),
            SliceExpression slice => EvaluateSlice(slice),
            ConditionalExpression conditional => EvaluateConditional(conditional),
            UnaryExpression unary => EvaluateUnary(unary),
            BinaryExpression binary => EvaluateBinary(binary),
            _ => throw ScriptException.Error(
                DiagnosticCode.SyntaxError, "unsupported expression", expression.Line, expression.Column
            )
        };

    private Value EvaluateName(NameExpression name)
    {
        if (_scope.Variables.TryGetValue(name.Name, out var value))
            return value;
        
        if (_scope.InputFallback.TryGetValue(name.Name, out var fallback))
            return fallback;
        
        if (name.Name.Equals(ScriptingConstants.Builtins.System, StringComparison.OrdinalIgnoreCase) ||
            name.Name.Equals(ScriptingConstants.Builtins.Doc, StringComparison.OrdinalIgnoreCase))
                throw ScriptException.Error(
                    DiagnosticCode.UnknownBuiltin,
                    $"'{name.Name.ToLowerInvariant()}' needs a member — e.g. {name.Name.ToLowerInvariant()}.name",
                    name.Line, name.Column
                );
        
        throw ScriptException.Error(
            DiagnosticCode.UndefinedVariable,
            $"name '{name.Name}' is not defined",
            name.Line, name.Column
        );
    }

    private Value EvaluateBuiltinRef(BuiltinRefExpression builtinRef)
        => _scope.ResolveBuiltin(builtinRef.Ns, builtinRef.Member)
                ?? throw ScriptException.Error(DiagnosticCode.UnknownBuiltin,
                    $"unknown built-in {builtinRef.Ns}.{builtinRef.Member}", builtinRef.Line, builtinRef.Column
                );

    private Value EvaluateFunctionCall(CallExpression call)
    {
        var arguments = new Value[call.Args.Count];

        for (int index = 0; index < arguments.Length; index++)
            arguments[index] = Evaluate(call.Args[index]);
        
        return ScriptFunctions.Invoke(call.Name, arguments, _scope.FormatCulture, call.Line, call.Column);
    }

    private Value EvaluateMethodCall(MethodCallExpression methodCall)
    {
        var receiver = Evaluate(methodCall.Receiver);
        var arguments = new Value[methodCall.Args.Count];

        for (int index = 0; index < arguments.Length; index++)
            arguments[index] = Evaluate(methodCall.Args[index]);
        
        return StringMethods.Invoke(receiver, methodCall.Name, arguments, methodCall.Line, methodCall.Column);
    }

    private Value EvaluateIndex(IndexExpression indexExpression)
    {
        var target = Evaluate(indexExpression.Target);

        if (target.Kind != ValueKind.String)
            throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                $"'[…]' works on str values, got {target.KindName}",
                indexExpression.Line, indexExpression.Column
            );
        
        int index = IndexNumber(Evaluate(indexExpression.Index), indexExpression);
        string text = target.AsString;

        if (index < 0)
            index += text.Length;
        
        if (index < 0 || index >= text.Length)
            throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                "string index out of range",
                indexExpression.Line, indexExpression.Column
            );
        
        return Value.Str(text[index].ToString());
    }

    private Value EvaluateSlice(SliceExpression sliceExpression)
    {
        var target = Evaluate(sliceExpression.Target);

        if (target.Kind != ValueKind.String)
            throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                $"'[…]' works on str values, got {target.KindName}",
                sliceExpression.Line, sliceExpression.Column
            );
        
        string text = target.AsString;
        int length = text.Length;
        int start = sliceExpression.Start is null ? 0 : IndexNumber(Evaluate(sliceExpression.Start), sliceExpression);
        int end = sliceExpression.End is null ? length : IndexNumber(Evaluate(sliceExpression.End), sliceExpression);
        
        if (start < 0)
            start += length;
        
        if (end < 0)
            end += length;
        
        start = Math.Clamp(start, 0, length);
        end = Math.Clamp(end, 0, length);
        return Value.Str(start >= end ? "" : text[start..end]);
    }

    private static int IndexNumber(Value value, Expression at)
        => value.Kind == ValueKind.Number
            ? (int)value.AsNumber
            : throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                $"string indices must be numbers, got {value.KindName}",
                at.Line, at.Column
            );

    private Value EvaluateConditional(ConditionalExpression conditional)
    {
        var condition = Evaluate(conditional.Condition);

        if (condition.Kind != ValueKind.Boolean)
            throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                $"the condition in 'a if condition else b' must be True or False, got {condition.KindName}",
                conditional.Condition.Line, conditional.Condition.Column
            );
        
        return condition.AsBool ? Evaluate(conditional.Then) : Evaluate(conditional.Else);
    }

    private Value EvaluateUnary(UnaryExpression unary)
    {
        var operand = Evaluate(unary.Operand);

        if (unary.Op == UnOp.Neg)
        {
            if (operand.Kind != ValueKind.Number)
                throw ScriptException.Error(
                    DiagnosticCode.TypeMismatch,
                    $"unary '-' requires a number, got {operand.KindName}",
                    unary.Line, unary.Column
                );
            
            return Value.Num(-operand.AsNumber);
        }

        if (operand.Kind != ValueKind.Boolean)
            throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                $"'not' requires True or False, got {operand.KindName}",
                unary.Line, unary.Column
            );
        
        return Value.Bool(!operand.AsBool);
    }

    private Value EvaluateBinary(BinaryExpression binary)
        => binary.Op switch
        {
            BinaryOperator.And or BinaryOperator.Or => EvaluateShortCircuitLogic(binary),
            BinaryOperator.In or BinaryOperator.NotIn => EvaluateMembership(binary),
            BinaryOperator.Add => EvaluateAddition(binary),
            BinaryOperator.Sub or BinaryOperator.Mul or BinaryOperator.Div or BinaryOperator.Mod => EvaluateArithmetic(binary),
            _ => EvaluateComparison(binary)
        };

    private Value EvaluateShortCircuitLogic(BinaryExpression binary)
    {
        string opName = binary.Op == BinOp.And ? ScriptingConstants.Keywords.And : ScriptingConstants.Keywords.Or;
        bool left = RequireBool(Evaluate(binary.Left), opName, binary.Left);
        if (binary.Op == BinOp.And && !left) return Value.Bool(false);
        if (binary.Op == BinOp.Or && left) return Value.Bool(true);
        return Value.Bool(RequireBool(Evaluate(binary.Right), opName, binary.Right));
    }

    private Value EvaluateMembership(BinaryExpr binary)
    {
        var needle = Evaluate(binary.Left);
        var haystack = Evaluate(binary.Right);
        if (needle.Kind != ValueKind.String || haystack.Kind != ValueKind.String)
            throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"'in' requires str on both sides, got {needle.KindName} and {haystack.KindName}",
                binary.Line, binary.Column);
        bool contains = haystack.AsString.Contains(needle.AsString, StringComparison.Ordinal);
        return Value.Bool(binary.Op == BinOp.In ? contains : !contains);
    }

    private Value EvaluateAddition(BinaryExpr binary)
    {
        var left = Evaluate(binary.Left);
        var right = Evaluate(binary.Right);
        if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number)
            return Value.Num(left.AsNumber + right.AsNumber);
        if (left.Kind == ValueKind.String && right.Kind == ValueKind.String)
        {
            if ((long)left.AsString.Length + right.AsString.Length > MaxStringLength)
                throw ScriptException.Error(DiagnosticCode.LimitExceeded,
                    $"string exceeds the maximum length ({MaxStringLength:N0} characters)", binary.Line, binary.Column);
            return Value.Str(left.AsString + right.AsString);
        }
        if (left.Kind == ValueKind.String || right.Kind == ValueKind.String)
            throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"can only concatenate str to str (got {left.KindName} and {right.KindName}) — wrap values with str(…)",
                binary.Line, binary.Column);
        throw ScriptException.Error(DiagnosticCode.TypeMismatch,
            $"unsupported operand types for '+': {left.KindName} and {right.KindName}", binary.Line, binary.Column);
    }

    private Value EvaluateArithmetic(BinaryExpr binary)
    {
        var left = Evaluate(binary.Left);
        var right = Evaluate(binary.Right);
        string symbol = binary.Op switch { BinOp.Sub => "-", BinOp.Mul => "*", BinOp.Div => "/", _ => "%" };
        if (left.Kind != ValueKind.Number || right.Kind != ValueKind.Number)
            throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"'{symbol}' requires numbers, got {left.KindName} and {right.KindName}", binary.Line, binary.Column);
        if (binary.Op is BinOp.Div or BinOp.Mod && right.AsNumber == 0)
            throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                "division by zero", binary.Line, binary.Column);
        return Value.Num(binary.Op switch
        {
            BinOp.Sub => left.AsNumber - right.AsNumber,
            BinOp.Mul => left.AsNumber * right.AsNumber,
            BinOp.Div => left.AsNumber / right.AsNumber,
            _ => PythonModulo(left.AsNumber, right.AsNumber)
        });
    }

    /// <summary>Python's floored modulo: the result has the sign of the divisor.</summary>
    private static double PythonModulo(double dividend, double divisor)
        => dividend - divisor * Math.Floor(dividend / divisor);

    private static bool RequireBool(Value value, string opName, Expr at)
        => value.Kind == ValueKind.Boolean
            ? value.AsBool
            : throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"'{opName}' requires True/False operands, got {value.KindName}", at.Line, at.Column);

    private Value EvaluateComparison(BinaryExpr binary)
    {
        var left = Evaluate(binary.Left);
        var right = Evaluate(binary.Right);
        bool equalityOnly = binary.Op is BinOp.Eq or BinOp.Neq;
        int comparison;
        if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number)
            comparison = left.AsNumber.CompareTo(right.AsNumber);
        else if (left.IsTemporal || right.IsTemporal)
        {
            // ==/!= must never raise on a non-temporal or unparseable operand (Python semantics, and
            // the mixed-type rule below) — only ordering (<, >, …) parses-or-throws.
            if (equalityOnly)
            {
                if (!TryCompareTemporal(left, right, out comparison))
                    return Value.Bool(binary.Op == BinOp.Neq);
            }
            else
                comparison = CompareTemporal(left, right, binary.Line, binary.Column);
        }
        else if (left.Kind == ValueKind.String && right.Kind == ValueKind.String)
            comparison = string.CompareOrdinal(left.AsString, right.AsString); // case-sensitive, like Python
        else if (left.Kind == ValueKind.Boolean && right.Kind == ValueKind.Boolean)
        {
            if (!equalityOnly)
                throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                    "bool values support only == and !=", binary.Line, binary.Column);
            comparison = left.AsBool == right.AsBool ? 0 : 1;
        }
        else
        {
            // mixed types: == is False and != is True (like Python); ordering is an error
            if (equalityOnly) return Value.Bool(binary.Op == BinOp.Neq);
            throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"'{OpSymbol(binary.Op)}' is not supported between {left.KindName} and {right.KindName}",
                binary.Line, binary.Column);
        }

        return Value.Bool(binary.Op switch
        {
            BinOp.Eq => comparison == 0,
            BinOp.Neq => comparison != 0,
            BinOp.Lt => comparison < 0,
            BinOp.Le => comparison <= 0,
            BinOp.Gt => comparison > 0,
            BinOp.Ge => comparison >= 0,
            _ => false
        });
    }

    private static string OpSymbol(BinOp op) => op switch
    {
        BinOp.Lt => "<",
        BinOp.Le => "<=",
        BinOp.Gt => ">",
        BinOp.Ge => ">=",
        BinOp.Eq => "==",
        BinOp.Neq => "!=",
        _ => op.ToString()
    };

    /// <summary>Non-throwing temporal comparison for ==/!= — false when either side isn't a parseable temporal.</summary>
    private static bool TryCompareTemporal(Value left, Value right, out int comparison)
    {
        try { comparison = CompareTemporal(left, right, 0, 0); return true; }
        catch (ScriptException) { comparison = 0; return false; }
    }

    /// <summary>Spec §9.4: either side temporal → parse the other side, compare on the timeline.</summary>
    private static int CompareTemporal(Value left, Value right, int line, int col)
    {
        if (left.Kind == ValueKind.TimeOfDay || right.Kind == ValueKind.TimeOfDay)
            return ToTime(left, line, col).CompareTo(ToTime(right, line, col));
        return ToDateTime(left, line, col).CompareTo(ToDateTime(right, line, col));
    }

    private static TimeSpan ToTime(Value value, int line, int col) => value.Kind switch
    {
        ValueKind.TimeOfDay => value.AsTime,
        ValueKind.String when TimeSpan.TryParseExact(value.AsString.Trim(),
            ScriptingConstants.Temporal.TimeFormats, CultureInfo.InvariantCulture, out var time) => time,
        ValueKind.String => throw ScriptException.Error(DiagnosticCode.TemporalParse,
            $"\"{value.AsString}\" is not a time — expected HH:mm or HH:mm:ss", line, col),
        _ => throw ScriptException.Error(DiagnosticCode.TypeMismatch,
            $"cannot compare a time with a {value.KindName}", line, col)
    };

    private static DateTime ToDateTime(Value value, int line, int col) => value.Kind switch
    {
        ValueKind.DateTime => value.AsDateTime,
        ValueKind.Date => value.AsDateTime,
        ValueKind.String when DateTime.TryParseExact(value.AsString.Trim(),
            ScriptingConstants.Temporal.DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime) => dateTime,
        ValueKind.String => throw ScriptException.Error(DiagnosticCode.TemporalParse,
            $"\"{value.AsString}\" is not a date/time — expected yyyy-MM-dd [HH:mm[:ss]]", line, col),
        _ => throw ScriptException.Error(DiagnosticCode.TypeMismatch,
            $"cannot compare a date with a {value.KindName}", line, col)
    };
}