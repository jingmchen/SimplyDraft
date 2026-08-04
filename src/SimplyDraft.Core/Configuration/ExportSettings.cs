using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Configuration;

public sealed record ExportSettings
{
    public DocumentKind DefaultFormat {get; set;} = DocumentKind.Docx;
    public bool TxtBom {get; set;}
    public MissingVariablePolicy Policy {get; set;} = MissingVariablePolicy.ErrorOnExport;
}