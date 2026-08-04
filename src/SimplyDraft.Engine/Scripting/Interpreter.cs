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
    public static readonly TimeSpan MaxWall = TimeSpan.FromSeconds(2);
    public const int MaxDepth = 256;
    // Caps the length of a single string produced by '+' concatenation / replace(). CPU is bounded
    // (MaxStatements/MaxWall), but memory was not: `s = "A"` then ~28 lines of `s = s + s` is only ~28
    // statements yet each doubling is one allocation the per-statement checks never see, reaching
    // multiple GB (OutOfMemoryException). 1M chars is far beyond any real document value.
    public const int MaxStringLength = 1_000_000;

    private readonly ScriptScope _scope;
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private int _steps;
    private int _depth;

    public Interpreter(ScriptScope scope) { _scope = scope; }

    public void Execute(IReadOnlyList<Statement> stmts)
    {
        foreach (var s in stmts) Exec(s);
    }

    private void Exec(Statement s)
    {
        if (++_steps > MaxStatements || _sw.Elapsed > MaxWall)
            throw ScriptException.Error(DiagnosticCode.LimitExceeded,
                $"script execution limit exceeded ({MaxStatements} statements / {MaxWall.TotalSeconds:0} s)", s.Line, s.Column);
        switch (s)
        {
            case AssignmentStatement assign:
                _scope.Vars[assign.Name] = Eval(assign.Value);
                break;
            
            case IfStatement ifs:
                foreach (var (cond, body) in ifs.Branches)
                {
                    if (cond is null) { Execute(body); break; }
                    var v = Eval(cond);
                    if (v.Kind != ValueKind.Bool)
                        throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                            $"if condition must be True or False, got {v.KindName} — compare explicitly (e.g. name != \"\")",
                            cond.Line, cond.Column);
                    if (v.AsBool) { Execute(body); break; }
                }
                break;
            
            case MatchStatement match:
            {
                var subject = Eval(match.Subject);
                foreach (var c in match.Cases)
                {
                    if (c.Literal is null || LiteralMatches(subject, c.Literal))
                    {
                        Execute(c.Body);
                        break;
                    }
                }
                break;
            }
        }
    }

    /// <summary>Case-pattern equality: same-kind literal comparison (str Ordinal, like ==); kind mismatch never matches.</summary>
    private static bool LiteralMatches(Value subject, Value literal) => (subject.Kind, literal.Kind) switch
    {
        (ValueKind.Str, ValueKind.Str) => string.Equals(subject.AsString, literal.AsString, StringComparison.Ordinal),
        (ValueKind.Num, ValueKind.Num) => subject.AsNumber.Equals(literal.AsNumber),
        (ValueKind.Bool, ValueKind.Bool) => subject.AsBool == literal.AsBool,
        _ => false
    };

    public Value Eval(Expression e)
    {
        // Depth guard: inline {= … } and =-formulas call Eval directly, bypassing Exec's per-statement
        // limits, so a deeply nested expression — or a long operator chain's left spine — could
        // overflow the stack (an uncatchable crash). Throw a catchable LimitExceeded instead.
        if (_depth >= MaxDepth)
            throw ScriptException.Error(DiagnosticCode.LimitExceeded,
                $"expression nested too deeply (limit {MaxDepth})", e.Line, e.Column);
        _depth++;
        try { return EvalCore(e); }
        finally { _depth--; }
    }

    private Value EvalCore(Expression e)
    {
        switch (e)
        {
            case LiteralExpression lit:
                return lit.Value;
            
            case NameExpression name:
                if (_scope.Vars.TryGetValue(name.Name, out var val)) return val;
                // Abstract-template preview: a declared input a child will fill is readable as empty,
                // so a script that READS an input (e.g. `match channel:`) runs instead of aborting.
                // This never masks content placeholders — those resolve against Vars alone.
                if (_scope.InputFallback.TryGetValue(name.Name, out var seed)) return seed;
                if (name.Name.Equals(ScriptingConstants.Builtins.System, StringComparison.OrdinalIgnoreCase) ||
                    name.Name.Equals(ScriptingConstants.Builtins.Doc, StringComparison.OrdinalIgnoreCase))
                    throw ScriptException.Error(DiagnosticCode.UnknownBuiltin,
                        $"'{name.Name.ToLowerInvariant()}' needs a member — e.g. {name.Name.ToLowerInvariant()}.name",
                        name.Line, name.Column);
                throw ScriptException.Error(DiagnosticCode.UndefinedVariable,
                    $"name '{name.Name}' is not defined", name.Line, name.Column);
            case BuiltinRefExpression b:
                return _scope.Builtins.Lookup(b.Namespace, b.Member)
                    ?? throw ScriptException.Error(DiagnosticCode.UnknownBuiltin,
                        $"unknown built-in {b.Namespace}.{b.Member}", b.Line, b.Column);
            case CallExpression f:
            {
                var args = new Value[f.Args.Count];
                for (int i = 0; i < args.Length; i++) args[i] = Eval(f.Args[i]);
                return ScriptFunctions.Invoke(f.Name, args, _scope.FormatCulture, f.Line, f.Column);
            }
            case MethodCallExpression m:
            {
                var receiver = Eval(m.Receiver);
                var args = new Value[m.Args.Count];
                for (int i = 0; i < args.Length; i++) args[i] = Eval(m.Args[i]);
                return StringMethods.Invoke(receiver, m.Name, args, m.Line, m.Column);
            }
            case IndexExpression ix:
                return EvalIndex(ix);
            case SliceExpression sl:
                return EvalSlice(sl);
            case ConditionalExpression cond:
            {
                var c = Eval(cond.Condition);
                if (c.Kind != ValueKind.Bool)
                    throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                        $"the condition in 'a if condition else b' must be True or False, got {c.KindName}",
                        cond.Condition.Line, cond.Condition.Column);
                return c.AsBool ? Eval(cond.Then) : Eval(cond.Else);
            }
            case UnaryExpression u:
                return EvalUnary(u);
            case BinaryExpression bin:
                return EvalBinary(bin);
            default:
                throw ScriptException.Error(DiagnosticCode.SyntaxError, "unsupported expression", e.Line, e.Column);
        }
    }

    private Value EvalIndex(IndexExpression ix)
    {
        var target = Eval(ix.Target);
        if (target.Kind != ValueKind.Str)
            throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"'[…]' works on str values, got {target.KindName}", ix.Line, ix.Column);
        int i = IndexNumber(Eval(ix.Index), ix);
        string s = target.AsString;
        if (i < 0) i += s.Length;
        if (i < 0 || i >= s.Length)
            throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                "string index out of range", ix.Line, ix.Column);
        return Value.Str(s[i].ToString());
    }

    private Value EvalSlice(SliceExpression sl)
    {
        var target = Eval(sl.Target);
        if (target.Kind != ValueKind.Str)
            throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"'[…]' works on str values, got {target.KindName}", sl.Line, sl.Column);
        string s = target.AsString;
        int len = s.Length;
        int start = sl.Start is null ? 0 : IndexNumber(Eval(sl.Start), sl);
        int end = sl.End is null ? len : IndexNumber(Eval(sl.End), sl);
        if (start < 0) start += len;
        if (end < 0) end += len;
        start = Math.Clamp(start, 0, len);
        end = Math.Clamp(end, 0, len);
        return Value.Str(start >= end ? "" : s[start..end]);
    }

    private static int IndexNumber(Value v, Expression at)
        => v.Kind == ValueKind.Num
            ? (int)v.AsNumber
            : throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"string indices must be numbers, got {v.KindName}", at.Line, at.Column);

    private Value EvalUnary(UnaryExpression u)
    {
        var v = Eval(u.Operand);
        if (u.Op == UnaryOperator.Negate)
        {
            if (v.Kind != ValueKind.Num)
                throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                    $"unary '-' requires a number, got {v.KindName}", u.Line, u.Column);
            return Value.Num(-v.AsNumber);
        }
        if (v.Kind != ValueKind.Bool)
            throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"'not' requires True or False, got {v.KindName}", u.Line, u.Column);
        return Value.Bool(!v.AsBool);
    }

    private Value EvalBinary(BinaryExpression b)
    {
        switch (b.Op)
        {
            case BinaryOperator.And:
            {
                var l = RequireBool(Eval(b.Left), ScriptingConstants.Keywords.And, b.Left);
                if (!l) return Value.Bool(false);
                return Value.Bool(RequireBool(Eval(b.Right), ScriptingConstants.Keywords.And, b.Right));
            }
            case BinaryOperator.Or:
            {
                var l = RequireBool(Eval(b.Left), ScriptingConstants.Keywords.Or, b.Left);
                if (l) return Value.Bool(true);
                return Value.Bool(RequireBool(Eval(b.Right), ScriptingConstants.Keywords.Or, b.Right));
            }
            case BinaryOperator.In:
            case BinaryOperator.NotIn:
            {
                var needle = Eval(b.Left);
                var hay = Eval(b.Right);
                if (needle.Kind != ValueKind.Str || hay.Kind != ValueKind.Str)
                    throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                        $"'in' requires str on both sides, got {needle.KindName} and {hay.KindName}", b.Line, b.Column);
                bool contains = hay.AsString.Contains(needle.AsString, StringComparison.Ordinal);
                return Value.Bool(b.Op == BinaryOperator.In ? contains : !contains);
            }
            case BinaryOperator.Add:
            {
                var l = Eval(b.Left);
                var r = Eval(b.Right);
                if (l.Kind == ValueKind.Num && r.Kind == ValueKind.Num)
                    return Value.Num(l.AsNumber + r.AsNumber);
                if (l.Kind == ValueKind.Str && r.Kind == ValueKind.Str)
                {
                    if ((long)l.AsString.Length + r.AsString.Length > MaxStringLength)
                        throw ScriptException.Error(DiagnosticCode.LimitExceeded,
                            $"string exceeds the maximum length ({MaxStringLength:N0} characters)", b.Line, b.Column);
                    return Value.Str(l.AsString + r.AsString);
                }
                if (l.Kind == ValueKind.Str || r.Kind == ValueKind.Str)
                    throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                        $"can only concatenate str to str (got {l.KindName} and {r.KindName}) — wrap values with str(…)",
                        b.Line, b.Column);
                throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                    $"unsupported operand types for '+': {l.KindName} and {r.KindName}", b.Line, b.Column);
            }
            case BinaryOperator.Subtract:
            case BinaryOperator.Multiply:
            case BinaryOperator.Divide:
            case BinaryOperator.Modulo:
            {
                var l = Eval(b.Left);
                var r = Eval(b.Right);
                string sym = b.Op switch { BinaryOperator.Subtract => "-", BinaryOperator.Multiply => "*", BinaryOperator.Divide => "/", _ => "%" };
                if (l.Kind != ValueKind.Num || r.Kind != ValueKind.Num)
                    throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                        $"'{sym}' requires numbers, got {l.KindName} and {r.KindName}", b.Line, b.Column);
                if (b.Op is BinaryOperator.Divide or BinaryOperator.Modulo && r.AsNumber == 0)
                    throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                        "division by zero", b.Line, b.Column);
                return Value.Num(b.Op switch
                {
                    BinaryOperator.Subtract => l.AsNumber - r.AsNumber,
                    BinaryOperator.Multiply => l.AsNumber * r.AsNumber,
                    BinaryOperator.Divide => l.AsNumber / r.AsNumber,
                    _ => PythonMod(l.AsNumber, r.AsNumber)
                });
            }
            default:
                return Compare(b);
        }
    }

    /// <summary>Python's floored modulo: the result has the sign of the divisor.</summary>
    private static double PythonMod(double a, double b) => a - b * Math.Floor(a / b);

    private static bool RequireBool(Value v, string op, Expression at)
        => v.Kind == ValueKind.Bool
            ? v.AsBool
            : throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"'{op}' requires True/False operands, got {v.KindName}", at.Line, at.Column);

    private Value Compare(BinaryExpression b)
    {
        var l = Eval(b.Left);
        var r = Eval(b.Right);
        bool eqOnly = b.Op is BinaryOperator.Equal or BinaryOperator.NotEqual;
        int c;
        if (l.Kind == ValueKind.Num && r.Kind == ValueKind.Num)
            c = l.AsNumber.CompareTo(r.AsNumber);
        else if (l.IsTemporal || r.IsTemporal)
        {
            // ==/!= must never raise on a non-temporal or unparseable operand (Python semantics, and
            // the mixed-type rule below) — only ordering (<, >, …) parses-or-throws.
            if (eqOnly)
            {
                if (!TryCompareTemporal(l, r, out c))
                    return Value.Bool(b.Op == BinaryOperator.NotEqual);
            }
            else
                c = CompareTemporal(l, r, b.Line, b.Column);
        }
        else if (l.Kind == ValueKind.Str && r.Kind == ValueKind.Str)
            c = string.CompareOrdinal(l.AsString, r.AsString);
        else if (l.Kind == ValueKind.Bool && r.Kind == ValueKind.Bool)
        {
            if (!eqOnly)
                throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                    "bool values support only == and !=", b.Line, b.Column);
            c = l.AsBool == r.AsBool ? 0 : 1;
        }
        else
        {
            // mixed types: == is False and != is True (like Python); ordering is an error
            if (eqOnly) return Value.Bool(b.Op == BinaryOperator.NotEqual);
            throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"'{OpSymbol(b.Op)}' is not supported between {l.KindName} and {r.KindName}", b.Line, b.Column);
        }

        bool result = b.Op switch
        {
            BinaryOperator.Equal => c == 0,
            BinaryOperator.NotEqual => c != 0,
            BinaryOperator.Less => c < 0,
            BinaryOperator.LessOrEqual => c <= 0,
            BinaryOperator.Greater => c > 0,
            BinaryOperator.GreaterOrEqual => c >= 0,
            _ => false
        };
        return Value.Bool(result);
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

    /// <summary>Non-throwing temporal comparison for ==/!= — false when either side isn't a parseable temporal.</summary>
    private static bool TryCompareTemporal(Value l, Value r, out int cmp)
    {
        try { cmp = CompareTemporal(l, r, 0, 0); return true; }
        catch (ScriptException) { cmp = 0; return false; }
    }

    /// <summary>Spec §9.4: either side temporal → parse the other side, compare on the timeline.</summary>
    private static int CompareTemporal(Value l, Value r, int line, int col)
    {
        if (l.Kind == ValueKind.Time || r.Kind == ValueKind.Time)
            return ToTime(l, line, col).CompareTo(ToTime(r, line, col));
        return ToDateTime(l, line, col).CompareTo(ToDateTime(r, line, col));
    }

    private static TimeSpan ToTime(Value v, int line, int col) => v.Kind switch
    {
        ValueKind.Time => v.AsTime,
        ValueKind.Str when TimeSpan.TryParseExact(v.AsString.Trim(), ScriptingConstants.Temporal.TimeFormats, CultureInfo.InvariantCulture, out var ts) => ts,
        ValueKind.Str => throw ScriptException.Error(DiagnosticCode.TemporalParse,
            $"\"{v.AsString}\" is not a time — expected HH:mm or HH:mm:ss", line, col),
        _ => throw ScriptException.Error(DiagnosticCode.TypeMismatch,
            $"cannot compare a time with a {v.KindName}", line, col)
    };

    private static DateTime ToDateTime(Value v, int line, int col) => v.Kind switch
    {
        ValueKind.DateTime => v.AsDateTime,
        ValueKind.Date => v.AsDateTime,
        ValueKind.Str when DateTime.TryParseExact(v.AsString.Trim(), ScriptingConstants.Temporal.DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) => dt,
        ValueKind.Str => throw ScriptException.Error(DiagnosticCode.TemporalParse,
            $"\"{v.AsString}\" is not a date/time — expected yyyy-MM-dd [HH:mm[:ss]]", line, col),
        _ => throw ScriptException.Error(DiagnosticCode.TypeMismatch,
            $"cannot compare a date with a {v.KindName}", line, col)
    };
}