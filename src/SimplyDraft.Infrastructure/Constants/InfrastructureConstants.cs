// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Infrastructure.Constants;

internal static class InfrastructureConstants
{
    internal static class Bundled
    {
        internal static class FileName
        {
            internal const string AppSettings = "appsettings.json";
        }

        internal static class FolderName
        {
            internal const string Samples = "samples";
        }
    }

    internal static class UserData
    {
        internal static class FileName
        {
            internal const string AppSettings = "appsettings.json";
            internal const string LatestLog = "latest.log";
            internal const string ArchivedLog = "archived.log";
        }

        internal static class FolderName
        {
            internal const string Logs = "logs";
            internal const string DocumentsParent = "Documents";
            internal const string Templates = "Templates";
            internal const string Children = "Children";
            internal const string Exports = "Exports";
            internal const string Trash = "Trash";
        }
    }

    internal static class FileExtension
    {
        internal const string Template = ".sdt";
        internal const string Children = ".sdc";
        internal const string Docx = ".docx";
        internal const string Txt = ".txt";
    }

    internal static class Service
    {
        internal static class AppInfo
        {
            internal const string ProductDefault = "SimplyDraft";
            internal const string CompanyDefault = "Tan Jing Ming";
            internal const string AuthorsDefault = "Tan Jing Ming";
            internal const string CopyrightDefault = $"Copyright (c) {CompanyDefault}. Use of this software is governed by LICENSE.md.";
        }
    }

    internal static class Schema
    {
        internal static class DocxFormat
        {
            internal const string NsW = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            internal const string NsR = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            internal const string NsWp = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
            internal const string NsA = "http://schemas.openxmlformats.org/drawingml/2006/main";
            internal const string NsPic = "http://schemas.openxmlformats.org/drawingml/2006/picture";
            internal const string RelBase = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            internal const string XmlDecl = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";
            internal const string DocRoot =
                "<w:document xmlns:w=\"" + NsW + "\" xmlns:r=\"" + NsR + "\" xmlns:wp=\"" + NsWp +
                "\" xmlns:a=\"" + NsA + "\" xmlns:pic=\"" + NsPic + "\"><w:body>";
            internal const string RootRels =
                XmlDecl +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"" + RelBase + "/officeDocument\" Target=\"word/document.xml\"/>" +
                "</Relationships>";
        }
    }
}