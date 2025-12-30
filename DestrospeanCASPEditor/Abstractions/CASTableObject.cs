using System.Collections.Generic;
using Destrospean.CmarNYCBorrowed;
using Destrospean.S3PIAbstractions;

namespace Destrospean.DestrospeanCASPEditor.Abstractions
{
    public abstract class CASTableObject
    {
        protected RIG mCurrentRig;

        public List<Preset> AllPresets
        {
            get
            {
                var allPresets = new List<Preset>(Presets);
                if (DefaultPreset != null)
                {
                    allPresets.Insert(0, DefaultPreset);
                }
                return allPresets;
            }
        }

        public abstract RIG CurrentRig
        {
            get;
        }

        public readonly Preset DefaultPreset;

        public readonly string DefaultPresetKey;

        public readonly s3pi.Interfaces.IPackage ParentPackage;

        public readonly List<Preset> Presets;

        public CASTableObject(s3pi.Interfaces.IPackage package, s3pi.Interfaces.IResourceIndexEntry resourceIndexEntry)
        {
            ParentPackage = package;
            var defaultPresetResourceIndexEntries = ParentPackage.FindAll(x => x.ResourceType == ResourceUtils.GetResourceType("_XML") && x.ResourceGroup == resourceIndexEntry.ResourceGroup && x.Instance == resourceIndexEntry.Instance);
            if (defaultPresetResourceIndexEntries.Count == 0)
            {
                DefaultPresetKey = null;
                DefaultPreset = null;
            }
            else
            {
                DefaultPresetKey = defaultPresetResourceIndexEntries[0].ReverseEvaluateResourceKey();
                DefaultPreset = new Preset(this, new System.IO.StreamReader(s3pi.WrapperDealer.WrapperDealer.GetResource(0, ParentPackage, defaultPresetResourceIndexEntries[0]).Stream));
            }
            Presets = new List<Preset>();
        }

        public abstract void AdjustPresetCount();

        public void ClearCurrentRig()
        {
            mCurrentRig = null;
        }

        public void SaveDefaultPreset()
        {   
            if (DefaultPreset == null || DefaultPresetKey == null)
            {
                return;
            }
            var defaultPresetResourceIndexEntry = ParentPackage.EvaluateResourceKey(DefaultPresetKey).ResourceIndexEntry;
            var tempResourceIndexEntry = ParentPackage.AddResource(defaultPresetResourceIndexEntry, new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(AllPresets[0].XmlFile.ReadToEnd())), false);
            ParentPackage.ReplaceResource(defaultPresetResourceIndexEntry, s3pi.WrapperDealer.WrapperDealer.GetResource(0, ParentPackage, tempResourceIndexEntry));
            ParentPackage.DeleteResource(tempResourceIndexEntry);
        }

        public abstract void SavePreset(int index);

        public abstract void SavePresets();
    }
}
