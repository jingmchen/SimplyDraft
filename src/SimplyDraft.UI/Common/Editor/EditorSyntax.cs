// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Xml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using SimplyDraft.Core.Domains.Editor;

namespace SimplyDraft.UI.Common.Editor;

public static class EditorSyntax
{
    private const byte SelectionAlphaLight = 0x4D;
    private const byte SelectionAlphaDark = 0x66;
    private const string FallbackAccent = "#007ACC";
    private static bool _dark = true;
    private static Color _accentColor;
    private static IHighlightingDefinition? _scriptDark, _scriptLight, _templateDark, _templateLight;

    public static IHighlightingDefinition? Script
        => _dark
            ? _scriptDark ??= Load(EditorSyntaxDefinitions.ScriptXshd(EditorSyntaxDefinitions.DarkScript))
            : _scriptLight ??= Load(EditorSyntaxDefinitions.ScriptXshd(EditorSyntaxDefinitions.LightScript));

    public static IHighlightingDefinition? Template
        => _dark
            ? _templateDark ??= Load(EditorSyntaxDefinitions.TemplateXshd(EditorSyntaxDefinitions.DarkTemplate))
            : _templateLight ??= Load(EditorSyntaxDefinitions.TemplateXshd(EditorSyntaxDefinitions.LightTemplate));
    
    // Raised when palette flips dark / light for editors to reassign highlighting
    public static event Action? PaletteChanged;

    public static void SetTheme(bool dark, Color accentColor)
    {
        if (_dark == dark && _accentColor == accentColor)
            return;
        _dark = dark;
        _accentColor = accentColor;
        PaletteChanged?.Invoke();
    }

    public static void ApplySelectionColors(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        byte alpha = _dark ? SelectionAlphaDark : SelectionAlphaLight;
        
        // Highlight color
        editor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(alpha, _accentColor.R, _accentColor.G, _accentColor.B));
        editor.TextArea.SelectionForeground = null;
    }

    private static IHighlightingDefinition? Load(string xshd)
    {
        try
        {
            using var text = new StringReader(xshd);
            using var reader = XmlReader.Create(text);
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
        catch
        {
            return null;
        }
    }
}