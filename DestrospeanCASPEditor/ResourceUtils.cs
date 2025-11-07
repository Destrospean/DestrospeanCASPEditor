using System;
using System.Collections.Generic;
using s3pi.Extensions;
using s3pi.Filetable;
using s3pi.Interfaces;

namespace Destrospean.DestrospeanCASPEditor
{
    public static class ResourceUtils
    {
        static Dictionary<PackageTag, IPackage> mGameContentPackages;

        static Dictionary<PackageTag, IPackage> mGameImageResourcePackages;

        static List<string> mMissingResourceKeys;

        public static Dictionary<PackageTag, IPackage> GameContentPackages
        {
            get
            {
                if (mGameContentPackages == null)
                {
                    mGameContentPackages = new Dictionary<PackageTag, IPackage>();
                    foreach (var game in GameFolders.Games)
                    {
                        var enumerator = game.GameContent.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            mGameContentPackages.Add(enumerator.Current, s3pi.Package.Package.OpenPackage(0, enumerator.Current.Path));
                        }
                    }
                }
                return mGameContentPackages;
            }
        }

        public static Dictionary<PackageTag, IPackage> GameImageResourcePackages
        {
            get
            {
                if (mGameImageResourcePackages == null)
                {
                    mGameImageResourcePackages = new Dictionary<PackageTag, IPackage>();
                    foreach (var game in GameFolders.Games)
                    {
                        var enumerator = game.DDSImages.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            mGameImageResourcePackages.Add(enumerator.Current, s3pi.Package.Package.OpenPackage(0, enumerator.Current.Path));
                        }
                    }
                }
                return mGameImageResourcePackages;
            }
        }

        public static List<string> MissingResourceKeys
        {
            get
            {
                if (mMissingResourceKeys == null)
                {
                    mMissingResourceKeys = new List<string>();
                }
                return mMissingResourceKeys;
            }
        }

        public class AttributeNotFoundException : Exception
        {
            public AttributeNotFoundException()
            {
            }

            public AttributeNotFoundException(string message) : base(message)
            {
            }

            public AttributeNotFoundException(string message, Exception innerException) : base(message, innerException)
            {
            }

            public int ErrorCode
            {
                get;
                set;
            }
        }

        public class ResourceIndexEntryNotFoundException : Exception
        {
            public ResourceIndexEntryNotFoundException()
            {
            }

            public ResourceIndexEntryNotFoundException(string message) : base(message)
            {
            }

            public ResourceIndexEntryNotFoundException(string message, Exception innerException) : base(message, innerException)
            {
            }

            public int ErrorCode
            {
                get;
                set;
            }
        }

        public class ResourceKey : IResourceKey
        {
            ulong mInstance;

            uint mResourceGroup, mResourceType;

            public ulong Instance
            {
                get
                {
                    return mInstance;
                }
                set
                {
                    mInstance = value;
                }
            }

            public uint ResourceGroup
            {
                get
                {
                    return mResourceGroup;
                }
                set
                {
                    mResourceGroup = value;
                }
            }

            public uint ResourceType
            {
                get
                {
                    return mResourceType;
                }
                set
                {
                    mResourceType = value;
                }
            }

            public ResourceKey(uint type, uint group, ulong instance)
            {
                ResourceType = type;
                ResourceGroup = group;
                Instance = instance;
            }

            public int CompareTo(IResourceKey other)
            {
                var result = ResourceType.CompareTo(other.ResourceType);
                if (result != 0)
                {
                    return result;
                }
                result = ResourceGroup.CompareTo(other.ResourceGroup);
                if (result != 0)
                {
                    return result;
                }
                return Instance.CompareTo(other.Instance);
            }

            public bool Equals(IResourceKey other)
            {
                return CompareTo(other) == 0;
            }

            public bool Equals(IResourceKey a, IResourceKey b)
            {
                return a.Equals(b);
            }

            public override int GetHashCode()
            {
                return ResourceType.GetHashCode() ^ ResourceGroup.GetHashCode() ^ Instance.GetHashCode();
            }

            public int GetHashCode(IResourceKey resourceKey)
            {
                return resourceKey.GetHashCode();
            }
        }

        static Tuple<IPackage, IResourceIndexEntry> EvaluateResourceKeyInternal(IPackage package, string key)
        {   
            var tgi = new ulong[3];
            var i = 0;
            foreach (var hex in key.Substring(4).Split(':'))
            {
                tgi[i++] = Convert.ToUInt64(hex, 16);
            }
            var results = package.FindAll(x => x.ResourceType == tgi[0] && x.ResourceGroup == tgi[1] && x.Instance == tgi[2]);
            if (results.Count == 0)
            {
                throw new ResourceIndexEntryNotFoundException();
            }
            return new Tuple<IPackage, IResourceIndexEntry>(package, results[0]);
        }

        public static IResourceIndexEntry AddResource(IPackage package, string filename, IResourceKey resourceKey = null, bool rejectDups = true)
        {
            return package.AddResource(resourceKey ?? new ResourceKey(0, 0, System.Security.Cryptography.FNV64.GetHash(Guid.NewGuid().ToString())), System.IO.File.OpenRead(filename), rejectDups);
        }

        public static Tuple<IPackage, IResourceIndexEntry> EvaluateImageResourceKey(IPackage package, string key)
        {   
            try
            {
                return EvaluateResourceKeyInternal(package, key);
            }
            catch (ResourceIndexEntryNotFoundException)
            {
                foreach (var gamePackage in GameImageResourcePackages.Values)
                {
                    try
                    {
                        return EvaluateResourceKeyInternal(gamePackage, key);
                    }
                    catch (ResourceIndexEntryNotFoundException)
                    {
                    }
                }
                throw new ResourceIndexEntryNotFoundException("No image resource with the given key could be found.");
            }
        }

        public static Tuple<IPackage, IResourceIndexEntry> EvaluateResourceKey(IPackage package, System.Xml.XmlNode xmlNode)
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
                foreach (var gamePackage in GameContentPackages.Values)
                {
                    try
                    {
                        return EvaluateResourceKeyInternal(gamePackage, key);
                    }
                    catch (ResourceIndexEntryNotFoundException)
                    {
                    }
                }
                throw new ResourceIndexEntryNotFoundException("No resource with the key referenced in the XML node could be found.");
            }
        }

        public static IResourceIndexEntry GetResourceIndexEntry(IPackage package, IResourceKey resourceKey)
        {
            return package.Find(x => x.ResourceType == resourceKey.ResourceType && x.ResourceGroup == resourceKey.ResourceGroup && x.Instance == resourceKey.Instance);
        }

        public static uint GetResourceType(string tag)
        {
            foreach (var type in ExtList.Ext.Keys)
            {
                if (ExtList.Ext[type].Contains(tag))
                {
                    return Convert.ToUInt32(type, 16);
                }
            }
            return 0;
        }

        public static string GetResourceTypeTag(IResourceKey resourceKey)
        {
            return ExtList.Ext[resourceKey.ResourceType][0];
        }

        public static void ResolveResourceType(IPackage package, IResourceIndexEntry resourceIndexEntry)
        {
            IResource castResource = null;
            var resource = s3pi.WrapperDealer.WrapperDealer.GetResource(0, package, resourceIndexEntry);
            string tag = null;
            try
            {
                GDImageLibrary._DDS.LoadImage(resource.AsBytes);
                tag = "_IMG";
                goto FinalSteps;
            }
            catch
            {
            }
            try
            {
                castResource = new CASPartResource.CASPartResource(0, resource.Stream);
                tag = "CASP";
                goto FinalSteps;
            }
            catch
            {
            }
            try
            {
                castResource = new meshExpImp.ModelBlocks.GeometryResource(0, resource.Stream);
                tag = "GEOM";
                goto FinalSteps;
            }
            catch
            {
            }
            try
            {
                castResource = new TxtcResource.TxtcResource(0, resource.Stream);
                tag = "TXTC";
                goto FinalSteps;
            }
            catch
            {
            }
            try
            {
                var genericRCOLResource = new s3pi.GenericRCOLResource.GenericRCOLResource(0, resource.Stream);
                castResource = genericRCOLResource;
                tag = genericRCOLResource.ChunkEntries[0].RCOLBlock.Tag;
                goto FinalSteps;
            }
            catch
            {
            }
            FinalSteps:
            if (!string.IsNullOrEmpty(tag))
            {
                package.ReplaceResource(resourceIndexEntry, castResource ?? resource);
                resourceIndexEntry.ResourceType = GetResourceType(tag);
            }
        }

        public static string ReverseEvaluateResourceKey(IResourceKey resourceKey)
        {   
            var output = "key";
            foreach (var value in new Tuple<ulong, string>[]
                {
                    new Tuple<ulong, string>(resourceKey.ResourceType, "X8"),
                    new Tuple<ulong, string>(resourceKey.ResourceGroup, "X8"),
                    new Tuple<ulong, string>(resourceKey.Instance, "X16")
                })
            {
                output += ":" + value.Item1.ToString(value.Item2);
            }
            return output;
        }
    }
}
