namespace Destrospean.CASDesignerToolkit
{
    public partial class TextEntryDialog : Gtk.Dialog
    {
        public string TextEntryValue
        {
            get;
            private set;
        }

        public TextEntryDialog(string title, string message, Gtk.Window parent) : base(title, parent, Gtk.DialogFlags.Modal)
        {
            Build();
            this.RescaleAndReposition(parent);
            Label.Text = message;
            Response += (o, args) =>
                {
                    if (args.ResponseId == Gtk.ResponseType.Ok)
                    {
                        TextEntryValue = Entry.Text;
                    }
                };
        }
    }
}
