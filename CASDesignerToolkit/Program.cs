namespace Destrospean.CASDesignerToolkit
{
    class Program
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
