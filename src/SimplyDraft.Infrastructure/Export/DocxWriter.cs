// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using System.IO.Compression;
using System.Text;
using SimplyDraft.Core.Domains.Export;
using SimplyDraft.Core.Domains.Markup;
using SimplyDraft.Core.Domains.Markup.Blocks;
using SimplyDraft.Core.Domains.Markup.Inlines;
using SimplyDraft.Core.Enums;
using SimplyDraft.Infrastructure.Constants;

namespace SimplyDraft.Infrastructure.Export;

public static class DocxWriter
{
    private static readonly string[] TableBorderEdges = ["top", "left", "bottom", "right", "insideH", "insideV"];

    // ─── PUBLIC METHODS ────────────────────────
    public static void Write(
        string path,
        string text,
        string? fontName,
        int? fontSizePt,
        string? pageHeader = null)
    {
        bool hasHeaderFooter = !string.IsNullOrWhiteSpace(pageHeader);
        string documentXml = ComposeDocumentXml(BuildPlainBody(text), hasHeaderFooter);

        WritePackage(
            path, fontName, fontSizePt, documentXml, hasHeaderFooter ? pageHeader : null,
            media: Array.Empty<DocxMediaItem>(), updateFieldsOnOpen: false);
    }

    public static void WriteMarkup(
        string path,
        MarkupDocument doc,
        string? fontName,
        int? fontSizePt,
        string? pageHeader = null,
        string? baseDirectory = null)
    {
        var context = new RenderContext {BaseDirectory = baseDirectory};
        string body = BuildMarkupBody(doc, context);
        bool hasHeaderFooter = !string.IsNullOrWhiteSpace(pageHeader);
        string documentXml = ComposeDocumentXml(body, hasHeaderFooter);

        WritePackage(
            path, fontName, fontSizePt, documentXml, hasHeaderFooter ? pageHeader : null,
            context.Media, updateFieldsOnOpen: context.HasTableOfContents);
    }

    // ─── PRIVATE METHODS ───────────────────────
    private static string ComposeDocumentXml(string body, bool hasHeaderFooter)
        => DocxWriterConstants.XmlDeclaration + DocxWriterConstants.Sections.DocumentOpen + body
            + BuildSectionProperties(hasHeaderFooter) + DocxWriterConstants.Sections.DocumentClose;
    
    private static void WritePackage(
        string path,
        string? fontName,
        int? fontSizePt,
        string documentXml,
        string? pageHeader,
        IReadOnlyList<DocxMediaItem> media,
        bool updateFieldsOnOpen)
    {
        string font = string.IsNullOrWhiteSpace(fontName) ? DocxWriterConstants.Typography.FallbackFont : fontName!.Trim();
        
        int sizePoints = fontSizePt is int points && points > 0
            ? points
            : DocxWriterConstants.Typography.FallbackFontSizePoints;
        
        int sizeHalfPoints = DocxWriterConstants.Typography.HalfPointsPerPoint * sizePoints;
        bool hasHeaderFooter = pageHeader != null;
        string temporaryPath = path + ".tmp";

        try
        {
            using (var fileStream = File.Create(temporaryPath))
            using (var package = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                AddXmlEntry(
                    package, DocxWriterConstants.PartNames.ContentTypes,
                    BuildContentTypes(hasHeaderFooter, media, updateFieldsOnOpen));
                
                AddXmlEntry(
                    package, DocxWriterConstants.PartNames.RootRelationships, DocxWriterConstants.Sections.RootRelationshipsXml);
                
                AddXmlEntry(
                    package, DocxWriterConstants.PartNames.DocumentRelationships,
                    BuildDocumentRelationships(hasHeaderFooter, media, updateFieldsOnOpen));
                
                AddXmlEntry(
                    package, DocxWriterConstants.PartNames.Styles, BuildStyles(font, sizeHalfPoints));

                AddXmlEntry(
                    package, DocxWriterConstants.PartNames.Document, documentXml);

                if (hasHeaderFooter)
                {
                    AddXmlEntry(package, DocxWriterConstants.PartNames.Header, BuildHeaderXml(pageHeader!));
                    AddXmlEntry(package, DocxWriterConstants.PartNames.Footer, BuildFooterXml());
                }

                if (updateFieldsOnOpen)
                    AddXmlEntry(package, DocxWriterConstants.PartNames.Settings, DocxWriterConstants.Sections.SettingsXml);
                
                foreach (var mediaItem in media)
                    AddBinaryEntry(package, DocxWriterConstants.PartNames.MediaFolder + mediaItem.FileName, mediaItem.Data);
            }
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch { }

            throw;
        }
    }

