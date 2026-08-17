// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Configuration.UserStateSettings;

public sealed record UserStateSettings
{
    public TermsSettings Terms {get; set;} = new();
}