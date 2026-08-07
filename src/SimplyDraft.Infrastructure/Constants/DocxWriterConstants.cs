// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Infrastructure.Constants;

internal static class DocxWriterConstants
{
    internal const string XmlDeclaration = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";

    internal static class Namespaces
    {
        /// <summary>
        /// WordprocessingML main namespace — prefix <c>w</c>,the bulk of every part
        /// </summary>
        internal const string Main = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        /// <summary>
        /// Relationship-reference namespace — prefix <c>r</c> (<c>r:id</c>, <c>r:embed</c>)
        /// </summary>
        internal const string Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        /// <summary>
        /// Inline-drawing wrapper namespace — prefix <c>wp</c> (<c>wp:inline</c>, <c>wp:extent</c>).
        /// </summary>
        internal const string WordprocessingDrawing = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";

        /// <summary>
        /// DrawingML core namespace — prefix <c>a</c> (<c>a:graphic</c>, <c>a:blip</c>).
        /// </summary>
        internal const string DrawingMl = "http://schemas.openxmlformats.org/drawingml/2006/main";

        /// <summary>
        /// DrawingML picture namespace — prefix <c>pic</c>; doubles as the <c>a:graphicData</c> URI.
        /// </summary>
        internal const string Picture = "http://schemas.openxmlformats.org/drawingml/2006/picture";

        /// <summary>Namespace of the OPC relationship parts (<c>_rels/.rels</c> and friends).</summary>
        internal const string PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

        /// <summary>Namespace of <c>[Content_Types].xml</c>.</summary>
        internal const string PackageContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";
    }

    /// <summary>Zip entry names — where each part lives inside the package.</summary>
    internal static class PartNames
    {
        internal const string ContentTypes = "[Content_Types].xml";
        internal const string RootRelationships = "_rels/.rels";
        internal const string DocumentRelationships = "word/_rels/document.xml.rels";
        internal const string Document = "word/document.xml";
        internal const string Styles = "word/styles.xml";
        internal const string Header = "word/header1.xml";
        internal const string Footer = "word/footer1.xml";
        internal const string Settings = "word/settings.xml";

        /// <summary>Folder (with trailing slash) embedded images are stored under.</summary>
        internal const string MediaFolder = "word/media/";
    }

    /// <summary>
    /// Relationship ids.
    /// An id only needs to be unique within its own <c>.rels</c> part,
    /// which is why the root and document parts can both use <c>rId1</c>.
    /// </summary>
    internal static class RelationshipIds
    {
        /// <summary>Root rels → the main document part.</summary>
        internal const string OfficeDocument = "rId1";

        /// <summary>Document rels → the styles part.</summary>
        internal const string Styles = "rId1";

        internal const string Header = "rIdHdr";
        internal const string Footer = "rIdFtr";
        internal const string Settings = "rIdSet";

        /// <summary>Prefix for image relationships: <c>rIdImg1</c>, <c>rIdImg2</c>, … in embed order.</summary>
        internal const string ImagePrefix = "rIdImg";
    }

    /// <summary>Relationship type URIs — <see cref="Namespaces.Relationships"/> plus a role suffix.</summary>
    internal static class RelationshipTypes
    {
        internal const string OfficeDocument = Namespaces.Relationships + "/officeDocument";
        internal const string Styles = Namespaces.Relationships + "/styles";
        internal const string Header = Namespaces.Relationships + "/header";
        internal const string Footer = Namespaces.Relationships + "/footer";
        internal const string Settings = Namespaces.Relationships + "/settings";
        internal const string Image = Namespaces.Relationships + "/image";
    }

    internal static class RelationshipTargets
    {
        internal const string Document = "word/document.xml";
        internal const string Styles = "styles.xml";
        internal const string Header = "header1.xml";
        internal const string Footer = "footer1.xml";
        internal const string Settings = "settings.xml";

        /// <summary>Folder (with trailing slash) image targets point into.</summary>
        internal const string MediaFolder = "media/";
    }

    /// <summary>
    /// MIME content types declared in <c>[Content_Types].xml</c>
    /// </summary>
    internal static class ContentTypes
    {
        private const string WordprocessingMlPrefix =
            "application/vnd.openxmlformats-officedocument.wordprocessingml.";

        internal const string Relationships = "application/vnd.openxmlformats-package.relationships+xml";
        internal const string Xml = "application/xml";
        internal const string Png = "image/png";
        internal const string Jpeg = "image/jpeg";
        internal const string MainDocument = WordprocessingMlPrefix + "document.main+xml";
        internal const string Styles = WordprocessingMlPrefix + "styles+xml";
        internal const string Header = WordprocessingMlPrefix + "header+xml";
        internal const string Footer = WordprocessingMlPrefix + "footer+xml";
        internal const string Settings = WordprocessingMlPrefix + "settings+xml";
    }

    internal static class Page
    {
        /// <summary>210 mm.</summary>
        internal const int WidthTwips = 11906;

        /// <summary>297 mm.</summary>
        internal const int HeightTwips = 16838;

        /// <summary>2 cm, applied to all four sides.</summary>
        internal const int MarginTwips = 1134;

        /// <summary>Header/footer distance from the paper edge: 1.25 cm.</summary>
        internal const int HeaderFooterMarginTwips = 708;

        internal const int GutterTwips = 0;
    }

    internal static class Typography
    {
        /// <summary>Body font when the caller passes none.</summary>
        internal const string FallbackFont = "Calibri";

        /// <summary>Body size in points when the caller passes none or a non-positive value.</summary>
        internal const int FallbackFontSizePoints = 11;

