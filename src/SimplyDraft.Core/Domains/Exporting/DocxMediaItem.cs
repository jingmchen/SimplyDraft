// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Exporting;

public sealed record DocxMediaItem(
    string RelId,
    string FileName,
    byte[] Data,
    string ContentType
);