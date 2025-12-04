using System;
using System.Collections.Generic;
using System.IO;

namespace Destrospean.CmarNYCBorrowed
{
    public class BGEO
    {
        char[] mMagic;

        Section1[] mSection1;

        int mSection1Count, mSection1HeaderSize, mSection1LODCount, mSection1LODSize, mSection1Offset, mSection2Count, mSection2Offset, mSection3Count, mSection3Offset, mVersion;

        Section2[] mSection2;

        Section3[] mSection3;

        public int Section1Count
        {
            get
            {
                return mSection1Count;
            }
        }

        public int Section1LODCount
        {
            get
            {
                return mSection1LODCount;
            }
        }

        public float Weight;

        [Serializable]
        public class BlendException : ApplicationException
        {
            public BlendException()
            {
            }

            public BlendException(string message) : base(message)
            {
            }

            public BlendException(string message, Exception inner) : base(message, inner)
            {
            }

            protected BlendException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
            {
            }
        }

        public class Section1
        {
            uint mAgeGenderSpecies, mRegion;

            int[] mEntryCount, mFirstVertexID, mOriginalEntryCount, mVertexIDCount;

            public uint AgeGenderSpecies
            {
                get
                {
                    var temp = mAgeGenderSpecies;
                    if ((temp & 0x0F00) == 0)
                    {
                        temp = temp | (uint)Species.Human;
                    }
                    return temp;
                }
            }

            public uint Region
            {
                get
                {
                    return mRegion;
                }
            }

            public Section1()
            {
            }

            public Section1(int age, int gender, int species, int region, int[] firstVertexID, int[] vertexIDCount, int[] entryCount)
            {
                mAgeGenderSpecies = (uint)(age + (gender << 12) + (species << 8)) + (1 << 16);
                mRegion = (uint)region;
                mFirstVertexID = firstVertexID;
                mVertexIDCount = vertexIDCount;
                mEntryCount = entryCount;
                mOriginalEntryCount = entryCount;
                if (firstVertexID.Length != 4 || vertexIDCount.Length != 4 || entryCount.Length != 4)
                {
                    throw new BlendException("Section 1 constructor: LOD information arrays must have four elements.");
                }
            }

            public Section1(BinaryReader reader, int section1LODCount)
            {
                mAgeGenderSpecies = reader.ReadUInt32();
                mRegion = reader.ReadUInt32();
                mFirstVertexID = new int[section1LODCount];
                mVertexIDCount = new int[section1LODCount];
                mEntryCount = new int[section1LODCount];
                mOriginalEntryCount = new int[section1LODCount];
                for (var i = 0; i < section1LODCount; i++)
                {
                    mFirstVertexID[i] = reader.ReadInt32();
                    mVertexIDCount[i] = reader.ReadInt32();
                    mEntryCount[i] = reader.ReadInt32();
                    mOriginalEntryCount[i] = mEntryCount[i];
                }
            }

            public int FixSection3Count(Section2[] section2, int section1LODCount, int Section2Index)
            {
                var index = Section2Index;
                for (var i = 0; i < section1LODCount; i++)
                {
                    var fixedCount = 0;
                    for (var j = index; j < index + mVertexIDCount[i]; j++)
                    {
                        if (section2[j].HasPosition)
                        {
                            fixedCount++;
                        }
                        if (section2[j].HasNormals)
                        {
                            fixedCount++;
                        }
                    }
                    mEntryCount[i] = Math.Max(fixedCount, mOriginalEntryCount[i]);
                    index += mVertexIDCount[i];
                }
                return index;
            }

            public int[] GetLODData(int lod)
            {
                return new int[]
                {
                    mFirstVertexID[lod],
                    mVertexIDCount[lod],
                    mEntryCount[lod],
                    mOriginalEntryCount[lod]
                };
            }

