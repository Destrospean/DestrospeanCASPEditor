using System;
using System.Collections.Generic;
using System.IO;

namespace Destrospean.CmarNYCBorrowed
{
    public class VPXY
    {
        float[] mBoundingBox;

        byte mBoxType, mCount, mFlag;

        int mChunkPosition, mChunkSize, mExtCount, mFTPTIndex, mIndex3, mIntCount, mRCOLCount, mRCOLVersion, mTGICount, mTGIOffset, mTGISize, mVersion;

        Entry[] mEntries;

        TGI[] mExtITG, mIntITG, mTGIList;

        char[] mMagic;

        uint mUnknown;

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

        public enum EntryType : byte
        {
            MeshEntry = 0,
            BoneEntry = 1
        }

        public class Entry
        {
            byte mLOD;

            byte mRefCount;

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
                    return mType == EntryType.MeshEntry & mTGIRefs.Length > 0 ? (int)mLOD : -1;
                }
                set
                {
                    if (mType == EntryType.MeshEntry & mTGIRefs.Length > 0)
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
            mRCOLVersion = reader.ReadInt32();
            mRCOLCount = reader.ReadInt32();
            mIndex3 = reader.ReadInt32();
            mExtCount = reader.ReadInt32();
            mIntCount = reader.ReadInt32();
            if (mIntCount > 0)
            {
                mIntITG = new TGI[mIntCount];
            }
            for (var i = 0; i < mIntCount; i++)
            {
                var instance = reader.ReadUInt64();
                uint type = reader.ReadUInt32(), group = reader.ReadUInt32();
                mIntITG[i] = new TGI(type, group, instance);
            }
            if (mExtCount > 0) mExtITG = new TGI[mExtCount];
            for (var i = 0; i < mExtCount; i++)
            {
                var instance = reader.ReadUInt64();
                uint type = reader.ReadUInt32(), group = reader.ReadUInt32();
                mExtITG[i] = new TGI(type, group, instance);
            }
            mChunkPosition = reader.ReadInt32();
            mChunkSize = reader.ReadInt32();
            mMagic = new char[4];
            mMagic = reader.ReadChars(4);
            mVersion = reader.ReadInt32();
            mTGIOffset = reader.ReadInt32();
            mTGISize = reader.ReadInt32();
            mCount = reader.ReadByte();
            mEntries = new Entry[mCount];
            for (var i = 0; i < mCount; i++)
            {
                mEntries[i] = new Entry(reader);
            }
            mBoxType = reader.ReadByte();
            mBoundingBox = new float[6];
            for (var i = 0; i < 6; i++)
            {
                mBoundingBox[i] = reader.ReadSingle();
            }
            mUnknown = reader.ReadUInt32();
            if (mVersion <= 4)
            {
                mFlag = reader.ReadByte();
                if (mFlag == 1)
                {
                    mFTPTIndex = reader.ReadInt32();
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
    }
}
