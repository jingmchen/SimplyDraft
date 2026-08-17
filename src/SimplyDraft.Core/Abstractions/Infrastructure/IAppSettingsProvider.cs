// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface ISettingsProvider<T> where T : class
{
    T Current {get;}
    void Save();
    void Reload();
}