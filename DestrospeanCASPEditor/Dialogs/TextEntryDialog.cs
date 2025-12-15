namespace Destrospean.DestrospeanCASPEditor
{
    public partial class TextEntryDialog : Gtk.Dialog
    {
        public string TextEntryValue
        {
            get;
            private set;
        }

        public TextEntryDialog(string title, string label, Gtk.Window parent) : base(title, parent, Gtk.DialogFlags.Modal)
        {
            Build();
            this.RescaleAndReposition(parent);
            Label.Text = label;
            Response += (o, args) =>
                {
                    if (args.ResponseId == Gtk.ResponseType.Ok)
                    {
                        this.TextEntryValue = Entry.Text;
                    }
                };
        }
    }
}
