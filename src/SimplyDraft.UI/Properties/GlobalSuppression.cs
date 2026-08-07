// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "Runs once at application startup.",
    Scope = "member",
    Target = "~M:SimplyDraft.UI.Services.ThemeService.Initialize")]

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "Runs once on unhandled exception crash.",
    Scope = "member",
    Target = "~M:SimplyDraft.UI.App.OnDispatcherUnhandledException(System.Object,Avalonia.Threading.DispatcherUnhandledExceptionEventArgs)")]

[assembly: SuppressMessage("Performance", "CA1873",
    Justification = "Evaluation will not be expensive as passed argument is Int32.",
    Scope = "member",
    Target = "~M:SimplyDraft.UI.Services.StartupTasks.Run()")]

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "Runs once on application startup.",
    Scope = "member",
    Target = "~M:SimplyDraft.UI.Services.StartupTasks.Run()")]