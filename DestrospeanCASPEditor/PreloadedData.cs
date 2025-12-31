using System;
using System.Collections.Generic;
using Destrospean.DestrospeanCASPEditor.OpenGL;

namespace Destrospean.DestrospeanCASPEditor
{
    public class PreloadedData
    {
        public readonly Dictionary<string, Abstractions.CASPart> CASParts = new Dictionary<string, Abstractions.CASPart>(StringComparer.InvariantCultureIgnoreCase);

        public readonly Dictionary<string, CmarNYCBorrowed.GEOM> GEOMs = new Dictionary<string, CmarNYCBorrowed.GEOM>(StringComparer.InvariantCultureIgnoreCase);

        public readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(StringComparer.InvariantCultureIgnoreCase);

        public readonly List<Volume> Meshes = new List<Volume>();

        public static readonly PreloadedData Singleton = new PreloadedData();

        public readonly Dictionary<string, int> TextureIDs = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

        public readonly Dictionary<string, s3pi.GenericRCOLResource.GenericRCOLResource> VPXYs = new Dictionary<string, s3pi.GenericRCOLResource.GenericRCOLResource>(StringComparer.InvariantCultureIgnoreCase);
    }
}
