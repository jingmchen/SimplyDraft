// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Document.Segments;

public sealed class PlaceholderSegment : Segment
{
    public string Name {get;}
    public string Namespace {get;}
    public string Member {get;}
    public bool IsBuiltin {get;}

    /// <summary>
    /// Plain user-defined variable placeholder.
    /// </summary>
    public PlaceholderSegment(string name, int line, int column) : base(line, column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Namespace = "";
        Member = "";
    }

    /// <summary>
    /// Built-in reference placeholder, i.e., {doc.member}.
    /// </summary>
    public PlaceholderSegment(string ns, string member, int line, int column) : base(line, column)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ns);
        ArgumentException.ThrowIfNullOrWhiteSpace(member);

        Name = ns + "." + member;
        Namespace = ns;
        Member = member;
        IsBuiltin = true;
    }
}