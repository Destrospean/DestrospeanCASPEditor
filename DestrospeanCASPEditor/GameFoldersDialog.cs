using System.Collections.Generic;
using Gtk;
using Newtonsoft.Json;
using s3pi.Filetable;

namespace Destrospean.DestrospeanCASPEditor
{
    public partial class GameFoldersDialog : Dialog
    {
        public static readonly string ConfigurationPath = System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "GameFolders.json";

        public static Dictionary<Game, string> InstallDirectories
        {
            get
            {
                var installDirectories = new Dictionary<Game, string>();
                foreach (var installDir in GameFolders.InstallDirs.Split(';'))
                {
                    var installDirectoryKvp = installDir.Split('=');
                    if (installDirectoryKvp.Length == 2)
                    {
                        installDirectories.Add(GameFolders.byName(installDirectoryKvp[0]), installDirectoryKvp[1]);
                    }
                }
                return installDirectories;
            }
        }

        public GameFoldersDialog(Window parent) : base("Game Folders", parent, DialogFlags.Modal)
        {
            Build();
            SetSizeRequest(WidthRequest == -1 ? -1 : (int)(WidthRequest * ComponentUtils.Scale), HeightRequest == -1 ? -1 : (int)(HeightRequest * ComponentUtils.Scale));
            int parentHeight, parentWidth, parentX, parentY;
            parent.GetPosition(out parentX, out parentY);
            parent.GetSize(out parentWidth, out parentHeight);
            Move(parentX + parentWidth / 2 - WidthRequest / 2, parentY + parentHeight / 2 - HeightRequest / 2);
            foreach (var game in GameFolders.Games)
            {
                var alignment = new Alignment(0, .5f, 1, 0);
                var path = "";
                var entry = new Entry
                    {
                        Text = InstallDirectories.TryGetValue(game, out path) ? path : ""
                    };
                alignment.Add(entry);
                entry.Changed += (sender, e) => SetInstallDirectory(game, entry.Text);
                GameFolderTable.Attach(new Label
                    {
                        Text = game.Name == "base" ? "Base Game" : game.Longname.Replace("The Sims 3 ", ""),
                        UseUnderline = false,
                        Xalign = 0
                    }, 0, 1, GameFolderTable.NRows - 1, GameFolderTable.NRows, AttachOptions.Fill | AttachOptions.Shrink, 0, 0, 0);
                GameFolderTable.Attach(alignment, 1, 2, GameFolderTable.NRows - 1, GameFolderTable.NRows, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
                GameFolderTable.NRows++;
            }
        }

        public static void LoadGameFolders()
        {
            if (System.IO.File.Exists(ConfigurationPath))
            {
                var output = "";
                using (var stream = System.IO.File.OpenText(ConfigurationPath))
                {
                    foreach (var installDirectoryKvp in JsonConvert.DeserializeObject<Dictionary<string, string>>(stream.ReadToEnd()))
                    {
                        output += ";" + installDirectoryKvp.Key + "=" + installDirectoryKvp.Value;
                    }
                }
                GameFolders.InstallDirs = output.Substring(1);
            }
        }

        public static void SetInstallDirectory(Game game, string path)
        {
            var installDirectories = InstallDirectories;
            installDirectories[game] = path;
            var output = "";
            var outputDictionary = new Dictionary<string, string>();
            foreach (var installDirectoryKvp in installDirectories)
            {
                output += ";" + installDirectoryKvp.Key.Name + "=" + installDirectoryKvp.Value;
                outputDictionary.Add(installDirectoryKvp.Key.Name, installDirectoryKvp.Value);
            }
            GameFolders.InstallDirs = output.Substring(1);
            System.IO.File.WriteAllText(ConfigurationPath, JsonConvert.SerializeObject(outputDictionary, Formatting.Indented));
        }

        protected void OnButtonOkClicked(object sender, System.EventArgs e)
        {
            Destroy();
        }
    }
}
