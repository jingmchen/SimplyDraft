// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;

namespace SimplyDraft.Core.Abstractions.UI;

public interface IConsoleLog
{
    ObservableCollection<string> Entries {get;}
    void Clear();
}