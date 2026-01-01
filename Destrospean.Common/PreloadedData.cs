using System;
using System.Collections.Generic;
using Destrospean.Common.OpenGL;

namespace Destrospean.Common
{
    public static class PreloadedData
    {
        public static readonly Dictionary<string, Destrospean.Common.Abstractions.CASPart> CASParts = new Dictionary<string, Destrospean.Common.Abstractions.CASPart>(StringComparer.InvariantCultureIgnoreCase);

        public static readonly Dictionary<string, CmarNYCBorrowed.GEOM> GEOMs = new Dictionary<string, CmarNYCBorrowed.GEOM>(StringComparer.InvariantCultureIgnoreCase);

        public static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(StringComparer.InvariantCultureIgnoreCase);

        public static readonly List<Volume> Meshes = new List<Volume>();

        public static readonly Dictionary<string, int> TextureIDs = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

        public static readonly Dictionary<string, s3pi.GenericRCOLResource.GenericRCOLResource> VPXYs = new Dictionary<string, s3pi.GenericRCOLResource.GenericRCOLResource>(StringComparer.InvariantCultureIgnoreCase);
    }
}
