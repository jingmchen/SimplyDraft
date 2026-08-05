// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Core.Domains.Scripting.Expressions;

namespace SimplyDraft.Core.Abstractions.Engine;

public interface IBuiltinProvider
{
    Value? Lookup(BuiltinContext context, string ns, string member);
}