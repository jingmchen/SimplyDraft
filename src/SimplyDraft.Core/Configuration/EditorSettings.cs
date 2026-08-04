namespace SimplyDraft.Core.Configuration;

public sealed record EditorSettings
{
    public bool WordWrap {get; set;} = true;
    public int AutoSaveMinutes {get; set;} = 2;
}