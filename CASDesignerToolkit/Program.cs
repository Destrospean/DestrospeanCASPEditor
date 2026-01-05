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
                    var mainWindow = (MainWindow)MainWindow.Singleton;
                    mainWindow.CurrentPackage = s3pi.Package.Package.OpenPackage(0, args[0], true);
                    mainWindow.RefreshWidgets();
                    mainWindow.NextState = NextStateOptions.NoUnsavedChanges;
                    mainWindow.AddFilePathToWindowTitle(args[0]);
                }
                Gtk.Application.Run();
            }
            catch (System.Exception ex)
            {
                ProgramUtils.WriteError(ex);
                throw;
            }
        }
    }
}
