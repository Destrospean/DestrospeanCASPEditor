using System.Collections.Generic;

namespace Destrospean.Graphics.OpenGL
{
    public static class GlobalState
    {
        public static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(System.StringComparer.InvariantCultureIgnoreCase);

        public static readonly List<Volume> Meshes = new List<Volume>();

        public static readonly Dictionary<string, int> TextureIDs = new Dictionary<string, int>(System.StringComparer.InvariantCultureIgnoreCase);
    }
}
