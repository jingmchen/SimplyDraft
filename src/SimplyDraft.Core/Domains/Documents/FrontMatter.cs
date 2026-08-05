// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Documents;

public sealed class FrontMatter
{
    public string? Name {get; set;}
    public string? Description {get; set;}
    public string? TemplatePath {get; set;}
    public bool HasMarkup {get; set;}
    public string? DocxHeader {get; set;}
    public string? DocxFont {get; set;}
    public int? DocxSizePt {get; set;}
    public Dictionary<string, string> Variables {get;} = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Values {get;} = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Types {get;} = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Extras {get;} = [];
}