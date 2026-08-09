// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Xml;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using SimplyDraft.Core.Domains.Editor;

namespace SimplyDraft.UI.Common.Editor;

public static class EditorSyntax
{
    private static bool _dark = true;
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

    public static void SetDark(bool dark)
    {
        if (_dark == dark)
            return;
        _dark = dark;
        PaletteChanged?.Invoke();
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