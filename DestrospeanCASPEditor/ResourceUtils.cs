using System.Collections.Generic;
using s3pi.Interfaces;

namespace Destrospean.DestrospeanCASPEditor
{
    public static class ResourceUtils
    {
        static Dictionary<s3pi.Filetable.PackageTag, IPackage> mGamePackages;

        public static Dictionary<s3pi.Filetable.PackageTag, IPackage> GamePackages
        {
            get
            {
                if (mGamePackages == null)
                {
                    mGamePackages = new Dictionary<s3pi.Filetable.PackageTag, IPackage>();
                    foreach (var game in s3pi.Filetable.GameFolders.Games)
                    {
                        var enumerator = game.GameContent.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            mGamePackages.Add(enumerator.Current, s3pi.Package.Package.OpenPackage(0, enumerator.Current.Path));
                        }
                    }
                }
                return mGamePackages;
            }
        }

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

        static System.Tuple<IPackage, IResourceIndexEntry> EvaluateResourceKeyInternal(IPackage package, string key)
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
            return new System.Tuple<IPackage, IResourceIndexEntry>(package, results[0]);
        }

        public static System.Tuple<IPackage, IResourceIndexEntry> EvaluateImageResourceKey(IPackage package, string key)
        {   
            try
            {
                return EvaluateResourceKeyInternal(package, key);
            }
            catch (ResourceIndexEntryNotFoundException)
            {
                foreach (var packageKvp in ImageUtils.GameImageResources)
                {
                    try
                    {
                        return EvaluateResourceKeyInternal(packageKvp.Value, key);
                    }
                    catch (ResourceIndexEntryNotFoundException)
                    {
                    }
                }
                throw new ResourceIndexEntryNotFoundException("No resource with the key referenced in the XML node could be found in the given package.");
            }
        }

        public static System.Tuple<IPackage, IResourceIndexEntry> EvaluateResourceKey(IPackage package, System.Xml.XmlNode xmlNode)
        {   
            if (!((System.Xml.XmlElement)xmlNode).HasAttribute("reskey"))
            {
                throw new AttributeNotFoundException("The XML node given does not have the \"reskey\" attribute.");
            }
            var key = xmlNode.Attributes["reskey"].Value;
            try
            {
                return EvaluateResourceKeyInternal(package, key);
            }
            catch (ResourceIndexEntryNotFoundException)
            {
                foreach (var packageKvp in ResourceUtils.GamePackages)
                {
                    try
                    {
                        return EvaluateResourceKeyInternal(packageKvp.Value, key);
                    }
                    catch (ResourceIndexEntryNotFoundException)
                    {
                    }
                }
                throw new ResourceIndexEntryNotFoundException("No resource with the key referenced in the XML node could be found in the given package.");
            }
        }

        public static string GetResourceTypeTag(IResourceIndexEntry resourceIndexEntry)
        {
            return s3pi.Extensions.ExtList.Ext[resourceIndexEntry.ResourceType][0];
        }

        public static string ReverseEvaluateResourceKey(IResourceIndexEntry resourceIndexEntry)
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
