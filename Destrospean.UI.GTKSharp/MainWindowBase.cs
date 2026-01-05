namespace Destrospean.DestrospeanCASPEditor
{
    [System.Flags]
    public enum NextStateOptions : byte
    {
        NoUnsavedChanges,
        UnsavedChanges,
        UpdateModels,
        UnsavedChangesAndUpdateModels
    }

    public abstract class MainWindowBase : Gtk.Window
    {
        public abstract NextStateOptions NextState
        {
            set;
        }

        public static MainWindowBase Singleton
        {
            get;
            private set;
        }

        public MainWindowBase(Gtk.WindowType windowType) : base(windowType)
        {
            Singleton = this;
        }

        public abstract void RescaleAndReposition(bool skipRescale = false);
    }
}
