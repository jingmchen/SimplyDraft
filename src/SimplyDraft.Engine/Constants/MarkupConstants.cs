// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Engine.Constants;

internal static class MarkupConstants
{
    private const string SyntaxStartText = "\\";
    private const string GroupOpenText = "{";
    private const string GroupCloseText = "}";

    internal static class Delimiters
    {
        /// <summary>Escape / command prefix — the '\' that starts every markup command (e.g. <c>\section</c>).</summary>
        public const char SyntaxStart = '\\';

        /// <summary>Comment marker — an unescaped '%' comments out the rest of the line.</summary>
        public const char Comment = '%';

        /// <summary>Group-open brace, delimiting a command argument (e.g. <c>\textbf{…}</c>).</summary>
        public const char GroupOpen = '{';

        /// <summary>Group-close brace.</summary>
        public const char GroupClose = '}';

        /// <summary>Column separator inside a <c>tabular</c> row.</summary>
        public const char CellSeparator = '&';

        /// <summary>Starred-variant suffix — <c>\section*</c> and friends suppress numbering.</summary>
        public const char StarredSuffix = '*';

        /// <summary>Optional-argument open bracket — <c>\item[term]</c>, <c>\includegraphics[opts]{…}</c>.</summary>
        public const char OptionalArgOpen = '[';

        /// <summary>Optional-argument close bracket.</summary>
        public const char OptionalArgClose = ']';

        /// <summary>The soft line-break token <c>\\</c> (two backslashes) inside a paragraph or table row.</summary>
        public const string LineBreak = SyntaxStartText + SyntaxStartText;
    }

    internal static class EscapableChars
    {
        public const char Hash = '#';
        public const char Dollar = '$';
        public const char Underscore = '_';
    }

    internal static class ColumnAlignment
    {
        public const char AlignLeft = 'l';
        public const char AlignCenter = 'c';
        public const char AlignRight = 'r';
    }

    internal static class Formats
    {
        /// <summary>Long date format emitted by <c>\today</c> and <c>\maketitle</c>.</summary>
        public const string DateFormat = "MMMM d, yyyy";
    }

    internal static class Commands
    {
        public const string Section = "section";
        public const string Subsection = "subsection";
        public const string Subsubsection = "subsubsection";
        public const string Paragraph = "paragraph";
        public const string Subparagraph = "subparagraph";
        public const string Title = "title";
        public const string Author = "author";
        public const string Date = "date";
        public const string MakeTitle = "maketitle";
        public const string TableOfContents = "tableofcontents";
        public const string ListOfFigures = "listoffigures";
        public const string Item = "item";
        public const string Caption = "caption";
        public const string IncludeGraphics = "includegraphics";
        public const string Label = "label";
        public const string Ref = "ref";
        public const string Begin = "begin";
        public const string End = "end";
        public const string Input = "input";
        public const string Centering = "centering";
        public const string PageBreak = "pagebreak";
        public const string NewPage = "newpage";
        public const string ClearPage = "clearpage";
        public const string HRule = "hrule";
        public const string HLine = "hline";
        public const string NoIndent = "noindent";
        public const string BigSkip = "bigskip";
        public const string MedSkip = "medskip";
        public const string SmallSkip = "smallskip";
        public const string VSpace = "vspace";
        public const string HSpace = "hspace";
        public const string NewLine = "newline";
        public const string TextBackslash = "textbackslash";
        public const string Today = "today";
        public const string Ldots = "ldots";
        public const string Dots = "dots";
        public const string LaTeX = "LaTeX";
        public const string TeX = "TeX";
        public const string Quad = "quad";
        public const string QQuad = "qquad";
        public const string TextBold = "textbf";
        public const string TextItalic = "textit";
        public const string Emph = "emph";
        public const string TextSlanted = "textsl";
        public const string TextSmallCaps = "textsc";
        public const string Underline = "underline";
        public const string TextTypewriter = "texttt";
    }