    private static void AddXmlEntry(ZipArchive package, string entryName, string xml)
    {
        var entry = package.CreateEntry(entryName, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(xml);
    }

    private static void AddBinaryEntry(ZipArchive package, string entryName, byte[] bytes)
    {
        var entry = package.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string BuildContentTypes(
        bool hasHeaderFooter, IReadOnlyList<DocxMediaItem> media, bool hasSettings)
    {
        var builder = new StringBuilder(DocxWriterConstants.XmlDeclaration);

        builder.Append("<Types xmlns=\"").Append(DocxWriterConstants.Namespaces.PackageContentTypes).Append("\">");
        
        AppendDefaultContentType(
            builder, DocxWriterConstants.FileExtensions.Relationships, DocxWriterConstants.ContentTypes.Relationships);
        
        AppendDefaultContentType(
            builder, DocxWriterConstants.FileExtensions.Xml, DocxWriterConstants.ContentTypes.Xml);
        
        if (media.Any(item => item.ContentType == DocxWriterConstants.ContentTypes.Png))
            AppendDefaultContentType(
                builder, DocxWriterConstants.FileExtensions.Png, DocxWriterConstants.ContentTypes.Png);
        
        if (media.Any(item => item.ContentType == DocxWriterConstants.ContentTypes.Jpeg))
            AppendDefaultContentType(
                builder, DocxWriterConstants.FileExtensions.Jpg, DocxWriterConstants.ContentTypes.Jpeg);
        
        AppendOverrideContentType(
            builder, DocxWriterConstants.PartNames.Document, DocxWriterConstants.ContentTypes.MainDocument);
        
        AppendOverrideContentType(
            builder, DocxWriterConstants.PartNames.Styles, DocxWriterConstants.ContentTypes.Styles);
        
        if (hasHeaderFooter)
        {
            AppendOverrideContentType(
                builder, DocxWriterConstants.PartNames.Header, DocxWriterConstants.ContentTypes.Header);
            AppendOverrideContentType(
                builder, DocxWriterConstants.PartNames.Footer, DocxWriterConstants.ContentTypes.Footer);
        }
        
        if (hasSettings)
            AppendOverrideContentType(
                builder, DocxWriterConstants.PartNames.Settings, DocxWriterConstants.ContentTypes.Settings);
        
        builder.Append("</Types>");
        return builder.ToString();
    }

    private static void AppendDefaultContentType(StringBuilder builder, string extension, string contentType)
        => builder.Append("<Default Extension=\"")
                  .Append(extension)
                  .Append("\" ContentType=\"")
                  .Append(contentType)
                  .Append("\"/>");

    private static void AppendOverrideContentType(StringBuilder builder, string partName, string contentType)
        => builder.Append("<Override PartName=\"/")
                  .Append(partName)
                  .Append("\" ContentType=\"")
                  .Append(contentType)
                  .Append("\"/>");
    
    private static string BuildDocumentRelationships(
        bool hasHeaderFooter, IReadOnlyList<DocxMediaItem> media, bool hasSettings)
    {
        var builder = new StringBuilder(DocxWriterConstants.XmlDeclaration);

        builder.Append("<Relationships xmlns=\"").Append(DocxWriterConstants.Namespaces.PackageRelationships).Append("\">");
        
        AppendRelationship(
            builder, DocxWriterConstants.RelationshipIds.Styles, DocxWriterConstants.RelationshipTypes.Styles, DocxWriterConstants.RelationshipTargets.Styles);
        
        if (hasHeaderFooter)
        {
            AppendRelationship(
                builder, DocxWriterConstants.RelationshipIds.Header, DocxWriterConstants.RelationshipTypes.Header, DocxWriterConstants.RelationshipTargets.Header);
            AppendRelationship(
                builder, DocxWriterConstants.RelationshipIds.Footer, DocxWriterConstants.RelationshipTypes.Footer, DocxWriterConstants.RelationshipTargets.Footer);
        }
        
        if (hasSettings)
            AppendRelationship(
                builder, DocxWriterConstants.RelationshipIds.Settings, DocxWriterConstants.RelationshipTypes.Settings, DocxWriterConstants.RelationshipTargets.Settings);
        
        foreach (var mediaItem in media)
            AppendRelationship(
                builder, mediaItem.RelId, DocxWriterConstants.RelationshipTypes.Image, DocxWriterConstants.RelationshipTargets.MediaFolder + mediaItem.FileName);
        
        builder.Append("</Relationships>");
        return builder.ToString();
    }

    private static void AppendRelationship(StringBuilder builder, string id, string type, string target)
        => builder.Append("<Relationship Id=\"")
                  .Append(id)
                  .Append("\" Type=\"")
                  .Append(type)
                  .Append("\" Target=\"")
                  .Append(target)
                  .Append("\"/>");
    
    private static string BuildSectionProperties(bool hasHeaderFooter)
        =>  $"""
            <w:sectPr>
                {(hasHeaderFooter
                    ? $"""
                    <w:headerReference
                        w:type="default"
                        r:id="{DocxWriterConstants.RelationshipIds.Header}" />
                    <w:footerReference
                        w:type="default"
                        r:id="{DocxWriterConstants.RelationshipIds.Footer}" />
                    """
                    : string.Empty)}
                {DocxWriterConstants.Sections.PageGeometryXml}
            </w:sectPr>
            """;
    
    private static string BuildStyles(string font, int halfPoints)
    {
        string escapedFont = EscapeAttribute(font);

        string heading1 = BuildHeadingStyle(
            DocxWriterConstants.ParagraphStyles.Heading1.Id,
            DocxWriterConstants.ParagraphStyles.Heading1.Name,
            DocxWriterConstants.ParagraphStyles.Heading1.SpacingBeforeTwips,
            DocxWriterConstants.ParagraphStyles.Heading1.SpacingAfterTwips,
            DocxWriterConstants.ParagraphStyles.Heading1.OutlineLevel,
            DocxWriterConstants.ParagraphStyles.Heading1.SizeHalfPoints);

        string heading2 = BuildHeadingStyle(
            DocxWriterConstants.ParagraphStyles.Heading2.Id,
            DocxWriterConstants.ParagraphStyles.Heading2.Name,
            DocxWriterConstants.ParagraphStyles.Heading2.SpacingBeforeTwips,
            DocxWriterConstants.ParagraphStyles.Heading2.SpacingAfterTwips,
            DocxWriterConstants.ParagraphStyles.Heading2.OutlineLevel,
            DocxWriterConstants.ParagraphStyles.Heading2.SizeHalfPoints);

        string heading3 = BuildHeadingStyle(
            DocxWriterConstants.ParagraphStyles.Heading3.Id,
            DocxWriterConstants.ParagraphStyles.Heading3.Name,
            DocxWriterConstants.ParagraphStyles.Heading3.SpacingBeforeTwips,
            DocxWriterConstants.ParagraphStyles.Heading3.SpacingAfterTwips,
            DocxWriterConstants.ParagraphStyles.Heading3.OutlineLevel,
            DocxWriterConstants.ParagraphStyles.Heading3.SizeHalfPoints);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"""
            {DocxWriterConstants.XmlDeclaration}
            <w:styles xmlns:w="{DocxWriterConstants.Namespaces.Main}">
                <w:docDefaults>
                    <w:rPrDefault>
                        <w:rPr>
                            <w:rFonts
                                w:ascii="{escapedFont}"
                                w:hAnsi="{escapedFont}"
                                w:cs="{escapedFont}" />
                            <w:sz w:val="{halfPoints}" />
                            <w:szCs w:val="{halfPoints}" />
                        </w:rPr>
                    </w:rPrDefault>
                    <w:pPrDefault />
                </w:docDefaults>
                <w:style
                    w:type="paragraph"
                    w:default="1"
                    w:styleId="{DocxWriterConstants.ParagraphStyles.NormalId}">
                    <w:name w:val="{DocxWriterConstants.ParagraphStyles.NormalName}" />
                </w:style>
                {heading1}
                {heading2}
                {heading3}
            </w:styles>
            """);
    }

    private static string BuildHeadingStyle(
        string styleId, string displayName, int spacingBeforeTwips, int spacingAfterTwips, int outlineLevel, int sizeHalfPoints)
            => string.Create(
            CultureInfo.InvariantCulture,
            $"""
            <w:style w:type="paragraph" w:styleId="{styleId}">
                <w:name w:val="{displayName}" />
                <w:basedOn w:val="{DocxWriterConstants.ParagraphStyles.NormalId}" />
                <w:pPr>
                    <w:spacing
                        w:before="{spacingBeforeTwips}"
                        w:after="{spacingAfterTwips}" />
                    <w:outlineLvl w:val="{outlineLevel}" />
                </w:pPr>
                <w:rPr>
                    <w:b />
                    <w:sz w:val="{sizeHalfPoints}" />
                    <w:szCs w:val="{sizeHalfPoints}" />
                </w:rPr>
            </w:style>
            """);

    private static string BuildHeaderXml(string headerText)
    {
        var builder = new StringBuilder(DocxWriterConstants.XmlDeclaration);

        builder.Append("<w:hdr xmlns:w=\"").Append(DocxWriterConstants.Namespaces.Main).Append("\">");
        builder.Append("<w:p><w:pPr><w:jc w:val=\"right\"/></w:pPr>");

        AppendRuns(builder, headerText, DocxWriterConstants.Sections.NoRunProperties);
        
        builder.Append("</w:p></w:hdr>");
        
        return builder.ToString();
    }
    
    private static string BuildFooterXml()
        =>  $"""
            {DocxWriterConstants.XmlDeclaration}
            <w:ftr xmlns:w="{DocxWriterConstants.Namespaces.Main}">
                <w:p>
                    <w:pPr>
                        <w:jc w:val="center" />
                    </w:pPr>
                    <w:r>
                        <w:t xml:space="preserve">{DocxWriterConstants.FixedText.FooterPagePrefix}</w:t>
                    </w:r>
                    <w:fldSimple w:instr="{DocxWriterConstants.FieldInstructions.CurrentPage}">
                        <w:r>
                            <w:t>1</w:t>
                        </w:r>
                    </w:fldSimple>
                    <w:r>
                        <w:t xml:space="preserve">{DocxWriterConstants.FixedText.FooterOfSeparator}</w:t>
                    </w:r>
                    <w:fldSimple w:instr="{DocxWriterConstants.FieldInstructions.PageCount}">
                        <w:r>
                            <w:t>1</w:t>
                        </w:r>
                    </w:fldSimple>
                </w:p>
            </w:ftr>
            """;

    // Document bodies
    private static string BuildPlainBody(string text)
    {
        var builder = new StringBuilder();
        string normalized = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');

        foreach (var line in normalized.Split('\n'))
        {
            if (line.Length == 0)
            {
                builder.Append("<w:p/>");
                continue;
            }

            builder.Append("<w:p>");
            AppendRuns(builder, line, DocxWriterConstants.Sections.NoRunProperties);
            builder.Append("</w:p>");
        }
        return builder.ToString();
    }

    private static string BuildMarkupBody(MarkupDocument doc, RenderContext context)
    {
        var builder = new StringBuilder();

        foreach (var block in doc.Blocks)
        {
            switch (block)
            {
                case PageBreakBlock:
                    builder.Append("<w:p><w:r><w:br w:type=\"page\"/></w:r></w:p>");
                    break;

                case RuleBlock:
                    AppendHorizontalRule(builder);
                    break;

                case TableBlock table:
                    AppendTable(builder, table);
                    break;

                case TableOfContentsBlock toc:
                    context.HasTableOfContents = true;
                    AppendTableOfContents(builder, toc);
                    break;

                case ListOfFiguresBlock listOfFigures:
                    AppendListOfFigures(builder, listOfFigures);
                    break;

                case ImageBlock image:
                    AppendImage(builder, image, context);
                    break;
                    
                case ParagraphBlock paragraph:
                    AppendParagraph(builder, paragraph);
                    break;
            }
        }
        return builder.ToString();
    }

    private static void AppendHorizontalRule(StringBuilder builder)
    => builder.Append(
        CultureInfo.InvariantCulture,
        $"""
        <w:p>
            <w:pPr>
                <w:pBdr>
                    <w:bottom
                        w:val="single"
                        w:sz="{DocxWriterConstants.HorizontalRule.BorderSizeEighthPoints}"
                        w:space="{DocxWriterConstants.HorizontalRule.BorderSpacePoints}"
                        w:color="auto" />
                </w:pBdr>
            </w:pPr>
        </w:p>
        """);

    private static void AppendParagraph(StringBuilder builder, ParagraphBlock paragraph)
    {
        builder.Append("<w:p>");
        var properties = new StringBuilder();

        switch (paragraph.Kind)
        {
            case ParagraphKind.Heading1:
                properties.Append("<w:pStyle w:val=\"").Append(DocxWriterConstants.ParagraphStyles.Heading1.Id).Append("\"/>");
                break;

            case ParagraphKind.Heading2:
                properties.Append("<w:pStyle w:val=\"").Append(DocxWriterConstants.ParagraphStyles.Heading2.Id).Append("\"/>");
                break;

            case ParagraphKind.Heading3:
                properties.Append("<w:pStyle w:val=\"").Append(DocxWriterConstants.ParagraphStyles.Heading3.Id).Append("\"/>");
                break;

            case ParagraphKind.Quote:
                properties.Append(CultureInfo.InvariantCulture, $"<w:ind w:left=\"{DocxWriterConstants.Indents.QuoteTwips}\"/>");
                break;

            case ParagraphKind.BulletItem:
            case ParagraphKind.NumberItem:
                properties.Append(CultureInfo.InvariantCulture,
                    $"<w:ind w:left=\"{DocxWriterConstants.Indents.ListLevelTwips * Math.Max(1, paragraph.ListLevel)}\"/>");
                break;

            case ParagraphKind.DescriptionItem:
                properties.Append(CultureInfo.InvariantCulture,
                    $"<w:ind w:left=\"{DocxWriterConstants.Indents.ListLevelTwips * Math.Max(1, paragraph.ListLevel)}\" w:hanging=\"{DocxWriterConstants.Indents.DescriptionHangingTwips}\"/>");
                break;
        }

        if (paragraph.Centered)
            properties.Append("<w:jc w:val=\"center\"/>");
        
        if (properties.Length > 0)
            builder.Append("<w:pPr>").Append(properties).Append("</w:pPr>");

        AppendItemMarker(builder, paragraph);
        AppendInlines(builder, paragraph.Inlines);
        builder.Append("</w:p>");
    }

    private static void AppendItemMarker(StringBuilder builder, ParagraphBlock paragraph)
    {
        if (paragraph.Kind is ParagraphKind.Heading1 or ParagraphKind.Heading2 or ParagraphKind.Heading3)
        {
            if (paragraph.HeadingNumber.Length > 0)
                AppendRuns(builder, paragraph.HeadingNumber + DocxWriterConstants.FixedText.NumberTextGap, DocxWriterConstants.Sections.NoRunProperties);
            return;
        }

        if (paragraph.Kind == ParagraphKind.BulletItem)
        {
            AppendRuns(builder, DocxWriterConstants.FixedText.BulletMarker, DocxWriterConstants.Sections.NoRunProperties);
        }
        else if (paragraph.Kind == ParagraphKind.NumberItem)
        {
            AppendRuns(builder, paragraph.Number + DocxWriterConstants.FixedText.NumberSuffix, DocxWriterConstants.Sections.NoRunProperties);
        }
        else if (paragraph.Kind == ParagraphKind.DescriptionItem && paragraph.Term.Count > 0)
        {
            AppendInlines(builder, paragraph.Term, forceBold: true);
            AppendRuns(builder, DocxWriterConstants.FixedText.DescriptionTermGap, DocxWriterConstants.Sections.NoRunProperties);
        }
    }

    private static void AppendInlines(
        StringBuilder builder, IEnumerable<Inline> inlines, bool forceBold = false, bool forceItalic = false)
    {
        foreach (var inline in inlines)
        {
            if (inline is LineBreak)
            {
                builder.Append("<w:r><w:br/></w:r>");
                continue;
            }
            
            if (inline is TextRun text)
            {
                AppendRuns(builder, text.Text, BuildRunProperties(
                    text.Bold || forceBold, text.Italic || forceItalic, text.Underline, text.Mono, text.SmallCaps));
            }
            else if (inline is RefRun reference) // defensive: the parser resolves these before rendering
            {
                AppendRuns(builder, "?" + reference.Key + "?", BuildRunProperties(
                    forceBold, forceItalic, underline: false, mono: false));
            }
        }
    }

    // Tables
    private static void AppendTable(StringBuilder builder, TableBlock table)
    {
        int columnCount = table.ColumnCount;
        
        if (columnCount == 0)
            return;

        builder.Append("<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/><w:tblBorders>");
        
        foreach (var edge in TableBorderEdges)
            builder.Append("<w:")
                   .Append(edge)
                   .Append(CultureInfo.InvariantCulture, $" w:val=\"single\" w:sz=\"{DocxWriterConstants.Tables.BorderSizeEighthPoints}\" w:space=\"0\" w:color=\"auto\"/>");
        
        builder.Append("</w:tblBorders></w:tblPr><w:tblGrid>");
        
        for (int i = 0; i < columnCount; i++)
            builder.Append("<w:gridCol/>");
        
        builder.Append("</w:tblGrid>");

        for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            bool isHeaderRow = rowIndex == 0 && table.Rows.Count > 1;
            builder.Append("<w:tr>");
            
            if (isHeaderRow)
                builder.Append("<w:trPr><w:tblHeader/></w:trPr>");
            
            for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                builder.Append("<w:tc><w:tcPr>");

                if (isHeaderRow)
                {
                    builder.Append("<w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"")
                           .Append(DocxWriterConstants.Tables.HeaderShadingHex)
                           .Append("\"/>");
                }

                builder.Append("</w:tcPr><w:p>");
                char alignment = columnIndex < table.Alignments.Count
                    ? table.Alignments[columnIndex]
                    : DocxWriterConstants.ColumnAlignment.Left;
                
                if (alignment is DocxWriterConstants.ColumnAlignment.Center or DocxWriterConstants.ColumnAlignment.Right)
                {
                    builder.Append("<w:pPr><w:jc w:val=\"")
                           .Append(alignment == DocxWriterConstants.ColumnAlignment.Center ? "center" : "right")
                           .Append("\"/></w:pPr>");
                }

                if (columnIndex < table.Rows[rowIndex].Cells.Count)
                    AppendInlines(builder, table.Rows[rowIndex].Cells[columnIndex], forceBold: isHeaderRow);
                
                builder.Append("</w:p></w:tc>");
            }
            builder.Append("</w:tr>");
        }
        builder.Append("</w:tbl><w:p/>"); // spacer: keeps adjacent tables from merging
    }

