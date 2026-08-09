// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.UI;

namespace SimplyDraft.Core.Domains.Editor;

public static class EditorSyntaxDefinitions
{
    public static readonly ScriptPalette DarkScript = new("#6A9955", "#CE9178", "#B5CEA8", "#569CD6", "#4EC9B0", "#DCDCAA");
    public static readonly ScriptPalette LightScript = new("#008000", "#A31515", "#098658", "#0000FF", "#267F99", "#795E26");
    public static readonly TemplatePalette DarkTemplate = new("#6E6E6E", "#DCDCAA", "#569CD6", "#C586C0");
    public static readonly TemplatePalette LightTemplate = new("#808080", "#795E26", "#0000FF", "#AF00DB");

    // Script pane XSHD document (python-style script syntax)
    public static string ScriptXshd(ScriptPalette p)
        => $$"""
            <SyntaxDefinition name="SimplyDraftScript" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Comment" foreground="{{p.Comment}}" fontStyle="italic" />
            <Color name="String" foreground="{{p.Str}}" />
            <Color name="Number" foreground="{{p.Number}}" />
            <Color name="Keyword" foreground="{{p.Keyword}}" fontWeight="bold" />
            <Color name="Builtin" foreground="{{p.Builtin}}" />
            <Color name="Method" foreground="{{p.Method}}" />
            <RuleSet>
                <Span color="Comment" begin="#" />
                <Span color="String">
                <Begin>"</Begin>
                <End>"</End>
                <RuleSet>
                    <Span begin="\\" end="." />
                </RuleSet>
                </Span>
                <Span color="String">
                <Begin>'</Begin>
                <End>'</End>
                <RuleSet>
                    <Span begin="\\" end="." />
                </RuleSet>
                </Span>
                <Keywords color="Keyword">
                <Word>if</Word>
                <Word>elif</Word>
                <Word>else</Word>
                <Word>match</Word>
                <Word>case</Word>
                <Word>and</Word>
                <Word>or</Word>
                <Word>not</Word>
                <Word>in</Word>
                <Word>True</Word>
                <Word>False</Word>
                </Keywords>
                <Rule color="Builtin">\b(system|doc)\.[a-zA-Z_][a-zA-Z0-9_]*</Rule>
                <Rule color="Method">\.[a-zA-Z_][a-zA-Z0-9_]*(?=\()</Rule>
                <Rule color="Number">\b\d+(\.\d+)?</Rule>
            </RuleSet>
            </SyntaxDefinition>
            """;
    
    // Content pane XSHD document (template markup syntax) in the given palette
    public static string TemplateXshd(TemplatePalette p)
        => $$"""
            <SyntaxDefinition name="SimplyDraftTemplate" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Escape" foreground="{{p.Escape}}" />
            <Color name="Expr" foreground="{{p.Expression}}" />
            <Color name="Placeholder" foreground="{{p.Placeholder}}" fontWeight="bold" />
            <Color name="MarkupCmd" foreground="{{p.MarkupCommand}}" />
            <RuleSet>
                <Rule color="Escape">\{\{|\}\}</Rule>
                <Rule color="Expr">\{=[^}]*\}</Rule>
                <Rule color="Placeholder">\{[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)?\}</Rule>
                <Rule color="MarkupCmd">\\[a-zA-Z]+</Rule>
            </RuleSet>
            </SyntaxDefinition>
            """;
}