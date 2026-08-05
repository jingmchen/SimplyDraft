// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface ILibraryPaths
{
    string Root {get;}
    string TemplatesFolder {get;}
    string ChildrenFolder {get;}
    string ExportsFolder {get;}
    string TrashFolder {get;}
}