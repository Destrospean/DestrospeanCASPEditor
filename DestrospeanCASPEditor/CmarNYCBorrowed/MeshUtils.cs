using System;
using System.Collections.Generic;
using Xmods.DataLib;

namespace Destrospean.CmarNYCBorrowed
{
    public static class MeshUtils
    {
        public static GEOM LoadBGEOMorph(GEOM baseMesh, BGEO morph, int lod, XmodsEnums.Species species, XmodsEnums.Age age, XmodsEnums.Gender gender)
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
                    morphMesh.setPosition(i, (position + (delta * weight)).Coordinates);
                    morphMesh.setNormal(i, (normal + (delta * weight)).Coordinates);
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
            var deltaPositions = new Dictionary<int, Vector3>();
            var deltaNormals = new Dictionary<int, Vector3>();
            for (var i = 0; i < morphs.Length; i++)
            {
                for (var j = 0; j < morphs[i].numberVertices; j++)
                {
                    var id = morphs[i].getVertexID(j);
                    if (!deltaPositions.ContainsKey(id))
                    {
                        deltaPositions.Add(id, new Vector3(morphs[i].getPosition(j)));
                    }
                    if (!deltaNormals.ContainsKey(id))
                    {
                        deltaNormals.Add(id, new Vector3(morphs[i].getNormal(j)));
                    }
                }
            }
            for (var i = 0; i < morphMesh.numberVertices; i++)
            {
                var vertexID = morphMesh.getVertexID(i);
                Vector3 delta = new Vector3(),
                normal = new Vector3(morphMesh.getNormal(i)),
                position = new Vector3(morphMesh.getPosition(i));
                deltaPositions.TryGetValue(vertexID, out delta);
                morphMesh.setPosition(i, (position + (delta * weight)).Coordinates);
                morphMesh.setNormal(i, (normal + (delta * weight)).Coordinates);
            }
            return morphMesh;
        }
    }
}

