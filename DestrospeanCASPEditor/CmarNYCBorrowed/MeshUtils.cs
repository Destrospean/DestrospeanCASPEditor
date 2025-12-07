using System.Collections.Generic;
using Destrospean.DestrospeanCASPEditor;
using GeometryResource = meshExpImp.ModelBlocks.GeometryResource;

namespace Destrospean.CmarNYCBorrowed
{
    public static class MeshUtils
    {
        public static string GetRigPrefix(Species species, AgeGender age, AgeGender gender)
        {
            var specifier = "";
            switch (age)
            {
                case AgeGender.Toddler:
                    specifier = (species == Species.Human ? "p" : "c");
                    break;
                case AgeGender.Child:
                    specifier = "c";
                    break;
                default:
                    specifier = "a";
                    break;
            }
            switch (species)
            {
                case Species.Human:
                    specifier += "u";
                    break;
                default:
                    specifier += (age == AgeGender.Child && species == Species.LittleDog) ? "d" : species.ToString().Substring(0, 1).ToLower();
                    break;
            }
            return specifier;
        }

        public static RIG GetTS3Rig(this s3pi.Interfaces.IPackage package, Species species, AgeGender age)
        {
            var rigName = GetRigPrefix(species, age, AgeGender.Unisex) + "Rig";
            var evaluated = package.EvaluateResourceKey(new ResourceUtils.ResourceKey(ResourceUtils.GetResourceType("_RIG"), 0, System.Security.Cryptography.FNV64.GetHash(rigName)).ReverseEvaluateResourceKey());
            return new RIG(new System.IO.BinaryReader(s3pi.WrapperDealer.WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry).Stream));
        }

        public static GEOM ToGEOM(this GEOM baseMesh, BGEO morph, int lod, Species species, AgeGender age, AgeGender gender)
        {
            if (baseMesh == null || !baseMesh.HasVertexIDs)
            {
                return baseMesh;
            }
            if (morph == null)
            {
                return new GEOM(baseMesh);
            }
            var morphMesh = new GEOM(baseMesh);
            var entry = morph.GetSection1EntryIndex(species, age, gender);
            if (entry < 0)
            {
                return new GEOM(baseMesh);
            }
            var vertexDeltas = morph.GetDeltas(entry, lod);
            var weight = morph.Weight;
            for (var i = 0; i < morphMesh.VertexCount; i++)
            {
                var vertexID = morphMesh.GetVertexID(i);
                if (vertexDeltas.Exists(x => x.VertexID == vertexID))
                {
                    var vertexData = vertexDeltas.Find(x => x.VertexID == vertexID);
                    Vector3 delta = vertexData.Position,
                    normal = new Vector3(morphMesh.GetNormal(i)),
                    position = new Vector3(morphMesh.GetPosition(i));
                    morphMesh.SetPosition(i, (position + delta * weight).Coordinates);
                    morphMesh.SetNormal(i, (normal + delta * weight).Coordinates);
                }
            }
            return morphMesh;
        }

        public static GEOM LoadBONDMorph(this GEOM baseMesh, BOND boneDelta, RIG rig)
        {
            if (baseMesh == null)
            {
                return null;
            }
            if (boneDelta == null)
            {
                return baseMesh;
            }
            var missingBones = "";
            var morphMesh = new GEOM(baseMesh);
            var unit = new Vector3(1, 1, 1);
            var weight = boneDelta.Weight;
            morphMesh.SetupDeltas();
            foreach (var delta in boneDelta.Adjustments)
            {
                var bone = rig.GetBone(delta.SlotHash);
                if (bone == null)
                {
                    missingBones += "Bone not found: " + delta.SlotHash.ToString("X8") + ", ";
                    continue;
                }
                Vector3 localOffset = new Vector3(delta.OffsetX, delta.OffsetY, delta.OffsetZ),
                localScale = new Vector3(delta.ScaleX, delta.ScaleY, delta.ScaleZ);
                var localRotation = new Quaternion(delta.QuatX, delta.QuatY, delta.QuatZ, delta.QuatW);
                if (localRotation.IsEmpty)
                {
                    localRotation = Quaternion.Identity;
                }
                if (!localRotation.IsNormalized)
                {
                    localRotation.Balance();
                }           
                morphMesh.BoneMorpher(bone, weight, (bone.MorphRotation * localOffset * bone.MorphRotation.Conjugate()).ToVector3(), (bone.MorphRotation.ToMatrix3D() * Matrix3D.FromScale(localScale + unit)).Scale - unit, bone.MorphRotation * localRotation * bone.MorphRotation.Conjugate());
            }
            morphMesh.UpdatePositions();
            foreach (var delta in boneDelta.Adjustments)
            {
                var bone = rig.GetBone(delta.SlotHash);
                if (bone == null)
                {
                    continue;
                }
                Vector3 localOffset = new Vector3(delta.OffsetX, delta.OffsetY, delta.OffsetZ),
                localScale = new Vector3(delta.ScaleX, delta.ScaleY, delta.ScaleZ);
                var localRotation = new Quaternion(delta.QuatX, delta.QuatY, delta.QuatZ, delta.QuatW);
                if (localRotation.IsEmpty)
                {
                    localRotation = Quaternion.Identity;
                }
                if (!localRotation.IsNormalized)
                {
                    localRotation.Balance();
                }
                rig.BoneMorpher(bone, weight, localScale, localOffset, localRotation);
            }
            return morphMesh;
        }

        /// <summary>
        /// Apply morph meshes
        /// </summary>
        /// <param name="baseMesh">base</param>
        /// <param name="morphs">morph meshes for one morph: fat, fit, thin or special</param>
        /// <param name="weight">morph weight</param>
        /// <returns></returns>
        public static GEOM LoadGEOMMorph(this GEOM baseMesh, GEOM[] morphs, float weight)
        {
            if (baseMesh == null)
            {
                return baseMesh;
            }
            if (morphs == null || morphs.Length == 0)
            {
                return new GEOM(baseMesh);
            }
            var morphMesh = new GEOM(baseMesh);
            Dictionary<int, Vector3> deltaNormals = new Dictionary<int, Vector3>(),
            deltaPositions = new Dictionary<int, Vector3>();
            for (var i = 0; i < morphs.Length; i++)
            {
                for (var j = 0; j < morphs[i].VertexCount; j++)
                {
                    var id = morphs[i].GetVertexID(j);
                    if (!deltaNormals.ContainsKey(id))
                    {
                        deltaNormals.Add(id, new Vector3(morphs[i].GetNormal(j)));
                    }
                    if (!deltaPositions.ContainsKey(id))
                    {
                        deltaPositions.Add(id, new Vector3(morphs[i].GetPosition(j)));
                    }
                }
            }
            for (var i = 0; i < morphMesh.VertexCount; i++)
            {
                var vertexID = morphMesh.GetVertexID(i);
                Vector3 delta = new Vector3(),
                normal = new Vector3(morphMesh.GetNormal(i)),
                position = new Vector3(morphMesh.GetPosition(i));
                deltaPositions.TryGetValue(vertexID, out delta);
                morphMesh.SetPosition(i, (position + delta * weight).Coordinates);
                morphMesh.SetNormal(i, (normal + delta * weight).Coordinates);
            }
            return morphMesh;
        }

        public static BGEO ToBGEO(this CASPartResource.BlendGeometryResource blendGeometryResource)
        {
            return new BGEO(new System.IO.BinaryReader(blendGeometryResource.Stream));
        }

        public static GEOM ToGEOM(this GeometryResource geometryResource)
        {
            return new GEOM(new System.IO.BinaryReader(geometryResource.Stream));
        }
    }
}
