using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Destrospean.DestrospeanCASPEditor
{
    public static class ApplicationSpecificSettings
    {
        public static readonly string SettingsFilePath = string.Format("{0}{1}Destrospean{1}UserSettings.json", System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), Path.DirectorySeparatorChar);

        public static Dictionary<string, Dictionary<string, string>> Settings
        {
            get;
            private set;
        }

        public static class JSONNodeNames
        {
            public const string GameFolders = "The Sims 3 Installation Directories";
        }

        public static void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                var installDirs = "";
                using (var stream = File.OpenText(SettingsFilePath))
                {
                    Settings = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(stream.ReadToEnd());
                    foreach (var installDirectoryKvp in Settings[JSONNodeNames.GameFolders])
                    {
                        installDirs += ";" + installDirectoryKvp.Key + "=" + installDirectoryKvp.Value;
                    }
                }
                s3pi.Filetable.GameFolders.InstallDirs = installDirs.Substring(1);
                return;
            }
            Settings = new Dictionary<string, Dictionary<string, string>>();
        }

        public static void SaveSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
            File.WriteAllText(SettingsFilePath, JsonConvert.SerializeObject(Settings, Formatting.Indented));
        }
    }
}
