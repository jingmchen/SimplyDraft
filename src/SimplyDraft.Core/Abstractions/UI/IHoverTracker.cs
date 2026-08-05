// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Abstractions.UI;

public interface IHoverTracker
{
    void SetHovered(object? item);
    void ClearHovered(object? item);
}