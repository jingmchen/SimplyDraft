// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting.Completion;

public static class ScriptCompletionCatalog
{    
    // Identifier-triggered list: template's variables (top priority), keywords, functions, and the system/doc roots
    public static List<CompletionItem> General(IReadOnlyList<string> variableNames)
    {
        var entries = new List<CompletionItem>();

        foreach (var name in variableNames.Distinct(StringComparer.OrdinalIgnoreCase))
            entries.Add(new CompletionItem(name, name, "template variable", Priority: 2));
        
        entries.AddRange(Keywords);
        entries.AddRange(Functions);
        entries.Add(new CompletionItem("system", "system", "built-in values — type '.' for members", Priority: 1));
        entries.Add(new CompletionItem("doc", "doc", "document values — type '.' for members", Priority: 1));
        
        return entries;
    }

    public static readonly CompletionItem[] Keywords =
    {
        new("if", "if", "if condition:  — condition must be True/False"),
        new("elif", "elif", "elif condition:"),
        new("else", "else:", "else:"),
        new("match", "match", "match name:  — then case \"label\": clauses"),
        new("case", "case", "case \"label\":  or  case _:  (fallback)"),
        new("and", "and", "both sides True"),
        new("or", "or", "either side True"),
        new("not", "not", "negation"),
        new("in", "in", "\"needle\" in text — contains (case-sensitive)"),
        new("True", "True", "boolean true"),
        new("False", "False", "boolean false"),
    };

    public static readonly CompletionItem[] Functions =
    {
        Fn("len", "len()", "len(text) — length"),
        Fn("str", "str()", "str(value) — convert to text"),
        Fn("float", "float()", "float(text) — convert to number"),
        Fn("format", "format(, \"0.00\")", "format(value, fmt) — .NET format string"),
    };

    public static readonly CompletionItem[] SystemMembers =
    {
        new("now", "now", "current date + time"),
        new("date", "date", "current date"),
        new("time", "time", "current time of day"),
        new("year", "year", "current year (number)"),
        new("month", "month", "current month (number)"),
        new("day", "day", "current day (number)"),
        new("username", "username", "OS user name"),
        new("machine", "machine", "machine name"),
        new("os", "os", "Windows / macOS / Linux"),
    };

    public static readonly CompletionItem[] DocMembers =
    {
        new("name", "name", "this document's name"),
        new("template", "template", "source template's name"),
        new("created", "created", "file creation time"),
        new("modified", "modified", "file last-write time"),
    };

    public static readonly CompletionItem[] StringMethods =
    {
        Fn("upper", "upper()", ".upper() — UPPERCASE"),
        Fn("lower", "lower()", ".lower() — lowercase"),
        Fn("strip", "strip()", ".strip() — trim spaces"),
        Fn("replace", "replace(, )", ".replace(old, new)"),
        Fn("startswith", "startswith(\"\")", ".startswith(prefix) — case-sensitive"),
        Fn("endswith", "endswith(\"\")", ".endswith(suffix) — case-sensitive"),
        Fn("rjust", "rjust(2, \"0\")", ".rjust(width, fill) — pad left"),
        Fn("ljust", "ljust(2, \" \")", ".ljust(width, fill) — pad right"),
    };
     
    private static CompletionItem Fn(string text, string insertText, string description)
        => new(text, insertText, description, CaretBack: insertText.Length - insertText.IndexOf('(') - 1);
}
