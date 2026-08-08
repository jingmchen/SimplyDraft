// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting;

public sealed record ScenarioChoice(
    string Variable,
    IReadOnlyList<string> Options,
    bool HasFallback
);