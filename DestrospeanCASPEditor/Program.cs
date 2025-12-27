namespace Destrospean.DestrospeanCASPEditor
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Gtk.Application.Init();
            new MainWindow();
            Gtk.Application.Run();
        }

        public static void WriteError(System.Exception ex)
        {
            System.IO.File.WriteAllText(System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "error-" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".log", ex.Message + "\n" + ex.StackTrace);
            throw ex;
        }
    }
}
