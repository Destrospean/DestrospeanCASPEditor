using System.Collections.Generic;

namespace Destrospean.DestrospeanCASPEditor
{
    public class PreloadedData
    {
        public readonly Dictionary<string, Abstractions.CASPart> CASParts = new Dictionary<string, Abstractions.CASPart>(System.StringComparer.InvariantCultureIgnoreCase);

        public readonly Dictionary<string, CmarNYCBorrowed.GEOM> GEOMs = new Dictionary<string, CmarNYCBorrowed.GEOM>(System.StringComparer.InvariantCultureIgnoreCase);

        public static PreloadedData Singleton = new PreloadedData();

        public readonly Dictionary<string, s3pi.GenericRCOLResource.GenericRCOLResource> VPXYs = new Dictionary<string, s3pi.GenericRCOLResource.GenericRCOLResource>(System.StringComparer.InvariantCultureIgnoreCase);
    }
}
