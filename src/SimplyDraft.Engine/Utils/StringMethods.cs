// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Exceptions;
using SimplyDraft.Engine.Constants;
using SimplyDraft.Engine.Scripting;

namespace SimplyDraft.Engine.Utils;

public static class StringMethods
{
    private const int MaxPadWidth = 10_000;
    private sealed record Method(int Min, int Max, Func<string, Value[], int, int, Value> Impl);

    private static readonly Dictionary<string, Method> Map = new(StringComparer.Ordinal)
    {
        [ScriptingConstants.Methods.Upper] = new(0, 0, (s, a, l, c) => Value.Str(s.ToUpperInvariant())),
        [ScriptingConstants.Methods.Lower] = new(0, 0, (s, a, l, c) => Value.Str(s.ToLowerInvariant())),
        [ScriptingConstants.Methods.Strip] = new(0, 0, (s, a, l, c) => Value.Str(s.Trim())),

        [ScriptingConstants.Methods.Replace] = new(2, 2, (s, a, l, c) =>
        {
            string find = Text(a[0], ScriptingConstants.Methods.Replace, l, c);
            string repl = Text(a[1], ScriptingConstants.Methods.Replace, l, c);
            
            if (find.Length == 0)
                return Value.Str(s);
            
            if (repl.Length > find.Length)
            {
                long projected = s.Length + CountOccurrences(s, find) * (long)(repl.Length - find.Length);
                if (projected > Interpreter.MaxStringLength)
                    throw ScriptException.Error(DiagnosticCode.LimitExceeded,
                        $"replace() would exceed the maximum string length ({Interpreter.MaxStringLength:N0} characters)",
                        l, c);
            }
            return Value.Str(s.Replace(find, repl, StringComparison.Ordinal));
        }),

        [ScriptingConstants.Methods.StartsWith] = new(1, 1, (s, a, l, c)
            => Value.Bool(s.StartsWith(Text(a[0], ScriptingConstants.Methods.StartsWith, l, c), StringComparison.Ordinal))),
        
        [ScriptingConstants.Methods.EndsWith] = new(1, 1, (s, a, l, c)
            => Value.Bool(s.EndsWith(Text(a[0], ScriptingConstants.Methods.EndsWith, l, c), StringComparison.Ordinal))),
        
        [ScriptingConstants.Methods.RJust] = new(1, 2, (s, a, l, c) => Value.Str(s.PadLeft(Width(a[0], ScriptingConstants.Methods.RJust, l, c), Fill(a, ScriptingConstants.Methods.RJust, l, c)))),
        [ScriptingConstants.Methods.LJust] = new(1, 2, (s, a, l, c) => Value.Str(s.PadRight(Width(a[0], ScriptingConstants.Methods.LJust, l, c), Fill(a, ScriptingConstants.Methods.LJust, l, c)))),
    };

    private static string Text(Value v, string method, int line, int column)
        => v.Kind == ValueKind.Str
            ? v.AsString
            : throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"{method}() expects str arguments, got {v.KindName}", line, column);

    private static int Width(Value v, string method, int line, int column)
        => v.Kind == ValueKind.Num
            ? Math.Clamp((int)v.AsNumber, 0, MaxPadWidth)
            : throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"{method}() width must be a number, got {v.KindName}", line, column);

    private static char Fill(Value[] a, string method, int line, int column)
    {
        if (a.Length < 2) return ' ';
        if (a[1].Kind != ValueKind.Str || a[1].AsString.Length != 1)
            throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $"{method}() fill character must be a single-character str", line, column);
        return a[1].AsString[0];
    }

    private static long CountOccurrences(string s, string find)
    {
        long n = 0;
        int idx = 0;
        while ((idx = s.IndexOf(find, idx, StringComparison.Ordinal)) >= 0) { n++; idx += find.Length; }
        return n;
    }

    public static Value Invoke(Value receiver, string name, Value[] args, int line, int column)
    {
        if (!Map.TryGetValue(name, out var method))
        {
            var canonical = Map.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
            throw ScriptException.Error(DiagnosticCode.UnknownFunction,
                canonical != null
                    ? $"unknown method '.{name}()' — did you mean '.{canonical}()'? (names are case-sensitive)"
                    : $"str values have no method '.{name}()' — available: .upper() .lower() .strip() .replace() .startswith() .endswith() .rjust() .ljust()",
                line, column);
        }
        if (receiver.Kind != ValueKind.Str)
            throw ScriptException.Error(DiagnosticCode.TypeMismatch,
                $".{name}() works on str values, got {receiver.KindName} — wrap it with str(…)", line, column);
        if (args.Length < method.Min || args.Length > method.Max)
        {
            string expected = method.Min == method.Max ? method.Min.ToString(CultureInfo.InvariantCulture) : $"{method.Min}-{method.Max}";
            throw ScriptException.Error(DiagnosticCode.WrongArity,
                $".{name}() takes {expected} argument(s), got {args.Length}", line, column);
        }
        return method.Impl(receiver.AsString, args, line, column);
    }
}