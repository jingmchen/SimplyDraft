using SimplyDraft.Core.Configuration;

namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface IAppSettingsProvider
{
    AppSettings Current {get;}
    void Save();
    void Reload();
}