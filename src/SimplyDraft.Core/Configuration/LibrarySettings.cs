namespace SimplyDraft.Core.Configuration;

public sealed record LibrarySettings
{
    public int TrashPurgeDays {get; set;} = 7;
}