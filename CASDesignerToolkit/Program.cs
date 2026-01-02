namespace Destrospean.DestrospeanCASPEditor
{
    class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Gtk.Application.Init();
                new MainWindow();
                if (args.Length > 0)
                {
                    MainWindow.Singleton.CurrentPackage = s3pi.Package.Package.OpenPackage(0, args[0], true);
                    MainWindow.Singleton.RefreshWidgets();
                    MainWindow.Singleton.NextState = NextStateOptions.NoUnsavedChanges;
                    MainWindow.Singleton.AddFilePathToWindowTitle(args[0]);
                }
                Gtk.Application.Run();
            }
            catch (System.Exception ex)
            {
                WriteError(ex);
                throw;
            }
        }

        public static void WriteError(System.Exception ex)
        {
            System.IO.File.WriteAllText(System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "error-" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".log", ex.Message + "\n" + ex.StackTrace);
        }
    }
}
