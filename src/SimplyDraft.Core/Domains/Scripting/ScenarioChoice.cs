namespace SimplyDraft.Core.Domains.Scripting;

public sealed record ScenarioChoice(
    string Variable,
    IReadOnlyList<string> Options,
    bool HasFallback
);