// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Editor;

public static class PagePreview
{
    // A4 (21 cm) − 2×2 cm margins = 17 cm of text = 642.5 device-independent pixels
    public const double PageWidthDip = 642.5;

    // Textbox / page chrome that sits inside MaxWidth: 2×28 padding + 2×1 border (Inputs.axaml)
    public const double PageChromeDip = 58;
    private const string MonospaceStack = "Cascadia Code,Consolas,Menlo,DejaVu Sans Mono,monospace";

    // Page view - the template export font (Calibri fallback). Text view is monospace instead
    public static string FontFamily(bool pageView, string? docxFont)
        => pageView
            ? (string.IsNullOrWhiteSpace(docxFont) ? "Calibri" : docxFont)
            : MonospaceStack;

    /// Page view - docx points at true screen scale (1 pt = 96/72 DIP). Text view is 12 instead
    public static double FontSize(bool pageView, int? docxSizePt)
        => pageView
            ? DocxPointSize(docxSizePt) * 96.0 / 72.0
            : 12;

    // Page view - the docx text column plus chrome. Text view is unbound instead
    public static double MaxWidth(bool pageView)
        => pageView
            ? PageWidthDip + PageChromeDip
            : double.PositiveInfinity;

    // The effective docx font size in points, defaulting to 11 when unset or invalid
    public static int DocxPointSize(int? docxSizePt)
        => docxSizePt is int pt && pt > 0
            ? pt
            : 11;
}