// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.UI;

public sealed record EditorGenerationInput(
    bool IsTemplate,
    string TemplateBody,
    Dictionary<string, string> Defaults,
    Dictionary<string, string>? ChildValues,
    DocInfo Doc,
    MissingVariablePolicy Policy,
    GenerationMode Mode,
    CultureInfo Culture,
    bool Broken,
    Dictionary<string, string> Types,
    bool ExpandIncludes,
    IReadOnlyCollection<string>? PreviewInputs = null
);