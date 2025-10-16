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

    public static bool IsRunningUnderWine
    {
        get
        {
            return OS.HasFlag(OSFlags.Windows) && WineDetector.IsRunningUnderWine;
        }
    }

    [System.Flags]
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
            switch ((int)System.Environment.OSVersion.Platform)
            {
                case 4:
                case 128:
                    Platform.OSFlags os = Platform.OSFlags.Unix;
                    string uname = GetCommandOutput("uname");
                    switch (uname.TrimEnd('\n'))
                    {
                        case "Darwin":
                        case "Linux":
                            os |= (Platform.OSFlags)System.Enum.Parse(typeof(Platform.OSFlags), uname);
                            break;
                    }
                    return os;
                default:
                    return Platform.OSFlags.Windows;
            }
        }
    }

    public static string GetCommandOutput(string command, string arguments = "")
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        using (Process process = new Process
            {
                StartInfo = startInfo
            })
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd(), error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return string.IsNullOrEmpty(error) ? output : string.Format("Error: {0}\nOutput: {1}", error, output);
        }
    }
}
