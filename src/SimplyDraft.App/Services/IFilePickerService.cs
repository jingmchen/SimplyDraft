namespace SimplyDraft.App.Services;

public interface IFilePickerService
{
    Task<string?> PickFileAsync();
}