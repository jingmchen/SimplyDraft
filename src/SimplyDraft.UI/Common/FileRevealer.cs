// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Diagnostics;

namespace SimplyDraft.UI.Common;

public static class FileRevealer
{
    public static void Reveal(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
            }
            else if (OperatingSystem.IsMacOS())
            {
                var psi = new ProcessStartInfo("open");
                psi.ArgumentList.Add("-R");
                psi.ArgumentList.Add(path);
                Process.Start(psi);
            }
            else
            {
                var psi = new ProcessStartInfo("xdg-open");
                psi.ArgumentList.Add(Path.GetDirectoryName(path) ?? ".");
                Process.Start(psi);
            }
        }
        catch { }
    }
}