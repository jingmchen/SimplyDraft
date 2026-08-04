namespace SimplyDraft.Engine.Constants;

internal static class ScriptingConstants
{
    /// <summary>
    /// Lexer-level characters: the line-comment marker, the two string delimiters, and the in-string escape lead-in.
    /// </summary>
    internal static class Lexical
    {
        /// <summary>Line-comment marker — '#' comments out the rest of a script line.</summary>
        internal const char Comment = '#';

        /// <summary>Double-quote string delimiter.</summary>
        internal const char DoubleQuote = '"';

        /// <summary>Single-quote string delimiter.</summary>
        internal const char SingleQuote = '\'';

        /// <summary>In-string escape lead-in — <c>\n</c>, <c>\t</c>, <c>\"</c>, …</summary>
        internal const char StringEscape = '\\';
    }

    /// <summary>Keywords of the script language (hard keywords plus the <c>match</c>/<c>case</c> soft keywords).</summary>
    internal static class Keywords
    {
        internal const string If = "if";
        internal const string Elif = "elif";
        internal const string Else = "else";
        internal const string And = "and";
        internal const string Or = "or";
        internal const string Not = "not";
        internal const string In = "in";
        internal const string True = "True";
        internal const string False = "False";

        // soft keywords — ordinary identifiers except in statement-leading position
        internal const string Match = "match";
        internal const string Case = "case";

        /// <summary>Wildcard / discard pattern in a <c>match</c> case.</summary>
        internal const string Discard = "_";
    }

    /// <summary>
    /// Real Python keywords that are deliberately unsupported — reserved
    /// so the tokenizer can raise a clear "not supported" diagnostic instead of treating them as identifiers.
    /// </summary>
    internal static readonly IReadOnlySet<string> ReservedWords = new HashSet<string>(StringComparer.Ordinal)
    {
        "None", "as", "assert", "async", "await", "break", "class", "continue", "def", "del",
        "except", "finally", "for", "from", "global", "import", "is", "lambda", "nonlocal",
        "pass", "raise", "return", "try", "while", "with", "yield"
    };

    /// <summary>The <c>system.*</c> and <c>doc.*</c> built-in namespaces and their members.</summary>
    internal static class Builtins
    {
        /// <summary>Canonical (lowercase) namespace names. Lookups are case-insensitive.</summary>
        internal const string System = "system";
        internal const string Doc = "doc";

        internal static class SystemMembers
        {
            internal const string Now = "now";
            internal const string Date = "date";
            internal const string Time = "time";
            internal const string Year = "year";
            internal const string Month = "month";
            internal const string Day = "day";
            internal const string UserName = "username";
            internal const string Machine = "machine";
            internal const string Os = "os";
        }

        internal static class DocMembers
        {
            internal const string Name = "name";
            internal const string Template = "template";
            internal const string Created = "created";
            internal const string Modified = "modified";
        }
    }

    /// <summary>Free functions of the script language.</summary>
    internal static class Functions
    {
        internal const string Len = "len";
        internal const string Str = "str";
        internal const string Float = "float";
        internal const string Format = "format";
    }

    /// <summary>str methods of the script language.</summary>
    internal static class Methods
    {
        internal const string Upper = "upper";
        internal const string Lower = "lower";
        internal const string Strip = "strip";
        internal const string Replace = "replace";
        internal const string StartsWith = "startswith";
        internal const string EndsWith = "endswith";
        internal const string RJust = "rjust";
        internal const string LJust = "ljust";
    }

    /// <summary>Markers that embed scripts / expressions / variables into a template body.</summary>
    internal static class Template
    {
        /// <summary>Opens a script block on its own line.</summary>
        internal const string ScriptOpen = "#SCRIPT";

        /// <summary>Closes a script block.</summary>
        internal const string ScriptClose = "#ENDSCRIPT";

        /// <summary>Placeholder / group open brace — <c>{name}</c>, <c>{system.date}</c>.</summary>
        internal const char PlaceholderOpen = '{';

        /// <summary>Placeholder / group close brace.</summary>
        internal const char PlaceholderClose = '}';

        /// <summary>The '=' immediately after '{' that marks an inline expression <c>{= … }</c>.</summary>
        internal const char ExpressionMarker = '=';

        /// <summary>Namespace / member separator inside a dotted placeholder (<c>system.date</c>).</summary>
        internal const char MemberSeparator = '.';
    }

    /// <summary>The no-code <c>=</c> formula tier (Excel-style values that begin with '=').</summary>
    internal static class Formula
    {
        /// <summary>A raw value starting with '=' is a formula.</summary>
        internal const char Prefix = '=';

        /// <summary>A leading apostrophe escapes an otherwise-special leading '=' or apostrophe.</summary>
        internal const char EscapeChar = '\'';
    }

    /// <summary>Temporal parse formats, shared by the interpreter's comparisons and the formula engine's type check.</summary>
    internal static class Temporal
    {
        /// <summary>ISO date format (also the date-only member of <see cref="DateTimeFormats"/>).</summary>
        internal const string IsoDate = "yyyy-MM-dd";
        internal static readonly string[] TimeFormats = { @"h\:mm", @"hh\:mm", @"h\:mm\:ss", @"hh\:mm\:ss" };
        internal static readonly string[] DateTimeFormats = { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", IsoDate };
    }
}