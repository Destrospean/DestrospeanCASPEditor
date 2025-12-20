namespace Destrospean.DestrospeanCASPEditor
{
    public partial class MessageFramelessWindow : Gtk.Window
    {
        public MessageFramelessWindow(string label, Gtk.Window parent) : base(Gtk.WindowType.Toplevel)
        {
            Build();
            Label.Text = label;
            this.RescaleAndReposition(parent);
            Reposition();
            new System.Threading.Thread(() =>
                {
                    MainWindow.Singleton.Hide();
                    ChoosePatternDialog.GenerateCache(s3pi.Package.Package.NewPackage(0));
                    MainWindow.Singleton.Show();
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
