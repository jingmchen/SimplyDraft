// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Diagnostics;
using System.Globalization;
using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Core.Domains.Scripting.Expressions;
using SimplyDraft.Core.Domains.Scripting.Statements;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Exceptions;
using SimplyDraft.Engine.Constants;
using SimplyDraft.Engine.Utils;

namespace SimplyDraft.Engine.Scripting;

public sealed class Interpreter
{
    public const int MaxStatements = 10_000;
    public const int MaxDepth = 256;
    public const int MaxStringLength = 1_000_000;
    public static readonly TimeSpan MaxWall = TimeSpan.FromSeconds(2);
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

    public Value Evaluate(Expression expression)
    {
        if (_evaluationDepth >= MaxDepth)
            throw ScriptException.Error(
                DiagnosticCode.LimitExceeded,
                $"expression nested too deeply (limit {MaxDepth})",
                expression.Line, expression.Column
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

    private void ExecuteStatement(Statement statement)
    {
        if (++_statementsExecuted > MaxStatements || _stopwatch.Elapsed > MaxWall)
            throw ScriptException.Error(
                DiagnosticCode.LimitExceeded,
                $"script execution limit exceeded ({MaxStatements} statements / {MaxWall.TotalSeconds:0} s)",
                statement.Line, statement.Column
            );
        
        switch (statement)
        {
            case AssignmentStatement assignment:
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

            if (conditionValue.Kind != ValueKind.Bool)
                throw ScriptException.Error(
                    DiagnosticCode.TypeMismatch,
                    $"if condition must be True or False, got {conditionValue.KindName} — compare explicitly (e.g. name != \"\")",
                    condition.Line, condition.Column
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

    private static bool LiteralMatches(Value subject, Value literal) => (subject.Kind, literal.Kind) switch
    {
        (ValueKind.Str, ValueKind.Str) => string.Equals(subject.AsString, literal.AsString, StringComparison.Ordinal),
        (ValueKind.Num, ValueKind.Num) => subject.AsNumber.Equals(literal.AsNumber),
        (ValueKind.Bool, ValueKind.Bool) => subject.AsBool == literal.AsBool,
        _ => false
    };

    private Value EvaluateCore(Expression expression) => expression switch
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
        _ => throw ScriptException.Error(DiagnosticCode.SyntaxError, "unsupported expression", expression.Line, expression.Column)
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
            DiagnosticCode.UndefinedVariable, $"name '{name.Name}' is not defined", name.Line, name.Column
        );
    }

    private Value EvaluateBuiltinRef(BuiltinRefExpression builtinRef)
        => _scope.ResolveBuiltin(builtinRef.Namespace, builtinRef.Member)
           ?? throw ScriptException.Error(
                DiagnosticCode.UnknownBuiltin,
                $"unknown built-in {builtinRef.Namespace}.{builtinRef.Member}",
                builtinRef.Line, builtinRef.Column
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

        if (target.Kind != ValueKind.Str)
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

        if (target.Kind != ValueKind.Str)
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
        => value.Kind == ValueKind.Num
            ? (int)value.AsNumber
            : throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                $"string indices must be numbers, got {value.KindName}",
                at.Line, at.Column
            );

    private Value EvaluateConditional(ConditionalExpression conditional)
    {
        var condition = Evaluate(conditional.Condition);

        if (condition.Kind != ValueKind.Bool)
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

        if (unary.Op == UnaryOperator.Negate)
        {
            if (operand.Kind != ValueKind.Num)
                throw ScriptException.Error(
                    DiagnosticCode.TypeMismatch,
                    $"unary '-' requires a number, got {operand.KindName}",
                    unary.Line, unary.Column
                );
            
            return Value.Num(-operand.AsNumber);
        }
        if (operand.Kind != ValueKind.Bool)
            throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                $"'not' requires True or False, got {operand.KindName}",
                unary.Line, unary.Column
            );
        
        return Value.Bool(!operand.AsBool);
    }

    private Value EvaluateBinary(BinaryExpression binary) => binary.Op switch
    {
        BinaryOperator.And or BinaryOperator.Or => EvaluateShortCircuitLogic(binary),
        BinaryOperator.In or BinaryOperator.NotIn => EvaluateMembership(binary),
        BinaryOperator.Add => EvaluateAddition(binary),
        BinaryOperator.Subtract or BinaryOperator.Multiply or BinaryOperator.Divide or BinaryOperator.Modulo => EvaluateArithmetic(binary),
        _ => EvaluateComparison(binary)
    };

    private Value EvaluateShortCircuitLogic(BinaryExpression binary)
    {
        string opName = binary.Op == BinaryOperator.And ? ScriptingConstants.Keywords.And : ScriptingConstants.Keywords.Or;
        bool left = RequireBool(Evaluate(binary.Left), opName, binary.Left);

        if (binary.Op == BinaryOperator.And && !left)
            return Value.Bool(false);
        
        if (binary.Op == BinaryOperator.Or && left)
            return Value.Bool(true);
        
        return Value.Bool(RequireBool(Evaluate(binary.Right), opName, binary.Right));
    }

    private Value EvaluateMembership(BinaryExpression binary)
    {
        var needle = Evaluate(binary.Left);
        var haystack = Evaluate(binary.Right);

        if (needle.Kind != ValueKind.Str || haystack.Kind != ValueKind.Str)
            throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                $"'in' requires str on both sides, got {needle.KindName} and {haystack.KindName}",
                binary.Line, binary.Column
            );
        
        bool contains = haystack.AsString.Contains(needle.AsString, StringComparison.Ordinal);

        return Value.Bool(binary.Op == BinaryOperator.In ? contains : !contains);
    }

    private Value EvaluateAddition(BinaryExpression binary)
    {
        var left = Evaluate(binary.Left);
        var right = Evaluate(binary.Right);

        if (left.Kind == ValueKind.Num && right.Kind == ValueKind.Num)
            return Value.Num(left.AsNumber + right.AsNumber);
        
        if (left.Kind == ValueKind.Str && right.Kind == ValueKind.Str)
        {
            if ((long)left.AsString.Length + right.AsString.Length > MaxStringLength)
                throw ScriptException.Error(
                    DiagnosticCode.LimitExceeded,
                    $"string exceeds the maximum length ({MaxStringLength:N0} characters)",
                    binary.Line, binary.Column
                );
            
            return Value.Str(left.AsString + right.AsString);
        }
        if (left.Kind == ValueKind.Str || right.Kind == ValueKind.Str)
            throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                $"can only concatenate str to str (got {left.KindName} and {right.KindName}) — wrap values with str(…)",
                binary.Line, binary.Column
            );
        
        throw ScriptException.Error(
            DiagnosticCode.TypeMismatch,
            $"unsupported operand types for '+': {left.KindName} and {right.KindName}",
            binary.Line, binary.Column
        );
    }

