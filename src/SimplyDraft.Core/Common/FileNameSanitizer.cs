// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;

namespace SimplyDraft.Core.Common;

public static class FileNameSanitizer
{
    private static readonly char[] _invalid =
        Path.GetInvalidFileNameChars()
            .Concat("<>:\"/\\|?*".ToCharArray())
            .Concat(Enumerable.Range(0, 32).Select(i => (char)i))
            .Distinct().ToArray();
    
    private static readonly HashSet<string> _reserved;

    static FileNameSanitizer()
    {
        _reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL"
        };

        for (int i = 1; i <= 9; i++)
        {
            _reserved.Add("COM" + i);
            _reserved.Add("LPT" + i);
        }
    }

    public static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "document";

        var sb = new StringBuilder(name.Length);
        
        foreach (var c in name)
            sb.Append(Array.IndexOf(_invalid, c) >= 0 ? '_' : c);
        
        var s = sb.ToString().Trim().TrimEnd('.', ' ');
        
        if (s.Length == 0)
            s = "document";

        var stem = s.Split('.', 2)[0];
        
        if (_reserved.Contains(stem))
            s = "_" + s;
        
        if (s.Length > 120)
        {
            s = s[..120];
            
            if (s.Length > 0 && char.IsHighSurrogate(s[^1]))
                s = s[..^1];
            
            s = s.TrimEnd('.', ' ');

            if (s.Length == 0)
                s = "document";
        }
        
        return s;
    }
}