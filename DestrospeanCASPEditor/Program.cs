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
    }
}
