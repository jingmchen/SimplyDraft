// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface ILibraryWatcher
{
    event Action? Changed;
    void Rebuild();
}