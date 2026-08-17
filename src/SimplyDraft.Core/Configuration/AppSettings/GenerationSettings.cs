// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using System.Text.Json.Serialization;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Configuration.AppSettings;

public sealed class GenerationSettings
{
    public MissingVariablePolicy Policy {get; set;} = MissingVariablePolicy.ErrorOnExport;
    public CultureMode FormatCulture {get; set;} = CultureMode.System;

    [JsonIgnore]
    public CultureInfo Culture =>
        FormatCulture == CultureMode.Invariant
            ? CultureInfo.InvariantCulture
            : CultureInfo.CurrentCulture;
}