namespace Destrospean.DestrospeanCASPEditor
{
    public partial class UnsavedChangesDialog : Gtk.Dialog
    {
        public UnsavedChangesDialog(Gtk.Window parent) : base("Unsaved Changes", parent, Gtk.DialogFlags.Modal)
        {
            Build();
            this.RescaleAndReposition(parent);
            WarningIconAlignment.LeftPadding = (uint)WidgetUtils.SmallImageSize >> 1;
        }
    }
}
