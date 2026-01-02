namespace Destrospean.DestrospeanCASPEditor
{
    public partial class CacheGenerationWindow : Gtk.Window
    {
        public CacheGenerationWindow(string message, Gtk.Window parent) : base(Gtk.WindowType.Toplevel)
        {
            Build();
            Label.Text = message;
            this.RescaleAndReposition(parent);
            Reposition();
            new System.Threading.Thread(() =>
                {
                    MainWindow.Singleton.RescaleAndReposition(true);
                    MainWindow.Singleton.Sensitive = false;
                    try
                    {
                        ChoosePatternDialog.GenerateCache(s3pi.Package.Package.NewPackage(0));
                    }
                    catch (System.Exception ex)
                    {
                        Program.WriteError(ex);
                        throw;
                    }
                    MainWindow.Singleton.Sensitive = true;
                    Destroy();
                }).Start();
        }

        void Reposition()
        {
            var monitorGeometry = Screen.GetMonitorGeometry(Screen.GetMonitorAtWindow(GdkWindow));
            Move(((int)(monitorGeometry.Width / WidgetUtils.WineScaleDenominator) - WidthRequest) >> 1, ((int)(monitorGeometry.Height / WidgetUtils.WineScaleDenominator) - HeightRequest) >> 1);
        }
    }
}
