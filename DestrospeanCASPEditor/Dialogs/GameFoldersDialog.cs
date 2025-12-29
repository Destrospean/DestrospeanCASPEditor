using System.Collections.Generic;
using Gtk;
using s3pi.Filetable;

namespace Destrospean.DestrospeanCASPEditor
{
    public partial class GameFoldersDialog : Dialog
    {
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
            this.RescaleAndReposition(parent);
            foreach (var game in GameFolders.Games)
            {
                var path = "";
                var entry = new Entry
                    {
                        Text = InstallDirectories.TryGetValue(game, out path) ? path.Replace('/', System.IO.Path.DirectorySeparatorChar) : ""
                    };
                entry.Changed += (sender, e) => SetInstallDirectory(game, entry.Text);
                Alignment entryAlignment = new Alignment(0, .5f, 1, 0)
                    {
                        LeftPadding = (uint)WidgetUtils.SmallImageSize,
                    },
                labelAlignment = new Alignment(0, .5f, 1, 0)
                    {
                        LeftPadding = (uint)WidgetUtils.SmallImageSize
                    };
                entryAlignment.Add(entry);
                labelAlignment.Add(new Label(game.Name == "base" ? "Base Game" : game.Longname.Replace("The Sims 3 ", ""))
                    {
                        UseUnderline = false,
                        Xalign = 0
                    });
                GameFolderTable.Attach(labelAlignment, 0, 1, GameFolderTable.NRows - 1, GameFolderTable.NRows, AttachOptions.Fill | AttachOptions.Shrink, 0, 0, 0);
                GameFolderTable.Attach(entryAlignment, 1, 2, GameFolderTable.NRows - 1, GameFolderTable.NRows, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
                GameFolderTable.NRows++;
            }
            ShowAll();
        }

        public static void SetInstallDirectory(Game game, string path)
        {
            var installDirectories = InstallDirectories;
            installDirectories[game] = path;
            var output = "";
            var outputDictionary = new Dictionary<string, string>();
            foreach (var installDirectoryKvp in installDirectories)
            {
                output += ";" + installDirectoryKvp.Key.Name + "=" + installDirectoryKvp.Value.Replace('\\', '/');
                outputDictionary.Add(installDirectoryKvp.Key.Name, installDirectoryKvp.Value.Replace('\\', '/'));
            }
            GameFolders.InstallDirs = output.Substring(1);
            ApplicationSpecificSettings.Settings[ApplicationSpecificSettings.JSONNodeNames.GameFolders] = outputDictionary;
            ApplicationSpecificSettings.SaveSettings();
        }

        protected void OnOKButtonClicked(object sender, System.EventArgs e)
        {
            Destroy();
        }
    }
}