    // Table of contents / List of figures
    private static void AppendTableOfContents(StringBuilder builder, TableOfContentsBlock toc)
    {
        builder.Append("<w:p><w:pPr><w:pStyle w:val=\"")
               .Append(DocxWriterConstants.ParagraphStyles.Heading1.Id)
               .Append("\"/></w:pPr><w:r><w:t>")
               .Append(DocxWriterConstants.FixedText.TocHeading)
               .Append("</w:t></w:r></w:p>");

        if (toc.Entries.Count == 0)
        {
            builder.Append("<w:p>").Append(DocxWriterConstants.Sections.TableOfContentsFieldBegin);

            AppendRuns(builder, DocxWriterConstants.FixedText.TocRefreshHint, DocxWriterConstants.Sections.NoRunProperties);
            
            builder.Append(DocxWriterConstants.Sections.TableOfContentsFieldEnd).Append("</w:p>");
            
            return;
        }
        
        for (int entryIndex = 0; entryIndex < toc.Entries.Count; entryIndex++)
        {
            var entry = toc.Entries[entryIndex];
            builder.Append("<w:p>");
            int indentTwips = DocxWriterConstants.Indents.TocLevelTwips * Math.Max(0, entry.Level - 1);

            if (indentTwips > 0)
                builder.Append(CultureInfo.InvariantCulture, $"<w:pPr><w:ind w:left=\"{indentTwips}\"/></w:pPr>");
            
            if (entryIndex == 0)
                builder.Append(DocxWriterConstants.Sections.TableOfContentsFieldBegin);
            
            AppendRuns(builder,
                (entry.Number.Length > 0 ? entry.Number + DocxWriterConstants.FixedText.NumberTextGap : "") + entry.Text,
                DocxWriterConstants.Sections.NoRunProperties);
            
            if (entryIndex == toc.Entries.Count - 1)
                builder.Append(DocxWriterConstants.Sections.TableOfContentsFieldEnd);
            
            builder.Append("</w:p>");
        }
    }

