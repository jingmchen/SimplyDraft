// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.UI.Inputs;

public sealed record EditorGenerationInput(
    bool IsTemplate,
    string TemplateBody,
    DocInfo Doc,
    MissingVariablePolicy Policy,
    GenerationMode Mode,
    CultureInfo Culture,
    Dictionary<string, string> Types,
    bool ExpandIncludes,
    IReadOnlyCollection<string>? PreviewInputs = null
);