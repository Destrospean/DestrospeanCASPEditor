using System;
using System.Collections.Generic;
using Gtk;
using s3pi.Filetable;

namespace Destrospean.DestrospeanCASPEditor
{
    public partial class GameFoldersDialog : Dialog
    {
        public static readonly string GameFoldersJsonPath = AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "GameFolders.json";

        public static Dictionary<Game, string> InstallDirs
        {
            get
            {
                var installDirs = new Dictionary<Game, string>();
                foreach (var installDir in GameFolders.InstallDirs.Split(';'))
                {
                    var installDirKvp = installDir.Split('=');
                    if (installDirKvp.Length == 2)
                    {
                        installDirs.Add(GameFolders.byName(installDirKvp[0]), installDirKvp[1]);
                    }
                }
                return installDirs;
            }
        }

        public GameFoldersDialog(Window parent) : base("Game Folders", parent, DialogFlags.Modal)
        {
            Build();
            SetSizeRequest(WidthRequest == -1 ? -1 : (int)(WidthRequest * MainWindow.Scale), HeightRequest == -1 ? -1 : (int)(HeightRequest * MainWindow.Scale));
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
                        Text = InstallDirs.TryGetValue(game, out path) ? path : ""
                    };
                alignment.Add(entry);
                entry.Changed += (sender, e) => SetInstallDir(game, entry.Text);
                GameFolderTable.Attach(new Label
                    {
                        Text = game.Name == "base" ? "Base Game" : game.Longname.Replace("The Sims 3 ", ""),
                        UseUnderline = false,
                        Xalign = 0
                    }, 0, 1, GameFolderTable.NRows - 1, GameFolderTable.NRows, AttachOptions.Fill | AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
                GameFolderTable.Attach(alignment, 1, 2, GameFolderTable.NRows - 1, GameFolderTable.NRows, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
                GameFolderTable.NRows++;
            }
        }

        public static void SetInstallDir(Game game, string path)
        {
            var installDirs = InstallDirs;
            installDirs[game] = path;
            var output = "";
            var outputDictionary = new Dictionary<string, string>();
            foreach (var installDirKvp in installDirs)
            {
                output += ";" + installDirKvp.Key.Name + "=" + installDirKvp.Value;
                outputDictionary.Add(installDirKvp.Key.Name, installDirKvp.Value);
            }
            GameFolders.InstallDirs = output.Substring(1);
            System.IO.File.WriteAllText(GameFoldersJsonPath, Newtonsoft.Json.JsonConvert.SerializeObject(outputDictionary, Newtonsoft.Json.Formatting.Indented));
        }

        protected void OnButtonOkClicked(object sender, EventArgs e)
        {
            Destroy();
        }
    }
}

