using System;
using System.Collections.Generic;
using System.IO;
using Destrospean.CmarNYCBorrowed;
using Destrospean.S3PIAbstractions;
using s3pi.GenericRCOLResource;
using s3pi.Interfaces;
using s3pi.WrapperDealer;

namespace Destrospean.DestrospeanCASPEditor.Abstractions
{
    public class CASPart : CASTableObject
    {
        public AgeGender AdjustedAge
        {
            get
            {
                var age = (AgeGender)(uint)CASPartResource.AgeGender.Age;
                return age >= AgeGender.Teen && age <= AgeGender.Elder ? AgeGender.Adult : age;
            }
        }

        public Species AdjustedSpecies
        {
            get
            {
                var species = (Species)((uint)CASPartResource.AgeGender.Species << 8);
                return species == 0 ? Species.Human : species;
            }
        }
            
        public readonly CASPartResource.CASPartResource CASPartResource;

        public override RIG CurrentRig
        {
            get
            {
                if (mCurrentRig == null)
                {
                    mCurrentRig = MeshUtils.GetRig(ParentPackage, AdjustedSpecies, AdjustedAge);
                }
                return mCurrentRig;
            }
        }

        public readonly Dictionary<int, List<GEOM>> LODs = new Dictionary<int, List<GEOM>>();

        public CASPart(IPackage package, IResourceIndexEntry resourceIndexEntry, Dictionary<string, GEOM> geometryResources, Dictionary<string, GenericRCOLResource> vpxyResources) : base(package, resourceIndexEntry)
        {
            CASPartResource = (CASPartResource.CASPartResource)WrapperDealer.GetResource(0, package, resourceIndexEntry);
            Presets.AddRange(CASPartResource.Presets.ConvertAll(x => new Preset(this, x)));
            LoadLODs(geometryResources, vpxyResources);
        }

        public void AddMeshGroup(int lod, Dictionary<string, GEOM> geometryResources, Dictionary<string, GenericRCOLResource> vpxyResources)
        {
            var vpxyResourceIndexEntry = ParentPackage.GetResourceIndexEntry(CASPartResource.TGIBlocks[CASPartResource.VPXYIndexes[0]]);
            var vpxyKey = vpxyResourceIndexEntry.ReverseEvaluateResourceKey();
            GenericRCOLResource vpxyResource;
            if (!vpxyResources.TryGetValue(vpxyKey, out vpxyResource))
            {
                vpxyResources.Add(vpxyKey, (GenericRCOLResource)WrapperDealer.GetResource(0, ParentPackage, vpxyResourceIndexEntry));
                vpxyResource = vpxyResources[vpxyKey];
            }
            var vpxy = new CmarNYCBorrowed.VPXY(new BinaryReader(vpxyResource.Stream));
            var geomTGIs = new TGI[4][];
            for (var i = 0; i < geomTGIs.GetLength(0); i++)
            {
                var geomTGIList = new List<TGI>(vpxy.GetMeshLinks(i));
                if (i == lod || lod == -1)
                {
                    var temp = "_lod" + i.ToString() + "-" + (geomTGIList.Count + 1).ToString();
                    var newGEOMTGI = new TGI(ResourceUtils.GetResourceType("GEOM"), geomTGIList[geomTGIList.Count - 1].Group, System.Security.Cryptography.FNV64.GetHash(CASPartResource.Unknown1 + temp + Environment.UserName + Environment.TickCount.ToString() + temp));
                    var geomStream = new MemoryStream();
                    var geom = geometryResources[new ResourceUtils.ResourceKey(geomTGIList[geomTGIList.Count - 1].Type, geomTGIList[geomTGIList.Count - 1].Group, geomTGIList[geomTGIList.Count - 1].Instance).ReverseEvaluateResourceKey()];
                    geom.Write(new BinaryWriter(geomStream));
                    var newGEOMResourceIndexEntry = ParentPackage.AddResource(new ResourceUtils.ResourceKey(newGEOMTGI.Type, newGEOMTGI.Group, newGEOMTGI.Instance), geomStream, true);
                    geometryResources.Add(newGEOMResourceIndexEntry.ReverseEvaluateResourceKey(), geom);
                    geomTGIList.Add(newGEOMTGI);
                }
                geomTGIs[i] = geomTGIList.ToArray();
            }
            var vpxyStream = new MemoryStream();
            new CmarNYCBorrowed.VPXY(new TGI(vpxyResourceIndexEntry.ResourceType, vpxyResourceIndexEntry.ResourceGroup, vpxyResourceIndexEntry.Instance), vpxy.BondLinks, geomTGIs).Write(new BinaryWriter(vpxyStream));
            vpxyResource = new GenericRCOLResource(0, vpxyStream);
            ParentPackage.ReplaceResource(vpxyResourceIndexEntry, vpxyResource);
            vpxyResources[vpxyKey] = vpxyResource;
        }

