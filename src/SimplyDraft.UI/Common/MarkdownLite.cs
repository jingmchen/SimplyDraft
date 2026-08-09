// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace SimplyDraft.UI.Common;

public static class MarkdownLite
{
    private const double BodyFontSize = 13;
    private const double H1FontSize = 20;
    private const double H2FontSize = 16;
    private const double H3FontSize = 14;

    public static StackPanel Render(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        var host = new StackPanel { Spacing = 2 };
        var paragraph = new List<string>();

        foreach (var raw in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            string line = raw.TrimEnd();
            string trimmed = line.TrimStart();

            if (trimmed.Length == 0)
            {
                FlushParagraph(host, paragraph);
                continue;
            }

            if (trimmed is "---" or "***")
            {
                FlushParagraph(host, paragraph);
                host.Children.Add(CreateSeparator());
                continue;
            }

            if (trimmed.StartsWith('#'))
            {
                FlushParagraph(host, paragraph);
                host.Children.Add(CreateHeading(trimmed));
                continue;
            }

            if (trimmed.StartsWith("* ", StringComparison.Ordinal) ||
                trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                FlushParagraph(host, paragraph);
                host.Children.Add(CreateBullet(trimmed[2..]));
                continue;
            }

            paragraph.Add(trimmed);
        }
        FlushParagraph(host, paragraph);
        return host;
    }

    private static void FlushParagraph(StackPanel host, List<string> lines)
    {
        if (lines.Count == 0)
            return;
        var block = CreateBody(string.Join(' ', lines));
        block.Margin = new Avalonia.Thickness(0, 2, 0, 6);
        host.Children.Add(block);
        lines.Clear();
    }

    private static SelectableTextBlock CreateHeading(string line)
    {
        int level = 0;
        
        while (level < line.Length && line[level] == '#')
            level++;
        
        string text = line[level..].TrimStart();
        var block = CreateBody(text);
        
        block.FontSize = level switch
        {
            1 => H1FontSize,
            2 => H2FontSize,
            _ => H3FontSize
        };

        block.FontWeight = level == 1
            ? FontWeight.Bold
            : FontWeight.SemiBold;
        
        block.Margin = new Avalonia.Thickness(0, level == 1 ? 4 : 12, 0, 4);
        
        return block;
    }

    private static Grid CreateBullet(string text)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("18,*"),
            Margin = new Avalonia.Thickness(10, 1, 0, 1),
        };

        var dot = new TextBlock
        {
            Text = "•",
            FontSize = BodyFontSize,
            VerticalAlignment = VerticalAlignment.Top
        };

        var body = CreateBody(text);
        Grid.SetColumn(body, 1);
        grid.Children.Add(dot);
        grid.Children.Add(body);
        
        return grid;
    }

    private static Border CreateSeparator()
    {
        var border = new Border { Margin = new Avalonia.Thickness(0, 8) };
        border.Classes.Add("separator");
        return border;
    }

    private static SelectableTextBlock CreateBody(string text)
    {
        var block = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = BodyFontSize,
        };

        var segments = text.Split("**");

        if (segments.Length == 1)
        {
            block.Text = text;
            return block;
        }

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i].Length == 0)
                continue;
            
            var run = new Run(segments[i]);
            
            if (i % 2 == 1)
                run.FontWeight = FontWeight.Bold;
            
            block.Inlines?.Add(run);
        }
        return block;
    }
}