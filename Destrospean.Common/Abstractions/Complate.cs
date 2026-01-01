using System.Collections.Generic;
using System.Xml;

namespace Destrospean.Common.Abstractions
{
    public abstract class Complate
    {
        protected readonly IDictionary<string, string> mPropertiesTyped;

        protected readonly IDictionary<string, XmlNode> mPropertiesXmlNodes;

        protected readonly XmlDocument mXmlDocument;

        public abstract CASTableObject CASTableObject
        {
            get;
        }

        public static CmarNYCBorrowed.Action MarkModelsNeedUpdated, MarkUnsavedChangesCallback;

        public static CmarNYCBorrowed.TextureUtils.GetTextureDelegate GetTextureCallback;

        public abstract s3pi.Interfaces.IPackage ParentPackage
        {
            get;
        }

        public virtual IDictionary<string, string> PropertiesTyped
        {
            get
            {
                return mPropertiesTyped;
            }
        }

        public virtual string[] PropertyNames
        {
            get
            {
                return new List<string>(mPropertiesXmlNodes.Keys).ToArray();
            }
        }

        public string this[string propertyName]
        {
            get
            {
                return GetValue(propertyName);
            }
            set
            {
                SetValue(propertyName, value);
            }
        }

        protected class PropertyNameComparer : IComparer<string>
        {
            public int Compare(string a, string b)
            {
                string aCopy = a,
                bCopy = b;
                while (aCopy.Length < bCopy.Length)
                {
                    aCopy += " ";
                }
                while (aCopy.Length > bCopy.Length)
                {
                    bCopy += " ";
                }
                for (var i = 0; i < aCopy.Length; i++)
                {
                    if (aCopy[i] != bCopy[i] && aCopy.Substring(0, i) == bCopy.Substring(0, i))
                    {
                        bool aCharIsNum = '0' <= aCopy[i] && aCopy[i] <= '9',
                        bCharIsNum = '0' <= bCopy[i] && bCopy[i] <= '9';
                        if (aCharIsNum && !bCharIsNum)
                        {
                            return 1;
                        }
                        if (!aCharIsNum && bCharIsNum)
                        {
                            return -1;
                        }
                    }
                }
                return string.Compare(a, b);
            }
        }

        public Complate()
        {
            mXmlDocument = new XmlDocument();
            mPropertiesXmlNodes = new SortedDictionary<string, XmlNode>(new PropertyNameComparer());
            mPropertiesTyped = new SortedDictionary<string, string>(new PropertyNameComparer());
        }

        public virtual string GetValue(string propertyName)
        {
            return mPropertiesXmlNodes[propertyName].Attributes["value"].Value;
        }

        public static float[] ParseCommaSeparatedValues(string text)
        {
            return System.Array.ConvertAll(text.Split(','), x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture));
        }

        public virtual void SetValue(string propertyName, string newValue, CmarNYCBorrowed.Action beforeMarkUnsaved = null)
        {
            mPropertiesXmlNodes[propertyName].Attributes["value"].Value = newValue;
            if (beforeMarkUnsaved != null)
            {
                beforeMarkUnsaved();
            }
        }
    }
}
