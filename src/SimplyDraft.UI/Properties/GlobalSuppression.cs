using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "Cold path; runs once at application startup.",
    Scope = "member",
    Target = "~M:SimplyDraft.UI.Services.ThemeService.Initialize")]

[assembly: SuppressMessage("Performance", "CA1848",
    Justification = "Cold path; runs once on unhandled exception crash.",
    Scope = "member",
    Target = "~M:SimplyDraft.UI.App.OnDispatcherUnhandledException(System.Object,Avalonia.Threading.DispatcherUnhandledExceptionEventArgs)")]