        public override void AdjustPresetCount()
        {
            while (CASPartResource.Presets.Count < Presets.Count)
            {
                CASPartResource.Presets.Add(new CASPartResource.CASPartResource.Preset(0, null));
            }
            while (CASPartResource.Presets.Count > Presets.Count)
            {
                CASPartResource.Presets.RemoveAt(0);
            }
        }

        public void DeleteMeshGroup(int lod, int groupIndex, Dictionary<string, GEOM> geometryResources, Dictionary<string, GenericRCOLResource> vpxyResources)
        {
            var vpxyResourceIndexEntry = ParentPackage.GetResourceIndexEntry(CASPartResource.TGIBlocks[CASPartResource.VPXYIndexes[0]]);
            var vpxyKey = vpxyResourceIndexEntry.ReverseEvaluateResourceKey();
            GenericRCOLResource vpxyResource;
            if (!vpxyResources.TryGetValue(vpxyKey, out vpxyResource))
            {
                vpxyResources.Add(vpxyKey, (GenericRCOLResource)WrapperDealer.GetResource(0, ParentPackage, vpxyResourceIndexEntry));
                vpxyResource = vpxyResources[vpxyKey];
            }
            var vpxy = new CmarNYCBorrowed.VPXY(new BinaryReader(vpxyResource.Stream));
            var geomTGIs = new TGI[4][];
            for (var i = 0; i < geomTGIs.GetLength(0); i++)
            {
                var geomTGIList = new List<TGI>(vpxy.GetMeshLinks(i));
                if (i == lod || lod == -1)
                {
                    var geomKey = new ResourceUtils.ResourceKey(geomTGIList[groupIndex].Type, geomTGIList[groupIndex].Group, geomTGIList[groupIndex].Instance).ReverseEvaluateResourceKey();
                    ParentPackage.DeleteResource(ParentPackage.EvaluateResourceKey(geomKey).ResourceIndexEntry);
                    geometryResources.Remove(geomKey);
                    geomTGIList.RemoveAt(groupIndex);
                }
                geomTGIs[i] = geomTGIList.ToArray();
            }
            var vpxyStream = new MemoryStream();
            new CmarNYCBorrowed.VPXY(new TGI(vpxyResourceIndexEntry.ResourceType, vpxyResourceIndexEntry.ResourceGroup, vpxyResourceIndexEntry.Instance), vpxy.BondLinks, geomTGIs).Write(new BinaryWriter(vpxyStream));
            vpxyResource = new GenericRCOLResource(0, vpxyStream);
            ParentPackage.ReplaceResource(vpxyResourceIndexEntry, vpxyResource);
            vpxyResources[vpxyKey] = vpxyResource;
        }

        public void LoadLODs(Dictionary<string, GEOM> geometryResources, Dictionary<string, GenericRCOLResource> vpxyResources)
        {
            var vpxyResourceIndexEntry = ParentPackage.GetResourceIndexEntry(CASPartResource.TGIBlocks[CASPartResource.VPXYIndexes[0]]);
            var vpxyKey = vpxyResourceIndexEntry.ReverseEvaluateResourceKey();
            GenericRCOLResource vpxyResource;
            if (!vpxyResources.TryGetValue(vpxyKey, out vpxyResource))
            {
                vpxyResources.Add(vpxyKey, (GenericRCOLResource)WrapperDealer.GetResource(0, ParentPackage, vpxyResourceIndexEntry));
                vpxyResource = vpxyResources[vpxyKey];
            }
            foreach (var entry in ((s3pi.GenericRCOLResource.VPXY)vpxyResource.ChunkEntries[0].RCOLBlock).Entries)
            {
                var entry00 = entry as s3pi.GenericRCOLResource.VPXY.Entry00;
                if (entry00 != null)
                {
                    LODs[entry00.EntryID] = new List<GEOM>();
                    foreach (var tgiIndex in entry00.TGIIndexes)
                    {
                        var geometryResourceIndexEntry = ParentPackage.GetResourceIndexEntry(entry00.ParentTGIBlocks[tgiIndex]);
                        var geometryResourceKey = geometryResourceIndexEntry.ReverseEvaluateResourceKey();
                        GEOM geometryResource;
                        if (!geometryResources.TryGetValue(geometryResourceKey, out geometryResource))
                        {
                            geometryResources.Add(geometryResourceKey, new GEOM(new BinaryReader(((APackage)ParentPackage).GetResource(geometryResourceIndexEntry))));
                            geometryResource = geometryResources[geometryResourceKey];
                        }
                        LODs[entry00.EntryID].Add(geometryResource);
                    }
                }
            }
        }

        public override void SavePreset(int index)
        {
            CASPartResource.Presets[index].Unknown1 = (uint)index;
            CASPartResource.Presets[index].XmlFile = Presets[index].XmlFile;
        }

        public override void SavePresets()
        {
            SaveDefaultPreset();
            AdjustPresetCount();
            for (var i = 0; i < CASPartResource.Presets.Count; i++)
            {
                SavePreset(i);
            }
        }
    }
}
