namespace SimplyDraft.Core.Abstractions.UI;

public interface IHoverTracker
{
    void SetHovered(object? item);
    void ClearHovered(object? item);
}