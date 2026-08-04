namespace SimplyDraft.Core.Domains.Documents;

public sealed record DocInfo(
    string Name,
    string TemplateName,
    DateTime Created,
    DateTime Modified
);