        /// <summary>OOXML measures font size in half-points (<c>w:sz</c>): 11 pt → 22.</summary>
        internal const int HalfPointsPerPoint = 2;

        /// <summary>Font for mono (<c>\texttt</c>-style) runs.</summary>
        internal const string MonospaceFont = "Consolas";
    }

    internal static class ParagraphStyles
    {
        /// <summary>The default style every heading is based on.</summary>
        internal const string NormalId = "Normal";
        internal const string NormalName = "Normal";

        internal static class Heading1
        {
            internal const string Id = "Heading1";
            internal const string Name = "heading 1";
            internal const int SpacingBeforeTwips = 240;
            internal const int SpacingAfterTwips = 120;
            internal const int OutlineLevel = 0;
            internal const int SizeHalfPoints = 32;
        }

        internal static class Heading2
        {
            internal const string Id = "Heading2";
            internal const string Name = "heading 2";
            internal const int SpacingBeforeTwips = 200;
            internal const int SpacingAfterTwips = 100;
            internal const int OutlineLevel = 1;
            internal const int SizeHalfPoints = 26;
        }

        internal static class Heading3
        {
            internal const string Id = "Heading3";
            internal const string Name = "heading 3";
            internal const int SpacingBeforeTwips = 160;
            internal const int SpacingAfterTwips = 80;
            internal const int OutlineLevel = 2;
            internal const int SizeHalfPoints = 24;
        }
    }

    internal static class Indents
    {
        /// <summary>Left indent of a quote paragraph.</summary>
        internal const int QuoteTwips = 720;

        /// <summary>Left indent added per list nesting level (bullet, numbered and description items).</summary>
        internal const int ListLevelTwips = 720;

        /// <summary>Hanging indent that pulls a description term left of its definition text.</summary>
        internal const int DescriptionHangingTwips = 360;

        /// <summary>Left indent added per TOC level below the first.</summary>
        internal const int TocLevelTwips = 240;
    }

    internal static class Tables
    {
        /// <summary>Border weight in eighths of a point (the <c>w:sz</c> unit): a 0.5 pt hairline.</summary>
        internal const int BorderSizeEighthPoints = 4;

        /// <summary>Header-row shading — Word's standard light grey.</summary>
        internal const string HeaderShadingHex = "E7E6E6";
    }

    internal static class HorizontalRule
    {
        /// <summary>Border weight in eighths of a point: 0.75 pt.</summary>
        internal const int BorderSizeEighthPoints = 6;

        /// <summary>Gap between the (empty) paragraph text and the border, in points.</summary>
        internal const int BorderSpacePoints = 1;
    }

    internal static class Images
    {
        internal const int EmuPerPixel = 9525;
        internal const int MaxWidthPixels = 642;

        /// <summary>Assumed size when the PNG/JPEG dimensions cannot be read from the file header.</summary>
        internal const int FallbackWidthPixels = 400;
        internal const int FallbackHeightPixels = 300;

        /// <summary>Media file-name prefix: <c>image1.png</c>, <c>image2.jpg</c>, …</summary>
        internal const string FileNamePrefix = "image";

        /// <summary>Display-name prefix on <c>wp:docPr</c>/<c>pic:cNvPr</c>: "Picture 1", "Picture 2", …</summary>
        internal const string DrawingNamePrefix = "Picture ";
    }

    internal static class FieldInstructions
    {
        /// <summary>
        /// Table of contents:
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///     <item>
        ///         <description>outline levels 1–3 (<c>\o</c>)</description>
        ///     </item>
        ///     <item>
        ///         <description>hyperlinked entries (<c>\h</c>)</description>
        ///     </item>
        ///     <item>
        ///         <description>no tab leaders in web view (<c>\z</c>)</description>
        ///     </item>
        ///     <item>
        ///         <description>driven by outline levels (<c>\u</c>)</description>
        ///     </item>
        /// </list>
        /// </remarks>
        internal const string TableOfContents = " TOC \\o \"1-3\" \\h \\z \\u ";

        /// <summary>Current page number (footer).</summary>
        internal const string CurrentPage = " PAGE ";

        /// <summary>Total page count (footer).</summary>
        internal const string PageCount = " NUMPAGES ";
    }

    internal static class FixedText
    {
        internal const string TocHeading = "Contents";
        internal const string ListOfFiguresHeading = "List of Figures";

        /// <summary>Shown inside an empty TOC field so the reader knows how to populate it.</summary>
        internal const string TocRefreshHint = "Right-click and choose Update Field to build the table of contents.";

        /// <summary>The footer reads "Page {PAGE} of {NUMPAGES}".</summary>
        internal const string FooterPagePrefix = "Page ";
        internal const string FooterOfSeparator = " of ";

        /// <summary>Marker before a bullet-list item.</summary>
        internal const string BulletMarker = "• ";

        /// <summary>After a numbered-list item's number: "1. first".</summary>
        internal const string NumberSuffix = ". ";

        /// <summary>Gap between a bolded description term and its definition (two spaces).</summary>
        internal const string DescriptionTermGap = "  ";

        /// <summary>Space between a heading or TOC number and its text: "1.2 Title".</summary>
        internal const string NumberTextGap = " ";

        /// <summary>Captions read "Figure 3: …" — under images and in the list of figures.</summary>
        internal const string FigureCaptionPrefix = "Figure ";
        internal const string FigureNumberSuffix = ": ";
    }
    
    internal static class ColumnAlignment
    {
        internal const char Left = 'l';
        internal const char Center = 'c';
        internal const char Right = 'r';
    }
}