    private Value EvaluateArithmetic(BinaryExpression binary)
    {
        var left = Evaluate(binary.Left);
        var right = Evaluate(binary.Right);

        string symbol = binary.Op switch
        {
            BinaryOperator.Subtract => "-",
            BinaryOperator.Multiply => "*",
            BinaryOperator.Divide => "/", _ => "%"
        };
        
        if (left.Kind != ValueKind.Num || right.Kind != ValueKind.Num)
            throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                $"'{symbol}' requires numbers, got {left.KindName} and {right.KindName}",
                binary.Line, binary.Column
            );
        
        if (binary.Op is BinaryOperator.Divide or BinaryOperator.Modulo && right.AsNumber == 0)
            throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                "division by zero",
                binary.Line, binary.Column
            );
        
        return Value.Num(binary.Op switch
        {
            BinaryOperator.Subtract => left.AsNumber - right.AsNumber,
            BinaryOperator.Multiply => left.AsNumber * right.AsNumber,
            BinaryOperator.Divide => left.AsNumber / right.AsNumber,
            _ => PythonModulo(left.AsNumber, right.AsNumber)
        });
    }

    private static double PythonModulo(double dividend, double divisor)
        => dividend - divisor * Math.Floor(dividend / divisor);

    private static bool RequireBool(Value value, string opName, Expression at)
        => value.Kind == ValueKind.Bool
            ? value.AsBool
            : throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                $"'{opName}' requires True/False operands, got {value.KindName}",
                at.Line, at.Column
            );

    private Value EvaluateComparison(BinaryExpression binary)
    {
        var left = Evaluate(binary.Left);
        var right = Evaluate(binary.Right);
        bool equalityOnly = binary.Op is BinaryOperator.Equal or BinaryOperator.NotEqual;
        int comparison;

        if (left.Kind == ValueKind.Num && right.Kind == ValueKind.Num)
        {
            comparison = left.AsNumber.CompareTo(right.AsNumber);
        }
        else if (left.IsTemporal || right.IsTemporal)
        {
            if (equalityOnly)
            {
                if (!TryCompareTemporal(left, right, out comparison))
                    return Value.Bool(binary.Op == BinaryOperator.NotEqual);
            }
            else
            {
                comparison = CompareTemporal(left, right, binary.Line, binary.Column);
            }
        }
        else if (left.Kind == ValueKind.Str && right.Kind == ValueKind.Str)
        {
            comparison = string.CompareOrdinal(left.AsString, right.AsString);
        }
        else if (left.Kind == ValueKind.Bool && right.Kind == ValueKind.Bool)
        {
            if (!equalityOnly)
                throw ScriptException.Error(
                    DiagnosticCode.TypeMismatch,
                    "bool values support only == and !=",
                    binary.Line, binary.Column
                );
            
            comparison = left.AsBool == right.AsBool ? 0 : 1;
        }
        else
        {
            if (equalityOnly)
                return Value.Bool(binary.Op == BinaryOperator.NotEqual);
            
            throw ScriptException.Error(
                DiagnosticCode.TypeMismatch,
                $"'{OpSymbol(binary.Op)}' is not supported between {left.KindName} and {right.KindName}",
                binary.Line, binary.Column
            );
        }

        return Value.Bool(binary.Op switch
        {
            BinaryOperator.Equal => comparison == 0,
            BinaryOperator.NotEqual => comparison != 0,
            BinaryOperator.Less => comparison < 0,
            BinaryOperator.LessOrEqual => comparison <= 0,
            BinaryOperator.Greater => comparison > 0,
            BinaryOperator.GreaterOrEqual => comparison >= 0,
            _ => false
        });
    }

    private static string OpSymbol(BinaryOperator op) => op switch
    {
        BinaryOperator.Less => "<",
        BinaryOperator.LessOrEqual => "<=",
        BinaryOperator.Greater => ">",
        BinaryOperator.GreaterOrEqual => ">=",
        BinaryOperator.Equal => "==",
        BinaryOperator.NotEqual => "!=",
        _ => op.ToString()
    };

    private static bool TryCompareTemporal(Value left, Value right, out int comparison)
    {
        try {
            comparison = CompareTemporal(left, right, 0, 0);
            return true;
        }
        catch (ScriptException)
        {
            comparison = 0;
            return false;
        }
    }

    private static int CompareTemporal(Value left, Value right, int line, int col)
    {
        if (left.Kind == ValueKind.Time || right.Kind == ValueKind.Time)
            return ToTime(left, line, col).CompareTo(ToTime(right, line, col));
        
        return ToDateTime(left, line, col).CompareTo(ToDateTime(right, line, col));
    }

    private static TimeSpan ToTime(Value value, int line, int col) => value.Kind switch
    {
        ValueKind.Time => value.AsTime,

        ValueKind.Str when TimeSpan.TryParseExact(
            value.AsString.Trim(), ScriptingConstants.Temporal.TimeFormats, CultureInfo.InvariantCulture, out var time
        ) => time,

        ValueKind.Str => throw ScriptException.Error(
            DiagnosticCode.TemporalParse, $"\"{value.AsString}\" is not a time — expected HH:mm or HH:mm:ss", line, col
        ),

        _ => throw ScriptException.Error(DiagnosticCode.TypeMismatch, $"cannot compare a time with a {value.KindName}", line, col)
    };

    private static DateTime ToDateTime(Value value, int line, int col) => value.Kind switch
    {
        ValueKind.DateTime => value.AsDateTime,

        ValueKind.Date => value.AsDateTime,

        ValueKind.Str when DateTime.TryParseExact(
            value.AsString.Trim(), ScriptingConstants.Temporal.DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime
        ) => dateTime,
        
        ValueKind.Str => throw ScriptException.Error(
            DiagnosticCode.TemporalParse,
            $"\"{value.AsString}\" is not a date/time — expected yyyy-MM-dd [HH:mm[:ss]]",
            line, col
        ),

        _ => throw ScriptException.Error(
            DiagnosticCode.TypeMismatch,
            $"cannot compare a date with a {value.KindName}",
            line, col
        )
    };
}