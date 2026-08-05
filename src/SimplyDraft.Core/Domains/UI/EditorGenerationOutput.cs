// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Generation;

namespace SimplyDraft.Core.Domains.UI;

public sealed record EditorGenerationOutput(
    GenerationResult Result,
    IReadOnlyList<string> ScannedVariables
);