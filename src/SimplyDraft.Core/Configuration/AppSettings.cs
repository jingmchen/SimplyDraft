namespace SimplyDraft.Core.Configuration;

public sealed class AppSettings
{
    public LibrarySettings LibrarySection {get; set;} = new();
    public EditorSettings EditorSection {get; set;} = new();
    public GenerationSettings GenerationSection {get; set;} = new();
    public ExportSettings ExportSection {get; set;} = new();
    public ThemeSettings ThemeSection {get; set;} = new();
    public LoggingSettings LoggingSection {get; set;} = new();
}