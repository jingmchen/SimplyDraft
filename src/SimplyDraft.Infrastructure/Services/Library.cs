using System.Globalization;
using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Common;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Enums;
using SimplyDraft.Engine.Parsing;
using SimplyDraft.Infrastructure.Constants;
using SimplyDraft.Infrastructure.Utils;

namespace SimplyDraft.Infrastructure.Services;

public sealed partial class Library : ILibrary
{
    private readonly ILogger<Library> _logger;
    private readonly ILibraryPaths _paths;
    private readonly IScriptingEngine _scriptingEngine;

    public Library(ILogger<Library> logger, ILibraryPaths paths, IScriptingEngine scriptingEngine)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _scriptingEngine = scriptingEngine ?? throw new ArgumentNullException(nameof(scriptingEngine));
    }


    // ─── PUBLIC METHODS ────────────────────────
    public List<LibraryItem> Scan()
    {
        var items = new List<LibraryItem>();

        foreach (var f in EnumerateFilesSafe(_paths.TemplatesFolder, InfrastructureConstants.UserData.FileExtension.Template))
            items.Add(ScanSingle(f, LibraryItemKind.Template));
        
        foreach (var f in EnumerateFilesSafe(_paths.ChildrenFolder, InfrastructureConstants.UserData.FileExtension.Children))
            items.Add(ScanSingle(f, LibraryItemKind.Child));
        
        items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return items;
    }

    public string CreateTemplate(string name)
    {
        var fm = new FrontMatter{Name = name, HasMarkup = true};
        fm.Variables["name"] = "";

        string path = DirectoryHelper.MakeUniquePath(
            _paths.TemplatesFolder,
            FileNameSanitizer.Sanitize(name),
            InfrastructureConstants.UserData.FileExtension.Template
        );

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
            DiagnosticList = warns
        };
    }

    public void SaveTemplate(TemplateDocument doc)
        => AtomicFile.WriteTo(doc.FilePath, FrontMatterParser.Write(doc.Fm, doc.Body));

    public string CreateChild(string templatePath, string name)
    {
        var template = LoadTemplate(templatePath);
        var fm = new FrontMatter {Name = name};

        string path = DirectoryHelper.MakeUniquePath(
            _paths.ChildrenFolder,
            FileNameSanitizer.Sanitize(name),
            InfrastructureConstants.UserData.FileExtension.Children
        );

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
            _paths.ChildrenFolder,
            FileNameSanitizer.Sanitize(name),
            InfrastructureConstants.UserData.FileExtension.Children
        );

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

        var resolved = fm.TemplatePath == null
            ? null
            : ResolveTemplatePath(path, fm.TemplatePath);
        
        if (resolved != null && !File.Exists(resolved))
            resolved = null;
        
        return new ChildDocument
        {
            FilePath = path,
            Fm = fm,
            ResolvedTemplatePath = resolved,
            Body = body,
            DiagnosticList = warns
        };
    }

    public void SaveChild(ChildDocument doc)
        => AtomicFile.WriteTo(doc.FilePath, FrontMatterParser.Write(doc.Fm, doc.Body));

    public string Duplicate(LibraryItem item)
    {
        string directory = Path.GetDirectoryName(item.FilePath)!;
        string extension = Path.GetExtension(item.FilePath);

        var (fm, body, _) = FrontMatterParser.Parse(File.ReadAllText(item.FilePath));

        string newName = (
            string.IsNullOrWhiteSpace(fm.Name) ? item.Name : fm.Name!
        ) + " copy";
        
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
            _paths.TrashFolder,
            $"{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}_{Path.GetFileName(item.FilePath)}"
        );

        int n = 2;

        while (File.Exists(dest))
            dest = Path.Combine(
                _paths.TrashFolder,
                $"{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}_{n++}_{Path.GetFileName(item.FilePath)}"
            );
        
        File.Move(item.FilePath, dest);
        File.SetLastWriteTime(dest, DateTime.Now);
    }

    public void PurgeTrash(int days)
    {
        if (!Directory.Exists(_paths.TrashFolder))
            return;
        
        var cutoff = DateTime.Now.AddDays(-Math.Max(0, days));

        foreach (var f in Directory.EnumerateFiles(_paths.TrashFolder))
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
        //
    }

    public (DateTime Created, DateTime Modified) GetTimestamps(string filePath)
    {
        DateTime created, modified;

        try {created = File.GetCreationTime(filePath);} catch {created = DateTime.Now;}
        try {modified = File.GetLastWriteTime(filePath);} catch {modified = DateTime.Now;}
        
        return (created, modified);
    }

    List<string> SeedIfEmpty();

    // ─── PRIVATE METHODS ───────────────────────
    private void RetargetChildren(string oldTemplatePath, string newTemplatePath)
    {
        foreach (var f in EnumerateFilesSafe(_paths.ChildrenFolder, InfrastructureConstants.UserData.FileExtension.Children))
        {
            try
            {
                var (fm, body, _) = FrontMatterParser.Parse(File.ReadAllText(f));
                if (fm.TemplatePath == null) continue;
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
        bool broken = false;
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
                
                if (!baked)
                {
                    var resolved = fm.TemplatePath == null
                        ? null
                        : ResolveTemplatePath(file, fm.TemplatePath);
                    
                    broken = resolved == null || !File.Exists(resolved);
                }
            }
        }
        catch {broken = true;}

        DateTime modified;

        try {modified = File.GetLastWriteTime(file);}
        catch {modified = DateTime.MinValue;}

        return new LibraryItem(file, kind, name, tref, modified, broken, baked);
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

    private static IEnumerable<string> EnumerateFilesSafe(string dir, string pattern)
        => Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories)
            : [];

    [LoggerMessage(
        EventId = 1001, Level = LogLevel.Warning, Message = "Unable to delete file at: {Path}"
    )]
    private partial void LogUnableToDeleteFile(Exception ex, string path);
}