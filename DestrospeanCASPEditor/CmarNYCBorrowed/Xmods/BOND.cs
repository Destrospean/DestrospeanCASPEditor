using System;
using System.IO;

namespace Destrospean.CmarNYCBorrowed
{
    public class BOND
    {
        public uint ContextVersion = 3;
        public TGI[] PublicKey;
        public TGI[] ExternalKey;
        public TGI[] DelayLoadKey;
        public ObjectData[] ObjectKey;
        uint version = 1;
        public BoneAdjust[] Adjustments;

        public float Weight = 1f;
        public string Name = "unknown";

        public void RemoveBoneAdjust(int index)
        {
            BoneAdjust[] tmp = new BoneAdjust[this.Adjustments.Length - 1];
            Array.Copy(this.Adjustments, 0, tmp, 0, index);
            Array.Copy(this.Adjustments, index + 1, tmp, index, tmp.Length - index);
            this.Adjustments = tmp;
        }

        public void AddBoneAdjust(BoneAdjust adjust)
        {
            BoneAdjust[] tmp = new BoneAdjust[this.Adjustments.Length + 1];
            Array.Copy(this.Adjustments, tmp, this.Adjustments.Length);
            tmp[tmp.Length - 1] = new BoneAdjust(adjust);
            this.Adjustments = tmp;
        }

        /// <summary>
        /// Tests for extreme scaling
        /// </summary>
        /// <returns></returns>
        public bool IsSizeMorph()
        {
            foreach (BoneAdjust b in this.Adjustments)
            {
                if ((b.SlotHash == 0xFEAE6981 || b.SlotHash == 0x57884BB9 || b.SlotHash == 0x556B181A ||
                    b.SlotHash == 0x6FA96266 || b.SlotHash == 0xAFAC05CF || b.SlotHash == 0x6FAF7238) &&
                    (Math.Abs(b.ScaleX) > 0.1f || Math.Abs(b.ScaleY) > 0.1f || Math.Abs(b.ScaleZ) > 0.1f))
                    return true;
            }
            return false;
        }

        public BOND(BinaryReader br)
        {
            br.BaseStream.Position = 0;
            this.ContextVersion = br.ReadUInt32();
            uint publicKeyCount = br.ReadUInt32();
            uint externalKeyCount = br.ReadUInt32();
            uint delayLoadKeyCount = br.ReadUInt32();
            uint objectKeyCount = br.ReadUInt32();
            this.PublicKey = new TGI[publicKeyCount];
            for (int i = 0; i < publicKeyCount; i++) PublicKey[i] = new TGI(br, TGI.TGIsequence.ITG);
            this.ExternalKey = new TGI[externalKeyCount];
            for (int i = 0; i < externalKeyCount; i++) ExternalKey[i] = new TGI(br, TGI.TGIsequence.ITG);
            this.DelayLoadKey = new TGI[delayLoadKeyCount];
            for (int i = 0; i < delayLoadKeyCount; i++) DelayLoadKey[i] = new TGI(br, TGI.TGIsequence.ITG);
            this.ObjectKey = new ObjectData[objectKeyCount];
            //for (int i = 0; i < objectKeyCount; i++) objectKey[i] = new ObjectData(br);
            ObjectKey[0] = new ObjectData(br);
            version = br.ReadUInt32();
            uint boneAdjustCount = br.ReadUInt32();
            Adjustments = new BoneAdjust[boneAdjustCount];
            for (uint i = 0; i < boneAdjustCount; i++)
            {
                Adjustments[i] = new BoneAdjust(br);
            }
        }

        public BOND()
        {
            this.PublicKey = new TGI[] { new TGI() };
            this.Adjustments = new BoneAdjust[0];
        }

        internal void Write(BinaryWriter bw)
        {
            bw.Write(this.ContextVersion);
            if (this.PublicKey == null) this.PublicKey = new TGI[0];
            bw.Write(PublicKey.Length);
            if (this.ExternalKey == null) this.ExternalKey = new TGI[0];
            bw.Write(ExternalKey.Length);
            if (this.DelayLoadKey == null) this.DelayLoadKey = new TGI[0];
            bw.Write(DelayLoadKey.Length);
            bw.Write(1);
            for (int i = 0; i < PublicKey.Length; i++) PublicKey[i].Write(bw, TGI.TGIsequence.ITG);
            for (int i = 0; i < ExternalKey.Length; i++) ExternalKey[i].Write(bw, TGI.TGIsequence.ITG);
            for (int i = 0; i < DelayLoadKey.Length; i++) DelayLoadKey[i].Write(bw, TGI.TGIsequence.ITG);
            this.ObjectKey = new ObjectData[] { new ObjectData((uint)(20 + (PublicKey.Length * 16) + (ExternalKey.Length * 16) + (DelayLoadKey.Length * 16) + 8),
                (uint)(4 + (Adjustments.Length * 44))) };
            for (int i = 0; i < ObjectKey.Length; i++) ObjectKey[i].Write(bw);
            bw.Write(version);
            if (Adjustments == null) Adjustments = new BoneAdjust[0];
            bw.Write(Adjustments.Length);
            for (uint i = 0; i < Adjustments.Length; i++)
            {
                Adjustments[i].Write(bw);
            }
        }

        public class ObjectData
        {
            internal uint position;
            internal uint length;

            internal ObjectData(BinaryReader br)
            {
                this.position = br.ReadUInt32();
                this.length = br.ReadUInt32();
            }

            internal ObjectData(uint position, uint length)
            {
                this.position = position;
                this.length = length;
            }

            internal void Write(BinaryWriter bw)
            {
                bw.Write(this.position);
                bw.Write(this.length);
            }
        }

        public class BoneAdjust
        {
            public uint SlotHash;
            public float OffsetX;
            public float OffsetY;
            public float OffsetZ;
            public float ScaleX;
            public float ScaleY;
            public float ScaleZ;
            public float QuatX;
            public float QuatY;
            public float QuatZ;
            public float QuatW;

            public BoneAdjust(BinaryReader br)
            {
                SlotHash = br.ReadUInt32();
                OffsetX = br.ReadSingle();
                OffsetY = br.ReadSingle();
                OffsetZ = br.ReadSingle();
                ScaleX = br.ReadSingle();
                ScaleY = br.ReadSingle();
                ScaleZ = br.ReadSingle();
                QuatX = br.ReadSingle();
                QuatY = br.ReadSingle();
                QuatZ = br.ReadSingle();
                QuatW = br.ReadSingle();
            }

            public BoneAdjust() {}

            public BoneAdjust(BoneAdjust other)
            {
                SlotHash = other.SlotHash;
                OffsetX = other.OffsetX;
                OffsetY = other.OffsetY;
                OffsetZ = other.OffsetZ;
                ScaleX = other.ScaleX;
                ScaleY = other.ScaleY;
                ScaleZ = other.ScaleZ;
                QuatX = other.QuatX;
                QuatY = other.QuatY;
                QuatZ = other.QuatZ;
                QuatW = other.QuatW;
            }

            internal void Write(BinaryWriter bw)
            {
                bw.Write(SlotHash);
                bw.Write(OffsetX);
                bw.Write(OffsetY);
                bw.Write(OffsetZ);
                bw.Write(ScaleX);
                bw.Write(ScaleY);
                bw.Write(ScaleZ);
                bw.Write(QuatX);
                bw.Write(QuatY);
                bw.Write(QuatZ);
                if (QuatX > 0f || QuatY > 0f || QuatZ > 0f)
                {
                    bw.Write(QuatW);
                }
                else
                {
                    bw.Write(0f);
                }
            }
        }
    }
}