    internal static class Environments
    {
        public const string Verbatim = "verbatim";
        public const string Itemize = "itemize";
        public const string Enumerate = "enumerate";
        public const string Description = "description";
        public const string Quote = "quote";
        public const string Center = "center";
        public const string Figure = "figure";
        public const string Tabular = "tabular";
    }

    internal static class Tokens
    {
        // "\begin{"
        private const string BeginBrace = SyntaxStartText + Commands.Begin + GroupOpenText;

        // "\end{"
        private const string EndBrace = SyntaxStartText + Commands.End + GroupOpenText;

        // sectioning & run-in headings (matched by StartsWith)
        public const string Section = SyntaxStartText + Commands.Section;
        public const string Subsection = SyntaxStartText + Commands.Subsection;
        public const string Subsubsection = SyntaxStartText + Commands.Subsubsection;
        public const string Paragraph = SyntaxStartText + Commands.Paragraph;
        public const string Subparagraph = SyntaxStartText + Commands.Subparagraph;

        // title metadata (matched as the token followed by '{')
        public const string Title = SyntaxStartText + Commands.Title;
        public const string Author = SyntaxStartText + Commands.Author;
        public const string Date = SyntaxStartText + Commands.Date;

        // argument commands
        public const string Label = SyntaxStartText + Commands.Label;
        public const string Ref = SyntaxStartText + Commands.Ref;
        public const string Item = SyntaxStartText + Commands.Item;
        public const string Caption = SyntaxStartText + Commands.Caption;
        public const string CaptionOpen = Caption + GroupOpenText;
        public const string IncludeGraphics = SyntaxStartText + Commands.IncludeGraphics;

        // standalone whole-line commands
        public const string Centering = SyntaxStartText + Commands.Centering;
        public const string PageBreak = SyntaxStartText + Commands.PageBreak;
        public const string NewPage = SyntaxStartText + Commands.NewPage;
        public const string ClearPage = SyntaxStartText + Commands.ClearPage;
        public const string HRule = SyntaxStartText + Commands.HRule;
        public const string HLine = SyntaxStartText + Commands.HLine;
        public const string TableOfContents = SyntaxStartText + Commands.TableOfContents;
        public const string ListOfFigures = SyntaxStartText + Commands.ListOfFigures;
        public const string MakeTitle = SyntaxStartText + Commands.MakeTitle;
        public const string BigSkip = SyntaxStartText + Commands.BigSkip;
        public const string MedSkip = SyntaxStartText + Commands.MedSkip;
        public const string SmallSkip = SyntaxStartText + Commands.SmallSkip;
        public const string NoIndent = SyntaxStartText + Commands.NoIndent;

        // environments — \begin{env} / \end{env}
        public const string BeginVerbatim = BeginBrace + Environments.Verbatim + GroupCloseText;
        public const string EndVerbatim = EndBrace + Environments.Verbatim + GroupCloseText;
        public const string BeginItemize = BeginBrace + Environments.Itemize + GroupCloseText;
        public const string EndItemize = EndBrace + Environments.Itemize + GroupCloseText;
        public const string BeginEnumerate = BeginBrace + Environments.Enumerate + GroupCloseText;
        public const string EndEnumerate = EndBrace + Environments.Enumerate + GroupCloseText;
        public const string BeginDescription = BeginBrace + Environments.Description + GroupCloseText;
        public const string EndDescription = EndBrace + Environments.Description + GroupCloseText;
        public const string BeginQuote = BeginBrace + Environments.Quote + GroupCloseText;
        public const string EndQuote = EndBrace + Environments.Quote + GroupCloseText;
        public const string BeginCenter = BeginBrace + Environments.Center + GroupCloseText;
        public const string EndCenter = EndBrace + Environments.Center + GroupCloseText;
        public const string BeginFigure = BeginBrace + Environments.Figure + GroupCloseText;
        public const string EndFigure = EndBrace + Environments.Figure + GroupCloseText;
        public const string BeginTabular = BeginBrace + Environments.Tabular + GroupCloseText;
        public const string EndTabular = EndBrace + Environments.Tabular + GroupCloseText;
    }
}