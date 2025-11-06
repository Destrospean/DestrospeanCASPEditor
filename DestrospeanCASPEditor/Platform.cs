using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class Platform
{
    static class WineDetector
    {
        [DllImport("ntdll.dll", EntryPoint = "wine_get_version")]
        static extern IntPtr WineGetVersion();

        public static bool IsRunningUnderWine
        {
            get
            {
                try
                {
                    return WineGetVersion() != IntPtr.Zero;
                }
                catch (EntryPointNotFoundException)
                {
                    return false;
                }
            }
        }
    }

    public static bool IsLinux
    {
        get
        {
            return OS.HasFlag(OSFlags.Linux);
        }
    }

    public static bool IsMac
    {
        get
        {
            return OS.HasFlag(OSFlags.Darwin);
        }
    }

    public static bool IsRunningUnderWine
    {
        get
        {
            return IsWindows && WineDetector.IsRunningUnderWine;
        }
    }

    public static bool IsUnix
    {
        get
        {
            return OS.HasFlag(OSFlags.Unix);
        }
    }

    public static bool IsWindows
    {
        get
        {
            return OS.HasFlag(OSFlags.Windows);
        }
    }

    [Flags]
    public enum OSFlags
    {
        Windows = 0x1,
        Unix = 0x2,
        Linux = 0x4,
        Darwin = 0x8
    }

    public static OSFlags OS
    {
        get
        {
            switch ((int)Environment.OSVersion.Platform)
            {
                case 4:
                case 128:
                    var os = OSFlags.Unix;
                    var uname = GetCommandOutput("uname").TrimEnd('\n');
                    switch (uname)
                    {
                        case "Darwin":
                        case "Linux":
                            os |= (OSFlags)Enum.Parse(typeof(OSFlags), uname);
                            break;
                    }
                    return os;
                default:
                    return OSFlags.Windows;
            }
        }
    }

    public static string GetCommandOutput(string command, string arguments = "")
    {
        var startInfo = new ProcessStartInfo
            {
                Arguments = arguments,
                CreateNoWindow = true,
                FileName = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
        using (var process = new Process
            {
                StartInfo = startInfo
            })
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd(),
            error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return string.IsNullOrEmpty(error) ? output : string.Format("Error: {0}\nOutput: {1}", error, output);
        }
    }
}
