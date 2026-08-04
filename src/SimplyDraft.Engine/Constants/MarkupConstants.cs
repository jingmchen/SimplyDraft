namespace SimplyDraft.Engine.Constants;

internal static class MarkupConstants
{
    private const string SyntaxStartText = "\\";
    private const string GroupOpenText = "{";
    private const string GroupCloseText = "}";

    /// <summary>
    /// Structural characters (and the composed line-break token) that delimit commands, arguments, table cells and rows.
    /// </summary>
    internal static class Delimiters
    {
        /// <summary>Escape / command prefix — the '\' that starts every markup command (e.g. <c>\section</c>).</summary>
        internal const char SyntaxStart = '\\';

        /// <summary>Comment marker — an unescaped '%' comments out the rest of the line.</summary>
        internal const char Comment = '%';

        /// <summary>Group-open brace, delimiting a command argument (e.g. <c>\textbf{…}</c>).</summary>
        internal const char GroupOpen = '{';

        /// <summary>Group-close brace.</summary>
        internal const char GroupClose = '}';

        /// <summary>Column separator inside a <c>tabular</c> row.</summary>
        internal const char CellSeparator = '&';

        /// <summary>Starred-variant suffix — <c>\section*</c> and friends suppress numbering.</summary>
        internal const char StarredSuffix = '*';

        /// <summary>Optional-argument open bracket — <c>\item[term]</c>, <c>\includegraphics[opts]{…}</c>.</summary>
        internal const char OptionalArgOpen = '[';

        /// <summary>Optional-argument close bracket.</summary>
        internal const char OptionalArgClose = ']';

        /// <summary>The soft line-break token <c>\\</c> (two backslashes) inside a paragraph or table row.</summary>
        internal const string LineBreak = SyntaxStartText + SyntaxStartText;
    }

    /// <summary>
    /// Characters that carry no active markup role and are recognised only so their backslash-escaped
    /// forms (<c>\#</c>, <c>\$</c>, <c>\_</c>) can be unescaped back to the literal character.
    /// The inline parser consults these when unescaping.
    /// </summary>
    internal static class EscapableChars
    {
        internal const char Hash = '#';
        internal const char Dollar = '$';
        internal const char Underscore = '_';
    }

    /// <summary>
    /// Tabular column-alignment specifiers. Shared with <c>TxtMarkupRenderer</c>, which pads cells to the same specifiers.
    /// </summary>
    internal static class ColumnAlignment
    {
        internal const char AlignLeft = 'l';
        internal const char AlignCenter = 'c';
        internal const char AlignRight = 'r';
    }

    /// <summary>.NET format strings applied when rendering generated values.</summary>
    internal static class Formats
    {
        /// <summary>Long date format emitted by <c>\today</c> and <c>\maketitle</c>.</summary>
        internal const string DateFormat = "MMMM d, yyyy";
    }

    /// <summary>Command names, without the leading '\'. These are the vocabulary the inline parser matches.</summary>
    internal static class Commands
    {
        internal const string Section = "section";
        internal const string Subsection = "subsection";
        internal const string Subsubsection = "subsubsection";
        internal const string Paragraph = "paragraph";
        internal const string Subparagraph = "subparagraph";
        internal const string Title = "title";
        internal const string Author = "author";
        internal const string Date = "date";
        internal const string MakeTitle = "maketitle";
        internal const string TableOfContents = "tableofcontents";
        internal const string ListOfFigures = "listoffigures";
        internal const string Item = "item";
        internal const string Caption = "caption";
        internal const string IncludeGraphics = "includegraphics";
        internal const string Label = "label";
        internal const string Ref = "ref";
        internal const string Begin = "begin";
        internal const string End = "end";
        internal const string Input = "input";
        internal const string Centering = "centering";
        internal const string PageBreak = "pagebreak";
        internal const string NewPage = "newpage";
        internal const string ClearPage = "clearpage";
        internal const string HRule = "hrule";
        internal const string HLine = "hline";
        internal const string NoIndent = "noindent";
        internal const string BigSkip = "bigskip";
        internal const string MedSkip = "medskip";
        internal const string SmallSkip = "smallskip";
        internal const string VSpace = "vspace";
        internal const string HSpace = "hspace";
        internal const string NewLine = "newline";
        internal const string TextBackslash = "textbackslash";
        internal const string Today = "today";
        internal const string Ldots = "ldots";
        internal const string Dots = "dots";
        internal const string LaTeX = "LaTeX";
        internal const string TeX = "TeX";
        internal const string Quad = "quad";
        internal const string QQuad = "qquad";
        internal const string TextBold = "textbf";
        internal const string TextItalic = "textit";
        internal const string Emph = "emph";
        internal const string TextSlanted = "textsl";
        internal const string TextSmallCaps = "textsc";
        internal const string Underline = "underline";
        internal const string TextTypewriter = "texttt";
    }