            public string GetLODDataAsString(int lod)
            {
                return mFirstVertexID[lod].ToString() + ", " + mVertexIDCount[lod].ToString() + ", " + mEntryCount[lod].ToString();
            }

            public override string ToString()
            {
                return ((Species)mAgeGenderSpecies).ToString() + ((AgeGender)mAgeGenderSpecies).ToString() + Environment.NewLine +
                    "LOD 0: First vertex " + mFirstVertexID[0].ToString() + ", Number vertices " + mVertexIDCount[0].ToString() + ", Number entries " + mEntryCount[0] + Environment.NewLine +
                    "LOD 1: First vertex " + mFirstVertexID[1].ToString() + ", Number vertices " + mVertexIDCount[1].ToString() + ", Number entries " + mEntryCount[1] + Environment.NewLine +
                    "LOD 2: First vertex " + mFirstVertexID[2].ToString() + ", Number vertices " + mVertexIDCount[2].ToString() + ", Number entries " + mEntryCount[2] + Environment.NewLine +
                    "LOD 3: First vertex " + mFirstVertexID[3].ToString() + ", Number vertices " + mVertexIDCount[3].ToString() + ", Number entries " + mEntryCount[3];
            }

            public void Write(BinaryWriter writer, int section1LODCount)
            {
                writer.Write(mAgeGenderSpecies);
                writer.Write(mRegion);
                for (var i = 0; i < section1LODCount; i++)
                {
                    writer.Write(mFirstVertexID[i]);
                    writer.Write(mVertexIDCount[i]);
                    writer.Write(mEntryCount[i]);
                }
            }
        }

        public class Section2
        {
            bool mHasNormals, mHasPosition;

            int mOffset;

            public bool HasNormals
            {
                get
                {
                    return mHasNormals;
                }
            }

            public bool HasPosition
            {
                get
                {
                    return mHasPosition;
                }
            }

            public int Offset
            {
                get
                {
                    return mOffset;
                }
            }

            public Section2()
            {
                mHasPosition = false;
                mHasNormals = false;
                mOffset = 0;
            }

            public Section2(bool hasPosition, bool hasNormals, int offset)
            {
                mHasPosition = hasPosition;
                mHasNormals = hasNormals;
                mOffset = offset;
            }

            public Section2(BinaryReader reader)
            {
                var temp = reader.ReadInt16();
                mHasPosition = (temp & 1) == 1;
                mHasNormals = (temp & 2) == 2;
                mOffset = temp >> 2;
            }

            public override string ToString()
            {
                return "Position: " + mHasPosition.ToString() + ", Normals: " + mHasNormals.ToString() + ", Offset: " + mOffset.ToString();
            }

            public void Write(BinaryWriter writer)
            {
                var temp = (short)(mOffset << 2);
                if (mHasPosition)
                {
                    temp += 1;
                }
                if (mHasNormals)
                {
                    temp += 2;
                }
                writer.Write(temp);
            }
        }

        public class Section3
        {
            float mX, mY, mZ;

            public float[] DeltaValues
            {
                get
                {
                    return new float[]
                    {
                        mX,
                        mY,
                        mZ
                    };
                }
            }

            public Section3(BinaryReader reader)
            {
                mX = ConvertIn(reader.ReadUInt16());
                mY = ConvertIn(reader.ReadUInt16());
                mZ = ConvertIn(reader.ReadUInt16());
            }

            public Section3(float x, float y, float z)
            {
                mX = x;
                mY = y;
                mZ = z;
            }

            public float ConvertIn(ushort encode)
            {
                return (((int)encode ^ 0x8000) << 16 >> 16) * .0005f;
            }

            public ushort ConvertOut(float decode)
            {
                return BitConverter.ToUInt16(BitConverter.GetBytes(Convert.ToInt32(decode * 2000) ^ 0x8000), 0);
            }

