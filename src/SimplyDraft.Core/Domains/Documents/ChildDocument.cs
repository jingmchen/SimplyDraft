// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Documents;

public sealed class ChildDocument : LibraryDocument
{
    public bool IsBaked => !string.IsNullOrWhiteSpace(Body);
}