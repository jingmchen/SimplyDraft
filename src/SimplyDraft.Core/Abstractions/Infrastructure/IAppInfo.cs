namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface IAppInfo
{
    string Product {get;}
    string Company {get;}
    string Authors {get;}
    string Copyright {get;}
    string InfoVersion {get;}
}