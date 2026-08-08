// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.UI;

namespace SimplyDraft.UI.Common;

internal static class HelpContent
{
    internal static IReadOnlyList<HelpTopic> Markup {get;} =
    [
        new("Getting started",
            "New templates render markup out of the box. Per template, the \"LaTeX markup\" checkbox " +
            "above the preview (or Template ▸ LaTeX markup) switches it on/off.",
            [
                new(@"\command{…}", "Structural commands go at the start of their own line."),
                new(@"\title{IOQ Protocol}", "Set the document title."),
                new(@"\author{J. Tan}   \date{…}", "Optional; \\date defaults to today."),
                new(@"\maketitle", "Print the centered title block."),
            ]),
 
        new("Sections",
            "Auto-numbered 1, 1.1, 1.1.1. Text after a heading on the same line starts the next paragraph.",
            [
                new(@"\section{Title}", "Numbered heading, level 1."),
                new(@"\subsection{Title}", "Level 2."),
                new(@"\subsubsection{Title}", "Level 3."),
                new(@"\section*{Title}", "Unnumbered (the other headings have *-forms too)."),
                new(@"\paragraph{Lead-in}", "Run-in heading: bold lead-in text, paragraph continues."),
                new(@"\tableofcontents", "Insert a table of contents."),
            ],
            Note: "In .docx exports the TOC is a real Word field — press F9 in Word to refresh page numbers."),
 
        new("References",
            null,
            [
                new(@"\label{sec:intro}", "Name the current section/figure — put it after the heading or caption."),
                new(@"\ref{sec:intro}", "Prints its number, e.g. 2.1."),
            ],
            Note: "Forward references work."),
 
        new("Text style",
            "Inline and nestable.",
            [
                new(@"\textbf{bold}", "Bold."),
                new(@"\textit{…}   \emph{…}   \textsl{…}", "Italic."),
                new(@"\underline{u}", "Underline."),
                new(@"\texttt{mono}", "Monospace."),
                new(@"\textsc{Small Caps}", "Small caps."),
                new(@"\\   or   \newline", "Line break inside a paragraph."),
                new(@"\today", "Today's date."),
                new(@"\ldots   \LaTeX   \TeX", "Symbols: … and the two logotypes."),
                new(@"\quad   \qquad", "Wide spaces."),
            ]),
 
        new("Lists",
            null,
            [
                new("\\begin{itemize}\n\\end{itemize}", "• bullets — one \\item per line."),
                new("\\begin{enumerate}\n\\end{enumerate}", "1. 2. 3."),
                new("\\begin{description}\n\\end{description}", "Definition list."),
                new(@"\item[Term] explanation", "Description entry: bold term + text."),
            ]),
 
        new("Tables",
            "The first row is the header.",
            [
                new(@"\begin{tabular}{lcr}", "Start a table; l/c/r = column alignment."),
                new(@"Step & Check & Result \\", "One row — & separates cells, \\\\ ends the row."),
                new(@"\hline", "Decorative — borders are always drawn."),
                new(@"\end{tabular}", "End the table."),
            ]),
 
        new("Figures & images",
            "PNG / JPG. Paths are relative to the document's folder or the library folder.",
            [
                new(@"\includegraphics{logo.png}", "Insert an image."),
                new("\\begin{figure}\n  \\centering\n  \\includegraphics{chart.png}\n  \\caption{Results} \\label{fig:res}\n\\end{figure}",
                    "A figure is numbered when it has a \\caption."),
                new(@"\listoffigures", "Static list of captioned figures."),
            ]),
 
        new("Layout",
            null,
            [
                new("\\begin{center}\n\\end{center}", "Centered lines."),
                new("\\begin{quote}\n\\end{quote}", "Indented block."),
                new("\\begin{verbatim}\n\\end{verbatim}", "Kept exactly as typed."),
                new(@"\hrule", "Horizontal rule."),
                new(@"\pagebreak   \newpage   \clearpage", "Page break (.docx)."),
                new(@"\bigskip   \medskip   \smallskip", "Vertical space (approximated)."),
                new(@"\noindent   \vspace{…}   \hspace{…}", "Accepted (approximated)."),
            ]),
 
        new("Composition",
            null,
            [
                new(@"\input{Other Template}", "Splice that template's content here — by file or display name."),
            ],
            Note: "The spliced template's script is ignored; nesting up to 8 levels deep."),
 
        new("Headers & footers",
            ".docx only — set in the template's front matter.",
            [
                new("docx:\n  header: \"IOQ-2026-001\"", "Right-aligned page header + a centered \"Page X of Y\" footer."),
            ]),
 
        new("Escapes & comments",
            null,
            [
                new(@"\%  \&  \{  \}  \#  \$  \_", "The literal character."),
                new(@"\textbackslash", "A literal backslash."),
                new(@"% comment", "To end of line — never printed."),
            ]),
 
        new("Not supported",
            "By design: math mode ($…$), citations/bibliographies, \\footnote, packages, custom macros.",
            [],
            Note: "Unknown commands are kept as text and reported as W003 warnings."),
    ];
 
    internal static IReadOnlyList<HelpTopic> Script { get; } =
    [
        new("Statements",
            "Blocks are indented with 4 spaces, exactly like Python. In scripts, write bare names — " +
            "{braces} are only for content.",
            [
                new("name = expression", "Assign a variable."),
                new("if cond:", "Branch — cond must be True/False."),
                new("elif cond:   /   else:", "More branches."),
                new("match name:\n    case \"label\": …\n    case _: …", "Pick a branch by exact value; case _ is the fallback."),
                new("# comment", "To end of line."),
            ]),
 
        new("Operators",
            null,
            [
                new("+  -  *  /  %", "Numbers; + also joins str + str."),
                new("==  !=  <  <=  >  >=", "Comparisons (str is case-sensitive)."),
                new("and   or   not", "Boolean logic."),
                new("\"x\" in text   /   not in", "Contains (case-sensitive)."),
                new("a if cond else b", "Inline choice."),
            ]),
 
        new("Values",
            null,
            [
                new("\"text\"   'text'   12   3.5   True   False", "Literals."),
            ],
            Note: "Mixed types don't add — wrap with str(…), e.g. \"n=\" + str(5)."),
 
        new("Built-ins",
            "Read-only.",
            [
                new("system.now  .date  .time\nsystem.year  .month  .day", "Clock and date parts."),
                new("system.username  .machine  .os", "Environment."),
                new("doc.name  .template\ndoc.created  .modified", "The document being generated."),
                new("if system.time > \"18:00\":", "Example."),
            ]),
 
        new("Functions",
            null,
            [
                new("len(s)", "Length of text."),
                new("str(x)", "Convert to text."),
                new("float(s)", "Convert to number."),
                new("format(x, \"0.00\")", ".NET format — numbers, dates."),
            ]),
 
        new("String methods",
            null,
            [
                new("s.upper()   s.lower()   s.strip()", "Case and trim."),
                new("s.replace(old, new)", "Replace every occurrence."),
                new("s.startswith(p)   s.endswith(p)", "Prefix / suffix test."),
                new("s.rjust(width, \"0\")   s.ljust(width)", "Pad to a width."),
            ]),
 
        new("Slicing",
            null,
            [
                new("s[0]", "First char."),
                new("s[1:4]", "Chars 2–4."),
                new("s[:6]", "First six."),
                new("s[-2:]", "Last two."),
            ]),
 
        new("Limits & not supported",
            "By design: for/while loops, def, import, classes, None, f-strings.",
            [],
            Note: "Conditions must be True/False — no truthiness. Limits: 10,000 statements / 2 s per generation."),
    ];
}