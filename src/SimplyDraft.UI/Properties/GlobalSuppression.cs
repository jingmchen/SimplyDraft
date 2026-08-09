// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "Runs once at application startup.",
    Scope = "member",
    Target = "~M:SimplyDraft.UI.Services.ThemeService.Initialize")]

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "Runs once on unhandled exception crash.",
    Scope = "member",
    Target = "~M:SimplyDraft.UI.App.OnDispatcherUnhandledException(" +
        "System.Object," +
        "Avalonia.Threading.DispatcherUnhandledExceptionEventArgs)")]

[assembly: SuppressMessage("Performance", "CA1873",
    Justification = "Runs once on application startup.",
    Scope = "member",
    Target = "~M:SimplyDraft.UI.Services.StartupTasks.Run()")]

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "Runs once on application startup.",
    Scope = "member",
    Target = "~M:SimplyDraft.UI.Services.StartupTasks.Run()")]

[assembly: SuppressMessage("Performance", "CA1873",
    Justification = "On cold path.",
    Scope = "member",
    Target = "~M:SimplyDraft.UI.Services.ExportService.ExportAsync(" +
        "System.String," +
        "SimplyDraft.Core.Enums.DocumentKind," +
        "SimplyDraft.Core.Domains.Generation.GenerationResult," +
        "SimplyDraft.Core.Domains.Documents.FrontMatter," +
        "System.String)")]

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "On cold path.",
    Scope = "member",
    Target = "~M:SimplyDraft.UI.Services.ExportService.ExportAsync(" +
        "System.String," +
        "SimplyDraft.Core.Enums.DocumentKind," +
        "SimplyDraft.Core.Domains.Generation.GenerationResult," +
        "SimplyDraft.Core.Domains.Documents.FrontMatter," +
        "System.String)")]

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "On cold path.",
    Scope = "member",
    Target = "~M:SimplyDraft.UI.ViewModels.SettingsWindowViewModel.Save()")]