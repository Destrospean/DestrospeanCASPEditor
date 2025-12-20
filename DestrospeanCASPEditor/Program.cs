namespace Destrospean.DestrospeanCASPEditor
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            try
            {
                Gtk.Application.Init();
                new MainWindow();
                Gtk.Application.Run();
            }
            catch (System.Exception ex)
            {
                var directoryPath = System.IO.Path.GetDirectoryName(ApplicationSpecificSettings.SettingsFilePath);
                System.IO.Directory.CreateDirectory(directoryPath);
                System.IO.File.WriteAllText(directoryPath + System.IO.Path.DirectorySeparatorChar + "crash-" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt", ex.ToString());
            }
        }
    }
}
