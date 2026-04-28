using SimplyDraft.App.Configuration;

namespace SimplyDraft.App.Services.Settings;

public interface ISettingsProvider
{
    AppSettings Current {get;}
    void Save();
    void Reload();
}