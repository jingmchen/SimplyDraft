// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Common;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Enums;
using SimplyDraft.Engine.Templates;
using SimplyDraft.Engine.Utils;
using SimplyDraft.Infrastructure.Constants;
using SimplyDraft.Infrastructure.Utils;

namespace SimplyDraft.Infrastructure.Services;

public sealed partial class Library : ILibrary
{
    private const int MaxInputDepth = 8;
    private readonly IScriptingEngine _scripting;
    private readonly ILibraryPaths _libraryPaths;
    private readonly ILogger<Library> _logger;
    private static Assembly _assembly => typeof(Library).Assembly; // Bundle sample files with this dll instead of apphost

    public Library(IScriptingEngine scripting, ILibraryPaths libraryPaths, ILogger<Library> logger)
    {
        _scripting = scripting ?? throw new ArgumentNullException(nameof(scripting));
        _libraryPaths = libraryPaths ?? throw new ArgumentNullException(nameof(libraryPaths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ─── PUBLIC METHODS ────────────────────────
    public List<LibraryItem> Scan()
    {
        var items = new List<LibraryItem>();

        foreach (var f in EnumerateFilesSafe(_libraryPaths.TemplatesFolder, InfrastructureConstants.FileExtension.Template))
            items.Add(ScanSingle(f, LibraryItemKind.Template));
        
        foreach (var f in EnumerateFilesSafe(_libraryPaths.ChildrenFolder, InfrastructureConstants.FileExtension.Children))
            items.Add(ScanSingle(f, LibraryItemKind.Child));
        
        items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return items;
    }

    public string CreateTemplate(string name)
    {
        var fm = new FrontMatter{Name = name, HasMarkup = true};
        fm.Variables["name"] = "";

        string path = DirectoryHelper.MakeUniquePath(
            _libraryPaths.TemplatesFolder,
            FileNameSanitizer.Sanitize(name),
            InfrastructureConstants.FileExtension.Template);
        
        AtomicFile.WriteTo(path, FrontMatterParser.Write(fm, ""));

        return path;
    }

    public TemplateDocument LoadTemplate(string path)
    {
        var (fm, body, warns) = FrontMatterParser.Parse(File.ReadAllText(path));
        return new TemplateDocument
        {
            FilePath = path,
            Fm = fm,
            Body = body,
            LoadDiagnostics = warns
        };
    }

    public void SaveTemplate(TemplateDocument doc)
        => AtomicFile.WriteTo(doc.FilePath, FrontMatterParser.Write(doc.Fm, doc.Body));
    
    public string CreateChild(string templatePath, string name)
    {
        var template = LoadTemplate(templatePath);
        var fm = new FrontMatter {Name = name};

        string path = DirectoryHelper.MakeUniquePath(
            _libraryPaths.ChildrenFolder,
            FileNameSanitizer.Sanitize(name),
            InfrastructureConstants.FileExtension.Children);
        
        fm.TemplatePath = DirectoryHelper.MakeRelativePath(path, templatePath);

        foreach (var kv in template.Fm.Variables)
            fm.Values[kv.Key] = kv.Value;
        
        AtomicFile.WriteTo(path, FrontMatterParser.Write(fm, ""));

        return path;
    }

    public string CreateBakedChild(string templatePath, string name, string generatedText, FrontMatter templateFm)
    {
        var fm = new FrontMatter{Name = name};

        string path = DirectoryHelper.MakeUniquePath(
            _libraryPaths.ChildrenFolder,
            FileNameSanitizer.Sanitize(name),
            InfrastructureConstants.FileExtension.Children);
        
        fm.TemplatePath = DirectoryHelper.MakeRelativePath(path, templatePath);
        fm.HasMarkup = templateFm.HasMarkup;
        fm.DocxFont = templateFm.DocxFont;
        fm.DocxSizePt = templateFm.DocxSizePt;
        fm.DocxHeader = templateFm.DocxHeader;

        string body = generatedText.Replace("{", "{{").Replace("}", "}}");

        AtomicFile.WriteTo(path, FrontMatterParser.Write(fm, body));

        return path;
    }

    public ChildDocument LoadChild(string path)
    {
        var (fm, body, warns) = FrontMatterParser.Parse(File.ReadAllText(path));

        return new ChildDocument
        {
            FilePath = path,
            Fm = fm,
            Body = body,
            LoadDiagnostics = warns
        };
    }

    public void SaveChild(ChildDocument doc)
        => AtomicFile.WriteTo(doc.FilePath, FrontMatterParser.Write(doc.Fm, doc.Body));
    
    public string Duplicate(LibraryItem item)
    {
        string directory = Path.GetDirectoryName(item.FilePath)!;
        string extension = Path.GetExtension(item.FilePath);

        var (fm, body, _) = FrontMatterParser.Parse(File.ReadAllText(item.FilePath));
        string newName = (string.IsNullOrWhiteSpace(fm.Name) ? item.Name : fm.Name!) + " copy";
        fm.Name = newName;
        string target = DirectoryHelper.MakeUniquePath(directory, FileNameSanitizer.Sanitize(newName), extension);

        AtomicFile.WriteTo(target, FrontMatterParser.Write(fm, body));

        return target;
    }

    public string Rename(LibraryItem item, string newName)
    {
        var (fm, body, _) = FrontMatterParser.Parse(File.ReadAllText(item.FilePath));
        fm.Name = newName;

        AtomicFile.WriteTo(item.FilePath, FrontMatterParser.Write(fm, body));

        string directory = Path.GetDirectoryName(item.FilePath)!;
        string extension = Path.GetExtension(item.FilePath);
        string target = Path.Combine(directory, FileNameSanitizer.Sanitize(newName) + extension);

        if (DirectoryHelper.PathsEqual(target, item.FilePath))
            return item.FilePath;
        
        if (File.Exists(target))
            target = DirectoryHelper.MakeUniquePath(directory, FileNameSanitizer.Sanitize(newName), extension);
        
        File.Move(item.FilePath, target);

        if (item.Kind == LibraryItemKind.Template)
            RetargetChildren(item.FilePath, target);
        
        return target;
    }

    public void MoveToTrash(LibraryItem item)
    {
        string dest = Path.Combine(
            _libraryPaths.TrashFolder,
            $"{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}_{Path.GetFileName(item.FilePath)}");
        
        int n = 2;

        while (File.Exists(dest))
            dest = Path.Combine(
                _libraryPaths.TrashFolder,
                $"{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}_{n++}_{Path.GetFileName(item.FilePath)}");
        
        File.Move(item.FilePath, dest);
        File.SetLastWriteTime(dest, DateTime.Now);
    }

    public void PurgeTrash(int days)
    {
        if (!Directory.Exists(_libraryPaths.TrashFolder))
            return;
        
        var cutoff = DateTime.Now.AddDays(-Math.Max(0, days));

        foreach (var f in Directory.EnumerateFiles(_libraryPaths.TrashFolder))
        {
            try
            {
                if (File.GetLastWriteTime(f) < cutoff)
                    File.Delete(f);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                LogUnableToDeleteFile(ex, Path.GetFullPath(f));
            }
        }
    }

    public (string Text, List<Diagnostic> Warnings) ExpandIncludes(string contentText)
    {
        var warns = new List<Diagnostic>();

        var chain = new HashSet<string>(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        
        string Expand(string text, int depth)
        {
            if (!text.Contains("\\input{", StringComparison.Ordinal))
                return text;
            
            var sb = new System.Text.StringBuilder();
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            bool verbatim = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].Trim();

                if (t == "\\begin{verbatim}")
                    verbatim = true;
                else if (t == "\\end{verbatim}")
                    verbatim = false;
                
                if (verbatim || !t.StartsWith("\\input{", StringComparison.Ordinal) || !t.EndsWith('}'))
                {
                    Append(lines[i], i);
                    continue;
                }

                string name = t["\\input{".Length..^1].Trim();

                if (name.Length == 0)
                {
                    Warn(i, "\\input needs a template name, e.g. \\input{Boilerplate}");
                    Append("[missing input: ?]", i);
                    continue;
                }

                if (depth >= MaxInputDepth)
                {
                    Warn(i, $"\\input{{{name}}} skipped — nesting deeper than {MaxInputDepth} levels");
                    Append($"[missing input: {name}]", i);
                    continue;
                }

                string? file = ResolveInputName(name);

                if (file is null)
                {
                    Warn(i, $"\\input{{{name}}} — no template with that file or display name");
                    Append($"[missing input: {name}]", i);
                    continue;
                }

                string full = Path.GetFullPath(file);

                if (!chain.Add(full))
                {
                    Warn(i, $"\\input{{{name}}} skipped — circular include");
                    Append($"[missing input: {name}]", i);
                    continue;
                }

                try
                {
                    var (_, body, _) = FrontMatterParser.Parse(File.ReadAllText(file));
                    var (_, content) = BodySplitter.Split(body);
                    Append(Expand(content.TrimEnd('\n'), depth + 1), i);
                }
                catch (Exception ex)
                {
                    Warn(i, $"\\input{{{name}}} could not be read: {ex.Message}");
                    Append($"[missing input: {name}]", i);
                }
                finally
                {
                    chain.Remove(full);
                }
            }

            return sb.ToString();

            void Append(string s, int i)
            {
                sb.Append(s);
                if (i < lines.Length - 1) sb.Append('\n');
            }
            void Warn(int i, string msg)
                => warns.Add(new Diagnostic(
                    DiagnosticCode.MarkupWarning, DiagnosticSeverity.Warning, msg, i + 1, 1));
        }

        return (Expand(contentText ?? "", 0), warns);
    }

    public (DateTime Created, DateTime Modified) GetTimestamps(string filePath)
    {
        DateTime created, modified;
        try {created = File.GetCreationTime(filePath);} catch {created = DateTime.Now;}
        try {modified = File.GetLastWriteTime(filePath);} catch {modified = DateTime.Now;}
        return (created, modified);
    }

    public IReadOnlyList<string> ListSeedTemplates()
        => SeedTemplates()
            .Select(seed => seed.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    
    public string CreateTemplateFromSeed(string templateName, string? newName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);

        foreach (var (name, resource) in SeedTemplates())
        {
            if (!string.Equals(name, templateName, StringComparison.OrdinalIgnoreCase))
                continue;
            
            return newName is null
                ? WriteSeedCopy(resource, fileBaseName: templateName)
                : WriteRenamedSeedCopy(resource, newName.Trim());
        }
        throw new ArgumentException($"no bundled template named '{templateName}'", nameof(templateName));
    }

    public int SeedMissingTemplates()
    {
        int added = 0;

        foreach (var (name, resource) in SeedTemplates())
        {
            string target = Path.Combine(
                _libraryPaths.TemplatesFolder,
                FileNameSanitizer.Sanitize(name) + InfrastructureConstants.FileExtension.Template);
            
            if (File.Exists(target))
                continue;
            
            AtomicFile.WriteTo(target, ReadResource(resource));
            added++;
        }
        return added;
    }

    // ─── PRIVATE METHODS ───────────────────────
    private void RetargetChildren(string oldTemplatePath, string newTemplatePath)
    {
        foreach (var f in EnumerateFilesSafe(_libraryPaths.ChildrenFolder, InfrastructureConstants.FileExtension.Children))
        {
            try
            {
                var (fm, body, _) = FrontMatterParser.Parse(File.ReadAllText(f));
                
                if (fm.TemplatePath == null)
                    continue;
                
                var resolved = ResolveTemplatePath(f, fm.TemplatePath);

                if (resolved != null && DirectoryHelper.PathsEqual(resolved, oldTemplatePath))
                {
                    fm.TemplatePath = DirectoryHelper.MakeRelativePath(f, newTemplatePath);
                    AtomicFile.WriteTo(f, FrontMatterParser.Write(fm, body));
                }
            }
            catch { }
        }
    }

    private static LibraryItem ScanSingle(string file, LibraryItemKind kind)
    {
        string name = Path.GetFileNameWithoutExtension(file);
        string? tref = null;
        bool baked = false;

        try
        {
            var (fm, body, _) = FrontMatterParser.Parse(File.ReadAllText(file));

            if (!string.IsNullOrWhiteSpace(fm.Name))
                name = fm.Name!;
            
            if (kind == LibraryItemKind.Child)
            {
                baked = !string.IsNullOrWhiteSpace(body);
                tref = fm.TemplatePath == null
                    ? null
                    : Path.GetFileNameWithoutExtension(fm.TemplatePath);
            }
        }
        catch { /* Unreadable file */ }

        DateTime modified;

        try {modified = File.GetLastWriteTime(file);}
        catch {modified = DateTime.MinValue;}

        return new LibraryItem(file, kind, name, tref, modified, baked);
    }

    private static string? ResolveTemplatePath(string childFile, string relTemplate)
    {
        try
        {
            var dir = Path.GetDirectoryName(childFile)!;
            return Path.GetFullPath(Path.Combine(dir, relTemplate.Replace('/', Path.DirectorySeparatorChar)));
        }
        catch
        {
            return null;
        }
    }

    private string? ResolveInputName(string name)
    {
        string bare = name.EndsWith(InfrastructureConstants.FileExtension.Template, StringComparison.OrdinalIgnoreCase)
            ? name[..^4]
            : name;
        
        try
        {
            string direct = Path.Combine(_libraryPaths.TemplatesFolder, bare + InfrastructureConstants.FileExtension.Template);

            if (File.Exists(direct))
                return direct;
            
            foreach (var f in EnumerateFilesSafe(_libraryPaths.TemplatesFolder, InfrastructureConstants.FileExtension.Template))
            {
                if (string.Equals(Path.GetFileNameWithoutExtension(f), bare, StringComparison.OrdinalIgnoreCase))
                    return f;
                
                try
                {
                    var (fm, _, _) = FrontMatterParser.Parse(File.ReadAllText(f));
                    if (string.Equals(fm.Name, bare, StringComparison.OrdinalIgnoreCase))
                        return f;
                }
                catch { }
            }
        }
        catch { }
        return null;
    }

    private string WriteSeedCopy(string resource, string fileBaseName)
    {
        string path = DirectoryHelper.MakeUniquePath(
            _libraryPaths.TemplatesFolder,
            FileNameSanitizer.Sanitize(fileBaseName),
            InfrastructureConstants.FileExtension.Template);
        
        AtomicFile.WriteTo(path, ReadResource(resource));
        return path;
    }

    private string WriteRenamedSeedCopy(string resource, string newName)
    {
        var (fm, body, _) = FrontMatterParser.Parse(ReadResource(resource));
        fm.Name = newName;
        string path = DirectoryHelper.MakeUniquePath(
            _libraryPaths.TemplatesFolder,
            FileNameSanitizer.Sanitize(newName),
            InfrastructureConstants.FileExtension.Template);
        
        AtomicFile.WriteTo(path, FrontMatterParser.Write(fm, body));
        return path;
    }

    private static IEnumerable<(string Name, string Resource)> SeedTemplates()
    {
        var seedPrefix = InfrastructureConstants.Bundled.FolderName.Samples + "/";

        foreach (var resource in _assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith(seedPrefix, StringComparison.Ordinal) ||
                !resource.EndsWith(InfrastructureConstants.FileExtension.Template, StringComparison.OrdinalIgnoreCase))
                    continue;
            
            string name;

            try
            {
                var (fm, _, _) = FrontMatterParser.Parse(ReadResource(resource));
                name = string.IsNullOrWhiteSpace(fm.Name) ? Path.GetFileNameWithoutExtension(resource) : fm.Name!;
            }
            catch
            {
                name = Path.GetFileNameWithoutExtension(resource);
            }

            yield return (name, resource);
        }
    }

    private static string ReadResource(string resource)
    {
        using var stream = _assembly.GetManifestResourceStream(resource)
            ?? throw new FileNotFoundException($"bundled sample '{resource}' is missing from the assembly");
        
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    private static IEnumerable<string> EnumerateFilesSafe(string dir, string extension)
        => Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir, "*" + extension, SearchOption.AllDirectories)
            : [];
    
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "Unable to delete file at: {Path}")]
    private partial void LogUnableToDeleteFile(Exception ex, string path);
}