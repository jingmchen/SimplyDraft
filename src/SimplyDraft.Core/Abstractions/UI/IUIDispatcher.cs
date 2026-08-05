// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Abstractions.UI;

public interface IUIDispatcher
{
    bool IsOnUIThread {get;}
    void Post(Action action);
}