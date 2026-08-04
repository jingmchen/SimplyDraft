using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Exporting;

public sealed class ExportOptions
{
    public NewLineMode NewLine {get; init;} = NewLineMode.Platform;
    public bool WriteBom {get; init;}
}