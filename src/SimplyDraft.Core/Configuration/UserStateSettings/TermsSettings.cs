// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Configuration.UserStateSettings;

public sealed class TermsSettings
{
    public string? AcceptedTermsHash {get; set;}
    public DateTime? AcceptedAtUtc {get; set;}
    public string? AcceptedBy {get; set;}
}