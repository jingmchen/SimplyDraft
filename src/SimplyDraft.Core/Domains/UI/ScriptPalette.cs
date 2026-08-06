// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.UI;

public sealed record ScriptPalette(
    string Comment,
    string Str,
    string Number,
    string Keyword,
    string Builtin,
    string Method
);