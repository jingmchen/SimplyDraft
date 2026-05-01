namespace SimplyDraft.App.Models;

public sealed class FileIconMapping
{
    // Extensions -> Emoji
    public Dictionary<string, string> ExtensionIcons {get; set;}
        = new (StringComparer.OrdinalIgnoreCase)
        {
            // .NET / C#
            {".cs",            "⚙️"},
            {".csproj",        "📦"},
            {".sln",           "🗂️"},
            {".razor",         "🔷"},
            {".axaml",         "🎨"},
            {".xaml",          "🎨"},
 
            // Web
            {".html",          "🌐"},
            {".htm",           "🌐"},
            {".css",           "🎨"},
            {".scss",          "🎨"},
            {".less",          "🎨"},
            {".js",            "📜"},
            {".mjs",           "📜"},
            {".ts",            "📘"},
            {".tsx",           "⚛️"},
            {".jsx",           "⚛️"},
            {".vue",           "💚"},
            {".svelte",        "🔥"},
 
            // Data / Config
            {".json",          "📋"},
            {".jsonc",         "📋"},
            {".xml",           "📰"},
            {".yaml",          "📄"},
            {".yml",           "📄"},
            {".toml",          "📄"},
            {".ini",           "⚙️"},
            {".env",           "🔐"},
            {".editorconfig",  "⚙️"},
 
            // Images
            {".png",           "🖼️"},
            {".jpg",           "🖼️"},
            {".jpeg",          "🖼️"},
            {".gif",           "🖼️"},
            {".svg",           "✏️"},
            {".ico",           "🖼️"},
            {".webp",          "🖼️"},
            {".bmp",           "🖼️"},
 
            // Docs
            {".md",            "📝"},
            {".mdx",           "📝"},
            {".txt",           "📄"},
            {".pdf",           "📕"},
            {".docx",          "📘"},
            {".doc",           "📘"},
            {".xlsx",          "📗"},
            {".xls",           "📗"},
            {".pptx",          "📙"},
            {".ppt",           "📙"},
 
            // Shell / Scripts
            {".sh",            "💻"},
            {".bash",          "💻"},
            {".zsh",           "💻"},
            {".ps1",           "🔷"},
            {".bat",           "💻"},
            {".cmd",           "💻"},
 
            // Languages
            {".py",            "🐍"},
            {".rb",            "💎"},
            {".go",            "🐹"},
            {".rs",            "🦀"},
            {".cpp",           "⚙️"},
            {".c",             "⚙️"},
            {".h",             "⚙️"},
            {".java",          "☕"},
            {".kt",            "🎯"},
            {".swift",         "🦅"},
            {".dart",          "🎯"},
            {".php",           "🐘"},
            {".sql",           "🗄️"},
            {".r",             "📊"},
            {".lua",           "🌙"},
 
            // Archives
            {".zip",           "🗜️"},
            {".tar",           "🗜️"},
            {".gz",            "🗜️"},
            {".rar",           "🗜️"},
            {".7z",            "🗜️"},
 
            // Git
            {".gitignore",     "🔴"},
            {".gitattributes", "🔴"},
 
            // Lock / special
            {".lock",          "🔒"},
            {".log",           "📋"}
        };
    
    public Dictionary<string, string> FolderIcons {get; set;}
        = new (StringComparer.OrdinalIgnoreCase)
        {
            {"src",           "📂"},
            {"source",        "📂"},
            {"test",          "🧪"},
            {"tests",         "🧪"},
            {"assets",        "🎨"},
            {"images",        "🖼️"},
            {"img",           "🖼️"},
            {"icons",         "🖼️"},
            {"public",        "🌐"},
            {"dist",          "📦"},
            {"build",         "🔨"},
            {"docs",          "📚"},
            {"documentation", "📚"},
            {"scripts",       "💻"},
            {"config",        "⚙️"},
            {"configs",       "⚙️"},
            {".github",       "🐱"},
            {".vscode",       "🔷"},
            {"models",        "🗂️"},
            {"views",         "🗂️"},
            {"controllers",   "🗂️"},
            {"services",      "🗂️"},
            {"components",    "🧩"},
            {"utils",         "🔧"},
            {"helpers",       "🔧"},
            {"middleware",    "🔗"},
            {"migrations",    "🔄"},
            {"themes",        "🎨"}
        };
    
    // Fall-back emojis
    public string DefaultFileIcon {get; set;} = "📄";
    public string DefaultFolderIcon {get; set;} = "📁";
    public string DefaultFolderOpenIcon {get; set;} = "📂";

    // Lookup
    public string GetIcon(FileNode node, bool isExpanded = false)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.IsDirectory)
        {
            return FolderIcons.TryGetValue(node.Name, out var folderIcon)
                ? folderIcon
                : isExpanded ? DefaultFolderOpenIcon : DefaultFolderIcon;
        }

        if (!string.IsNullOrEmpty(node.Extension)
            && ExtensionIcons.TryGetValue(node.Extension, out var extIcon))
            return extIcon;

        if (ExtensionIcons.TryGetValue(node.Name, out var nameIcon))
            return nameIcon;

        return DefaultFileIcon;
    }

    public string GetIcon(string name, string extension, bool isDirectory, bool isExpanded)
        => GetIcon(
            new FileNode
            {
                Name = name,
                Extension = extension,
                IsDirectory = isDirectory
            },
            isExpanded
        );
}