    private static void AppendListOfFigures(StringBuilder builder, ListOfFiguresBlock listOfFigures)
    {
        builder.Append("<w:p><w:pPr><w:pStyle w:val=\"")
               .Append(DocxWriterConstants.ParagraphStyles.Heading1.Id)
               .Append("\"/></w:pPr><w:r><w:t>")
               .Append(DocxWriterConstants.FixedText.ListOfFiguresHeading)
               .Append("</w:t></w:r></w:p>");
        
        foreach (var entry in listOfFigures.Entries)
        {
            builder.Append("<w:p>");
            AppendRuns(builder,
                DocxWriterConstants.FixedText.FigureCaptionPrefix + entry.Number + DocxWriterConstants.FixedText.FigureNumberSuffix + entry.Text,
                DocxWriterConstants.Sections.NoRunProperties);
            builder.Append("</w:p>");
        }
    }

    // Images
    private static void AppendImage(StringBuilder builder, ImageBlock image, RenderContext context)
    {
        string centeredProperties = image.Centered
            ? "<w:pPr><w:jc w:val=\"center\"/></w:pPr>"
            : "";
        
        if (image.Path.Length > 0)
        {
            builder.Append("<w:p>").Append(centeredProperties);

            if (TryEmbedImage(image.Path, context, out string drawingXml, out string problem))
                builder.Append("<w:r>").Append(drawingXml).Append("</w:r>");
            else
                AppendRuns(builder, problem, BuildRunProperties(bold: false, italic: true, underline: false, mono: false));
            
            builder.Append("</w:p>");
        }
        if (image.FigureNumber > 0)
        {
            builder.Append("<w:p>").Append(centeredProperties);
            
            AppendRuns(builder,
                DocxWriterConstants.FixedText.FigureCaptionPrefix + image.FigureNumber + DocxWriterConstants.FixedText.FigureNumberSuffix,
                BuildRunProperties(bold: false, italic: true, underline: false, mono: false));
            
            AppendInlines(builder, image.Caption, forceItalic: true);
            builder.Append("</w:p>");
        }
    }

