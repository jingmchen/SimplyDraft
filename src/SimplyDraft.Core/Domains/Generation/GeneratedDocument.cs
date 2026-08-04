using SimplyDraft.Core.Domains.Documents;

namespace SimplyDraft.Core.Domains.Generation;

public sealed class GeneratedDocument
{
    public required string Text {get; init;}
    public string? FontName {get; init;}
    public int? FontSizePt {get; init;}
    public bool HasMarkup {get; init;}
    public string? BaseDirectory {get; init;}
    public string? PageHeader {get; init;}

    public static GeneratedDocument From(string text, FrontMatter fm, string? baseDirectory = null)
        => new()
        {
            Text = text,
            FontName = fm.DocxFront,
            FontSizePt = fm.DocxSizePt,
            HasMarkup = fm.HasMarkup,
            PageHeader = fm.DocxHeader,
            BaseDirectory = baseDirectory
        };
}