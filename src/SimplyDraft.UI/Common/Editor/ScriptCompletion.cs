// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using SimplyDraft.Core.Domains.Scripting.Completion;

namespace SimplyDraft.UI.Common.Editor;

public static class ScriptCompletion
{
    public static void Attach(TextEditor editor, Func<IReadOnlyList<string>> variableNames)
    {
        CompletionWindow? window = null;

        void Open(IReadOnlyList<CompletionItem> items, int prefixLength, string prefix)
        {
            if (items.Count == 0 || editor.IsReadOnly)
                return;
            
            var w = new CompletionWindow(editor.TextArea);
            w.StartOffset -= prefixLength;
            
            foreach (var item in items)
                w.CompletionList.CompletionData.Add(new CompletionData(item));
            
            if (prefix.Length > 0)
                w.CompletionList.SelectItem(prefix);
            
            w.Closed += (_, _) => window = null;
            window = w;
            w.Show();
        }

        void OpenGeneral()
        {
            var (prefix, _) = WordBefore(editor, editor.CaretOffset);
            Open(ScriptCompletionCatalog.General(variableNames()), prefix.Length, prefix);
        }

        editor.TextArea.TextEntered += (_, e) =>
        {
            if (window != null || string.IsNullOrEmpty(e.Text) || editor.IsReadOnly)
                return;
            
            char c = e.Text[0];
            
            if (c == '.')
            {
                int dot = editor.CaretOffset - 1;
                var (word, start) = WordBefore(editor, dot);

                if (word.Length > 0 && word.All(char.IsDigit))
                    return; // typing 1.5 — not a member access
                
                if (word.Equals("system", StringComparison.OrdinalIgnoreCase))
                    Open(ScriptCompletionCatalog.SystemMembers, 0, "");
                else if (word.Equals("doc", StringComparison.OrdinalIgnoreCase))
                    Open(ScriptCompletionCatalog.DocMembers, 0, "");
                else if (word.Length > 0 || EndsExpression(editor, start))
                    Open(ScriptCompletionCatalog.StringMethods, 0, "");
            }
            else if (char.IsLetter(c) || c == '_')
            {
                OpenGeneral();
            }
        };

        editor.TextArea.TextEntering += (_, e) =>
        {
            if (window != null && !string.IsNullOrEmpty(e.Text) && !(char.IsLetterOrDigit(e.Text[0]) || e.Text[0] == '_'))
                window.CompletionList.RequestInsertion(e);
        };

        editor.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                if (window == null)
                    OpenGeneral();
                e.Handled = true;
            }
        };
    }

    private static (string Word, int Start) WordBefore(TextEditor editor, int offset)
    {
        var doc = editor.Document;
        int start = Math.Clamp(offset, 0, doc.TextLength);

        while (start > 0)
        {
            char ch = doc.GetCharAt(start - 1);
            if (char.IsLetterOrDigit(ch) || ch == '_')
                start--;
            else
                break;
        }

        int end = Math.Clamp(offset, 0, doc.TextLength);
        
        return (doc.GetText(start, end - start), start);
    }

    private static bool EndsExpression(TextEditor editor, int offset)
    {
        if (offset <= 0 || offset > editor.Document.TextLength)
            return false;
        
        char ch = editor.Document.GetCharAt(offset - 1);
        
        return ch is '"' or '\'' or ')' or ']';
    }
}