namespace Destrospean.DestrospeanCASPEditor
{
    public partial class UnsavedChangesDialog : Gtk.Dialog
    {
        public UnsavedChangesDialog(Gtk.Window parent)
        {
            Build();
            this.RescaleAndReposition(parent);
        }
    }
}
