// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Configuration;

namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface IAppSettingsProvider
{
    AppSettings Current {get;}
    void Save();
    void Reload();
}