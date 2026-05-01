namespace SimplyDraft.Core.Models;

public enum DocumentBlockType
{
    Paragraph,
    Heading1,
    Heading2,
    Heading3,
    Heading4,
    ListItem,
    Table,
    HorizontalRule,
    Caption,
    CodeBlock
}

public sealed record TableCell(string Text, bool IsHeader);
public sealed record TableRow(List<TableCell> Cells);

public sealed class DocumentBlock
{
    public int Index {get; init;}
    public DocumentBlockType Type {get; init;} = DocumentBlockType.Paragraph;
    public string Text {get; init;} = string.Empty;
    public int ListLevel {get; init;}
    public bool IsOrdered {get; init;}
    public int ListItemNumber {get; init;}
    public List<TableRow> Rows {get; init;} = new();
    public bool IsBold {get; init;}
    public bool IsItalic {get; init;}
    public bool IsAllCaps {get; init;}
}