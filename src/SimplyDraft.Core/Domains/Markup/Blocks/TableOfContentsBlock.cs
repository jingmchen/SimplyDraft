using SimplyDraft.Core.Domains.Markup.Entries;

namespace SimplyDraft.Core.Domains.Markup.Blocks;

public sealed class TableOfContentsBlock : Block
{
    public List<TableOfContentsEntry> Entries {get;} = [];
}