    /// <summary>Environment names used inside <c>\begin{…}</c> / <c>\end{…}</c>.</summary>
    internal static class Environments
    {
        internal const string Verbatim = "verbatim";
        internal const string Itemize = "itemize";
        internal const string Enumerate = "enumerate";
        internal const string Description = "description";
        internal const string Quote = "quote";
        internal const string Center = "center";
        internal const string Figure = "figure";
        internal const string Tabular = "tabular";
    }

    /// <summary>
    /// Whole-line and prefix tokens the line dispatcher matches, composed from the parts above so the '\',
    /// the command name and the braces all trace back to one definition.
    /// </summary>
    internal static class Tokens
    {
        private const string BeginBrace = SyntaxStartText + Commands.Begin + GroupOpenText; // "\begin{"
        private const string EndBrace = SyntaxStartText + Commands.End + GroupOpenText;      // "\end{"

        // sectioning & run-in headings (matched by StartsWith)
        internal const string Section = SyntaxStartText + Commands.Section;
        internal const string Subsection = SyntaxStartText + Commands.Subsection;
        internal const string Subsubsection = SyntaxStartText + Commands.Subsubsection;
        internal const string Paragraph = SyntaxStartText + Commands.Paragraph;
        internal const string Subparagraph = SyntaxStartText + Commands.Subparagraph;

        // title metadata (matched as the token followed by '{')
        internal const string Title = SyntaxStartText + Commands.Title;
        internal const string Author = SyntaxStartText + Commands.Author;
        internal const string Date = SyntaxStartText + Commands.Date;

        // argument commands
        internal const string Label = SyntaxStartText + Commands.Label;
        internal const string Ref = SyntaxStartText + Commands.Ref;
        internal const string Item = SyntaxStartText + Commands.Item;
        internal const string Caption = SyntaxStartText + Commands.Caption;
        internal const string CaptionOpen = Caption + GroupOpenText;
        internal const string IncludeGraphics = SyntaxStartText + Commands.IncludeGraphics;

        // standalone whole-line commands
        internal const string Centering = SyntaxStartText + Commands.Centering;
        internal const string PageBreak = SyntaxStartText + Commands.PageBreak;
        internal const string NewPage = SyntaxStartText + Commands.NewPage;
        internal const string ClearPage = SyntaxStartText + Commands.ClearPage;
        internal const string HRule = SyntaxStartText + Commands.HRule;
        internal const string HLine = SyntaxStartText + Commands.HLine;
        internal const string TableOfContents = SyntaxStartText + Commands.TableOfContents;
        internal const string ListOfFigures = SyntaxStartText + Commands.ListOfFigures;
        internal const string MakeTitle = SyntaxStartText + Commands.MakeTitle;
        internal const string BigSkip = SyntaxStartText + Commands.BigSkip;
        internal const string MedSkip = SyntaxStartText + Commands.MedSkip;
        internal const string SmallSkip = SyntaxStartText + Commands.SmallSkip;
        internal const string NoIndent = SyntaxStartText + Commands.NoIndent;

        // environments — \begin{env} / \end{env}
        internal const string BeginVerbatim = BeginBrace + Environments.Verbatim + GroupCloseText;
        internal const string EndVerbatim = EndBrace + Environments.Verbatim + GroupCloseText;
        internal const string BeginItemize = BeginBrace + Environments.Itemize + GroupCloseText;
        internal const string EndItemize = EndBrace + Environments.Itemize + GroupCloseText;
        internal const string BeginEnumerate = BeginBrace + Environments.Enumerate + GroupCloseText;
        internal const string EndEnumerate = EndBrace + Environments.Enumerate + GroupCloseText;
        internal const string BeginDescription = BeginBrace + Environments.Description + GroupCloseText;
        internal const string EndDescription = EndBrace + Environments.Description + GroupCloseText;
        internal const string BeginQuote = BeginBrace + Environments.Quote + GroupCloseText;
        internal const string EndQuote = EndBrace + Environments.Quote + GroupCloseText;
        internal const string BeginCenter = BeginBrace + Environments.Center + GroupCloseText;
        internal const string EndCenter = EndBrace + Environments.Center + GroupCloseText;
        internal const string BeginFigure = BeginBrace + Environments.Figure + GroupCloseText;
        internal const string EndFigure = EndBrace + Environments.Figure + GroupCloseText;
        internal const string BeginTabular = BeginBrace + Environments.Tabular + GroupCloseText;
        internal const string EndTabular = EndBrace + Environments.Tabular + GroupCloseText;
    }
}