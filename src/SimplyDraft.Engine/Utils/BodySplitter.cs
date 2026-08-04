using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Utils;

public static class BodySplitter
{
    public static (string Script, string Content) Split(string body)
    {
        var normalized = (body ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        int firstNewline = normalized.IndexOf('\n');
        string firstLine = firstNewline < 0 ? normalized : normalized[..firstNewline];
        if (firstLine.Trim() != ScriptingConstants.Template.ScriptOpen)
            return ("", normalized);

        int position = firstNewline < 0 ? normalized.Length : firstNewline + 1;
        var scriptBuilder = new System.Text.StringBuilder();
        while (position < normalized.Length)
        {
            int newline = normalized.IndexOf('\n', position);
            bool hasNewline = newline >= 0;
            string lineText = hasNewline ? normalized[position..newline] : normalized[position..];
            position = hasNewline ? newline + 1 : normalized.Length;
            if (lineText.Trim() == ScriptingConstants.Template.ScriptClose)
                return (scriptBuilder.ToString().TrimEnd('\n'), normalized[position..]);
            scriptBuilder.Append(lineText).Append('\n');
        }
        // Unterminated block: leave everything in the content pane so nothing is hidden.
        return ("", normalized);
    }

    public static string Join(string script, string content)
    {
        script = (script ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim('\n');
        content = (content ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        if (script.Trim().Length == 0) return content;
        return ScriptingConstants.Template.ScriptOpen + "\n" + script + "\n" + ScriptingConstants.Template.ScriptClose + "\n" + content;
    }
}