// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using SimplyDraft.Core.Domains.Scripting.Completion;

namespace SimplyDraft.UI.Common.Editor;

public sealed class CompletionData : ICompletionData
{
    private readonly CompletionItem _item;
    public string Text => _item.Text;
    public object Content => _item.Text;
    public object Description => _item.Description;
    public double Priority => _item.Priority;
    public CompletionData(CompletionItem item) => _item = item;
    public IImage? Image => null;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, _item.InsertText);
        if (_item.CaretBack > 0)
            textArea.Caret.Offset -= _item.CaretBack;
    }
}