            public override string ToString()
            {
                return "X: " + mX.ToString() + ", Y: " + mY.ToString() + ", Z: " + mZ.ToString();
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(ConvertOut(mX));
                writer.Write(ConvertOut(mY));
                writer.Write(ConvertOut(mZ));
            }
        }

        public class VertexData
        {
            public Vector3 Normals, Position;

            public int VertexID;

            public VertexData(int id, Vector3 position, Vector3 normals)
            {
                VertexID = id;
                Position = new Vector3(position);
                Normals = new Vector3(normals);
            }
        }

        public BGEO()
        {
        }

        public BGEO(BinaryReader reader)
        {
            reader.BaseStream.Position = 0;
            mMagic = reader.ReadChars(4);
            if (new string(mMagic) != "BGEO")
            {
                throw new BlendException("Not a valid BGEO file.");
            }
            mVersion = reader.ReadInt32();
            if (mVersion != 768)
            {
                throw new BlendException("Not a recognized BGEO version.");
            }
            mSection1Count = reader.ReadInt32();
            mSection1LODCount = reader.ReadInt32();
            mSection2Count = reader.ReadInt32();
            mSection3Count = reader.ReadInt32();
            mSection1HeaderSize = reader.ReadInt32();
            mSection1LODSize = reader.ReadInt32();
            mSection1Offset = reader.ReadInt32();
            mSection2Offset = reader.ReadInt32();
            mSection3Offset = reader.ReadInt32();
            mSection1 = new Section1[mSection1Count];
            for (var i = 0; i < mSection1Count; i++)
            {
                mSection1[i] = new Section1(reader, mSection1LODCount);
            }
            mSection2 = new Section2[mSection2Count];
            for (var i = 0; i < mSection2Count; i++)
            {
                mSection2[i] = new Section2(reader);
            }
            mSection3 = new Section3[mSection3Count];
            for (var i = 0; i < mSection3Count; i++)
            {
                mSection3[i] = new Section3(reader);
            }
            var section2Index = 0;
            for (var i = 0; i < mSection1Count; i++)
            {
                section2Index = mSection1[i].FixSection3Count(mSection2, mSection1LODCount, section2Index);
            }
        }

        public List<VertexData> GetDeltas(int entry, int lod)
        {
            var vertexDeltas = new List<VertexData>();
            int runningOffset = GetLODInitialOffset(entry, lod),
            section2Count = GetSection2Count(entry, lod),
            section2Start = GetSection2StartIndex(entry, lod),
            section2StartVertexID = GetLODStartVertexID(entry, lod),
            section3Start = GetSection3StartIndex(entry, lod);
            for (var i = 0; i < section2Count; i++)
            {
                var section2 = GetSection2(section2Start + i);
                runningOffset += section2.Offset;
                Vector3 normal = new Vector3(),
                position = new Vector3();
                var tempIndex = runningOffset;
                if (section2.HasPosition)
                {
                    position = new Vector3(GetSection3(tempIndex + section3Start));
                    tempIndex++;
                }
                if (section2.HasNormals)
                {
                    normal = new Vector3(GetSection3(tempIndex + section3Start));
                }
                var vertexData = new VertexData(section2StartVertexID + i, position, normal);
                vertexDeltas.Add(vertexData);
            }
            return vertexDeltas;
        }

        public int GetLODInitialOffset(int section1EntryIndex, int lod)
        {
            if (section1EntryIndex == 0 && lod == 0)
            {
                return 0;
            }
            var offset = 0;
            for (var i = 0; i < section1EntryIndex; i++)
            {
                int count = GetSection2Count(i),
                start = GetSection2StartIndex(i, 0);
                for (var j = start; j < start + count; j++)
                {
                    offset += GetSection2(j).Offset;
                }
            }
            for (var i = 0; i < lod; i++)
            {
                int lodCount = GetSection2Count(section1EntryIndex, i),
                lodStart = GetSection2StartIndex(section1EntryIndex, i);
                for (var j = lodStart; j < lodStart + lodCount; j++)
                {
                    offset += GetSection2(j).Offset;
                }
            }
            return offset;
        }

