namespace Destrospean.DestrospeanCASPEditor
{
    public static class ResourceUtils
    {
        public class AttributeNotFoundException : System.Exception
        {
            public AttributeNotFoundException()
            {
            }

            public AttributeNotFoundException(string message) : base(message)
            {
            }

            public AttributeNotFoundException(string message, System.Exception innerException) : base(message, innerException)
            {
            }

            public int ErrorCode
            {
                get;
                set;
            }
        }

        public class ResourceIndexEntryNotFoundException : System.Exception
        {
            public ResourceIndexEntryNotFoundException()
            {
            }

            public ResourceIndexEntryNotFoundException(string message) : base(message)
            {
            }

            public ResourceIndexEntryNotFoundException(string message, System.Exception innerException) : base(message, innerException)
            {
            }

            public int ErrorCode
            {
                get;
                set;
            }
        }

        public static s3pi.Interfaces.IResourceIndexEntry EvaluateResourceKey(s3pi.Interfaces.IPackage package, string key)
        {   
            var tgi = new ulong[3];
            var i = 0;
            foreach (var hex in key.Substring(4).Split(':'))
            {
                tgi[i++] = System.Convert.ToUInt64(hex, 16);
            }
            var results = package.FindAll(x => x.ResourceType == tgi[0] && x.ResourceGroup == tgi[1] && x.Instance == tgi[2]);
            if (results.Count == 0)
            {
                throw new ResourceIndexEntryNotFoundException();
            }
            return results[0];
        }

        public static s3pi.Interfaces.IResourceIndexEntry EvaluateResourceKey(s3pi.Interfaces.IPackage package, System.Xml.XmlNode xmlNode)
        {   
            if (!((System.Xml.XmlElement)xmlNode).HasAttribute("reskey"))
            {
                throw new AttributeNotFoundException("The XML node given does not have the \"reskey\" attribute.");
            }
            try
            {
                return EvaluateResourceKey(package, xmlNode.Attributes["reskey"].Value);
            }
            catch (ResourceIndexEntryNotFoundException)
            {
                throw new ResourceIndexEntryNotFoundException("No resource with the key referenced in the XML node could be found in the given package.");
            }
        }

        public static string GetResourceTypeTag(s3pi.Interfaces.IResourceIndexEntry resourceIndexEntry)
        {
            return s3pi.Extensions.ExtList.Ext[resourceIndexEntry.ResourceType][0];
        }

        public static string ReverseEvaluateResourceKey(s3pi.Interfaces.IResourceIndexEntry resourceIndexEntry)
        {   
            var tgi = new ulong[]
                {
                    resourceIndexEntry.ResourceType,
                    resourceIndexEntry.ResourceGroup,
                    resourceIndexEntry.Instance
                };
            var output = "key";
            foreach (var value in tgi)
            {
                output += ":" + value.ToString("X8");
            }
            return output;
        }
    }
}
