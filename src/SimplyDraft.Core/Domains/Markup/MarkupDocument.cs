using SimplyDraft.Core.Domains.Markup.Blocks;

namespace SimplyDraft.Core.Domains.Markup;

public sealed class MarkupDocument
{
    public List<Block> Blocks {get;} = [];
    public List<Diagnostic> Warnings {get;} = [];
}