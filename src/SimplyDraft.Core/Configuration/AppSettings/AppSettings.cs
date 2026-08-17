// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Configuration.AppSettings;

public sealed class AppSettings
{
    public LibrarySettings Library {get; set;} = new();
    public EditorSettings Editor {get; set;} = new();
    public GenerationSettings Generation {get; set;} = new();
    public ExportSettings Export {get; set;} = new();
    public ThemeSettings Theme {get; set;} = new();
    public LoggingSettings Logging {get; set;} = new();
}