    private static bool TryEmbedImage(
        string markupPath, RenderContext context, out string drawingXml, out string problem)
    {
        drawingXml = "";
        problem = "";

        string? resolvedPath = ResolveImagePath(markupPath, context.BaseDirectory);
        
        if (resolvedPath is null)
        {
            problem = $"[image not found: {markupPath}]";
            return false;
        }

        string extension = Path.GetExtension(resolvedPath).TrimStart('.').ToLowerInvariant();
        string? contentType = extension switch
        {
            DocxWriterConstants.FileExtensions.Png => DocxWriterConstants.ContentTypes.Png,
            DocxWriterConstants.FileExtensions.Jpg or DocxWriterConstants.FileExtensions.Jpeg => DocxWriterConstants.ContentTypes.Jpeg,
            _ => null
        };

        if (contentType is null)
        {
            problem = $"[unsupported image type (png/jpg only): {markupPath}]";
            return false;
        }

        byte[] imageBytes;
        
        try
        {
            imageBytes = File.ReadAllBytes(resolvedPath);
        }
        catch
        {
            problem = $"[image could not be read: {markupPath}]";
            return false;
        }

        var (pixelWidth, pixelHeight) = ReadPixelDimensions(imageBytes, extension);
        
        if (pixelWidth <= 0 || pixelHeight <= 0)
            (pixelWidth, pixelHeight) = (DocxWriterConstants.Images.FallbackWidthPixels, DocxWriterConstants.Images.FallbackHeightPixels);

        // Shrink to the printable width, preserving aspect ratio; never enlarge.
        long displayWidthPixels = pixelWidth, displayHeightPixels = pixelHeight;
        
        if (displayWidthPixels > DocxWriterConstants.Images.MaxWidthPixels)
        {
            displayHeightPixels = Math.Max(1, displayHeightPixels * DocxWriterConstants.Images.MaxWidthPixels / displayWidthPixels);
            displayWidthPixels = DocxWriterConstants.Images.MaxWidthPixels;
        }
        
        long widthEmu = displayWidthPixels * DocxWriterConstants.Images.EmuPerPixel;
        long heightEmu = displayHeightPixels * DocxWriterConstants.Images.EmuPerPixel;
        int imageNumber = context.Media.Count + 1;
        string relationshipId = DocxWriterConstants.RelationshipIds.ImagePrefix + imageNumber;

        string storedExtension = extension == DocxWriterConstants.FileExtensions.Jpeg
            ? DocxWriterConstants.FileExtensions.Jpg
            : extension;
        
        context.Media.Add(new DocxMediaItem(
            relationshipId, $"{DocxWriterConstants.Images.FileNamePrefix}{imageNumber}.{storedExtension}", imageBytes, contentType));

        int drawingId = ++context.DrawingCount;

        drawingXml = BuildInlineDrawingXml(drawingId, relationshipId, widthEmu, heightEmu);
        return true;
    }

