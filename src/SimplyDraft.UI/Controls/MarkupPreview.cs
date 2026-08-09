// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Domains.Markup;
using SimplyDraft.Core.Domains.Markup.Blocks;
using SimplyDraft.Core.Domains.Markup.Inlines;
using SimplyDraft.Core.Domains.Editor;

namespace SimplyDraft.UI.Controls;

public sealed class MarkupPreview : ContentControl
{
    private IRenderEngine? _renderer;
    private static readonly IBrush SheetBrush = Brushes.White;
    private static readonly IBrush InkBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11));
    private static readonly IBrush GridLineBrush = new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8));
    private static readonly IBrush HeaderFillBrush = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2));

    public static readonly StyledProperty<MarkupDocument?> DocumentProperty =
        AvaloniaProperty.Register<MarkupPreview, MarkupDocument?>(nameof(Document));

    public static readonly StyledProperty<string?> PageFontFamilyProperty =
        AvaloniaProperty.Register<MarkupPreview, string?>(nameof(PageFontFamily));

    public static readonly StyledProperty<double> PageFontSizeProperty =
        AvaloniaProperty.Register<MarkupPreview, double>(nameof(PageFontSize), 16);

    public MarkupDocument? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public string? PageFontFamily
    {
        get => GetValue(PageFontFamilyProperty);
        set => SetValue(PageFontFamilyProperty, value);
    }

    public double PageFontSize
    {
        get => GetValue(PageFontSizeProperty);
        set => SetValue(PageFontSizeProperty, value);
    }

    // Resolved from DI and property-injected by owning window
    public IRenderEngine? Renderer
    {
        get => _renderer;
        set
        {
            _renderer = value;
            Rebuild();
        }
    }

    public MarkupPreview()
    {
        Background = SheetBrush;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DocumentProperty ||
            change.Property == PageFontFamilyProperty ||
            change.Property == PageFontSizeProperty)
            Rebuild();
    }

    private FontFamily ResolveFont()
        => string.IsNullOrWhiteSpace(PageFontFamily) ? FontFamily.Default : new FontFamily(PageFontFamily);

    private void Rebuild()
    {
        var doc = Document;

        // No renderer yet, not property-injected yet (window Load not ran)
        if (doc is null || _renderer is not { } renderer)
        {
            Content = null;
            return;
        }

        var font = ResolveFont();
        double size = PageFontSize > 0 ? PageFontSize : 16;
        var stack = new StackPanel();
        var run = new List<Block>();

        void FlushText()
        {
            if (run.Count > 0)
            {
                string text = renderer.Render(run, wrap: false).TrimEnd('\n');

                if (text.Length > 0)
                    stack.Children.Add(new SelectableTextBlock
                    {
                        Text = text,
                        FontFamily = font,
                        FontSize = size,
                        Foreground = InkBrush,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 8)
                    });
                run.Clear();
            }
        }

        foreach (var block in doc.Blocks)
        {
            if (block is TableBlock table)
            {
                FlushText();
                stack.Children.Add(BuildTable(table, font, size));
            }
            else
            {
                run.Add(block);
            }
        }

        FlushText();

        var sheet = new Border
        {
            Background = SheetBrush,
            Padding = new Thickness(28, 20),
            MaxWidth = PagePreview.PageWidthDip + PagePreview.PageChromeDip,
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = stack
        };

        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = sheet
        };
    }

    private static Border BuildTable(TableBlock table, FontFamily font, double size)
    {
        int cols = table.ColumnCount;
        var grid = new Grid {HorizontalAlignment = HorizontalAlignment.Left};
        
        for (int c = 0; c < cols; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        
        for (int r = 0; r < table.Rows.Count; r++)
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int r = 0; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];
            bool header = r == 0;
            
            for (int c = 0; c < cols; c++)
            {
                var cell = new TextBlock
                {
                    Text = c < row.Cells.Count ? Flatten(row.Cells[c]) : "",
                    FontFamily = font,
                    FontSize = size,
                    FontWeight = header ? FontWeight.SemiBold : FontWeight.Normal,
                    Foreground = InkBrush,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = AlignmentOf(table, c),
                    Margin = new Thickness(8, 4)
                };

                var cellBorder = new Border
                {
                    BorderBrush = GridLineBrush,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Background = header ? HeaderFillBrush : null,
                    Child = cell
                };

                Grid.SetRow(cellBorder, r);
                Grid.SetColumn(cellBorder, c);
                grid.Children.Add(cellBorder);
            }
        }

        return new Border
        {
            BorderBrush = GridLineBrush,
            BorderThickness = new Thickness(1, 1, 0, 0),
            Margin = new Thickness(0, 2, 0, 10),
            HorizontalAlignment = HorizontalAlignment.Left,
            MaxWidth = PagePreview.PageWidthDip,
            Child = grid
        };
    }

    private static TextAlignment AlignmentOf(TableBlock table, int column)
    {
        char a = column < table.Alignments.Count
            ? table.Alignments[column]
            : 'l';
        
        return a switch
        {
            'r' => TextAlignment.Right,
            'c' => TextAlignment.Center,
            _ => TextAlignment.Left
        };
    }

    private static string Flatten(IEnumerable<Inline> inlines)
    {
        var sb = new System.Text.StringBuilder();

        foreach (var inline in inlines)
        {
            if (inline is LineBreak)
                sb.Append(' ');
            else if (inline is TextRun r)
                sb.Append(r.SmallCaps ? r.Text.ToUpperInvariant() : r.Text);
            else if (inline is RefRun rr)
                sb.Append('?').Append(rr.Key).Append('?');
        }
        return sb.ToString();
    }
}