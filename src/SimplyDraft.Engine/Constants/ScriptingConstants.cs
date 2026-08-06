// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Engine.Constants;

internal static class ScriptingConstants
{
    internal static class Lexical
    {
        /// <summary>Line-comment marker — '#' comments out the rest of a script line.</summary>
        public const char Comment = '#';

        /// <summary>Double-quote string delimiter.</summary>
        public const char DoubleQuote = '"';

        /// <summary>Single-quote string delimiter.</summary>
        public const char SingleQuote = '\'';

        /// <summary>In-string escape lead-in — <c>\n</c>, <c>\t</c>, <c>\"</c>, …</summary>
        public const char StringEscape = '\\';
    }

    internal static class Keywords
    {
        public const string If = "if";
        public const string Elif = "elif";
        public const string Else = "else";
        public const string And = "and";
        public const string Or = "or";
        public const string Not = "not";
        public const string In = "in";
        public const string True = "True";
        public const string False = "False";

        // soft keywords — ordinary identifiers except in statement-leading position
        public const string Match = "match";
        public const string Case = "case";

        /// <summary>Wildcard / discard pattern in a <c>match</c> case.</summary>
        public const string Discard = "_";
    }

    public static readonly IReadOnlySet<string> ReservedWords = new HashSet<string>(StringComparer.Ordinal)
    {
        "None", "as", "assert", "async", "await", "break", "class", "continue", "def", "del",
        "except", "finally", "for", "from", "global", "import", "is", "lambda", "nonlocal",
        "pass", "raise", "return", "try", "while", "with", "yield"
    };

    internal static class Builtins
    {
        public const string System = "system";
        public const string Doc = "doc";

        internal static class SystemMembers
        {
            public const string Now = "now";
            public const string Date = "date";
            public const string Time = "time";
            public const string Year = "year";
            public const string Month = "month";
            public const string Day = "day";
            public const string UserName = "username";
            public const string Machine = "machine";
            public const string Os = "os";
        }

        internal static class DocMembers
        {
            public const string Name = "name";
            public const string Template = "template";
            public const string Created = "created";
            public const string Modified = "modified";
        }
    }

    internal static class Functions
    {
        public const string Len = "len";
        public const string Str = "str";
        public const string Float = "float";
        public const string Format = "format";
    }

    internal static class Methods
    {
        public const string Upper = "upper";
        public const string Lower = "lower";
        public const string Strip = "strip";
        public const string Replace = "replace";
        public const string StartsWith = "startswith";
        public const string EndsWith = "endswith";
        public const string RJust = "rjust";
        public const string LJust = "ljust";
    }

    internal static class Template
    {
        /// <summary>Opens a script block on its own line.</summary>
        public const string ScriptOpen = "#SCRIPT";

        /// <summary>Closes a script block.</summary>
        public const string ScriptClose = "#ENDSCRIPT";

        /// <summary>Placeholder / group open brace — <c>{name}</c>, <c>{system.date}</c>.</summary>
        public const char PlaceholderOpen = '{';

        /// <summary>Placeholder / group close brace.</summary>
        public const char PlaceholderClose = '}';

        /// <summary>The '=' immediately after '{' that marks an inline expression <c>{= … }</c>.</summary>
        public const char ExpressionMarker = '=';

        /// <summary>Namespace / member separator inside a dotted placeholder (<c>system.date</c>).</summary>
        public const char MemberSeparator = '.';
    }

    internal static class Formula
    {
        /// <summary>A raw value starting with '=' is a formula.</summary>
        public const char Prefix = '=';

        /// <summary>A leading apostrophe escapes an otherwise-special leading '=' or apostrophe.</summary>
        public const char EscapeChar = '\'';
    }
    
    internal static class Temporal
    {
        /// <summary>ISO date format (also the date-only member of <see cref="DateTimeFormats"/>).</summary>
        public const string IsoDate = "yyyy-MM-dd";

        public static readonly string[] TimeFormats = { @"h\:mm", @"hh\:mm", @"h\:mm\:ss", @"hh\:mm\:ss" };
        public static readonly string[] DateTimeFormats = { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", IsoDate };
    }
}