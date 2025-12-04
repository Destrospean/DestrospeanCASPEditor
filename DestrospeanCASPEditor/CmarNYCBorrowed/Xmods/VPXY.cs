using System;
using System.Collections.Generic;
using System.IO;

namespace Destrospean.CmarNYCBorrowed
{
    public class VPXY
    {
        float[] mBoundingBox;

        byte mCount, mFlag;

        Entry[] mEntries;

        int mExternalCount, mInternalCount, mTGICount, mTGISize, mVersion;

        TGI[] mExternalITG, mInternalITG, mTGIList;

        public TGI[] AllLinks
        {
            get
            {
                var temp = new List<TGI>();
                foreach (var entry in mEntries)
                {
                    foreach (var i in entry.IndexArray)
                    {
                        temp.Add(mTGIList[i]);
                    }
                }
                return temp.ToArray();
            }
        }

        public TGI[] BondLinks
        {
            get
            {
                var temp = new List<TGI>();
                foreach (var entry in mEntries)
                {
                    if (entry.Type == EntryType.BoneEntry)
                    {
                        foreach (var i in entry.IndexArray)
                        {
                            temp.Add(mTGIList[i]);
                        }
                    }
                }
                return temp.ToArray();
            }
        }

        public enum EntryType : byte
        {
            MeshEntry,
            BoneEntry
        }

        public class Entry
        {
            byte mLOD, mRefCount;

            int[] mTGIRefs;

            EntryType mType;

            public int[] IndexArray
            {
                get
                {
                    return mTGIRefs;
                }
            }

            public int LOD
            {
                get
                {
                    return mType == EntryType.MeshEntry && mTGIRefs.Length > 0 ? (int)mLOD : -1;
                }
                set
                {
                    if (mType == EntryType.MeshEntry && mTGIRefs.Length > 0)
                    {
                        mLOD = (byte)value;
                    }
                }
            }

            public int Size
            {
                get
                {
                    return mType == EntryType.MeshEntry ? 3 + (mRefCount * 4) : 5;
                }
            }

            public EntryType Type
            {
                get
                {
                    return mType;
                }
                set
                {
                    mType = value;
                }
            }

            public Entry(BinaryReader reader)
            {
                mType = (EntryType)reader.ReadByte();
                if (mType == EntryType.MeshEntry)
                {
                    mLOD = reader.ReadByte();
                    mRefCount = reader.ReadByte();
                    mTGIRefs = new int[mRefCount];
                    for (var i = 0; i < mRefCount; i++)
                    {
                        mTGIRefs[i] = reader.ReadInt32();
                    }
                }
                else if (mType == EntryType.BoneEntry)
                {
                    mTGIRefs = new int[1];
                    mTGIRefs[0] = reader.ReadInt32();
                }
            }

            public Entry(int lod, int tgiIndex) : this((byte)lod, new int[]
                {
                    tgiIndex
                })
            {
            }

            public Entry(byte lod, int[] tgiIndexArray)
            {
                mType = EntryType.MeshEntry;
                mLOD = lod;
                mTGIRefs = new int[tgiIndexArray.Length];
                mRefCount = (byte)tgiIndexArray.Length;
                for (var i = 0; i < tgiIndexArray.Length; i++)
                {
                    mTGIRefs[i] = tgiIndexArray[i];
                }
            }

            public Entry(int lod, int[] tgiIndexArray) : this((byte)lod, tgiIndexArray)
            {
            }

            public Entry(int tgiIndex)
            {
                mType = EntryType.BoneEntry;
                mTGIRefs = new int[]
                    {
                        tgiIndex
                    };
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write((byte)mType);
                if (mType == EntryType.MeshEntry)
                {
                    writer.Write(mLOD);
                    writer.Write(mRefCount);
                    for (var i = 0; i < mRefCount; i++)
                    {
                        writer.Write(mTGIRefs[i]);
                    }
                }
                else if (mType == EntryType.BoneEntry)
                {
                    writer.Write(mTGIRefs[0]);
                }
            }
        }

        public VPXY(BinaryReader reader)
        {
            reader.BaseStream.Position = 0;
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            mExternalCount = reader.ReadInt32();
            mInternalCount = reader.ReadInt32();
            if (mInternalCount > 0)
            {
                mInternalITG = new TGI[mInternalCount];
            }
            for (var i = 0; i < mInternalCount; i++)
            {
                mInternalITG[i] = new TGI(reader, TGI.TGISequence.ITG);
            }
            if (mExternalCount > 0)
            {
                mExternalITG = new TGI[mExternalCount];
            }
            for (var i = 0; i < mExternalCount; i++)
            {
                mExternalITG[i] = new TGI(reader, TGI.TGISequence.ITG);
            }
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadChars(4);
            mVersion = reader.ReadInt32();
            reader.ReadInt32();
            mTGISize = reader.ReadInt32();
            mCount = reader.ReadByte();
            mEntries = new Entry[mCount];
            for (var i = 0; i < mCount; i++)
            {
                mEntries[i] = new Entry(reader);
            }
            reader.ReadByte();
            mBoundingBox = new float[6];
            for (var i = 0; i < 6; i++)
            {
                mBoundingBox[i] = reader.ReadSingle();
            }
            reader.ReadUInt32();
            if (mVersion <= 4)
            {
                mFlag = reader.ReadByte();
                if (mFlag == 1)
                {
                    reader.ReadInt32();
                }
            }
            if (mTGISize > 0)
            {
                mTGICount = reader.ReadInt32();
                mTGIList = new TGI[mTGICount];
                for (var i = 0; i < mTGICount; i++)
                {
                    mTGIList[i] = new TGI(reader);
                }
            }
            else
            {
                mTGICount = 0;
                mTGIList = new TGI[0];
            }
        }

        public TGI[] MeshLinks(int lod)
        {
            var temp = new List<TGI>();
            foreach (var entry in mEntries)
            {
                if (entry.Type == EntryType.MeshEntry && entry.LOD == lod)
                {
                    foreach (var i in entry.IndexArray)
                    {
                        temp.Add(mTGIList[i]);
                    }
                }
            }
            return temp.ToArray();
        }
    }
}
