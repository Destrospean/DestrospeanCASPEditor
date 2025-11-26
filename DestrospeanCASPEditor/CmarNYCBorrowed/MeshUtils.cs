using System.Collections.Generic;
using CASPartResource;
using Destrospean.DestrospeanCASPEditor;
using Xmods.DataLib;
using GeometryResource = meshExpImp.ModelBlocks.GeometryResource;

namespace Destrospean.CmarNYCBorrowed
{
    public static class MeshUtils
    {
        public static GEOM LoadBGEOMorph(GeometryResource baseMesh, BlendGeometryResource morph, int lod, SpeciesType species, AgeFlags age, GenderFlags gender)
        {
            return LoadBGEOMorph(baseMesh.ToGEOM(), morph.ToBGEO(), lod, species, age, gender);
        }

        public static GEOM LoadBGEOMorph(GEOM baseMesh, BGEO morph, int lod, SpeciesType species, AgeFlags age, GenderFlags gender)
        {
            if (baseMesh == null || !baseMesh.hasVertexIDs)
            {
                return baseMesh;
            }
            if (morph == null)
            {
                return new GEOM(baseMesh);
            }
            var morphMesh = new GEOM(baseMesh);
            var weight = morph.Weight;
            var entry = morph.GetSection1EntryIndex(species, age, gender);
            if (entry < 0)
            {
                return new GEOM(baseMesh);
            }
            var vertexDeltas = morph.GetDeltas(entry, lod);
            for (var i = 0; i < morphMesh.numberVertices; i++)
            {
                var vertexID = morphMesh.getVertexID(i);
                if (vertexDeltas.Exists(x => x.VertexID == vertexID))
                {
                    var vertexData = vertexDeltas.Find(x => x.VertexID == vertexID);
                    Vector3 delta = vertexData.Position,
                    normal = new Vector3(morphMesh.getNormal(i)),
                    position = new Vector3(morphMesh.getPosition(i));
                    morphMesh.setPosition(i, (position + delta * weight).Coordinates);
                    morphMesh.setNormal(i, (normal + delta * weight).Coordinates);
                }
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
        public static GEOM LoadGEOMMorph(GeometryResource baseMesh, GeometryResource[] morphs, float weight)
        {
            return LoadGEOMMorph(baseMesh.ToGEOM(), new List<GeometryResource>(morphs).ConvertAll(x => x.ToGEOM()).ToArray(), weight);
        }

        /// <summary>
        /// Apply morph meshes
        /// </summary>
        /// <param name="baseMesh">base</param>
        /// <param name="morphs">morph meshes for one morph: fat, fit, thin or special</param>
        /// <param name="weight">morph weight</param>
        /// <returns></returns>
        public static GEOM LoadGEOMMorph(GEOM baseMesh, GEOM[] morphs, float weight)
        {
            if (baseMesh == null)
            {
                return baseMesh;
            }
            if (morphs.Length == 0 || morphs == null)
            {
                return new GEOM(baseMesh);
            }
            var morphMesh = new GEOM(baseMesh);
            Dictionary<int, Vector3> deltaNormals = new Dictionary<int, Vector3>(),
            deltaPositions = new Dictionary<int, Vector3>();
            for (var i = 0; i < morphs.Length; i++)
            {
                for (var j = 0; j < morphs[i].numberVertices; j++)
                {
                    var id = morphs[i].getVertexID(j);
                    if (!deltaNormals.ContainsKey(id))
                    {
                        deltaNormals.Add(id, new Vector3(morphs[i].getNormal(j)));
                    }
                    if (!deltaPositions.ContainsKey(id))
                    {
                        deltaPositions.Add(id, new Vector3(morphs[i].getPosition(j)));
                    }
                }
            }
            for (var i = 0; i < morphMesh.numberVertices; i++)
            {
                var vertexId = morphMesh.getVertexID(i);
                Vector3 delta = new Vector3(),
                normal = new Vector3(morphMesh.getNormal(i)),
                position = new Vector3(morphMesh.getPosition(i));
                deltaPositions.TryGetValue(vertexId, out delta);
                morphMesh.setPosition(i, (position + delta * weight).Coordinates);
                morphMesh.setNormal(i, (normal + delta * weight).Coordinates);
            }
            return morphMesh;
        }
    }
}
