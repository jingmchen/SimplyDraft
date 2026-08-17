// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Logging;

public static class LogEventIDs
{
    public static class Infrastructure
    {
        public static class SettingsProvider
        {
            public const int FileNotFound = 1001;
            public const int FileUnableToRead = 1002;
            public const int FileInvalidOrEmpty = 1003;
            public const int FileUnableToSave = 1004;
            public const int TempCleanupFailed = 1005;
        }

        public static class Library
        {
            public const int UnableToDeleteFile = 1101;
        }

        public static class LibraryWatcher
        {
            public const int FailedToCreateWatcher = 1201;
        }
    }

    public static class UI
    {
        public static class TermsService
        {
            public const int TermsAccepted = 2001;
            public const int TermsDeclined = 2002;
            public const int TermsUnavailable = 2003;
            public const int UnableToPersistAcceptance = 2004;
        }

        public static class LibraryActions
        {
            public const int GeneratedChildNotScanned = 2101;
            public const int GeneratedChildWrongKind = 2102;
        }

        public static class EditorWindowViewModel
        {
            public const int SavedSuccess = 2201;
            public const int SavedFailed = 2202;
            public const int ExportFailed = 2203;
        }

        public static class GenerateChildWindowViewModel
        {
            public const int CreateChildSuccess = 2301;
            public const int CreateChildFailed = 2302;
        }

        public static class EditorWindow
        {
            public const int UnableToSaveSettings = 2401;
        }
    }
}