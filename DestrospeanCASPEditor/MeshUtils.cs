using Xmods.DataLib;

namespace Destrospean.DestrospeanCASPEditor
{
    public static class MeshUtils
    {
        public static Destrospean.CmarNYCBorrowed.BGEO ToBGEO(this CASPartResource.BlendGeometryResource blendGeometryResource)
        {
            return new Destrospean.CmarNYCBorrowed.BGEO(new System.IO.BinaryReader(blendGeometryResource.Stream));
        }

        public static GEOM ToGEOM(this meshExpImp.ModelBlocks.GeometryResource geometryResource)
        {
            return new GEOM(new System.IO.BinaryReader(geometryResource.Stream));
        }
    }
}
