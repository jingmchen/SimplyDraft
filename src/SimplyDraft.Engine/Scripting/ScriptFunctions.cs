using System.Globalization;
using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Exceptions;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Scripting;

public static class ScriptFunctions
{
    private sealed record Fn(int Min, int Max, Func<Value[], CultureInfo, int, int, Value> Impl);

    private static readonly Dictionary<string, Fn> Map = new(StringComparer.Ordinal)
    {
        [ScriptingConstants.Functions.Len] = new(1, 1, (a, cu, l, c) => a[0].Kind == ValueKind.Str
            ? Value.Num(a[0].AsString.Length)
            : throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"len() expects a str, got {a[0].KindName}", l, c)),

        [ScriptingConstants.Functions.Str] = new(1, 1, (a, cu, l, c) => Value.Str(a[0].Render())),

        [ScriptingConstants.Functions.Float] = new(1, 1, (a, cu, l, c) => a[0].Kind switch
        {
            ValueKind.Num => a[0],
            ValueKind.Bool => Value.Num(a[0].AsBool ? 1 : 0),
            ValueKind.Str => double.TryParse(a[0].AsString.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                ? Value.Num(d)
                : throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                    $"could not convert str to float: \"{a[0].AsString.Trim()}\"", l, c),
            _ => throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"float() expects a str or number, got {a[0].KindName}", l, c)
        }),

        [ScriptingConstants.Functions.Format] = new(2, 2, (a, cu, l, c) =>
        {
            if (a[1].Kind != ValueKind.Str)
                throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                    $"format() second argument must be a str format, got {a[1].KindName}", l, c);
            string fmt = a[1].AsString;
            try
            {
                return a[0].Kind switch
                {
                    ValueKind.Num => Value.Str(a[0].AsNumber.ToString(fmt, cu)),
                    ValueKind.DateTime or ValueKind.Date => Value.Str(a[0].AsDateTime.ToString(fmt, cu)),
                    ValueKind.Time => Value.Str((new DateTime(2000, 1, 1) + a[0].AsTime).ToString(fmt, cu)),
                    _ => throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                        $"format() supports numbers and date/time values, got {a[0].KindName}", l, c)
                };
            }
            catch (FormatException)
            {
                throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                    $"format() format string \"{fmt}\" is invalid", l, c);
            }
        }),
    };

    /// <summary>Hints for names from the legacy language and near-miss Python idioms.</summary>
    private static readonly Dictionary<string, string> Hints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["upper"] = "string operations are methods: text.upper()",
        ["lower"] = "string operations are methods: text.lower()",
        ["trim"] = "string operations are methods: text.strip()",
        ["strip"] = "string operations are methods: text.strip()",
        ["replace"] = "string operations are methods: text.replace(old, new)",
        ["startswith"] = "string operations are methods: text.startswith(prefix)",
        ["endswith"] = "string operations are methods: text.endswith(suffix)",
        ["contains"] = "membership is an operator: needle in text",
        ["iif"] = "conditionals are expressions: value_if_true if condition else value_if_false",
        ["number"] = "conversion is float(text)",
        ["int"] = "conversion is float(text)",
        ["left"] = "use slicing: text[:n]",
        ["right"] = "use slicing: text[-n:]",
        ["mid"] = "use slicing: text[start:end]",
        ["padleft"] = "padding is a method: text.rjust(width, ch)",
        ["padright"] = "padding is a method: text.ljust(width, ch)",
    };

    public static Value Invoke(string name, Value[] args, CultureInfo formatCulture, int line, int col)
    {
        if (!Map.TryGetValue(name, out var fn))
        {
            var canonical = Map.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (canonical != null)
                throw ScriptException.Error(DiagnosticCode.UnknownFunction,
                    $"unknown function '{name}()' — did you mean '{canonical}()'? (names are case-sensitive)", line, col);
            if (Hints.TryGetValue(name, out var hint))
                throw ScriptException.Error(DiagnosticCode.UnknownFunction,
                    $"unknown function '{name}()' — {hint}", line, col);
            throw ScriptException.Error(DiagnosticCode.UnknownFunction,
                $"unknown function '{name}()' — available: len(), str(), float(), format()", line, col);
        }
        if (args.Length < fn.Min || args.Length > fn.Max)
        {
            string expected = fn.Min == fn.Max ? fn.Min.ToString(CultureInfo.InvariantCulture) : $"{fn.Min}-{fn.Max}";
            throw ScriptException.Error(DiagnosticCode.WrongArity,
                $"{name}() takes {expected} argument(s), got {args.Length}", line, col);
        }
        return fn.Impl(args, formatCulture, line, col);
    }
}