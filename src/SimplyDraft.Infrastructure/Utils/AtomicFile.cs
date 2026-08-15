// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Runtime.Versioning;
using System.Text;

namespace SimplyDraft.Infrastructure.Utils;

internal static class AtomicFile
{
    private static readonly Encoding DefaultEncoding =
        new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);
    
    internal static void WriteTo(
        string path,
        string contents,
        Encoding? encoding = null,
        Action<string>? cleanupFailed = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contents);

        string fullPath = Path.GetFullPath(path);
        string fileName = Path.GetFileName(fullPath);

        if (fileName.Length == 0)
            throw new ArgumentException("The destination path must include a file name.", nameof(path));
        
        string directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("The destination path has no parent directory.", nameof(path));
        
        Directory.CreateDirectory(directory);

        string tempPath = Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.tmp");

        try
        {
            WriteToTemporaryFile(tempPath, fullPath, contents, encoding ?? DefaultEncoding);
            ReplaceDestination(tempPath, fullPath);
        }
        finally
        {
            if (!TryDelete(tempPath))
                cleanupFailed?.Invoke(tempPath);
        }
    }

    internal static bool TryDelete(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void WriteToTemporaryFile(string tempPath, string destinationPath, string contents, Encoding encoding)
    {
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            BufferSize = 64 * 1024
        };

        if (!OperatingSystem.IsWindows() && TryGetUnixFileMode(destinationPath) is { } destinationMode)
            options.UnixCreateMode = destinationMode;
        
        using var stream = new FileStream(tempPath, options);

        using (var writer = new StreamWriter(stream, encoding, bufferSize: 64 * 1024, leaveOpen: true))
        {
            writer.Write(contents);
        }

        stream.Flush(flushToDisk: true);
    }

    private static void ReplaceDestination(string tempPath, string destinationPath)
    {
        if (OperatingSystem.IsWindows() && File.Exists(destinationPath))
        {
            try
            {
                File.Replace(tempPath, destinationPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
                return;
            }
            catch (FileNotFoundException)
            {
                // Fall back to move
            }
        }

        File.Move(tempPath, destinationPath, overwrite: true);
    }

    [UnsupportedOSPlatform("windows")]
    private static UnixFileMode? TryGetUnixFileMode(string path)
    {
        try
        {
            return File.Exists(path)
                ? File.GetUnixFileMode(path)
                : null;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return null;
        }
    }
}