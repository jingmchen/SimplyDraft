namespace SimplyDraft.Core.Domains.Document.Segments;

public sealed class PlaceholderSegment : Segment
{
    public string Name {get; set;} = "";
    public string Namespace {get; set;} = "";
    public string Member {get; set;} = "";
    public bool IsBuiltin {get; set;}

    /// <summary>
    /// Plain user-defined variable placeholder.
    /// </summary>
    public PlaceholderSegment(int line, int column, string name) : base(line, column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Namespace = "";
        Member = "";
    }

    /// <summary>
    /// Built-in reference placeholder, i.e., {doc.member}.
    /// </summary>
    public PlaceholderSegment(int line, int column, string ns, string member) : base(line, column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ns);
        ArgumentException.ThrowIfNullOrWhiteSpace(member);

        Name = ns + "." + member;
        Namespace = ns;
        Member = member;
        IsBuiltin = true;
    }
}