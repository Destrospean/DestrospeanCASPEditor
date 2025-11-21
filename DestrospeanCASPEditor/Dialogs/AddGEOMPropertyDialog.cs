using System.Collections.Generic;
using Gtk;
using s3pi.GenericRCOLResource;

namespace Destrospean.DestrospeanCASPEditor
{
    public partial class AddGEOMPropertyDialog : Dialog
    {
        public System.Type DataType
        {
            get;
            private set;
        }

        public FieldType Field
        {
            get;
            private set;
        }

        protected ComboBox DataTypeComboBox
        {
            get
            {
                return mDataTypeComboBox;
            }
        }

        protected Label DataTypeLabel
        {
            get
            {
                return mDataTypeLabel;
            }
        }

        protected ComboBox FieldComboBox
        {
            get
            {
                return mFieldComboBox;
            }
        }

        protected Label FieldLabel
        {
            get
            {
                return mFieldLabel;
            }
        }

        public AddGEOMPropertyDialog(string title, Window parent) : base(title, parent, DialogFlags.Modal)
        {
            Build();
            this.RescaleAndReposition(parent);
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(ShaderData));
            CellRendererText dataTypeCell = new CellRendererText(),
            fieldCell = new CellRendererText();
            ListStore dataTypeListStore = new ListStore(typeof(string)),
            fieldListStore = new ListStore(typeof(string));
            List<string> dataTypes = new List<string>(),
            fields = new List<string>(System.Enum.GetNames(typeof(FieldType)));
            foreach (var dataType in assembly.GetTypes())
            {
                if (dataType.IsClass && dataType.Namespace == "s3pi.GenericRCOLResource" && dataType.Name.StartsWith("Element") && !dataType.Name.EndsWith("Key"))
                {
                    dataTypes.Add(dataType.Name);
                }
            }
            dataTypes.Sort();
            dataTypes.ForEach(x => dataTypeListStore.AppendValues(x));
            DataTypeComboBox.Model = dataTypeListStore;
            DataTypeComboBox.Active = 0;
            DataTypeComboBox.PackStart(dataTypeCell, true);
            fields.Remove("None");
            fields.Sort();
            fields.Insert(0, "None");
            fields.ForEach(x => fieldListStore.AppendValues(x));
            FieldComboBox.Model = fieldListStore;
            FieldComboBox.Active = 0;
            FieldComboBox.PackStart(fieldCell, true);
            Response += (o, args) =>
                {
                    if (args.ResponseId == ResponseType.Ok)
                    {
                        DataType = assembly.GetType("s3pi.GenericRCOLResource." + dataTypes[DataTypeComboBox.Active]);
                        Field = (FieldType)System.Enum.Parse(typeof(FieldType), fields[FieldComboBox.Active]);
                    }
                };
        }

        public AddGEOMPropertyDialog(Window parent) : this("Add Property", parent)
        {
        }

        protected AddGEOMPropertyDialog(string title, Window parent, bool IsCleanSlate) : base(title, parent, DialogFlags.Modal)
        {
        }
    }
}
