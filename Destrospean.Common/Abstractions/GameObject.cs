using System;
using System.Collections.Generic;
using Destrospean.CmarNYCBorrowed;
using Destrospean.S3PIExtensions;
using s3pi.Interfaces;
using s3pi.WrapperDealer;

namespace Destrospean.Common.Abstractions
{
    public class GameObject : CASTableObject
    {
        ObjKeyResource.ObjKeyResource mObjKeyResource;

        public readonly CatalogResource.CatalogResource CatalogResource;

        public override Rig CurrentRig
        {
            get
            {
                return mCurrentRig;
            }
        }

        public CatalogResource.ObjectCatalogResource ObjectCatalogResource
        {
            get
            {
                return CatalogResource as CatalogResource.ObjectCatalogResource;
            }
        }

        public ObjKeyResource.ObjKeyResource ObjKeyResource
        {
            get
            {
                if (ObjectCatalogResource == null)
                {
                    return null;
                }
                if (mObjKeyResource == null)
                {
                    var evaluated = ParentPackage.EvaluateResourceKey(ObjectCatalogResource.TGIBlocks[(int)ObjectCatalogResource.OBJKIndex].ReverseEvaluateResourceKey());
                    mObjKeyResource = (ObjKeyResource.ObjKeyResource)WrapperDealer.GetResource(0, evaluated.Package, evaluated.ResourceIndexEntry);
                }
                return mObjKeyResource;
            }
        }

        public GameObject(IPackage package, IResourceIndexEntry resourceIndexEntry) : base(package, resourceIndexEntry)
        {
            CatalogResource = (CatalogResource.CatalogResource)WrapperDealer.GetResource(0, package, resourceIndexEntry);
            var propertyInfo = CatalogResource.GetType().GetProperty("Materials", typeof(CatalogResource.CatalogResource.MaterialList));
            if (propertyInfo != null)
            {
                Presets.AddRange(((CatalogResource.CatalogResource.MaterialList)propertyInfo.GetValue(CatalogResource, null)).ConvertAll(x => new Material(this, x.MaterialBlock) as IPreset));
            }
        }

        public override void SavePresets()
        {
            SaveDefaultPreset();
        }
    }
}
