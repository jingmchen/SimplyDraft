// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.UI.Inputs;

// Immutable snapshot for generate child window preview/bake needs
public sealed record ChildGenerationInput(
    string Body,
    Dictionary<string, string> Values,
    DocInfo Doc,
    MissingVariablePolicy Policy,
    GenerationMode Mode,
    CultureInfo Culture,
    Dictionary<string, string> Types
);