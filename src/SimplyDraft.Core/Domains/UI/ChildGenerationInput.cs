// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.UI;

/// <summary>
/// Immutable snapshot of everything the generate-child window's preview/bake needs, taken on the
/// UI thread and handed to a worker — so the computation never reads live view-model state.
/// </summary>
public sealed record ChildGenerationInput(
    string Body,
    Dictionary<string, string> Values,
    DocInfo Doc,
    MissingVariablePolicy Policy,
    GenerationMode Mode,
    CultureInfo Culture,
    Dictionary<string, string> Types
);