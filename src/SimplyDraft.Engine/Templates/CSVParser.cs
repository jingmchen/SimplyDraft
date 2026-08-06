// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;

namespace SimplyDraft.Engine.Templates;

public static class CSVParser
{
    public static List<string[]> Parse(string text, char delimiter = ',')
    {
        var rows = new List<string[]>();
        var currentRow = new List<string>();
        var fieldBuilder = new StringBuilder();
        bool insideQuotes = false;
        var normalized = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        int position = 0;

        while (position < normalized.Length)
        {
            char ch = normalized[position];

            if (insideQuotes)
            {
                if (ch == '"')
                {
                    if (position + 1 < normalized.Length && normalized[position + 1] == '"')
                    {
                        fieldBuilder.Append('"');
                        position += 2;
                        continue;
                    }

                    insideQuotes = false;
                    position++;
                    continue;
                }

                fieldBuilder.Append(ch);
                position++;
                continue;
            }

            if (ch == '"' && fieldBuilder.Length == 0)
            {
                insideQuotes = true;
                position++;
                continue;
            }

            if (ch == delimiter) {
                currentRow.Add(fieldBuilder.ToString());
                fieldBuilder.Clear();
                position++;
                continue;
            }

            if (ch == '\n')
            {
                currentRow.Add(fieldBuilder.ToString()); fieldBuilder.Clear();
                rows.Add(currentRow.ToArray()); currentRow.Clear();
                position++;
                continue;
            }

            fieldBuilder.Append(ch);
            position++;
        }

        if (fieldBuilder.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(fieldBuilder.ToString());
            rows.Add(currentRow.ToArray());
        }

        while (rows.Count > 0 && rows[^1].Length == 1 && rows[^1][0].Length == 0)
            rows.RemoveAt(rows.Count - 1);
        
        return rows;
    }
}