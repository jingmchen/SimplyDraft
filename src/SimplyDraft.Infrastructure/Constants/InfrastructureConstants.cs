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
        internal const string Xml = ".xml";
        internal const string Png = ".png";
        internal const string Jpg = ".jpg";
        internal const string Jpeg = ".jpeg";
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
}