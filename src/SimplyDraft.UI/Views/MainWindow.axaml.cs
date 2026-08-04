using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using SimplyDraft.Core.Abstractions;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.UI.Views;

public partial class MainWindow : Window
{
    private readonly IThemeService _theme;

    private static readonly (string Label, string Key)[] NeutralSwatches =
    {
        ("Bg0", "ThemeBg0Brush"),   ("Bg1", "ThemeBg1Brush"),   ("Bg2", "ThemeBg2Brush"),   ("Bg3", "ThemeBg3Brush"),
        ("Fg0", "ThemeFg0Brush"),   ("Fg1", "ThemeFg1Brush"),   ("Fg2", "ThemeFg2Brush"),   ("Fg3", "ThemeFg3Brush"),
        ("Border", "ThemeBorderBrush"), ("Border+", "ThemeBorderStrongBrush"),
        ("Hover", "ThemeHoverBrush"), ("Pressed", "ThemePressedBrush"), ("Selected", "ThemeSelectedBrush"),
        ("Error", "ThemeErrorBrush"), ("Warn", "ThemeWarningBrush"), ("Ok", "ThemeSuccessBrush"), ("Info", "ThemeInfoBrush"),
    };

    private static readonly (string Label, string Key)[] AccentSwatches =
    {
        ("Accent", "AccentBrush"), ("Hover", "AccentHoverBrush"), ("Pressed", "AccentPressedBrush"),
        ("Light1", "AccentLight1Brush"), ("Dark1", "AccentDark1Brush"), ("Subtle", "AccentSubtleBrush"),
        ("Fg", "AccentForegroundBrush"),
    };

    public MainWindow(IThemeService themeService)
    {
        _theme = themeService ?? throw new ArgumentNullException(nameof(themeService));
        InitializeComponent();

        BuildThemeButtons();
        BuildAccentButtons();
        BuildRamps();

        _theme.ThemeChanged += OnThemeChanged;
        UpdateCurrentLabel();
    }

    private void BuildThemeButtons()
    {
        foreach (var theme in Enum.GetValues<AppTheme>())
        {
            var button = new Button { Content = theme.ToString(), Tag = theme, Margin = new Thickness(0, 0, 8, 8) };
            button.Click += (_, _) => _theme.SetTheme((AppTheme)button.Tag!);
            ThemesPanel.Children.Add(button);
        }
    }

    private void BuildAccentButtons()
    {
        foreach (var accent in Enum.GetValues<AppAccent>())
        {
            var swatch = new Border
            {
                Width = 14, Height = 14, CornerRadius = new CornerRadius(3),
                Background = _theme.GetAccentSwatch(accent),
                Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center,
            };
            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(swatch);
            content.Children.Add(new TextBlock { Text = accent.ToString(), VerticalAlignment = VerticalAlignment.Center });

            var button = new Button { Content = content, Tag = accent, Margin = new Thickness(0, 0, 8, 8) };
            button.Click += (_, _) => _theme.SetAccent((AppAccent)button.Tag!);
            AccentsPanel.Children.Add(button);
        }
    }

    private void BuildRamps()
    {
        foreach (var (label, key) in NeutralSwatches)
            NeutralRamp.Children.Add(CreateSwatch(label, key));
        foreach (var (label, key) in AccentSwatches)
            AccentRamp.Children.Add(CreateSwatch(label, key));
    }

    // Chip + border + caption all track the live resource, so they repaint on switch.
    private StackPanel CreateSwatch(string label, string resourceKey)
    {
        var chip = new Border
        {
            Width = 48, Height = 34, CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1),
        };
        chip.Bind(Border.BackgroundProperty, this.GetResourceObservable(resourceKey));
        chip.Bind(Border.BorderBrushProperty, this.GetResourceObservable("ThemeBorderStrongBrush"));

        var caption = new TextBlock
        {
            Text = label, FontSize = 10, Margin = new Thickness(0, 3, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        caption.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("ThemeFg2Brush"));

        return new StackPanel { Margin = new Thickness(0, 0, 8, 8), Children = { chip, caption } };
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e) => UpdateCurrentLabel();

    private void UpdateCurrentLabel()
        => CurrentLabel.Text = $"Theme: {_theme.CurrentTheme}    Accent: {_theme.CurrentAccent}";

    protected override void OnClosed(EventArgs e)
    {
        _theme.ThemeChanged -= OnThemeChanged;
        base.OnClosed(e);
    }
}