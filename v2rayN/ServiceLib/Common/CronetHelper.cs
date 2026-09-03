namespace ServiceLib.Common;

/// <summary>
/// sing-box Naive outbound requires the cronet native library at runtime:
/// Windows: libcronet.dll next to sing-box.exe or in PATH
/// Linux (purego): libcronet.so next to binary or in system lib path
/// macOS: bundled via CGO in official builds.
/// </summary>
public static class CronetHelper
{
    public static string GetExpectedFileName()
    {
        if (Utils.IsWindows())
        {
            return "libcronet.dll";
        }
        if (Utils.IsLinux())
        {
            return "libcronet.so";
        }
        if (Utils.IsMacOS())
        {
            return "libcronet.dylib";
        }
        return "libcronet.dll";
    }

    public static string GetExpectedPath()
    {
        return Utils.GetBinPath(GetExpectedFileName(), ECoreType.sing_box.ToString());
    }

    public static bool IsCronetAvailable()
    {
        try
        {
            var path = GetExpectedPath();
            if (File.Exists(path))
            {
                return true;
            }
            // Fallback: search PATH (Windows) / system lib paths (Linux).
            var fileName = GetExpectedFileName();
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                foreach (var dir in pathEnv.Split(Path.PathSeparator))
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(dir) && File.Exists(Path.Combine(dir.Trim(), fileName)))
                        {
                            return true;
                        }
                    }
                    catch { }
                }
            }
            if (Utils.IsLinux())
            {
                foreach (var dir in new[] { "/usr/local/lib", "/usr/lib", "/lib/x86_64-linux-gnu" })
                {
                    try
                    {
                        if (File.Exists(Path.Combine(dir, fileName)))
                        {
                            return true;
                        }
                    }
                    catch { }
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public static string MissingCronetMessage()
    {
        var file = GetExpectedFileName();
        var dir = Utils.GetBinPath("", ECoreType.sing_box.ToString());
        return $"Naive requires {file} next to sing-box.exe (expected: {Path.Combine(dir, file)}). "
            + "Download it from the matching sing-box release (or SagerNet/cronet-go) and restart the core.";
    }
}
