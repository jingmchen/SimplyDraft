// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface IAppInfo
{
    string Product {get;}
    string Company {get;}
    string Authors {get;}
    string Copyright {get;}
    string InfoVersion {get;}
}