    private static string BuildInlineDrawingXml(
        int drawingId, string relationshipId, long widthEmu, long heightEmu)
        =>  $"""
            <w:drawing>
                <wp:inline distT="0" distB="0" distL="0" distR="0">
                    <wp:extent
                        cx="{widthEmu}"
                        cy="{heightEmu}" />
                    <wp:docPr
                        id="{drawingId}"
                        name="{DocxWriterConstants.Images.DrawingNamePrefix}{drawingId}" />
                    <wp:cNvGraphicFramePr>
                        <a:graphicFrameLocks noChangeAspect="1" />
                    </wp:cNvGraphicFramePr>
                    <a:graphic>
                        <a:graphicData uri="{DocxWriterConstants.Namespaces.Picture}">
                            <pic:pic>
                                <pic:nvPicPr>
                                    <pic:cNvPr
                                        id="{drawingId}"
                                        name="{DocxWriterConstants.Images.DrawingNamePrefix}{drawingId}" />
                                    <pic:cNvPicPr />
                                </pic:nvPicPr>
                                <pic:blipFill>
                                    <a:blip r:embed="{relationshipId}" />
                                    <a:stretch>
                                        <a:fillRect />
                                    </a:stretch>
                                </pic:blipFill>
                                <pic:spPr>
                                    <a:xfrm>
                                        <a:off x="0" y="0" />
                                        <a:ext
                                            cx="{widthEmu}"
                                            cy="{heightEmu}" />
                                    </a:xfrm>
                                    <a:prstGeom prst="rect">
                                        <a:avLst />
                                    </a:prstGeom>
                                </pic:spPr>
                            </pic:pic>
                        </a:graphicData>
                    </a:graphic>
                </wp:inline>
            </w:drawing>
            """;
    
