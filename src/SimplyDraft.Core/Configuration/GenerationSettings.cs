using System.Globalization;
using System.Text.Json.Serialization;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Configuration;

public sealed record GenerationSettings
{
    public MissingVariablePolicy Policy {get; set;} = MissingVariablePolicy.ErrorOnExport;
    public CultureMode FormatCulture {get; set;} = CultureMode.System;

    [JsonIgnore] public CultureInfo Culture =>
        FormatCulture == CultureMode.Invariant
            ? CultureInfo.InvariantCulture
            : CultureInfo.CurrentCulture;
}