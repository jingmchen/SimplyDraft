using SimplyDraft.Core.Domains.Scripting;

namespace SimplyDraft.Core.Abstractions.Engine;

public interface IBuiltinProvider
{
    Value? Lookup(string ns, string member);
}