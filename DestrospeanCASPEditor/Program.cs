namespace Destrospean.DestrospeanCASPEditor
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Gtk.Application.Init();
            new MainWindow().Show();
            Gtk.Application.Run();
        }
    }
}
