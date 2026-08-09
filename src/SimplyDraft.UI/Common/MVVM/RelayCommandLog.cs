// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;

namespace SimplyDraft.UI.Common.MVVM;

internal static partial class RelayCommandLog
{
    [LoggerMessage(EventId = 9000, Level = LogLevel.Error, Message = "Command Failed")]
    internal static partial void CommandFailed(ILogger logger, Exception ex);
}