        public int GetLODStartVertexID(int section1EntryIndex, int lod)
        {
            return mSection1[section1EntryIndex].GetLODData(lod)[0];
        }

        public Section1 GetSection1(int section1EntryIndex)
        {
            return mSection1[section1EntryIndex];
        }

        public int GetSection1EntryIndex(Species species, AgeGender age, AgeGender gender)
        {
            for (var i = 0; i < mSection1Count; i++)
            {
                if (((mSection1[i].AgeGenderSpecies & (uint)species) > 0) && ((mSection1[i].AgeGenderSpecies & (uint)age) > 0) && ((mSection1[i].AgeGenderSpecies & (uint)gender) > 0))
                {
                    return i;
                }
            }
            return -1;
        }

        public Section2 GetSection2(int section2Index)
        {
            return mSection2[section2Index];
        }

        public int GetSection2Count()
        {
            return mSection2Count;
        }

        public int GetSection2Count(int section1EntryIndex)
        {
            var section1 = GetSection1(section1EntryIndex);
            var temp = 0;
            for (var i = 0; i < mSection1LODCount; i++)
            {
                temp += section1.GetLODData(i)[1];
            }
            return temp;
        }

        public int GetSection2Count(int section1EntryIndex, int lod)
        {
            return GetSection1(section1EntryIndex).GetLODData(lod)[1];
        }

        public int GetSection2StartIndex(int section1EntryIndex, int lod)
        {
            var temp = 0;
            for (var i = 0; i < section1EntryIndex; i++)
            {
                for (var j = 0; j < Section1LODCount; j++)
                {
                    temp += mSection1[i].GetLODData(j)[1];
                }
            }
            for (var j = 0; j < lod; j++)
            {
                temp += mSection1[section1EntryIndex].GetLODData(j)[1];
            }
            return temp;
        }

        public int GetSection3Count()
        {
            return mSection3Count;
        }

        public int GetSection3Count(int section1EntryIndex)
        {
            var section1 = GetSection1(section1EntryIndex);
            var temp = 0;
            for (var i = 0; i < mSection1LODCount; i++)
            {
                temp += section1.GetLODData(i)[2];
            }
            return temp;
        }

        public float[] GetSection3(int section3Index)
        {
            return mSection3[section3Index].DeltaValues;
        }

        public int GetSection3Count(int section1EntryIndex, int lod)
        {
            return GetSection1(section1EntryIndex).GetLODData(lod)[2];
        }

        public int GetSection3StartIndex(int section1EntryIndex, int lod)
        {
            var temp = 0;
            for (var i = 0; i < section1EntryIndex; i++)
            {
                for (var j = 0; j < Section1LODCount; j++)
                {
                    temp += mSection1[i].GetLODData(j)[2];
                }
            }
            for (var j = 0; j < lod; j++)
            {
                temp += mSection1[section1EntryIndex].GetLODData(j)[2];
            }
            return temp;
        }

        public void WriteFile(BinaryWriter writer)
        {
            writer.Write(mMagic);
            writer.Write(mVersion);
            writer.Write(mSection1Count);
            writer.Write(mSection1LODCount);
            writer.Write(mSection2Count);
            writer.Write(mSection3Count);
            writer.Write(mSection1HeaderSize);
            writer.Write(mSection1LODSize);
            writer.Write(mSection1Offset);
            writer.Write(mSection2Offset);
            writer.Write(mSection3Offset);
            for (var i = 0; i < mSection1Count; i++)
            {
                mSection1[i].Write(writer, mSection1LODCount);
            }
            for (var i = 0; i < mSection2Count; i++)
            {
                mSection2[i].Write(writer);
            }
            for (var i = 0; i < mSection3Count; i++)
            {
                mSection3[i].Write(writer);
            }
        }
    }
}