    private static string? ResolveImagePath(string markupPath, string? baseDirectory)
    {
        try
        {
            if (Path.IsPathRooted(markupPath))
                return File.Exists(markupPath) ? markupPath : null;
            
            if (baseDirectory is null)
                return null;
            
            string relativePath = markupPath.Replace('/', Path.DirectorySeparatorChar);
            string documentLocal = Path.GetFullPath(Path.Combine(baseDirectory, relativePath));
            
            if (File.Exists(documentLocal))
                return documentLocal;
            
            string libraryLocal = Path.GetFullPath(Path.Combine(baseDirectory, "..", relativePath));
            return File.Exists(libraryLocal) ? libraryLocal : null;
        }
        catch
        {
            return null;
        }
    }

    private static (int Width, int Height) ReadPixelDimensions(byte[] bytes, string extension)
    {
        try
        {
            if (extension == DocxWriterConstants.FileExtensions.Png && IsPngWithHeader(bytes))
                return (ReadInt32BigEndian(bytes, 16), ReadInt32BigEndian(bytes, 20)); // IHDR: width, height

            if (extension is DocxWriterConstants.FileExtensions.Jpg or DocxWriterConstants.FileExtensions.Jpeg && IsJpeg(bytes))
            {
                int position = 2; // just past the FF D8 start-of-image marker

                while (position + 9 < bytes.Length)
                {
                    if (bytes[position] != 0xFF)
                    {
                        position++;
                        continue;
                    }

                    byte marker = bytes[position + 1];

                    if (marker == 0xFF) // fill byte
                    {
                        position++;
                        continue;
                    }

                    if (marker >= 0xD0 && marker <= 0xD9) // standalone markers
                    {
                        position += 2;
                        continue;
                    }
                    
                    if (marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
                        return (ReadUInt16BigEndian(bytes, position + 7), ReadUInt16BigEndian(bytes, position + 5));
                    
                    position += 2 + ReadUInt16BigEndian(bytes, position + 2); // skip segment by its length
                }
            }
        }
        catch { }

        return (DocxWriterConstants.Images.FallbackWidthPixels, DocxWriterConstants.Images.FallbackHeightPixels);
    }

    // Runs & escaping
    private static string BuildRunProperties(
        bool bold, bool italic, bool underline, bool mono, bool smallCaps = false)
    {
        if (!(bold || italic || underline || mono || smallCaps))
            return DocxWriterConstants.Sections.NoRunProperties;
        
        var properties = new StringBuilder("<w:rPr>");
        
        if (mono)
            properties.Append("<w:rFonts w:ascii=\"")
                      .Append(DocxWriterConstants.Typography.MonospaceFont)
                      .Append("\" w:hAnsi=\"")
                      .Append(DocxWriterConstants.Typography.MonospaceFont)
                      .Append("\" w:cs=\"")
                      .Append(DocxWriterConstants.Typography.MonospaceFont)
                      .Append("\"/>");
        
        if (bold)
            properties.Append("<w:b/>");
        
        if (italic)
            properties.Append("<w:i/>");
        
        if (smallCaps)
            properties.Append("<w:smallCaps/>");
        
        if (underline)
            properties.Append("<w:u w:val=\"single\"/>");
        
        properties.Append("</w:rPr>");
        
        return properties.ToString();
    }

    private static void AppendRuns(StringBuilder builder, string text, string runProperties)
    {
        var segments = text.Split('\t');

        for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
        {
            if (segmentIndex > 0)
                builder.Append("<w:r>")
                       .Append(runProperties)
                       .Append("<w:tab/></w:r>");
            
            if (segments[segmentIndex].Length > 0)
                builder.Append("<w:r>")
                       .Append(runProperties)
                       .Append("<w:t xml:space=\"preserve\">")
                       .Append(EscapeText(segments[segmentIndex]))
                       .Append("</w:t></w:r>");
        }
    }

    private static string EscapeText(string text)
    {
        var escaped = new StringBuilder(text.Length);

        foreach (var character in text)
        {
            if ((character < ' ' && character != '\t') || character >= (char)0xFFFE)
                continue;
            
            switch (character)
            {
                case '&':
                    escaped.Append("&amp;");
                    break;
                case '<':
                    escaped.Append("&lt;");
                    break;
                case '>':
                    escaped.Append("&gt;");
                    break;
                default:
                    escaped.Append(character);
                    break;
            }
        }
        return escaped.ToString();
    }

    private static bool IsPngWithHeader(byte[] bytes)
        => bytes.Length >= 24 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;

    private static bool IsJpeg(byte[] bytes)
        => bytes.Length > 4 && bytes[0] == 0xFF && bytes[1] == 0xD8;

    private static int ReadInt32BigEndian(byte[] bytes, int offset)
        => (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

    private static int ReadUInt16BigEndian(byte[] bytes, int offset)
        => (bytes[offset] << 8) | bytes[offset + 1];

    private static string EscapeAttribute(string text)
        => EscapeText(text).Replace("\"", "&quot;");
}