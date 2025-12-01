using System;
using System.IO;
using System.Collections.Generic;
using s3pi.GenericRCOLResource;

namespace Destrospean.CmarNYCBorrowed
{
    public class GEOM : IComparable<GEOM>
    {
        int mVersion1, mCount, mIndexCount, mExternalCount, mInternalCount;
        TGI mDummyTGI;
        int mAbsolutePosition, mMeshSize;
        char[] mMagic;
        int mVersion, mTGIOffset, mTGISize;
        uint mShaderHash;
        int mMTNFSize;
        MTNF mMeshMTNF;
        int mFaceCount, mMergeGroup, mSortOrder, mVertexCount;
        VertexFormat[] mVertexFormats = null;
        Position[] mPositions = null;
        Normal[] mNormals = null;
        UV[][] mUVs = null;
        Bones[] mBones = null;
        Tangent[] mTangents = null;
        TagValue[] mTags = null;
        int[] mVertexIDs = null;
        int mSubMeshCount;
        byte mBytesPerFacePoint;
        int mFacePointCount;
        Face[] mFaces = null;
        int mSKCONIndex;
        int mUVStitchCount;
        UVStitch[] mUVStitches = null;
        int mSeamStitchCount;
        SeamStitch[] mSeamStitches = null;
        int mSlotCount;
        SlotrayIntersection[] mSlotrayIntersections = null;
        int mBoneHashCount;
        uint[] mBoneHashArray = null;
        int mTGICount;
        TGI[] mTGIs = null;

        public bool CopyFaceMorphs = false;

        public static Vector3[][][][][] MeshSeamVertices = SetupSeamVertexPositions();

        public static List<int> FemaleWaistSequence = new List<int>
            {
                0x10,
                0x11,
                0x12,
                0x13,
                0x14,
                0x15,
                1,
                0xB,
                0xA,
                9,
                8,
                7,
                6,
                2,
                3,
                5,
                4,
                0,
                0xE,
                0xF,
                0xD,
                0xC
            };

        public static List<int> MaleWaistSequence = new List<int>
            {
                0x13,
                0x12,
                0x14,
                0x15,
                0x10,
                0x11,
                0xB,
                6,
                5,
                0xA,
                9,
                7,
                8,
                0,
                1,
                3,
                2,
                4,
                0xE,
                0xF,
                0xD,
                0xC
            };

        public Vector3[] DeltaPosition;

        public class VertexFormat
        {
            public int DataType, Subtype;
            public byte BytesPer;

            public int FormatDataType
            {
                get { return DataType; }
            }
            public int FormatSubType
            {
                get { return Subtype; }
            }
            public byte FormatDataLength
            {
                get { return BytesPer; }
            }

            public VertexFormat() { }

            public VertexFormat(VertexFormat source)
            {
                DataType = source.DataType;
                Subtype = source.Subtype;
                BytesPer = source.BytesPer;
            }

            public VertexFormat(int dataType, int subtype, byte bytesper)
            {
                DataType = dataType;
                Subtype = subtype;
                BytesPer = bytesper;
            }
            public VertexFormat(BinaryReader reader)
            {
                DataType = reader.ReadInt32();
                Subtype = reader.ReadInt32();
                BytesPer = reader.ReadByte();
            }
            public void vertexformatWrite(BinaryWriter bw)
            {
                bw.Write(DataType);
                bw.Write(Subtype);
                bw.Write(BytesPer);
            }
            public override string ToString()
            {
                return Enum.GetName(typeof(VertexFormatNames), DataType);
            }
        }

        public class Position
        {
            float mX, mY, mZ;

            public float[] Coordinates
            {
                get { return new float[3] { mX, mY, mZ }; }
            }
            public float X
            {
                get { return mX; }
                set { mX = value; }
            }
            public float Y
            {
                get { return mY; }
                set { mY = value; }
            }
            public float Z
            {
                get { return mZ; }
                set { mZ = value; }
            }

            public Position()
            {
                mX = 0f;
                mY = 0f;
                mZ = 0f;
            }

            public Position(Position source)
            {
                mX = source.mX;
                mY = source.mY;
                mZ = source.mZ;
            }

            public Position(float x, float y, float z)
            {
                mX = x;
                mY = y;
                mZ = z;
            }
            public Position(float[] newPosition)
            {
                mX = newPosition[0];
                mY = newPosition[1];
                mZ = newPosition[2];
            }
            public Position(BinaryReader br)
            {
                mX = br.ReadSingle();
                mY = br.ReadSingle();
                mZ = br.ReadSingle();
            }
            public void Write(BinaryWriter bw)
            {
                bw.Write(mX);
                bw.Write(mY);
                bw.Write(mZ);
            }
            public bool Equals(Position comparePosition)
            {
                return (IsEqual(mX, comparePosition.mX) && IsEqual(mY, comparePosition.mY) && IsEqual(mZ, comparePosition.mZ));
            }
            public override string ToString()
            {
                return mX.ToString() + ", " + mY.ToString() + ", " + mZ.ToString();
            }
            public float[] Data()
            {
                return new float[3] { mX, mY, mZ };
            }
            public void addDeltas(float[] deltas)
            {
                mX += deltas[0];
                mY += deltas[1];
                mZ += deltas[2];
            }
        }
        public class Normal
        {
            float mX, mY, mZ;

            public float[] Coordinates
            {
                get { return new float[3] { mX, mY, mZ }; }
            }
            public float X
            {
                get { return mX; }
                set { mX = value; }
            }
            public float Y
            {
                get { return mY; }
                set { mY = value; }
            }
            public float Z
            {
                get { return mZ; }
                set { mZ = value; }
            }

            public Normal()
            {
                mX = 0f;
                mY = 0f;
                mZ = 0f;
            }
            public Normal(Normal source)
            {
                mX = source.mX;
                mY = source.mY;
                mZ = source.mZ;
            }
            public Normal(float x, float y, float z)
            {
                mX = x;
                mY = y;
                mZ = z;
            }
            public Normal(float[] newNormal)
            {
                mX = newNormal[0];
                mY = newNormal[1];
                mZ = newNormal[2];
            }
            public Normal(BinaryReader br)
            {
                mX = br.ReadSingle();
                mY = br.ReadSingle();
                mZ = br.ReadSingle();
            }
            public void Write(BinaryWriter writer)
            {
                writer.Write(mX);
                writer.Write(mY);
                writer.Write(mZ);
            }
            public bool Equals(Normal compareNormal)
            {
                return (IsEqual(mX, compareNormal.mX) && IsEqual(mY, compareNormal.mY) && IsEqual(mZ, compareNormal.mZ));
            }
            public override string ToString()
            {
                return mX.ToString() + ", " + mY.ToString() + ", " + mZ.ToString();
            }
            public float[] Data()
            {
                return new float[3] { mX, mY, mZ };
            }
            public void addDeltas(float[] deltas)
            {
                mX += deltas[0];
                mY += deltas[1];
                mZ += deltas[2];
            }
        }

        public class UV
        {
            float mX, mY;

            public float U
            {
                get { return mX; }
                set { mX = value; }
            }
            public float V
            {
                get { return mY; }
                set { mY = value; }
            }

            public UV()
            {
            }

            public UV(UV source)
            {
                mX = source.mX;
                mY = source.mY;
            }
            public UV(float u, float v)
            {
                mX = u;
                mY = v;
            }
            public UV(float[] newUV)
            {
                mX = newUV[0];
                mY = newUV[1];
            }
            public UV(float[] newUV, bool verticalFlip)
            {
                mX = newUV[0];
                if (verticalFlip)
                {
                    mY = 1f - newUV[1];
                }
                else
                {
                    mY = newUV[1];
                }
            }
            public UV(BinaryReader br)
            {
                mX = br.ReadSingle();
                mY = br.ReadSingle();
            }
            public void Write(BinaryWriter bw)
            {
                bw.Write(mX);
                bw.Write(mY);
            }
            public bool Equals(UV compareUV)
            {
                return (IsEqual(mX, compareUV.mX) && IsEqual(mY, compareUV.mY));
            }
            public bool CloseTo(UV other)
            {
                const float diff = 0.001f;
                return
                    (
                        (Math.Abs(mX - other.mX) < diff) &&
                        (Math.Abs(mY - other.mY) < diff)
                    );
            }
            public override string ToString()
            {
                return mX.ToString() + ", " + mY.ToString();
            }
            public float[] Data()
            {
                return new float[2] { mX, mY };
            }
        }
        public class Bones
        {
            byte[] mAssignments = new byte[4];
            byte[] mWeights = new byte[4];

            public Bones()
            {
            }

            public Bones(Bones source)
            {
                for (int i = 0; i < 4; i++)
                {
                    mAssignments[i] = source.mAssignments[i];
                    mWeights[i] = source.mWeights[i];
                }
            }
            public Bones(byte[] assignmentsIn, float[] weightsIn)
            {
                for (int i = 0; i < 4; i++)
                {
                    mAssignments[i] = assignmentsIn[i];
                    mWeights[i] = (byte)(Math.Min(weightsIn[i] * 255f, 255f));
                }
            }
            public Bones(int[] assignmentsIn, float[] weightsIn)
            {
                for (int i = 0; i < 4; i++)
                {
                    mAssignments[i] = (byte)assignmentsIn[i];
                    mWeights[i] = (byte)(Math.Min(weightsIn[i] * 255f, 255f));
                }
            }
            public Bones(byte[] assignmentsIn, byte[] weightsIn)
            {
                for (int i = 0; i < 4; i++)
                {
                    mAssignments[i] = assignmentsIn[i];
                    mWeights[i] = weightsIn[i];
                    //weights[i] = (float)weightsIn[i] / 255f;
                }
            }
            public Bones(int[] assignmentsIn, byte[] weightsIn)
            {
                for (int i = 0; i < 4; i++)
                {
                    mAssignments[i] = (byte)assignmentsIn[i];
                    mWeights[i] = weightsIn[i];
                    //weights[i] = (float)weightsIn[i] / 255f;
                }
            }
            public void ReadAssignments(BinaryReader br)
            {
                for (int i = 0; i < 4; i++)
                {
                    mAssignments[i] = br.ReadByte();
                }
            }
            public void ReadWeights(BinaryReader br, int subtype)
            {
                if (subtype == 1)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float temp = br.ReadSingle();
                        mWeights[i] = (byte)(Math.Min(temp * 255f, 255f));
                    }
                }
                else if (subtype == 2)
                {
                    //for (int i = 2; i >= 0; i--)          // for CAS demo meshes only!
                    //{
                    //    byte temp = br.ReadByte();
                    //    weights[i] = (float)temp / 255f;
                    //}
                    //byte temp2 = br.ReadByte();
                    //weights[3] = (float)temp2 / 255f;
                    for (int i = 0; i < 4; i++)
                    {
                        mWeights[i] = br.ReadByte();
                        //weights[i] = (float)weightsNew[i] / 255f;
                    }
                }
            }
            public void WriteAssignments(BinaryWriter bw, int version, int maxBoneIndex)
            {
                if (version == 5)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (mWeights[i] > 0f)
                        {
                            bw.Write(mAssignments[i]);
                        }
                        else
                        {
                            bw.Write((byte)2);
                        }
                    }
                }
                else if (version >= 12)
                {
                    //byte temp = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        //if (weights[i] > 0)
                        //{
                        bw.Write(mAssignments[i]);
                        //}
                        //else
                        //{
                        //    bw.Write(temp);
                        //    temp = (byte)Math.Min(temp++, maxBoneIndex);
                        //}
                    }
                }

            }
            public void WriteWeights(BinaryWriter bw, int version)
            {
                if (version == 5)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float temp = (float)mWeights[i] / 255f;
                        bw.Write(temp);
                    }
                }
                else if (version >= 12)
                {
                    bw.Write(mWeights);
                }
            }
            public bool Equals(Bones compareBones)
            {
                return (mAssignments[0] == compareBones.mAssignments[0] && mAssignments[1] == compareBones.mAssignments[1] &&
                    mAssignments[2] == compareBones.mAssignments[2] && mAssignments[3] == compareBones.mAssignments[3] &&
                    mWeights[0] == compareBones.mWeights[0] && mWeights[1] == compareBones.mWeights[1] &&
                    mWeights[2] == compareBones.mWeights[2] && mWeights[3] == compareBones.mWeights[3]);
            }
            public override string ToString()
            {
                return mAssignments[0].ToString() + mAssignments[1].ToString() + mAssignments[2].ToString() + mAssignments[3].ToString() +
                    mWeights[0].ToString() + ", " + mWeights[1].ToString() + ", " + mWeights[2].ToString() + ", " + mWeights[3].ToString();
            }
            public byte[] BoneAssignments
            {
                get { return new byte[] { mAssignments[0], mAssignments[1], mAssignments[2], mAssignments[3] }; }
                set
                {
                    for (int i = 0; i < mAssignments.Length; i++)
                    {
                        mAssignments[i] = value[i];
                    }
                }
            }
            public float[] BoneWeightsV5
            {
                get { return new float[] { mWeights[0], mWeights[1], mWeights[2], mWeights[3] }; }
                set
                {
                    for (int i = 0; i < mAssignments.Length; i++)
                    {
                        //weights[i] = value[i];
                        mWeights[i] = (byte)(Math.Min(value[i] * 255f, 255f));
                    }
                }
            }
            public byte[] BoneWeights
            {
                get { return new byte[] { mWeights[0], mWeights[1], mWeights[2], mWeights[3] }; }
                set
                {
                    int tot = 0;
                    for (int i = 0; i < mAssignments.Length; i++)
                    {
                        mWeights[i] = value[i];
                        tot += value[i];
                        //weights[i] = (float)value[i] / 255f;
                    }
                    mWeights[0] += (byte)(255 - tot);
                }
            }
            public void Sort(int version)
            {
                for (int i = mAssignments.Length - 2; i >= 0; i--)
                {
                    for (int j = 0; j <= i; j++)
                    {
                        if (mWeights[j] < mWeights[j + 1])
                        {
                            byte tb = mAssignments[j];
                            mAssignments[j] = mAssignments[j + 1];
                            mAssignments[j + 1] = tb;
                            byte tw = mWeights[j];
                            mWeights[j] = mWeights[j + 1];
                            mWeights[j + 1] = tw;
                        }
                    }
                }
            }
        }

        public class Tangent
        {
            float mX, mY, mZ;
            public Tangent()
            {
            }

            public float X
            {
                get { return mX; }
                set { mX = value; }
            }
            public float Y
            {
                get { return mY; }
                set { mY = value; }
            }
            public float Z
            {
                get { return mZ; }
                set { mZ = value; }
            }
            public Tangent(Tangent source)
            {
                mX = source.mX;
                mY = source.mY;
                mZ = source.mZ;
            }
            public Tangent(float x, float y, float z)
            {
                mX = x;
                mY = y;
                mZ = z;
            }
            public Tangent(float[] newTangent)
            {
                mX = newTangent[0];
                mY = newTangent[1];
                mZ = newTangent[2];
            }
            public Tangent(BinaryReader reader)
            {
                mX = reader.ReadSingle();
                mY = reader.ReadSingle();
                mZ = reader.ReadSingle();
            }
            public void Write(BinaryWriter writer)
            {
                writer.Write(mX);
                writer.Write(mY);
                writer.Write(mZ);
            }
            public bool Equals(Tangent compareTangent)
            {
                return (IsEqual(mX, compareTangent.mX) && IsEqual(mY, compareTangent.mY) && IsEqual(mZ, compareTangent.mZ));
            }
            public override string ToString()
            {
                return mX.ToString() + ", " + mY.ToString() + ", " + mZ.ToString();
            }
            public float[] Data()
            {
                return new float[3] { mX, mY, mZ };
            }
        }
        public class TagValue
        {
            uint mTags;
            public TagValue() { }
            public TagValue(TagValue source)
            {
                mTags = source.mTags;
            }
            public TagValue(uint tagValue)
            {
                mTags = tagValue;
            }
            public TagValue(BinaryReader br)
            {
                mTags = br.ReadUInt32();
            }
            public void Write(BinaryWriter bw)
            {
                bw.Write(mTags);
            }
            public bool Equals(TagValue compareTagval)
            {
                return (mTags == compareTagval.mTags);
            }
            public override string ToString()
            {
                return Convert.ToString(mTags, 16).ToUpper().PadLeft(8, '0');
            }
            public uint Data()
            {
                return mTags;
            }
        }

        public class Face
        {
            uint[] mFace = new uint[3];
            public int FacePoint0
            {
                get { return (int)mFace[0]; }
            }
            public int FacePoint1
            {
                get { return (int)mFace[1]; }
            }

            public int FacePoint2
            {
                get { return (int)mFace[2]; }
            }

            public Face()
            {
            }

            public Face(Face source)
            {
                for (int i = 0; i < 3; i++)
                {
                    mFace[i] = source.mFace[i];
                }
            }
            public uint[] MeshFace
            {
                get
                {
                    return new uint[] { mFace[0], mFace[1], mFace[2] };
                }
            }
            public Face(byte[] face)
            {
                for (int i = 0; i < 3; i++)
                {
                    mFace[i] = (uint)face[i];
                }
            }
            public Face(ushort[] face)
            {
                for (int i = 0; i < 3; i++)
                {
                    mFace[i] = (uint)face[i];
                }
            }
            public Face(int[] face)
            {
                for (int i = 0; i < 3; i++)
                {
                    mFace[i] = (uint)face[i];
                }
            }
            public Face(uint[] face)
            {
                for (int i = 0; i < 3; i++)
                {
                    mFace[i] = face[i];
                }
            }
            public Face(int FacePoint0, int FacePoint1, int FacePoint2)
            {
                mFace[0] = (uint)FacePoint0;
                mFace[1] = (uint)FacePoint1;
                mFace[2] = (uint)FacePoint2;
            }
            public Face(uint FacePoint0, uint FacePoint1, uint FacePoint2)
            {
                mFace[0] = FacePoint0;
                mFace[1] = FacePoint1;
                mFace[2] = FacePoint2;
            }
            public Face(short FacePoint0, short FacePoint1, short FacePoint2)
            {
                mFace[0] = (uint)FacePoint0;
                mFace[1] = (uint)FacePoint1;
                mFace[2] = (uint)FacePoint2;
            }

            public Face(BinaryReader br, byte bytesperfacepnt)
            {
                for (int i = 0; i < 3; i++)
                {
                    switch (bytesperfacepnt)
                    {
                        case (1):
                            mFace[i] = br.ReadByte();
                            break;
                        case (2):
                            mFace[i] = br.ReadUInt16();
                            break;
                        case (4):
                            mFace[i] = br.ReadUInt32();
                            break;
                        default:
                            break;
                    }

                }
            }
            public void Write(BinaryWriter bw, byte bytesperfacepnt)
            {
                for (int i = 0; i < 3; i++)
                {
                    switch (bytesperfacepnt)
                    {
                        case (1):
                            bw.Write((byte)mFace[i]);
                            break;
                        case (2):
                            bw.Write((ushort)mFace[i]);
                            break;
                        case (4):
                            bw.Write(mFace[i]);
                            break;
                        default:
                            break;
                    }
                }
            }
            public static Face Reverse(Face source)
            {
                return new Face(source.FacePoint2, source.FacePoint1, source.FacePoint0);
            }
            public void Reverse()
            {
                uint temp = mFace[0];
                mFace[0] = mFace[2];
                mFace[2] = temp;
            }
            public bool Equals(Face f)
            {
                return ((mFace[0] == f.mFace[0]) && (mFace[1] == f.mFace[1]) && (mFace[2] == f.mFace[2]));
            }
            public override string ToString()
            {
                return mFace[0].ToString() + ", " + mFace[1].ToString() + ", " + mFace[2].ToString();
            }
        }

        public class UVStitch
        {
            int mVertexIndex;
            int mCount;
            float[][] mCoordinates;

            public int Index
            {
                get { return mVertexIndex; }
                set { mVertexIndex = value; }
            }
            public int Count
            {
                get { return mCount; }
            }
            public List<float[]> UV1Coordinates
            {
                get
                {
                    List<float[]> pairs = new List<float[]>();
                    for (int i = 0; i < mCoordinates.GetLength(0); i++)
                    {
                        pairs.Add(mCoordinates[i]);
                    }
                    return pairs;
                }
            }
            public List<Vector2> UV1Vectors
            {
                get
                {
                    List<Vector2> pairs = new List<Vector2>();
                    for (int i = 0; i < mCoordinates.GetLength(0); i++)
                    {
                        pairs.Add(new Vector2(mCoordinates[i]));
                    }
                    return pairs;
                }
            }
            public int Size
            {
                get { return 8 + (mCount * 8); }
            }

            public UVStitch(BinaryReader br)
            {
                mVertexIndex = br.ReadInt32();
                mCount = br.ReadInt32();
                mCoordinates = new float[mCount][];
                for (int i = 0; i < mCount; i++)
                {
                    mCoordinates[i] = new float[2];
                    mCoordinates[i][0] = br.ReadSingle();
                    mCoordinates[i][1] = br.ReadSingle();
                }
            }

            public UVStitch(UVStitch adjustment)
            {
                mVertexIndex = adjustment.mVertexIndex;
                mCount = adjustment.mCount;
                mCoordinates = new float[adjustment.mCount][];
                for (int i = 0; i < mCount; i++)
                {
                    mCoordinates[i] = new float[2];
                    mCoordinates[i][0] = adjustment.mCoordinates[i][0];
                    mCoordinates[i][1] = adjustment.mCoordinates[i][1];
                }
            }

            public UVStitch(int vertexIndex, Vector2[] uv1Coordinates)
            {
                mVertexIndex = vertexIndex;
                mCount = uv1Coordinates.Length;
                mCoordinates = new float[uv1Coordinates.Length][];
                for (int i = 0; i < uv1Coordinates.Length; i++)
                {
                    mCoordinates[i] = new float[2] { uv1Coordinates[i].X, uv1Coordinates[i].Y };
                }
            }

            public void Write(BinaryWriter bw)
            {
                bw.Write(mVertexIndex);
                bw.Write(mCount);
                for (int i = 0; i < mCount; i++)
                {
                    bw.Write(mCoordinates[i][0]);
                    bw.Write(mCoordinates[i][1]);
                }
            }
        }

        public class SeamStitch : IComparable, IEquatable<SeamStitch>
        {
            uint mIndex;

            ushort mVertexID;

            public float UVX;

            public int WaistSequence;

            public uint Index
            {
                get
                {
                    return mIndex;
                }
                set
                {
                    mIndex = value;
                }
            }

            public ushort VertexID
            {
                get
                {
                    return mVertexID;
                }
                set
                {
                    mVertexID = value;
                }
            }

            public int SeamType
            {
                get
                {
                    return mVertexID >> 12;
                }
            }

            public int SeamIndex
            {
                get
                {
                    return mVertexID & 0x0FFF;
                }
            }

            public int Size
            {
                get
                {
                    return 6;
                }
            }

            public SeamStitch(BinaryReader reader, UV[] uvs)
            {
                mIndex = reader.ReadUInt32();
                mVertexID = reader.ReadUInt16();
                if (SeamType == (int)GEOM.SeamType.WaistAdultFemale)
                {
                    WaistSequence = FemaleWaistSequence.IndexOf(SeamIndex);
                }
                else if (SeamType == (int)GEOM.SeamType.WaistAdultMale)
                {
                    WaistSequence = MaleWaistSequence.IndexOf(SeamIndex);
                }
                UVX = uvs[mIndex].U;
            }

            public SeamStitch(SeamStitch adjustment)
            {
                mIndex = adjustment.mIndex;
                mVertexID = adjustment.mVertexID;
                WaistSequence = adjustment.WaistSequence;
                UVX = adjustment.UVX;
            }

            public SeamStitch(int index, GEOM.SeamType seam, int vertex, UV[] uvs)
            {
                mIndex = (uint)index;
                mVertexID = (ushort)(((int)seam << 12) + vertex);
                if (SeamType == (int)GEOM.SeamType.WaistAdultFemale)
                {
                    WaistSequence = FemaleWaistSequence.IndexOf(SeamIndex);
                }
                else if (SeamType == (int)GEOM.SeamType.WaistAdultMale)
                {
                    WaistSequence = MaleWaistSequence.IndexOf(SeamIndex);
                }
                UVX = uvs[index].U;
            }

            public SeamStitch(int index, int seam, int vertex, UV[] uvs)
            {
                mIndex = (uint)index;
                mVertexID = (ushort)((seam << 12) + vertex);
                if (SeamType == (int)GEOM.SeamType.WaistAdultFemale)
                {
                    WaistSequence = FemaleWaistSequence.IndexOf(SeamIndex);
                }
                else if (SeamType == (int)GEOM.SeamType.WaistAdultMale)
                {
                    WaistSequence = MaleWaistSequence.IndexOf(SeamIndex);
                }
                UVX = uvs[index].U;
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(mIndex);
                writer.Write(mVertexID);
            }

            public override bool Equals(object obj)
            {
                if (obj is SeamStitch)
                {
                    return Equals(obj as SeamStitch);
                }
                else return false;
            }

            public bool Equals(SeamStitch ss)
            {
                return mVertexID == ss.mVertexID & UVX == ss.UVX;
            }

            public int CompareTo(object obj)
            {
                if (obj is SeamStitch)
                {
                    var seamStitch = obj as SeamStitch;
                    if (WaistSequence.Equals(seamStitch.WaistSequence))
                    {
                        return UVX.CompareTo(seamStitch.UVX);
                    }
                    else return WaistSequence.CompareTo(seamStitch.WaistSequence);
                }
                else return 0;
            }

            public override int GetHashCode()
            {
                return (mVertexID + UVX).GetHashCode();
            }
        }

        public class SlotrayIntersection
        {
            uint mSlotBone;
            short[] mVertexIndices;        // short[3] indices of vertices making up face
            float[] mCoordinates;        // float[2] Barycentric coordinates of the point of intersection
            float mDistance;             // distance from raycast origin to the intersection point
            float[] mOffsetFromIntersectionOS;  // Vector3 offset from the intersection point to the slot's average position (if outside geometry) in object space
            float[] mSlotAveragePosOS;   // Vector3 slot's average position in object space
            float[] mTransformToLS;     // Quaternion transform from object space to the slot's local space
            uint mPivotBone;             // index or hash of the bone that this slot pivots around, 0xFF or 0x00000000 if pivot does not exist
            int mParentVersion;

            public uint SlotIndex
            {
                get { return mSlotBone; }
                set { mSlotBone = value; }
            }
            public int[] TrianglePointIndices
            {
                get { return new int[] { mVertexIndices[0], mVertexIndices[1], mVertexIndices[2] }; }
                set { mVertexIndices[0] = (short)value[0]; mVertexIndices[1] = (short)value[1]; mVertexIndices[2] = (short)value[2]; }
            }
            public Vector2 Coordinates
            {
                get { return new Vector2(mCoordinates); }
                set { mCoordinates = value.Coordinates; }
            }
            public float Distance
            {
                get { return mDistance; }
                set { mDistance = value; }
            }
            public Vector3 OffsetFromIntersectionOS
            {
                get { return new Vector3(mOffsetFromIntersectionOS); }
                set { mOffsetFromIntersectionOS = value.Coordinates; }
            }
            public Vector3 SlotAveragePosOS
            {
                get { return new Vector3(mSlotAveragePosOS); }
                set { mSlotAveragePosOS = value.Coordinates; }
            }
            public Quaternion TransformToLS
            {
                get { return new Quaternion(mTransformToLS); }
                set { mTransformToLS = new float[] { (float)value.X, (float)value.Y, (float)value.Z, (float)value.W }; }
            }
            public uint PivotBone
            {
                get { return mPivotBone; }
                set { mPivotBone = value; }
            }

            public SlotrayIntersection(BinaryReader reader, int version)
            {
                mParentVersion = version;
                mSlotBone = reader.ReadUInt32();
                mVertexIndices = new short[3];
                for (int i = 0; i < 3; i++)
                {
                    mVertexIndices[i] = reader.ReadInt16();
                }
                mCoordinates = new float[2];
                for (int i = 0; i < 2; i++)
                {
                    mCoordinates[i] = reader.ReadSingle();
                }
                mDistance = reader.ReadSingle();
                mOffsetFromIntersectionOS = new float[3];
                for (int i = 0; i < 3; i++)
                {
                    mOffsetFromIntersectionOS[i] = reader.ReadSingle();
                }
                mSlotAveragePosOS = new float[3];
                for (int i = 0; i < 3; i++)
                {
                    mSlotAveragePosOS[i] = reader.ReadSingle();
                }
                mTransformToLS = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    mTransformToLS[i] = reader.ReadSingle();
                }
                if (mParentVersion >= 14)
                {
                    mPivotBone = reader.ReadUInt32();
                }
                else
                {
                    mPivotBone = reader.ReadByte();
                }
            }

            public SlotrayIntersection(SlotrayIntersection faceAdjustment)
            {
                mSlotBone = faceAdjustment.mSlotBone;
                mVertexIndices = new short[3];
                for (var i = 0; i < 3; i++)
                {
                    mVertexIndices[i] = faceAdjustment.mVertexIndices[i];
                }
                mCoordinates = new float[2];
                for (var i = 0; i < 2; i++)
                {
                    mCoordinates[i] = faceAdjustment.mCoordinates[i];
                }
                mDistance = faceAdjustment.mDistance;
                mOffsetFromIntersectionOS = new float[3];
                for (var i = 0; i < 3; i++)
                {
                    mOffsetFromIntersectionOS[i] = faceAdjustment.mOffsetFromIntersectionOS[i];
                }
                mSlotAveragePosOS = new float[3];
                for (var i = 0; i < 3; i++)
                {
                    mSlotAveragePosOS[i] = faceAdjustment.mSlotAveragePosOS[i];
                }
                mTransformToLS = new float[4];
                for (var i = 0; i < 4; i++)
                {
                    mTransformToLS[i] = faceAdjustment.mTransformToLS[i];
                }
                mPivotBone = faceAdjustment.mPivotBone;
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(mSlotBone);
                for (var i = 0; i < mVertexIndices.Length; i++)
                {
                    writer.Write(mVertexIndices[i]);
                }
                for (var i = 0; i < mCoordinates.Length; i++)
                {
                    writer.Write(mCoordinates[i]);
                }
                writer.Write(mDistance);
                for (var i = 0; i < mOffsetFromIntersectionOS.Length; i++)
                {
                    writer.Write(mOffsetFromIntersectionOS[i]);
                }
                for (var i = 0; i < mSlotAveragePosOS.Length; i++)
                {
                    writer.Write(mSlotAveragePosOS[i]);
                }
                for (var i = 0; i < mTransformToLS.Length; i++)
                {
                    writer.Write(mTransformToLS[i]);
                }
                if (mParentVersion >= 14)
                {
                    writer.Write(mPivotBone);
                }
                else
                {
                    writer.Write((byte)mPivotBone);
                }
            }
        }

        public class MTNF
        {
            char[] mMagic;
            int mZero, mDataSize, mParamCount;
            uint[][] mParamList;
            object[][] mDataList;

            public MTNF()
            {
            }

            public int ChunkSize
            {
                get { return 16 + (mParamCount * 16) + mDataSize; }
            }

            public uint[] GetParamsList()
            {
                uint[] temp = new uint[mParamCount];
                for (int i = 0; i < mParamCount; i++)
                {
                    temp[i] = mParamList[i][0];
                }
                return temp;
            }

            public object[] GetParamValue(uint parameter, out int valueType)
            {
                object[] temp = null;
                for (int i = 0; i < mParamCount; i++)
                {
                    if (mParamList[i][0] == parameter)
                    {
                        temp = new object[mParamList[i][2]];
                        for (int j = 0; j < temp.Length; j++)
                        {
                            temp[j] = mDataList[i][j];
                        }
                        valueType = (int)mParamList[i][1];
                        return temp;
                    }
                }
                valueType = 0;
                return null;
            }

            public MTNF(MTNF source)
            {
                mMagic = source.mMagic;
                mZero = source.mZero;
                mDataSize = source.mDataSize;
                mParamCount = source.mParamCount;
                mParamList = new uint[source.mParamList.Length][];
                for (int i = 0; i < source.mParamList.Length; i++)
                {
                    mParamList[i] = new uint[source.mParamList[i].Length];
                    for (int j = 0; j < source.mParamList[i].Length; j++)
                    {
                        mParamList[i][j] = source.mParamList[i][j];
                    }
                }
                mDataList = new object[source.mDataList.Length][];
                for (int i = 0; i < source.mDataList.Length; i++)
                {
                    mDataList[i] = new object[source.mDataList[i].Length];
                    for (int j = 0; j < source.mDataList[i].Length; j++)
                    {
                        mDataList[i][j] = source.mDataList[i][j];
                    }
                }
            }

            public MTNF(BinaryReader reader)
            {
                mMagic = reader.ReadChars(4);
                mZero = reader.ReadInt32();
                mDataSize = reader.ReadInt32();
                mParamCount = reader.ReadInt32();
                mParamList = new uint[mParamCount][];
                for (int i = 0; i < mParamCount; i++)
                {
                    mParamList[i] = new uint[4];
                    for (int j = 0; j < 4; j++)
                    {
                        mParamList[i][j] = reader.ReadUInt32();
                    }
                }
                mDataList = new object[mParamCount][];
                for (int i = 0; i < mParamCount; i++)
                {
                    mDataList[i] = new object[mParamList[i][2]];
                    if (mParamList[i][1] == 1)
                    {
                        for (int j = 0; j < mParamList[i][2]; j++)
                        {
                            mDataList[i][j] = reader.ReadSingle();
                        }
                    }
                    else if (mParamList[i][1] == 2)
                    {
                        for (int j = 0; j < mParamList[i][2]; j++)
                        {
                            mDataList[i][j] = reader.ReadInt32();
                        }
                    }
                    else
                    {
                        for (int j = 0; j < mParamList[i][2]; j++)
                        {
                            mDataList[i][j] = reader.ReadUInt32();
                        }
                    }
                }
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(mMagic);
                writer.Write(mZero);
                writer.Write(mDataSize);
                writer.Write(mParamCount);
                for (int i = 0; i < mParamCount; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        writer.Write(mParamList[i][j]);
                    }
                }
                for (int i = 0; i < mParamCount; i++)
                {
                    if (mParamList[i][1] == 1)
                    {
                        for (int j = 0; j < mParamList[i][2]; j++)
                        {
                            writer.Write((float)mDataList[i][j]);
                        }
                    }
                    else if (mParamList[i][1] == 2)
                    {
                        for (int j = 0; j < mParamList[i][2]; j++)
                        {
                            writer.Write((int)mDataList[i][j]);
                        }
                    }
                    else
                    {
                        for (int j = 0; j < mParamList[i][2]; j++)
                        {
                            writer.Write((uint)mDataList[i][j]);
                        }
                    }
                }
            }

            public int NormalIndex
            {
                get
                {
                    for (int i = 0; i < mParamCount; i++)
                    {
                        if (mParamList[i][0] == (uint)FieldType.NormalMap)
                        {
                            uint temp = (uint)mDataList[i][0];
                            return (int)temp;
                        }
                    }
                    return -1;
                }
                set
                {
                    for (int i = 0; i < mParamCount; i++)
                    {
                        if (mParamList[i][0] == (uint)FieldType.NormalMap)
                        {
                            mDataList[i][0] = (uint)value;
                        }
                    }

                }
            }

            public int EmissionIndex
            {
                get
                {
                    for (int i = 0; i < mParamCount; i++)
                    {
                        if (mParamList[i][0] == (uint)FieldType.EmissionMap)
                        {
                            uint temp = (uint)mDataList[i][0];
                            return (int)temp;
                        }
                    }
                    return -1;
                }
                set
                {
                    for (int i = 0; i < mParamCount; i++)
                    {
                        if (mParamList[i][0] == (uint)FieldType.EmissionMap)
                        {
                            mDataList[i][0] = (uint)value;
                        }
                    }

                }
            }

            public MTNF(uint[] shaderDataArray)
            {
                mMagic = System.Text.Encoding.UTF8.GetString(BitConverter.GetBytes(shaderDataArray[0])).ToCharArray();
                mZero = (int)shaderDataArray[1];
                mDataSize = (int)shaderDataArray[2];
                mParamCount = (int)shaderDataArray[3];
                mParamList = new uint[mParamCount][];
                mDataList = new object[mParamCount][];
                int ind = 4;
                for (int i = 0; i < mParamCount; i++)
                {
                    mParamList[i] = new uint[4];
                    for (int j = 0; j < 4; j++)
                    {
                        mParamList[i][j] = shaderDataArray[ind];
                        ind++;
                    }
                }
                for (int i = 0; i < mParamCount; i++)
                {
                    mDataList[i] = new object[mParamList[i][2]];
                    if (mParamList[i][1] == 1)
                    {
                        for (int j = 0; j < mParamList[i][2]; j++)
                        {
                            byte[] b = BitConverter.GetBytes(shaderDataArray[ind]);
                            mDataList[i][j] = BitConverter.ToSingle(b, 0);
                            ind++;
                        }
                    }
                    else if (mParamList[i][1] == 2)
                    {
                        for (int j = 0; j < mParamList[i][2]; j++)
                        {
                            mDataList[i][j] = (int)shaderDataArray[ind];
                            ind++;
                        }
                    }
                    else
                    {
                        for (int j = 0; j < mParamList[i][2]; j++)
                        {
                            mDataList[i][j] = shaderDataArray[ind];
                            ind++;
                        }
                    }
                }
            }

            public uint[] ToDataArray()
            {
                List<uint> temp = new List<uint>();
                temp.Add(BitConverter.ToUInt32(System.Text.Encoding.UTF8.GetBytes(mMagic), 0));
                temp.Add((uint)mZero);
                temp.Add((uint)mDataSize);
                temp.Add((uint)mParamCount);
                for (int i = 0; i < mParamCount; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        temp.Add(mParamList[i][j]);
                    }
                }
                for (int i = 0; i < mParamCount; i++)
                {
                    if (mParamList[i][1] == 1)
                    {
                        for (int j = 0; j < mParamList[i][2]; j++)
                        {
                            byte[] b = BitConverter.GetBytes((float)mDataList[i][j]);
                            temp.Add(BitConverter.ToUInt32(b, 0));
                        }
                    }
                    else if (mParamList[i][1] == 2)
                    {
                        for (int j = 0; j < mParamList[i][2]; j++)
                        {
                            int t = (int)mDataList[i][j];
                            temp.Add((uint)t);
                        }
                    }
                    else
                    {
                        for (int j = 0; j < mParamList[i][2]; j++)
                        {
                            temp.Add((uint)mDataList[i][j]);
                        }
                    }
                }
                return temp.ToArray();
            }

        }

        public class VertexData : IComparable<VertexData>
        {
            public Bones Bones;

            public uint ID;

            public ushort Index;

            public Normal Normal;

            public Position Position;

            public TagValue TagValue;

            public UV UV;

            public VertexData(ushort index, Position position, Normal norm, UV uv, Bones bones, TagValue color, uint vertexID)
            {
                ID = vertexID;
                Bones = bones;
                Index = index;
                Normal = norm;
                Position = position;
                TagValue = color;
                UV = uv;
            }

            public int CompareTo(VertexData other)
            {
                if (other == null)
                {
                    throw new ArgumentException("VertexData is null!");
                }
                return Index.CompareTo(other.Index);
            }

            public bool Equals(VertexData other)
            {
                return (Position.Equals(other.Position) && Normal.Equals(other.Normal) && UV.Equals(other.UV));
            }
        }

        [Serializable]
        public class MeshException : ApplicationException
        {
            public MeshException() { }
            public MeshException(string message) : base(message) { }
            public MeshException(string message, Exception inner) : base(message, inner) { }
            protected MeshException(
                System.Runtime.Serialization.SerializationInfo info,
                System.Runtime.Serialization.StreamingContext context)
                : base(info, context) { }
        }

        public enum VertexFormatNames
        {
            Position = 1,
            Normals,
            UV,
            BoneAssignment,
            BoneWeight,
            Tangents,
            TagVal,
            VertexID = 10
        }

        public enum SeamType
        {
            Ankles,
            Neck = 3,
            Waist,
            WaistAdultFemale,
            WaistAdultMale
        }

        public bool UpdateToLatestVersion()
        {
            if (mVersion < 13)
            {
                SetVersion(13);
                return true;
            }
            return false;
        }

        public void SetVersion(int newVersion)
        {
            if (newVersion == 5 && mVersion > 5)
            {
                for (int i = 0; i < mFaceCount; i++)
                {
                    if (mVertexFormats[i].DataType == 5)
                    {
                        mVertexFormats[i].Subtype = 1;
                        mVertexFormats[i].BytesPer = 16;
                    }
                }
            }
            if (newVersion >= 12 & mVersion == 5)
            {
                for (int i = 0; i < mFaceCount; i++)
                {
                    if (mVertexFormats[i].DataType == 5)
                    {
                        mVertexFormats[i].Subtype = 2;
                        mVertexFormats[i].BytesPer = 4;
                    }
                }
            }
            mVersion = newVersion;
        }

        public int Version
        {
            get
            {
                return mVersion;
            }
        }

        public int VertexCount
        {
            get
            {
                return mVertexCount;
            }
        }

        public int MergeGroup
        {
            get
            {
                return mMergeGroup;
            }
        }

        public int SortOrder
        {
            get
            {
                return mSortOrder;
            }
        }

        public int SubMeshCount
        {
            get
            {
                return mSubMeshCount;
            }
        }

        public int BytesPerFacePoint
        {
            get
            {
                return mBytesPerFacePoint;
            }
        }

        public UVStitch[] UVStitches
        {
            get
            {
                return mUVStitches;
            }
            set
            {
                if (value != null)
                {
                    mUVStitches = value;
                    mUVStitchCount = mUVStitches.Length;
                }
                else
                {
                    mUVStitches = new UVStitch[0];
                    mUVStitchCount = 0;
                }
            }
        }

        public int UVStitchesSize
        {
            get
            {
                var temp = 0;
                foreach (var uvStitch in mUVStitches)
                {
                    temp += uvStitch.Size;
                }
                return temp;
            }
        }

        public SeamStitch[] SeamStitches
        {
            get
            {
                return mSeamStitches;
            }
            set
            {
                if (value != null)
                {
                    mSeamStitches = value;
                    mSeamStitchCount = mSeamStitches.Length;
                }
                else
                {
                    mSeamStitches = new SeamStitch[0];
                    mSeamStitchCount = 0;
                }
            }
        }

        public SlotrayIntersection[] SlotrayAdjustments
        {
            get
            {
                return mSlotrayIntersections;
            }
            set
            {
                if (value != null)
                {
                    mSlotrayIntersections = value;
                    mSlotCount = mSlotrayIntersections.Length;
                }
                else
                {
                    mSlotrayIntersections = new SlotrayIntersection[0];
                    mSlotCount = 0;
                }
            }
        }

        public Vector3[] SlotrayTrianglePositions(int slotrayAdjustmentIndex)
        {
            var vertexIndices = SlotrayAdjustments[slotrayAdjustmentIndex].TrianglePointIndices;
            return new Vector3[]
            {
                new Vector3(mPositions[vertexIndices[0]].Coordinates), 
                new Vector3(mPositions[vertexIndices[1]].Coordinates),
                new Vector3(mPositions[vertexIndices[2]].Coordinates)
            };
        }

        public int SlotrayAdjustmentsSize
        {
            get
            {
                return mSlotrayIntersections.Length * 63;
            }
        }

        public bool IsValid
        {
            get
            {
                var isValid = false;
                if (new string(mMagic) == "GEOM" && mVertexCount > 0 && (mVersion == 5 || mVersion == 12 || mVersion == 13 || mVersion == 14) && mFaceCount > 2)
                {
                    isValid = true;
                    var uvIndex = 0;
                    for (var i = 0; i < VertexFormatList.Length; i++)
                    {
                        switch (VertexFormatList[i])
                        {
                            case 1:
                                if (mPositions.Length != mVertexCount)
                                {
                                    isValid = false;
                                }
                                break;
                            case 2:
                                if (mNormals.Length != mVertexCount)
                                {
                                    isValid = false;
                                }
                                break;
                            case 3:
                                if (mUVs[uvIndex].Length != mVertexCount)
                                {
                                    isValid = false;
                                }
                                uvIndex += 1;
                                break;
                            case 4:
                                if (mBones.Length != mVertexCount)
                                {
                                    isValid = false;
                                }
                                break;
                            case 5:
                                if (mBones.Length != mVertexCount)
                                {
                                    isValid = false;
                                }
                                break;
                            case 6:
                                if (mTangents.Length != mVertexCount)
                                {
                                    isValid = false;
                                }
                                break;
                            case 7:
                                if (mTags.Length != mVertexCount)
                                {
                                    isValid = false;
                                }
                                break;
                            case 10:
                                if (mVertexIDs.Length != mVertexCount)
                                {
                                    isValid = false;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                return isValid;
            }
        }

        public bool IsBase
        {
            get
            {
                return mFaceCount > 3;
            }
        }

        public bool IsMorph
        {
            get
            {
                return mFaceCount == 3 && mVertexFormats[0].DataType == 1 && mVertexFormats[0].Subtype == 1 && mVertexFormats[0].BytesPer == 12 && mVertexFormats[1].DataType == 2 && mVertexFormats[1].Subtype == 1 && mVertexFormats[1].BytesPer == 12 && mVertexFormats[2].DataType == 10 && mVertexFormats[2].Subtype == 4 && mVertexFormats[2].BytesPer == 4;
            }
        }

        public bool HasVertexIDs
        {
            get
            {
                if (Array.IndexOf(VertexFormatList, 10) > -1 && mVertexIDs.Length > 0)
                {
                    for (var i = 0; i < mVertexCount; i++)
                    {
                        if (mVertexIDs[i] > 0)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public bool VertexIDsAreSequential
        {
            get
            {
                for (var i = 0; i < mVertexCount; i++)
                {
                    if (mVertexIDs[i] != i)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool HasPositions
        {
            get
            {
                return Array.IndexOf(VertexFormatList, 1) > -1 && mPositions.Length > 0;
            }
        }

        public bool HasNormals
        {
            get
            {
                return Array.IndexOf(VertexFormatList, 2) > -1 && mNormals.Length > 0;
            }
        }

        public bool HasUVs
        {
            get
            {
                return Array.IndexOf(VertexFormatList, 3) > -1 && mUVs.Length > 0;
            }
        }

        public bool HasBones
        {
            get
            {
                return Array.IndexOf(VertexFormatList, 4) > -1 && mBones.Length > 0;
            }
        }

        public bool HasTangents
        {
            get
            {
                return Array.IndexOf(VertexFormatList, 6) > -1 && mTangents.Length > 0;
            }
        }

        public bool HasTags
        {
            get
            {
                return Array.IndexOf(VertexFormatList, 7) > -1 && mTags.Length > 0;
            }
        }

        public int UVCount
        {
            get
            {
                if (mUVs == null)
                {
                    return 0;
                }
                else
                {
                    return mUVs.Length;
                }
            }
        }

        public VertexFormat[] VertexFormats
        {
            get
            {
                return mVertexFormats;
            }
        }

        public int[] VertexFormatList
        {
            get
            {
                var temp = new int[mFaceCount];
                for (var i = 0; i < mFaceCount; i++)
                {
                    temp[i] = mVertexFormats[i].DataType;
                }
                return temp;
            }
        }

        public int VertexDataLength
        {
            get
            {
                var length = 0;
                for (var i = 0; i < mFaceCount; i++)
                {
                    length = length + mVertexFormats[i].BytesPer;
                }
                return length;
            }
        }

        public int MaxVertexID
        {
            get
            {
                if (mVertexIDs == null)
                {
                    return -1;
                }
                var m = 0;
                for (var i = 0; i < mVertexIDs.Length; i++)
                {
                    if (m < mVertexIDs[i])
                    {
                        m = mVertexIDs[i];
                    }
                }
                return m;
            }
        }

        public int MinVertexID
        {
            get
            {
                if (mVertexIDs == null)
                {
                    return -1;
                }
                var m = mVertexIDs[0];
                for (var i = 1; i < mVertexIDs.Length; i++)
                {
                    if (m > mVertexIDs[i])
                    {
                        m = mVertexIDs[i];
                    }
                }
                return m;
            }
        }

        public int FaceCount
        {
            get
            {
                return mFacePointCount / 3;
            }
        }

        public int BoneCount
        {
            get
            {
                return mBoneHashArray.Length;
            }
        }

        public uint[] BoneHashList
        {
            get
            {
                return mBoneHashArray;
            }
        }

        public int GetBoneIndex(uint boneHash)
        {
            for (var i = 0; i < BoneCount; i++)
            {
                if (boneHash == mBoneHashArray[i]) return i;
            }
            return -1;
        }

        public int[] GetVertexIndicesAssignedtoBone(uint boneHash)
        {
            var index = GetBoneIndex(boneHash);
            if (index < 0)
            {
                return new int[] { };
            }
            var vertices = new List<int>();
            for (var i = 0; i < VertexCount; i++)
            {
                byte[] bones = GetBones(i),
                weights = GetBoneWeights(i);
                for (var j = 0; j < 4; j++)
                {
                    if (weights[j] > 0 && bones[j] == index)
                    {
                        vertices.Add(i);
                    }
                } 
            }
            return vertices.ToArray();
        }

        public float GetBoneWeightForVertex(int vertexIndex, uint boneHash)
        {
            byte[] vertexBones = GetBones(vertexIndex),
            vertexWeights = GetBoneWeights(vertexIndex);
            for (var i = 0; i < 4; i++)
            {
                if (mBoneHashArray[vertexBones[i]] == boneHash)
                {
                    return (float)vertexWeights[i] / byte.MaxValue;
                }
            }
            return 0;
        }

        public float GetTotalBoneWeight(int vertexIndex, List<uint> boneHashes)
        {
            byte[] vertexBones = GetBones(vertexIndex),
            vertexWeights = GetBoneWeights(vertexIndex);
            var weight = 0f;
            for (var i = 0; i < 4; i++)
            {
                if (vertexBones[i] < mBoneHashArray.Length && vertexBones[i] >= 0 && boneHashes.Contains(mBoneHashArray[vertexBones[i]]))
                {
                    weight += (float)vertexWeights[i] / byte.MaxValue;
                }
            }
            return weight;
        }

        public bool ValidBones(int vertexSequenceNumber)
        {
            byte[] vertexBones = GetBones(vertexSequenceNumber),
            vertexWeights = GetBoneWeights(vertexSequenceNumber);
            if (mVersion == 5)
            {
                for (var i = 0; i < 4; i++)
                {
                    if (vertexWeights[i] > 0 && (vertexBones[i] < 0 || vertexBones[i] >= BoneCount))
                    {
                        return false;
                    }
                }
            }
            else if (mVersion >= 12)
            {
                for (var i = 0; i < 4; i++)
                {
                    if (vertexBones[i] < 0 || vertexBones[i] >= BoneCount)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public string[] TGIListText
        {
            get
            {
                var temp = new string[mTGICount];
                for (var i = 0; i < mTGICount; i++)
                {
                    temp[i] = mTGIs[i].ToString();
                }
                return temp;
            }
        }

        public TGI[] TGIList
        {
            get
            {
                var temp = new TGI[mTGICount];
                for (var i = 0; i < mTGICount; i++)
                {
                    temp[i] = mTGIs[i];
                }
                return temp;
            }
            set
            {
                mTGICount = value.Length;
                var temp = new TGI[mTGICount];
                for (var i = 0; i < mTGICount; i++)
                {
                    temp[i] = value[i];
                }
                mTGIs = temp;
            }
        }

        public uint ShaderHash
        {
            get
            {
                return mShaderHash;
            }
        }

        public MTNF Shader
        {
            get
            {
                return mMeshMTNF;
            }
        }

        public int SkeletonIndex
        {
            get
            {
                return mSKCONIndex;
            }
        }

        public void SetShader(uint shaderHash)
        {
            mShaderHash = shaderHash;
        }

        public void SetShader(uint shaderHash, MTNF shader)
        {
            mShaderHash = shaderHash;
            mMeshMTNF = shader;
        }

        public void SetTGI(int index, TGI tgi)
        {
            mTGIs[index] = new TGI(tgi);
        }

        public bool VertexFormatEquals(int[] testFormatList)
        {
            var temp = VertexFormatList;
            if (temp.Length != testFormatList.Length)
            {
                return false;
            }
            for (var i = 0; i < temp.Length; i++)
            {
                if (temp[i] != testFormatList[i])
                {
                    return false;
                }
            }
            return true;
        }

        public bool HasUVSet(int uvSequence)
        {
            return (Array.IndexOf(VertexFormatList, 3) > -1 && uvSequence < mUVs.Length && mUVs[uvSequence] != null);
        }

        public string[] DataString(int lineNumber)
        {
            var vertexFormatList = VertexFormatList;
            var text = new string[vertexFormatList.Length];
            var uvIndex = 0;
            for (var i = 0; i < vertexFormatList.Length; i++)
            {
                switch (vertexFormatList[i])
                {
                    case 1:
                        text[i] = "Position: " + mPositions[lineNumber].ToString();
                        break;
                    case 2:
                        text[i] = "Normals: " + mNormals[lineNumber].ToString();
                        break;
                    case 3:
                        text[i] = "UV: " + mUVs[uvIndex][lineNumber].ToString();
                        uvIndex += 1;
                        break;
                    case 4:
                        text[i] = "Bones: " + mBones[lineNumber].ToString();
                        break;
                    case 6:
                        text[i] = "Tangents: " + mTangents[lineNumber].ToString();
                        break;
                    case 7:
                        text[i] = "TagVals: " + mTags[lineNumber].ToString();
                        break;
                    case 10:
                        text[i] = "Vertex ID: " + mVertexIDs[lineNumber].ToString();
                        break;
                    default:
                        break;
                }
            }
            return text;
        }

        public string VertexDataString(int vertexSequenceNumber)
        {
            var vertexFormatList = VertexFormatList;
            string s = " | ",
            text = "";
            var uvIndex = 0;
            for (int i = 0; i < vertexFormatList.Length; i++)
            {
                switch (vertexFormatList[i])
                {
                    case 1:
                        text = text + mPositions[vertexSequenceNumber].ToString() + s;
                        break;
                    case 2:
                        text = text + mNormals[vertexSequenceNumber].ToString() + s;
                        break;
                    case 3:
                        text = text + mUVs[uvIndex][vertexSequenceNumber].ToString() + s;
                        uvIndex += 1;
                        break;
                    case 4:
                        text = text + mBones[vertexSequenceNumber].ToString() + s;
                        break;
                    case 6:
                        text = text + mTangents[vertexSequenceNumber].ToString() + s;
                        break;
                    case 7:
                        text = text + mTags[vertexSequenceNumber].ToString() + s;
                        break;
                    case 10:
                        text = text + mVertexIDs[vertexSequenceNumber].ToString() + s;
                        break;
                    default:
                        break;
                }
            }
            return text.Remove(text.LastIndexOf(s));
        }

        public int GetVertexID(int vertexSequenceNumber)
        {
            return (int)mVertexIDs[vertexSequenceNumber];
        }

        public float[] GetPosition(int vertexSequenceNumber)
        {
            return mPositions[vertexSequenceNumber].Data();
        }

        public float[] GetNormal(int vertexSequenceNumber)
        {
            return mNormals[vertexSequenceNumber].Data();
        }

        public float[] GetUV(int vertexSequenceNumber, int UVset)
        {
            return mUVs[UVset][vertexSequenceNumber].Data();
        }

        public byte[] GetBones(int vertexSequenceNumber)
        {
            return mBones[vertexSequenceNumber].BoneAssignments;
        }

        public float[] GetBoneWeightsV5(int vertexSequenceNumber)
        {
            return mBones[vertexSequenceNumber].BoneWeightsV5;
        }

        public byte[] GetBoneWeights(int vertexSequenceNumber)
        {
            return mBones[vertexSequenceNumber].BoneWeights;
        }

        public float[] GetTangent(int vertexSequenceNumber)
        {
            return mTangents[vertexSequenceNumber].Data();
        }

        public uint GetTagValue(int vertexSequenceNumber)
        {
            return mTags[vertexSequenceNumber].Data();
        }

        public int[] GetFaceIndices(int faceSequenceNumber)
        {
            return new int[]
            {
                (int)mFaces[faceSequenceNumber].MeshFace[0],
                (int)mFaces[faceSequenceNumber].MeshFace[1],
                (int)mFaces[faceSequenceNumber].MeshFace[2]
            };
        }

        public uint[] GetFaceIndicesUInt(int faceSequenceNumber)
        {
            return new uint[]
            {
                mFaces[faceSequenceNumber].MeshFace[0],
                mFaces[faceSequenceNumber].MeshFace[1],
                mFaces[faceSequenceNumber].MeshFace[2]
            };
        }

        public Vector3[] GetFacePoints(int faceSequenceNumber)
        {
            return new Vector3[]
            {
                new Vector3(mPositions[mFaces[faceSequenceNumber].MeshFace[0]].Coordinates), 
                new Vector3(mPositions[mFaces[faceSequenceNumber].MeshFace[1]].Coordinates), 
                new Vector3(mPositions[mFaces[faceSequenceNumber].MeshFace[2]].Coordinates)
            };
        }

        public void SetVertexID(int vertexSequenceNumber, int newVertexID)
        {
            mVertexIDs[vertexSequenceNumber] = newVertexID;
        }

        public void SetPosition(int vertexSequenceNumber, float[] newPosition)
        {
            mPositions[vertexSequenceNumber] = new Position(newPosition);
        }

        public void SetPosition(int vertexSequenceNumber, float x, float y, float z)
        {
            mPositions[vertexSequenceNumber] = new Position(x, y, z);
        }

        public void SetNormal(int vertexSequenceNumber, float[] newNormal)
        {
            mNormals[vertexSequenceNumber] = new Normal(newNormal);
        }

        public void SetNormal(int vertexSequenceNumber, float x, float y, float z)
        {
            mNormals[vertexSequenceNumber] = new Normal(x, y, z);
        }

        public void SetUV(int vertexSequenceNumber, int uvSet, float[] newUV)
        {
            mUVs[uvSet][vertexSequenceNumber] = new UV(newUV[0], newUV[1]);
        }

        public void SetUV(int vertexSequenceNumber, int uvSet, float u, float v)
        {
            mUVs[uvSet][vertexSequenceNumber] = new UV(u, v);
        }

        public void SetBoneList(uint[] newBoneHashList)
        {
            mBoneHashArray = newBoneHashList;
            mBoneHashCount = newBoneHashList.Length;
        }

        public void SetBones(int vertexSequenceNumber, byte[] newBones)
        {
            mBones[vertexSequenceNumber].BoneAssignments = newBones;
        }

        public void SetBones(int vertexSequenceNumber, byte bone0, byte bone1, byte bone2, byte bone3)
        {
            mBones[vertexSequenceNumber].BoneAssignments = new byte[] { bone0, bone1, bone2, bone3 };
        }

        public void SetBoneWeightsV5(int vertexSequenceNumber, float[] newWeights)
        {
            mBones[vertexSequenceNumber].BoneWeightsV5 = newWeights;
        }

        public void SetBoneWeightsV5(int vertexSequenceNumber, float weight0, float weight1, float weight2, float weight3)
        {
            mBones[vertexSequenceNumber].BoneWeightsV5 = new float[] { weight0, weight1, weight2, weight3 };
        }

        public void SetBoneWeights(int vertexSequenceNumber, byte[] newWeights)
        {
            mBones[vertexSequenceNumber].BoneWeights = newWeights;
        }

        public void SetBoneWeights(int vertexSequenceNumber, byte weight0, byte weight1, byte weight2, byte weight3)
        {
            mBones[vertexSequenceNumber].BoneWeights = new byte[] { weight0, weight1, weight2, weight3 };
        }

        public void SetTangent(int vertexSequenceNumber, float[] newTangent)
        {
            mTangents[vertexSequenceNumber] = new Tangent(newTangent);
        }

        public void SetTangent(int vertexSequenceNumber, float X, float Y, float Z)
        {
            mTangents[vertexSequenceNumber] = new Tangent(X, Y, Z);
        }

        public void SetTagValue(int vertexSequenceNumber, uint newTag)
        {
            mTags[vertexSequenceNumber] = new TagValue(newTag);
        }

        public void SetBoneHashList(uint[] boneHashList)
        {
            mBoneHashArray = boneHashList;
            mBoneHashCount = boneHashList.Length;
        }

        public List<float[]> GetStitchUVs(int vertexIndex)
        {
            var uvList = new List<float[]>();
            foreach (var uvStitch in mUVStitches)
            {
                if (uvStitch.Index == vertexIndex)
                {
                    uvList.AddRange(uvStitch.UV1Coordinates);
                    break;
                }
            }
            return uvList;
        }

        public float[] GetHeightAndDepth()
        {
            float yMax = 0,
            zMax = 0;
            foreach (var position in mPositions)
            {
                if (position.Y > yMax)
                {
                    yMax = position.Y;
                }
                if (position.Z > zMax)
                {
                    zMax = position.Z;
                }
            }
            return new float[]
            {
                yMax,
                zMax
            };
        }

        public GEOM()
        {
        }

        public GEOM(GEOM sourceMesh)
        {
            mVersion1 = sourceMesh.mVersion1;
            mCount = sourceMesh.mCount;
            mIndexCount = sourceMesh.mIndexCount;
            mExternalCount = sourceMesh.mExternalCount;
            mInternalCount = sourceMesh.mInternalCount;
            mDummyTGI = new TGI(sourceMesh.mDummyTGI);
            mAbsolutePosition = sourceMesh.mAbsolutePosition;
            mMagic = sourceMesh.mMagic;
            mVersion = sourceMesh.mVersion;
            mShaderHash = sourceMesh.mShaderHash;
            mMTNFSize = sourceMesh.mMTNFSize;
            if (mShaderHash > 0)
            {
                mMeshMTNF = new MTNF(sourceMesh.mMeshMTNF);
            }
            mMergeGroup = sourceMesh.mMergeGroup;
            mSortOrder = sourceMesh.mSortOrder;
            mVertexCount = sourceMesh.VertexCount;
            mFaceCount = sourceMesh.mFaceCount;
            mVertexFormats = new VertexFormat[sourceMesh.mFaceCount];
            for (var i = 0; i < sourceMesh.mFaceCount; i++)
            {
                mVertexFormats[i] = new VertexFormat(sourceMesh.mVertexFormats[i]);
            }
            if (sourceMesh.HasBones)
            {
                mBones = new Bones[mVertexCount];
            }
            for (var i = 0; i < mVertexFormats.Length; i++)
            {
                switch (mVertexFormats[i].DataType)
                {
                    case 1:
                        mPositions = new Position[mVertexCount];
                        for (var j = 0; j < mVertexCount; j++)
                        {
                            mPositions[j] = new Position(sourceMesh.mPositions[j]);
                        }
                        break;
                    case 2:
                        mNormals = new Normal[mVertexCount];
                        for (var j = 0; j < mVertexCount; j++)
                        {
                            mNormals[j] = new Normal(sourceMesh.mNormals[j]);
                        }
                        break;
                    case 3:
                        mUVs = new UV[sourceMesh.mUVs.Length][];
                        for (var j = 0; j < sourceMesh.mUVs.Length; j++)
                        {
                            mUVs[j] = new UV[mVertexCount];
                            for (var k = 0; k < mVertexCount; k++)
                            {
                                mUVs[j][k] = new UV(sourceMesh.mUVs[j][k]);
                            }
                        }
                        break;
                    case 4:
                        for (var j = 0; j < mVertexCount; j++)
                        {
                            if (mBones[j] == null)
                            {
                                mBones[j] = new Bones();
                            }
                            byte[] temp =
                                {
                                    sourceMesh.mBones[j].BoneAssignments[0],
                                    sourceMesh.mBones[j].BoneAssignments[1], 
                                    sourceMesh.mBones[j].BoneAssignments[2],
                                    sourceMesh.mBones[j].BoneAssignments[3]
                                };
                            mBones[j].BoneAssignments = temp;
                        }
                        break;
                    case 5:
                        for (var j = 0; j < mVertexCount; j++)
                        {
                            if (mBones[j] == null)
                            {
                                mBones[j] = new Bones();
                            }
                            byte[] temp =
                                {
                                    sourceMesh.mBones[j].BoneWeights[0],
                                    sourceMesh.mBones[j].BoneWeights[1], 
                                    sourceMesh.mBones[j].BoneWeights[2],
                                    sourceMesh.mBones[j].BoneWeights[3]
                                };
                            mBones[j].BoneWeights = temp;
                        }
                        break;
                    case 6:
                        mTangents = new Tangent[mVertexCount];
                        for (var j = 0; j < mVertexCount; j++)
                        {
                            mTangents[j] = new Tangent(sourceMesh.mTangents[j]);
                        }
                        break;
                    case 7:
                        mTags = new TagValue[mVertexCount];
                        for (var j = 0; j < mVertexCount; j++)
                        {
                            mTags[j] = new TagValue(sourceMesh.mTags[j]);
                        }
                        break;
                    case 10:
                        mVertexIDs = new int[mVertexCount];
                        for (var j = 0; j < mVertexCount; j++)
                        {
                            mVertexIDs[j] = sourceMesh.mVertexIDs[j];
                        }
                        break;
                    default:
                        break;
                }
            }
            mSubMeshCount = sourceMesh.mSubMeshCount;
            mBytesPerFacePoint = sourceMesh.mBytesPerFacePoint;
            mFacePointCount = sourceMesh.mFacePointCount;
            mFaces = new Face[sourceMesh.mFaces.Length];
            for (var i = 0; i < sourceMesh.mFaces.Length; i++)
            {
                mFaces[i] = new Face(sourceMesh.mFaces[i]);
            }
            if (sourceMesh.mVersion == 5)
            {
                mSKCONIndex = sourceMesh.mSKCONIndex;
            }
            else if (sourceMesh.mVersion >= 12)
            {
                mUVStitchCount = sourceMesh.mUVStitchCount;
                mUVStitches = new UVStitch[mUVStitchCount];
                for (var i = 0; i < mUVStitchCount; i++)
                {
                    mUVStitches[i] = new UVStitch(sourceMesh.mUVStitches[i]);
                }
                if (sourceMesh.mVersion >= 13)
                {
                    mSeamStitchCount = sourceMesh.mSeamStitchCount;
                    mSeamStitches = new SeamStitch[mSeamStitchCount];
                    for (var i = 0; i < mSeamStitchCount; i++)
                    {
                        mSeamStitches[i] = new SeamStitch(sourceMesh.mSeamStitches[i]);
                    }
                }
                mSlotCount = sourceMesh.mSlotCount;
                mSlotrayIntersections = new SlotrayIntersection[mSlotCount];
                for (var i = 0; i < mSlotCount; i++)
                {
                    mSlotrayIntersections[i] = new SlotrayIntersection(sourceMesh.mSlotrayIntersections[i]);
                }
            }
            mBoneHashCount = sourceMesh.mBoneHashCount;
            mBoneHashArray = new uint[sourceMesh.mBoneHashArray.Length];
            for (var i = 0; i < sourceMesh.mBoneHashArray.Length; i++)
            {
                mBoneHashArray[i] = sourceMesh.mBoneHashArray[i];
            }
            mTGICount = sourceMesh.mTGICount;
            mTGIs = new TGI[sourceMesh.mTGIs.Length];
            for (var i = 0; i < sourceMesh.mTGIs.Length; i++)
            {
                mTGIs[i] = new TGI(sourceMesh.mTGIs[i]);
            }
            CopyFaceMorphs = sourceMesh.CopyFaceMorphs;
        }

        public GEOM(GEOM baseMesh, Vector3[] deltaPositions, Vector3[] deltaNormals)
        {
            if (!baseMesh.IsValid || !baseMesh.IsBase || baseMesh.mVertexCount <= 0)
            {
                throw new MeshException("Invalid base mesh, cannot construct new mesh!");
            }
            if (baseMesh.VertexCount != deltaPositions.Length || baseMesh.VertexCount != deltaNormals.Length)
            {
                throw new MeshException("Lists of positions and normals do not match number of base mesh vertices!");
            }
            mVersion1 = baseMesh.mVersion1;
            mCount = baseMesh.mCount;
            mIndexCount = baseMesh.mIndexCount;
            mExternalCount = baseMesh.mExternalCount;
            mInternalCount = baseMesh.mInternalCount;
            mDummyTGI = new TGI(baseMesh.mDummyTGI.Type, 0, 0);
            mAbsolutePosition = baseMesh.mAbsolutePosition;
            mMagic = baseMesh.mMagic;
            mVersion = baseMesh.mVersion;
            mShaderHash = 0;
            mMTNFSize = 0;
            mMergeGroup = baseMesh.mMergeGroup;
            mSortOrder = baseMesh.mSortOrder;
            mVertexCount = baseMesh.VertexCount;
            mFaceCount = 3;
            mVertexFormats = new VertexFormat[3]
                {
                    new VertexFormat(1, 1, 12),
                    new VertexFormat(2, 1, 12),
                    new VertexFormat(10, 4, 4)
                };
            mPositions = new Position[mVertexCount];
            mNormals = new Normal[mVertexCount];
            mVertexIDs = baseMesh.mVertexIDs;
            mSubMeshCount = baseMesh.mSubMeshCount;
            mBytesPerFacePoint = baseMesh.mBytesPerFacePoint;
            mFacePointCount = baseMesh.mFacePointCount;
            mFaces = baseMesh.mFaces;
            mSKCONIndex = 0;
            mBoneHashCount = baseMesh.mBoneHashCount;
            mBoneHashArray = baseMesh.mBoneHashArray;
            mTGICount = 1;
            mTGIs = new TGI[1]
                {
                    new TGI(0, 0, 0)
                };
            for (var i = 0; i < mVertexCount; i++)
            {
                mPositions[i] = new Position(deltaPositions[i].X, deltaPositions[i].Y, deltaPositions[i].Z);
                mNormals[i] = new Normal(deltaNormals[i].X, deltaNormals[i].Y, deltaNormals[i].Z);
            }
        }

        public GEOM(BinaryReader reader)
        {
            ReadFile(reader);
        }

        public void ReadFile(BinaryReader reader)
        {
            reader.BaseStream.Position = 0;
            if (reader.BaseStream.Length < 12)
            {
                return;
            }
            reader.BaseStream.Position = 0;
            mVersion1 = reader.ReadInt32();
            mCount = reader.ReadInt32();
            mIndexCount = reader.ReadInt32();
            mExternalCount = reader.ReadInt32();
            mInternalCount = reader.ReadInt32();
            mDummyTGI = new TGI(reader);
            mAbsolutePosition = reader.ReadInt32();
            mMeshSize = reader.ReadInt32();
            mMagic = reader.ReadChars(4);
            if (new string(mMagic) != "GEOM")
            {
                throw new MeshException("Not a valid GEOM file.");
            }
            mVersion = reader.ReadInt32();
            mTGIOffset = reader.ReadInt32();
            mTGISize = reader.ReadInt32();
            mShaderHash = reader.ReadUInt32();
            if (mShaderHash != 0)
            {
                mMTNFSize = reader.ReadInt32();
                mMeshMTNF = new MTNF(reader);
            }
            mMergeGroup = reader.ReadInt32();
            mSortOrder = reader.ReadInt32();
            mVertexCount = reader.ReadInt32();
            mFaceCount = reader.ReadInt32();
            mVertexFormats = new VertexFormat[mFaceCount];
            for (var i = 0; i < mFaceCount; i++)
            {
                mVertexFormats[i] = new VertexFormat(reader);
            }
            var vertexFormatList = VertexFormatList;
            var uvIndex = 0;
            for (var i = 0; i < vertexFormatList.Length; i++)
            {
                switch (vertexFormatList[i])
                {
                    case 1:
                        mPositions = new Position[mVertexCount];
                        break;
                    case 2:
                        mNormals = new Normal[mVertexCount];
                        break;
                    case 3:
                        uvIndex += 1;
                        break;
                    case 4:
                        mBones = new Bones[mVertexCount];
                        break;
                    case 6:
                        mTangents = new Tangent[mVertexCount];
                        break;
                    case 7:
                        mTags = new TagValue[mVertexCount];
                        break;
                    case 10:
                        mVertexIDs = new int[mVertexCount];
                        break;
                    default:
                        break;
                }
            }
            if (uvIndex > 0)
            {
                mUVs = new UV[uvIndex][];
            }
            for (var i = 0; i < uvIndex; i++)
            {
                mUVs[i] = new UV[mVertexCount];
            }
            for (var i = 0; i < mVertexCount; i++)
            {
                uvIndex = 0;
                for (var j = 0; j < mFaceCount; j++)
                {
                    switch (mVertexFormats[j].DataType)
                    {
                        case 1:
                            mPositions[i] = new Position(reader);
                            break;
                        case 2:
                            mNormals[i] = new Normal(reader);
                            break;
                        case 3:
                            mUVs[uvIndex][i] = new UV(reader);
                            uvIndex += 1;
                            break;
                        case 4:
                            if (mBones[i] == null)
                            {
                                mBones[i] = new Bones();
                            }
                            mBones[i].ReadAssignments(reader);
                            break;
                        case 5:
                            if (mBones[i] == null)
                            {
                                mBones[i] = new Bones();
                            }
                            mBones[i].ReadWeights(reader, mVertexFormats[j].Subtype);
                            break;
                        case 6:
                            mTangents[i] = new Tangent(reader);
                            break;
                        case 7:
                            mTags[i] = new TagValue(reader);
                            break;
                        case 10:
                            mVertexIDs[i] = reader.ReadInt32();
                            break;
                        default:
                            break;
                    }
                }
            }
            mSubMeshCount = reader.ReadInt32();
            mBytesPerFacePoint = reader.ReadByte();
            mFacePointCount = reader.ReadInt32();
            mFaces = new Face[mFacePointCount / 3];
            for (var i = 0; i < mFacePointCount / 3; i++)
            {
                mFaces[i] = new Face(reader, mBytesPerFacePoint);
            }
            if (mVersion == 5)
            {
                mSKCONIndex = reader.ReadInt32();
            }
            else if (mVersion >= 12)
            {
                mUVStitchCount = reader.ReadInt32();
                mUVStitches = new UVStitch[mUVStitchCount];
                for (var i = 0; i < mUVStitchCount; i++)
                {
                    mUVStitches[i] = new UVStitch(reader);
                }
                if (mVersion >= 13)
                {
                    mSeamStitchCount = reader.ReadInt32();
                    mSeamStitches = new SeamStitch[mSeamStitchCount];
                    for (var i = 0; i < mSeamStitchCount; i++)
                    {
                        mSeamStitches[i] = new SeamStitch(reader, mUVs[0]);
                    }
                }
                mSlotCount = reader.ReadInt32();
                mSlotrayIntersections = new SlotrayIntersection[mSlotCount];
                for (var i = 0; i < mSlotCount; i++)
                {
                    mSlotrayIntersections[i] = new SlotrayIntersection(reader, mVersion);
                }
            }
            mBoneHashCount = reader.ReadInt32();
            mBoneHashArray = new uint[mBoneHashCount];
            for (var i = 0; i < mBoneHashCount; i++)
            {
                mBoneHashArray[i] = reader.ReadUInt32();
            }
            if (reader.BaseStream.Length <= reader.BaseStream.Position)
            {
                return;
            }
            mTGICount = reader.ReadInt32();
            mTGIs = new TGI[mTGICount];
            for (var i = 0; i < mTGICount; i++)
            {
                mTGIs[i] = new TGI(reader);
            }
            if (IsMorph & mSKCONIndex >= mTGICount)
            {
                mSKCONIndex = 0;
            }
            return;
        }

        public void WriteFile(BinaryWriter writer)
        {
            var temp = 0;
            if (mMeshMTNF != null)
            {
                mMTNFSize = mMeshMTNF.ChunkSize;
            }
            else
            {
                mMTNFSize = 0;
            }
            if (mShaderHash != 0)
            {
                temp = mMTNFSize + 4;
            }
            mTGIOffset = 37 + (mFaceCount * 9) + temp + (mVertexCount * VertexDataLength) + (mFacePointCount * mBytesPerFacePoint) + (mBoneHashCount * 4);
            if (mVersion == 5)
            {
                mTGIOffset += 4;
            }
            else if (mVersion >= 12)
            {
                mTGIOffset += 8 + UVStitchesSize + SlotrayAdjustmentsSize;
                if (mVersion >= 13)
                {
                    mTGIOffset += 4 + (mSeamStitchCount * 6);
                }
            }
            mMeshSize = mTGIOffset + 16 + mTGICount * 16;
            mTGISize = 4 + mTGICount * 16;
            writer.Write(mVersion1);
            writer.Write(mCount);
            writer.Write(mIndexCount);
            writer.Write(mExternalCount);
            writer.Write(mInternalCount);
            mDummyTGI.Write(writer);
            writer.Write(mAbsolutePosition);
            writer.Write(mMeshSize);
            writer.Write(mMagic);
            writer.Write(mVersion);
            writer.Write(mTGIOffset);
            writer.Write(mTGISize);
            writer.Write(mShaderHash);
            if (mShaderHash != 0)
            {
                writer.Write(mMTNFSize);
                mMeshMTNF.Write(writer);
            }
            writer.Write(mMergeGroup);
            writer.Write(mSortOrder);
            writer.Write(mVertexCount);
            writer.Write(mFaceCount);
            for (var i = 0; i < mFaceCount; i++)
            {
                mVertexFormats[i].vertexformatWrite(writer);
            }
            for (var i = 0; i < mVertexCount; i++)
            {
                var uvIndex = 0;
                for (var j = 0; j < mFaceCount; j++)
                {
                    switch (mVertexFormats[j].DataType)
                    {
                        case 1:
                            mPositions[i].Write(writer);
                            break;
                        case 2:
                            mNormals[i].Write(writer);
                            break;
                        case 3:
                            mUVs[uvIndex][i].Write(writer);
                            uvIndex += 1;
                            break;
                        case 4:
                            mBones[i].WriteAssignments(writer, mVersion, mBoneHashCount - 1);
                            break;
                        case 5:
                            mBones[i].WriteWeights(writer, mVersion);
                            break;
                        case 6:
                            mTangents[i].Write(writer);
                            break;
                        case 7:
                            mTags[i].Write(writer);
                            break;
                        case 10:
                            writer.Write(mVertexIDs[i]);
                            break;
                        default:
                            break;
                    }
                }
            }
            writer.Write(mSubMeshCount);
            writer.Write(mBytesPerFacePoint);
            writer.Write(mFaces.Length * 3);
            for (var i = 0; i < mFaces.Length; i++)
            {
                mFaces[i].Write(writer, mBytesPerFacePoint);
            }
            if (mVersion == 5)
            {
                writer.Write(mSKCONIndex);
            }
            else if (mVersion >= 12)
            {
                writer.Write(mUVStitchCount);
                for (var i = 0; i < mUVStitchCount; i++)
                {
                    mUVStitches[i].Write(writer);
                }
                if (mVersion >= 13)
                {
                    writer.Write(mSeamStitchCount);
                    for (var i = 0; i < mSeamStitchCount; i++)
                    {
                        mSeamStitches[i].Write(writer);
                    }
                }
                writer.Write(mSlotCount);
                for (var i = 0; i < mSlotCount; i++)
                {
                    mSlotrayIntersections[i].Write(writer);
                }
            }
            writer.Write(mBoneHashCount);
            for (var i = 0; i < mBoneHashCount; i++)
            {
                writer.Write(mBoneHashArray[i]);
            }
            writer.Write(mTGICount);
            for (var i = 0; i < mTGICount; i++)
            {
                mTGIs[i].Write(writer);
            }
            return;
        }

        public void StandardizeFormat()
        {
            if (mVersion < 14)
            {
                mVersion = 14;
            }
            if (mSeamStitches == null)
            {
                mSeamStitches = new SeamStitch[0];
            }
            if (mUVStitches == null)
            {
                mUVStitches = new UVStitch[0];
            }
            if (SlotrayAdjustments == null)
            {
                SlotrayAdjustments = new SlotrayIntersection[0];
            }
            if (mBoneHashArray == null)
            {
                mBoneHashArray = new uint[0];
            }
            if (mTGIs == null)
            {
                mTGIs = new TGI[0];
            }
            if (!HasUVSet(1))
            {
                InsertFormatDescriptor(MeshFormatFlag.UV);
                var temp = new UV[mPositions.Length];
                for (var i = 0; i < temp.Length; i++)
                {
                    temp[i] = new UV();
                }
                var newUV = new UV[2][];
                newUV[0] = mUVs[0];
                newUV[1] = temp;
                mUVs = newUV;
            }
            if (HasUVSet(2))
            {
                var newFormat = new VertexFormat[VertexFormatList.Length - 1];
                var uv = Array.LastIndexOf(VertexFormatList, (int)MeshFormatFlag.UV);
                Array.Copy(mVertexFormats, newFormat, uv);
                Array.Copy(mVertexFormats, uv + 1, newFormat, uv, mVertexFormats.Length - uv - 1);
                mVertexFormats = newFormat;
                mFaceCount--;
                var newUV = new UV[2][];
                newUV[0] = mUVs[0];
                newUV[1] = mUVs[1];
                mUVs = newUV;
            }
            if (!HasNormals)
            {
                InsertFormatDescriptor(MeshFormatFlag.Normals);
                mNormals = new Normal[mPositions.Length];
                for (var i = 0; i < mNormals.Length; i++)
                {
                    mNormals[i] = new Normal();
                }
            }
            if (!HasBones)
            {
                InsertFormatDescriptor(MeshFormatFlag.BoneIndices);
                InsertFormatDescriptor(MeshFormatFlag.BoneWeights);
                mBones = new Bones[mPositions.Length];
                for (var i = 0; i < mBones.Length; i++)
                {
                    mBones[i] = new Bones();
                }
            }
            if (!HasTangents)
            {
                InsertFormatDescriptor(MeshFormatFlag.Tangents);
                mTangents = new Tangent[mPositions.Length];
                for (var i = 0; i < mTangents.Length; i++)
                {
                    mTangents[i] = new Tangent();
                }
            }
            if (!HasTags)
            {
                InsertFormatDescriptor(MeshFormatFlag.Color);
                mTags = new TagValue[mPositions.Length];
                for (var i = 0; i < mTags.Length; i++)
                {
                    mTags[i] = new TagValue();
                }
            }
            if (!HasVertexIDs)
            {
                InsertFormatDescriptor(MeshFormatFlag.VertexID);
                mVertexIDs = new int[mPositions.Length];
                for (var i = 0; i < mVertexIDs.Length; i++)
                {
                    mVertexIDs[i] = -1;
                }
            }
        }

        public void InsertFormatDescriptor(MeshFormatFlag flag)
        {
            if (Array.IndexOf(VertexFormatList, (int)flag) >= 0)
            {
                return;
            }
            MeshFormatDataType dataType = 0;
            byte byteLength = 0;
            switch (flag)
            {
                case MeshFormatFlag.Position:
                    dataType = MeshFormatDataType.Float;
                    byteLength = 12;
                    break;
                case MeshFormatFlag.Normals:
                    dataType = MeshFormatDataType.Float;
                    byteLength = 12;
                    break;
                case MeshFormatFlag.UV:
                    dataType = MeshFormatDataType.Float;
                    byteLength = 8;
                    break;
                case MeshFormatFlag.BoneIndices:
                    dataType = MeshFormatDataType.Byte4;
                    byteLength = 4;
                    break;
                case MeshFormatFlag.BoneWeights:
                    dataType = MeshFormatDataType.Byte4;
                    byteLength = 4;
                    break;
                case MeshFormatFlag.Tangents:
                    dataType = MeshFormatDataType.Float;
                    byteLength = 12;
                    break;
                case MeshFormatFlag.Color:
                    dataType = MeshFormatDataType.Color;
                    byteLength = 4;
                    break;
                case MeshFormatFlag.VertexID:
                    dataType = MeshFormatDataType.Uint;
                    byteLength = 4;
                    break;
                default:
                    dataType = MeshFormatDataType.Float;
                    byteLength = 12;
                    break;
            }
            var newFormat = new VertexFormat[VertexFormatList.Length + 1];
            if (flag == MeshFormatFlag.UV && Array.IndexOf(VertexFormatList, (int)flag) >= 0)
            {
                var uv = Array.IndexOf(VertexFormatList, (int)flag);
                Array.Copy(mVertexFormats, newFormat, uv + 1);
                newFormat[uv + 1] = new VertexFormat((int)flag, (int)dataType, byteLength);
                Array.Copy(mVertexFormats, uv + 1, newFormat, uv + 2, mVertexFormats.Length - uv - 1);
            }
            else
            {
                Array.Copy(mVertexFormats, newFormat, VertexFormatList.Length);
                newFormat[VertexFormatList.Length] = new VertexFormat((int)flag, (int)dataType, byteLength);
            }
            mVertexFormats = newFormat;
            mFaceCount += 1;
        }

        public void MatchPartSeamStitches()
        {
            if (mSeamStitches == null)
            {
                return;
            }
            var ids = new List<ushort>();
            var hitCount = new List<int>();
            List<Vector3> normals = new List<Vector3>(),
            positions = new List<Vector3>();
            foreach (var seamStitch in mSeamStitches)
            {
                var i = ids.IndexOf(seamStitch.VertexID);
                if (i < 0)
                {
                    ids.Add(seamStitch.VertexID);
                    positions.Add(new Vector3(mPositions[seamStitch.Index].Coordinates));
                    normals.Add(new Vector3(mNormals[seamStitch.Index].Coordinates));
                    hitCount.Add(1);
                }
                else
                {
                    positions[i] = (new Vector3(mPositions[seamStitch.Index].Coordinates) + (hitCount[i] * positions[i])) / (hitCount[i] + 1);
                    normals[i] = (new Vector3(mNormals[seamStitch.Index].Coordinates) + (hitCount[i] * normals[i])) / (hitCount[i] + 1);
                    hitCount[i]++;
                }
            }
            foreach (var seamStitch in mSeamStitches)
            {
                var i = ids.IndexOf(seamStitch.VertexID);
                if (i >= 0)
                {
                    mPositions[seamStitch.Index] = new Position(positions[i].Coordinates);
                    mNormals[seamStitch.Index] = new Normal(normals[i].Coordinates);
                }
            }
        }

        public static void MatchSeamStitches(GEOM geom1, GEOM geom2)
        {
            var ids = new List<ushort>();
            List<Vector3> normals = new List<Vector3>(),
            positions = new List<Vector3>();
            foreach (var seamStitch1 in geom1.mSeamStitches)
            {
                foreach (var seamStitch2 in geom2.mSeamStitches)
                {
                    if (seamStitch1.VertexID == seamStitch2.VertexID)
                    {
                        var i = ids.IndexOf(seamStitch1.VertexID);
                        if (i >= 0)
                        {
                            geom1.mPositions[seamStitch1.Index] = new Position(positions[i].Coordinates);
                            geom2.mPositions[seamStitch2.Index] = new Position(positions[i].Coordinates);
                            geom1.mNormals[seamStitch1.Index] = new Normal(normals[i].Coordinates);
                            geom2.mNormals[seamStitch2.Index] = new Normal(normals[i].Coordinates);
                        }
                        else
                        {
                            Vector3 normal = (new Vector3(geom1.mNormals[seamStitch1.Index].Coordinates) + new Vector3(geom2.mNormals[seamStitch2.Index].Coordinates)) / 2,
                            position = (new Vector3(geom1.mPositions[seamStitch1.Index].Coordinates) + new Vector3(geom2.mPositions[seamStitch2.Index].Coordinates)) / 2;
                            geom1.mPositions[seamStitch1.Index] = new Position(position.Coordinates);
                            geom2.mPositions[seamStitch2.Index] = new Position(position.Coordinates);
                            geom1.mNormals[seamStitch1.Index] = new Normal(normal.Coordinates);
                            geom2.mNormals[seamStitch2.Index] = new Normal(normal.Coordinates);
                            ids.Add(seamStitch1.VertexID);
                            positions.Add(position);
                            normals.Add(normal);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fills gap at waist when wearing a female top and male bottom
        /// </summary>
        /// <param name="geom"></param>
        public void FillWaistGap()
        {
            uint femaleWaist = (uint)SeamType.WaistAdultFemale,
            maleWaist = (uint)SeamType.WaistAdultMale;
            List<SeamStitch> femaleVertices = new List<SeamStitch>(),
            maleVertices = new List<SeamStitch>();
            for (var i = 0; i < mSeamStitches.Length; i++)
            {
                if (mSeamStitches[i].SeamType == femaleWaist)
                {
                    femaleVertices.Add(mSeamStitches[i]);
                }
                if (mSeamStitches[i].SeamType == maleWaist)
                {
                    maleVertices.Add(mSeamStitches[i]);
                }
            }
            if (femaleVertices.Count == 0 || maleVertices.Count == 0)
            {
                return;
            }
            femaleVertices.Sort();
            maleVertices.Sort();
            SeamStitch femaleLast = femaleVertices[0],
            maleLast = maleVertices[0];
            femaleVertices.RemoveAt(0);
            femaleVertices.Add(femaleLast);
            maleVertices.RemoveAt(0);
            maleVertices.Add(maleLast);
            if (femaleVertices.Count != maleVertices.Count)
            {
                return;
            }
            var newFaces = new List<Face>(mFaces);
            for (var i = 0; i < femaleVertices.Count - 1; i++)
            {
                newFaces.Add(new Face(femaleVertices[i].Index, femaleVertices[i + 1].Index, maleVertices[i].Index));
                newFaces.Add(new Face(maleVertices[i].Index, femaleVertices[i + 1].Index, maleVertices[i + 1].Index));
            }
            mFaces = newFaces.ToArray();
            mFacePointCount = mFaces.Length * 3;
        }

        public static void MatchSeamVertices(GEOM geom1, GEOM geom2)
        {
            List<UVStitch> geom1Stitches = new List<UVStitch>(geom1.mUVStitches),
            geom2Stitches = new List<UVStitch>(geom2.mUVStitches);
            for (var i = 0; i < geom1.mVertexCount; i++)
            {
                for (var j = 0; j < geom2.mVertexCount; j++)
                {
                    if (new Vector3(geom1.GetPosition(i)).PositionMatches(geom2.GetPosition(j)))
                    {
                        byte[] sourceBones = geom1.GetBones(i),
                        sourceWeights = geom1.GetBoneWeights(i),
                        targetBones = new byte[sourceBones.Length];
                        for (var k = 0; k < sourceBones.Length; k++)
                        {
                            if (!TranslateBone(geom1.mBoneHashArray[sourceBones[k]], geom2.mBoneHashArray, out targetBones[k]) && sourceWeights[k] > 0)
                            {
                                var temp = new List<uint>(geom2.mBoneHashArray);
                                temp.Add(geom1.mBoneHashArray[sourceBones[k]]);
                                targetBones[k] = (byte)(temp.Count - 1);
                            }
                        }
                        geom2.SetBones(j, targetBones);
                        geom2.SetBoneWeights(j, sourceWeights);
                        geom2.SetNormal(j, geom1.GetNormal(i));
                        List<Vector2> geom1Stitch = new List<Vector2>(),
                        geom2Stitch = new List<Vector2>();
                        int geom1Index = -1,
                        geom2Index = -1;
                        for (var k = 0; k < geom1.mUVStitches.Length; k++)
                        {
                            if (geom1.mUVStitches[k].Index == i)
                            {
                                geom1Stitch.AddRange(geom1Stitches[k].UV1Vectors);
                                geom1Index = k;
                                break;
                            }
                        }
                        for (var k = 0; k < geom2.mUVStitches.Length; k++)
                        {
                            if (geom2.mUVStitches[k].Index == j)
                            {
                                geom2Stitch.AddRange(geom2Stitches[k].UV1Vectors);
                                geom2Index = k;
                                break;
                            }
                        }
                        Vector2 geom1UV1 = new Vector2(geom1.GetUV(i, 1)), 
                        geom2UV1 = new Vector2(geom2.GetUV(j, 1));
                        geom1UV1.X = Math.Abs(geom1UV1.X);
                        if (geom2Stitch.IndexOf(geom1UV1) < 0)
                        {
                            geom2Stitch.Add(geom1UV1);
                        }
                        geom2UV1.X = Math.Abs(geom2UV1.X);
                        if (geom1Stitch.IndexOf(geom2UV1) < 0)
                        {
                            geom1Stitch.Add(geom2UV1);
                        }
                        if (geom1Index >= 0)
                        {
                            geom1Stitches[geom1Index] = new UVStitch(i, geom1Stitch.ToArray());
                        }
                        else
                        {
                            geom1Stitches.Add(new UVStitch(i, geom1Stitch.ToArray()));
                        }
                        if (geom2Index >= 0)
                        {
                            geom2Stitches[geom2Index] = new UVStitch(j, geom2Stitch.ToArray());
                        }
                        else
                        {
                            geom2Stitches.Add(new UVStitch(j, geom2Stitch.ToArray()));
                        }
                    }
                }
            }
            geom1.mUVStitches = geom1Stitches.ToArray();
            geom2.mUVStitches = geom2Stitches.ToArray();
        }

        public static void MatchSeamVertices(GEOM geom)
        {
            for (var i = 0; i < geom.mVertexCount; i++)
            {
                for (var j = 0; j < geom.mVertexCount; j++)
                {
                    if (new Vector3(geom.GetPosition(i)).PositionMatches(geom.GetPosition(j)))
                    {
                        if (i == j)
                        {
                            continue;
                        }
                        byte[] sourceBones = geom.GetBones(i),
                        sourceWeights = geom.GetBoneWeights(i);
                        geom.SetBones(j, sourceBones);
                        geom.SetBoneWeights(j, sourceWeights);
                        geom.SetNormal(j, geom.GetNormal(i));
                        UVStitch stitch = null;
                        for (var k = 0; k < geom.mUVStitches.Length; k++)
                        {
                            if (geom.mUVStitches[k].Index == i)
                            {
                                stitch = new UVStitch(geom.mUVStitches[k]);
                                break;
                            }
                        }
                        for (var k = 0; k < geom.mUVStitches.Length; k++)
                        {
                            if (geom.mUVStitches[k].Index == j)
                            {
                                if (stitch != null)
                                {
                                    geom.mUVStitches[k] = stitch;
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static bool TranslateBone(uint boneHash, uint[] boneHashArray, out byte bone)
        {
            for (var i = 0; i < boneHashArray.Length; i++)
            {
                if (boneHash == boneHashArray[i])
                {
                    bone = (byte)i;
                    return true;
                }
            }
            bone = 0;
            return false;
        }

        public void MatchFormats(VertexFormat[] vertexFormatToMatch)
        {
            var uvIndex = 0;
            for (var i = 0; i < vertexFormatToMatch.Length; i++)
            {
                switch (vertexFormatToMatch[i].DataType)
                {
                    case 1:
                        if (mPositions == null || mPositions.Length != mVertexCount)
                        {
                            mPositions = new Position[mVertexCount];
                            for (var j = 0; j < mVertexCount; j++)
                            {
                                mPositions[j] = new Position();
                            }
                        }
                        break;
                    case 2:
                        if (mNormals == null || mNormals.Length != mVertexCount)
                        {
                            mNormals = new Normal[mVertexCount];
                            for (var j = 0; j < mVertexCount; j++)
                            {
                                mNormals[j] = new Normal();
                            }
                        }
                        break;
                    case 3:
                        uvIndex += 1;
                        break;
                    case 4:
                        if (mBones == null || mBones.Length != mVertexCount)
                        {
                            mBones = new Bones[mVertexCount];
                            for (var j = 0; j < mVertexCount; j++)
                            {
                                mBones[j] = new Bones();
                            }
                        }
                        break;
                    case 5:
                        if (mBones == null || mBones.Length != mVertexCount)
                        {
                            mBones = new Bones[mVertexCount];
                            for (var j = 0; j < mVertexCount; j++)
                            {
                                mBones[j] = new Bones();
                            }
                        }
                        break;
                    case 6:
                        if (!HasTangents)
                        {
                            mTangents = new Tangent[mVertexCount];
                        }
                        break;
                    case 7:
                        if (mTags == null || mTags.Length != mVertexCount)
                        {
                            mTags = new TagValue[mVertexCount];
                            for (var j = 0; j < mVertexCount; j++)
                            {
                                mTags[j] = new TagValue();
                            }
                        }
                        break;
                    case 10:
                        if (mVertexIDs == null || mVertexIDs.Length != mVertexCount)
                        {
                            mVertexIDs = new int[mVertexCount];
                            for (var j = 0; j < mVertexCount; j++)
                            {
                                mVertexIDs[j] = -1;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            if (uvIndex > 0 && UVCount != uvIndex)
            {
                var newUV = new UV[uvIndex][];
                for (var i = 0; i < uvIndex; i++)
                {
                    newUV[i] = new UV[mVertexCount];
                    if (HasUVSet(i))
                    {
                        for (var j = 0; j < mVertexCount; j++)
                        {
                            newUV[i][j] = mUVs[i][j];
                        }
                    }
                    else
                    {
                        for (var j = 0; j < mVertexCount; j++)
                        {
                            newUV[i][j] = new UV();
                        }
                    }
                }
                mUVs = newUV;
            }
            var newFormat = new VertexFormat[vertexFormatToMatch.Length];
            for (var i = 0; i < vertexFormatToMatch.Length; i++)
            {
                newFormat[i] = new VertexFormat(vertexFormatToMatch[i].FormatDataType, vertexFormatToMatch[i].FormatSubType, vertexFormatToMatch[i].FormatDataLength);
            }
            mVertexFormats = newFormat;
            mFaceCount = mVertexFormats.Length;
        }

        public void AppendMesh(GEOM meshToAppend)
        {
            if (meshToAppend == null)
            {
                return;
            }
            if (!IsValid)
            {
                throw new MeshException("Not a valid mesh!");
            }
            if (!meshToAppend.IsValid)
            {
                throw new MeshException("The mesh to be appended is not a valid mesh!");
            }
            int uvIndex = 0;
            for (var i = 0; i < VertexFormatList.Length; i++)
            {
                switch (VertexFormatList[i])
                {
                    case 1:
                        var newPositions = new Position[mVertexCount + meshToAppend.mVertexCount];
                        Array.Copy(mPositions, 0, newPositions, 0, mVertexCount);
                        Array.Copy(meshToAppend.mPositions, 0, newPositions, mVertexCount, meshToAppend.mVertexCount);
                        mPositions = newPositions;
                        break;
                    case 2:
                        var newNormals = new Normal[mVertexCount + meshToAppend.mVertexCount];
                        Array.Copy(mNormals, 0, newNormals, 0, mVertexCount);
                        Array.Copy(meshToAppend.mNormals, 0, newNormals, mVertexCount, meshToAppend.mVertexCount);
                        mNormals = newNormals;
                        break;
                    case 3:
                        uvIndex += 1;
                        break;
                    case 4:
                        var newBones = new Bones[mVertexCount + meshToAppend.mVertexCount];
                        var tempBoneHash = new List<uint>(mBoneHashArray);
                        foreach (var h in meshToAppend.mBoneHashArray)
                        {
                            if (tempBoneHash.IndexOf(h) < 0)
                            {
                                tempBoneHash.Add(h);
                            }
                        }
                        var newBoneHashArray = tempBoneHash.ToArray();
                        for (var j = 0; j < mVertexCount; j++)
                        {
                            byte[] oldBones = GetBones(j),
                            oldWeights = GetBoneWeights(j),
                            tempBones = new byte[oldBones.Length];
                            for (var k = 0; k < oldBones.Length; k++)
                            {
                                if (oldWeights[k] > 0 & oldBones[k] < mBoneHashArray.Length)
                                {
                                    tempBones[k] = (byte)Array.IndexOf(newBoneHashArray, mBoneHashArray[oldBones[k]]);
                                }
                                else
                                {
                                    tempBones[k] = 0;
                                }
                            }
                            newBones[j] = new Bones(tempBones, oldWeights);
                        }
                        for (var j = 0; j < meshToAppend.mVertexCount; j++)
                        {
                            byte[] oldBones = meshToAppend.GetBones(j);
                            byte[] oldWeights = meshToAppend.GetBoneWeights(j);
                            byte[] tempBones = new byte[oldBones.Length];
                            for (var k = 0; k < oldBones.Length; k++)
                            {
                                if (oldWeights[k] > 0 & oldBones[k] < meshToAppend.mBoneHashArray.Length)
                                {
                                    tempBones[k] = (byte)Array.IndexOf(newBoneHashArray, meshToAppend.mBoneHashArray[oldBones[k]]);
                                }
                                else
                                {
                                    tempBones[k] = 0;
                                }
                            }
                            newBones[j + mVertexCount] = new Bones(tempBones, oldWeights);
                        }
                        mBones = newBones;
                        mBoneHashArray = newBoneHashArray;
                        mBoneHashCount = newBoneHashArray.Length;
                        break;
                    case 6:
                        var newTan = new Tangent[mVertexCount + meshToAppend.mVertexCount];
                        Array.Copy(mTangents, 0, newTan, 0, mVertexCount);
                        if (meshToAppend.HasTangents)
                        {
                            Array.Copy(meshToAppend.mTangents, 0, newTan, mVertexCount, meshToAppend.mVertexCount);
                        }
                        else
                        {
                            for (var v = mVertexCount; v < newTan.Length; v++)
                            {
                                newTan[v] = new Tangent();
                            }
                        }
                        mTangents = newTan;
                        break;
                    case 7:
                        var newTag = new TagValue[mVertexCount + meshToAppend.mVertexCount];
                        Array.Copy(mTags, 0, newTag, 0, mVertexCount);
                        if (meshToAppend.HasTags)
                        {
                            Array.Copy(meshToAppend.mTags, 0, newTag, mVertexCount, meshToAppend.mVertexCount);
                        }
                        else
                        {
                            for (var v = mVertexCount; v < newTag.Length; v++)
                            {
                                newTag[v] = new TagValue();
                            }
                        }
                        mTags = newTag;
                        break;
                    case 10:
                        var newIDs = new int[mVertexCount + meshToAppend.mVertexCount];
                        Array.Copy(mVertexIDs, 0, newIDs, 0, mVertexCount);
                        if (meshToAppend.HasVertexIDs)
                        {
                            Array.Copy(meshToAppend.mVertexIDs, 0, newIDs, mVertexCount, meshToAppend.mVertexCount);
                        }
                        mVertexIDs = newIDs;
                        break;
                    default:
                        break;
                }
            }
            if (uvIndex > 0)
            {
                var newUV = new UV[uvIndex][];
                for (var i = 0; i < uvIndex; i++)
                {
                    newUV[i] = new UV[mVertexCount + meshToAppend.mVertexCount];
                    for (var j = 0; j < mVertexCount; j++)
                    {
                        newUV[i][j] = mUVs[i][j];
                    }
                    if (meshToAppend.HasUVSet(i))
                    {
                        for (var j = 0; j < meshToAppend.mVertexCount; j++)
                        {
                            newUV[i][j + mVertexCount] = meshToAppend.mUVs[i][j];
                        }
                    }
                    else
                    {
                        for (var j = 0; j < meshToAppend.mVertexCount; j++)
                        {
                            newUV[i][j + mVertexCount] = new UV();
                        }
                    }
                }
                mUVs = newUV;
            }
            var newFaces = new Face[FaceCount + meshToAppend.FaceCount];
            Array.Copy(mFaces, 0, newFaces, 0, FaceCount);
            for (var i = 0; i < meshToAppend.FaceCount; i++)
            {
                newFaces[i + FaceCount] = new Face(meshToAppend.mFaces[i].FacePoint0 + mVertexCount, meshToAppend.mFaces[i].FacePoint1 + mVertexCount, meshToAppend.mFaces[i].FacePoint2 + mVertexCount);
            }
            mFaces = newFaces;
            if (mVersion >= 12)
            {
                var adj0 = new UVStitch[(mUVStitches == null ? 0 : mUVStitches.Length) + (meshToAppend.mUVStitches == null ? 0 : meshToAppend.mUVStitches.Length)];
                if (mUVStitches != null)
                {
                    Array.Copy(mUVStitches, 0, adj0, 0, mUVStitches.Length);
                }
                if (meshToAppend.mUVStitches != null)
                {
                    for (var i = 0; i < meshToAppend.mUVStitches.Length; i++)
                    {
                        adj0[i + mUVStitches.Length] = new UVStitch(meshToAppend.mUVStitches[i]);
                        adj0[i + mUVStitches.Length].Index += mVertexCount;
                    }
                }
                mUVStitches = adj0;
                mUVStitchCount = adj0.Length;
                if (mVersion >= 13)
                {
                    var seam = new SeamStitch[(mSeamStitches == null ? 0 : mSeamStitches.Length) + (meshToAppend.mSeamStitches == null ? 0 : meshToAppend.mSeamStitches.Length)];
                    if (mSeamStitches != null)
                    {
                        Array.Copy(mSeamStitches, 0, seam, 0, mSeamStitches.Length);
                    }
                    if (meshToAppend.mSeamStitches != null)
                    {
                        for (var i = 0; i < meshToAppend.mSeamStitches.Length; i++)
                        {
                            seam[i + mSeamStitches.Length] = new SeamStitch(meshToAppend.mSeamStitches[i]);
                            seam[i + mSeamStitches.Length].Index += (uint)mVertexCount;
                        }
                    }
                    mSeamStitches = seam;
                    mSeamStitchCount = seam.Length;
                }
                var adj1 = new SlotrayIntersection[(mSlotrayIntersections == null ? 0 : mSlotrayIntersections.Length) + (meshToAppend.mSlotrayIntersections == null ? 0 : meshToAppend.mSlotrayIntersections.Length)];
                if (mSlotrayIntersections != null)
                {
                    Array.Copy(mSlotrayIntersections, 0, adj1, 0, mSlotrayIntersections.Length);
                }
                if (meshToAppend.mSlotrayIntersections != null)
                {
                    for (var i = 0; i < meshToAppend.mSlotrayIntersections.Length; i++)
                    {
                        adj1[i + mSlotrayIntersections.Length] = new SlotrayIntersection(meshToAppend.mSlotrayIntersections[i]);
                        int[] f = meshToAppend.mSlotrayIntersections[i].TrianglePointIndices;
                        for (int j = 0; j < f.Length; j++)
                        {
                            f[j] += mVertexCount;
                        }
                        adj1[i + mSlotrayIntersections.Length].TrianglePointIndices = f;
                    }
                }
                mSlotrayIntersections = adj1;
                mSlotCount = adj1.Length;
            }
            mVertexCount += meshToAppend.mVertexCount;
            mFacePointCount += meshToAppend.mFacePointCount;
        }

        public bool HasBlueVertexColor
        {
            get
            {
                for (var i = 0; i < VertexCount; i++)
                {
                    if ((GetTagValue(i) & 0xFF0000) > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void AutoVertexID(GEOM refMesh)
        {
            if (!HasVertexIDs)
            {
                insertVertexIDinFormatList();
                mVertexIDs = new int[VertexCount];
            }
            var refVerts = new Vector3[refMesh.VertexCount];
            for (var i = 0; i < refMesh.VertexCount; i++)
            {
                refVerts[i] = new Vector3(refMesh.GetPosition(i));
            }
            for (var i = 0; i < VertexCount; i++)
            {
                mVertexIDs[i] = refMesh.mVertexIDs[new Vector3(GetPosition(i)).NearestPointIndexSimple(refVerts)];
            }
            CopyFaceMorphs = true;
        }

        public void insertVertexIDinFormatList()
        {
            if (Array.IndexOf(VertexFormatList, 10) >= 0)
            {
                return;
            }
            VertexFormat[] newFormat = new VertexFormat[VertexFormatList.Length + 1];
            if (Array.IndexOf(VertexFormatList, 5) >= 0)
            {
                var index = 0;
                for (var i = 0; i < VertexFormatList.Length; i++)
                {
                    newFormat[index] = mVertexFormats[i];
                    index += 1;
                    if (mVertexFormats[i].FormatDataType == 5)
                    {
                        newFormat[index] = new VertexFormat(10, 4, 4);
                        index += 1;
                    }
                }
            }
            else
            {
                Array.Copy(mVertexFormats, newFormat, VertexFormatList.Length);
                newFormat[VertexFormatList.Length] = new VertexFormat(10, 4, 4);
            }
            mVertexFormats = newFormat;
            mFaceCount += 1;
        }

        public void FixUnusedBones()
        {
            if (mBoneHashArray == null || mBoneHashArray.Length == 0)
            {
                return;
            }
            var usedBones = new List<byte>();
            for (var i = 0; i < mVertexCount; i++)
            {
                byte[] sourceBones = GetBones(i),
                sourceWeights = GetBoneWeights(i);
                for (var j = 0; j < 4; j++)
                {
                    if (sourceWeights[j] > 0 & !(usedBones.IndexOf(sourceBones[j]) >= 0))
                    {
                        usedBones.Add(sourceBones[j]);
                    }
                }
            }
            var usedBoneHash = new List<uint>();
            var oldBoneHash = BoneHashList;
            foreach (byte b in usedBones)
            {
                usedBoneHash.Add(oldBoneHash[b]);
            }
            for (int i = 0; i < mVertexCount; i++)
            {
                byte[] sourceBones = GetBones(i),
                sourceWeights = GetBoneWeights(i),
                bones = new byte[sourceBones.Length];
                for (var j = 0; j < 4; j++)
                {
                    if (sourceWeights[j] > 0)
                    {
                        bones[j] = (byte)usedBoneHash.IndexOf(oldBoneHash[sourceBones[j]]);
                    }
                    else
                    {
                        bones[j] = 0;
                    }
                }
                SetBones(i, bones);
            }
            mBoneHashCount = usedBoneHash.Count;
            mBoneHashArray = usedBoneHash.ToArray();
        }

        public void BoneVectorize(RIG.Bone bone)
        {
            var bonePosition = bone.WorldPosition;
            DeltaPosition = new Vector3[mPositions.Length];
            for (var i = 0; i < mPositions.Length; i++)
            {
                DeltaPosition[i] = (new Vector3(mPositions[i].Coordinates)) - bonePosition;
            }
        }

        public void BoneDeVectorize(RIG.Bone bone, float weight)
        {
            if (weight == 0)
            {
                return;
            }
            var bonePosition = bone.WorldPosition;
            for (var i = 0; i < mPositions.Length; i++)
            {
                var vertWeight = GetBoneWeightForVertex(i, bone.BoneHash);
                if (vertWeight == 0)
                {
                    continue;
                }
                Vector3 newPosition = DeltaPosition[i] + bonePosition,
                oldPosition = new Vector3(mPositions[i].Coordinates);
                oldPosition += (newPosition - oldPosition) * vertWeight * weight;
                mPositions[i] = new Position(oldPosition.Coordinates);
            }
        }

        public void BoneMorpher2(RIG.Bone bone, float weight, Vector3 scale, Quaternion rotation)
        {
            if (weight == 0)
            {
                return;
            }
            var unit = new Vector3(1, 1, 1);
            for (var i = 0; i < mPositions.Length; i++)
            {
                var boneWeight = GetBoneWeightForVertex(i, bone.BoneHash);
                if (boneWeight == 0)
                {
                    continue;
                }
                Vector3 vertexNormal = new Vector3(GetNormal(i)) - bone.WorldPosition,
                weightedScale = (scale * boneWeight * weight) + unit;
                var weightedRotation = rotation * boneWeight * weight;
                DeltaPosition[i] = DeltaPosition[i].Scale(weightedScale);
                DeltaPosition[i] = (weightedRotation * DeltaPosition[i] * weightedRotation.Conjugate()).ToVector3();
                vertexNormal = (weightedRotation * vertexNormal * weightedRotation.Conjugate()).ToVector3();
                vertexNormal += bone.WorldPosition;
                SetNormal(i, vertexNormal.Coordinates);
            }
        }

        public void SetupDeltas()
        {
            DeltaPosition = new Vector3[mPositions.Length];
            for (int i = 0; i < mPositions.Length; i++)
            {
                DeltaPosition[i] = new Vector3();
            }
        }

        public void UpdatePositions()
        {
            for (int i = 0; i < mPositions.Length; i++)
            {
                mPositions[i] = new Position((new Vector3(GetPosition(i)) + DeltaPosition[i]).Coordinates);
            }
        }

        public void BoneMorpher(RIG.Bone bone, float weight, Vector3 offset, Vector3 scale, Quaternion rotation)
        {
            if (weight == 0)
            {
                return;
            }
            var unit = new Vector3(1, 1, 1);
            var boneHashes = new List<uint>();
            bone.Rig.GetDescendants(bone.BoneHash, ref boneHashes);
            for (var i = 0; i < mPositions.Length; i++)
            {
                var boneWeight = GetTotalBoneWeight(i, boneHashes);
                if (boneWeight == 0)
                {
                    continue;
                }
                var adjustedWeight = boneWeight * weight;
                Vector3 vertexNormal = new Vector3(GetNormal(i)),
                vertexPosition = new Vector3(GetPosition(i)) - bone.WorldPosition,
                weightedOffset = offset * adjustedWeight,
                weightedScale = (scale * adjustedWeight) + unit;
                var weightedRotation = rotation * adjustedWeight;
                var weightedTransform = weightedRotation.ToMatrix4D(weightedOffset, weightedScale);
                vertexPosition = weightedTransform * vertexPosition;
                vertexPosition += bone.WorldPosition;
                DeltaPosition[i] += vertexPosition - new Vector3(GetPosition(i));
                var normalTransform = weightedTransform.Inverse().Transpose();
                vertexNormal = (normalTransform * vertexNormal);
                vertexNormal.Normalize();
                SetNormal(i, vertexNormal.Coordinates);
            }
        }

        public int VertexIDSearch(uint vertexID)
        {
            return Array.IndexOf(mVertexIDs, vertexID);
        }

        public void SnapVerticesToHead(GEOM head)
        {
            for (var i = 0; i < mVertexCount; i++)
            {
                var position = new Vector3(GetPosition(i));
                for (var j = 0; j < head.mVertexCount; j++)
                {
                    if (position.PositionClose(head.GetPosition(j)))
                    {
                        SetPosition(i, head.GetPosition(j));
                        SetNormal(i, head.GetNormal(j));
                        SetTagValue(i, GetTagValue(i) & 0xFFFFFF00 | 0x00000063);
                    }
                }
            }
        }

        public void SnapVertices()
        {
            for (var i = 0; i < mVertexCount - 1; i++)
            {
                Vector3 normal = new Vector3(GetNormal(i)),
                position = new Vector3(GetPosition(i));
                for (var j = i + 1; j < mVertexCount; j++)
                {
                    if (position.PositionMatches(GetPosition(j)))
                    {
                        SetPosition(j, position.Coordinates);
                        if (normal.PositionMatches(GetNormal(j)))
                        {
                            SetNormal(j, normal.Coordinates);
                        }
                    }
                }
            }
        }

        public void Clean()
        {
            var indexTrans0 = new int[mVertexCount];
            for (var i = 0; i < indexTrans0.Length; i++)
            {
                indexTrans0[i] = i;
            }
            for (var i = 0; i < mVertexCount - 1; i++)
            {
                if (indexTrans0[i] != i)
                {
                    continue;
                }
                for (var j = i + 1; j < mVertexCount; j++)
                {
                    if (mPositions[i].Equals(mPositions[j]) & mNormals[i].Equals(mNormals[j]))
                    {
                        var match = true;
                        for (var u = 0; u < UVCount; u++)
                        {
                            if (!mUVs[u][i].CloseTo(mUVs[u][j]))
                            {
                                match = false;
                            }
                        }
                        if (match)
                        {
                            indexTrans0[j] = i;
                        }
                    }
                }
            }
            var indexTrans1 = new List<int>();
            for (var i = 0; i < indexTrans0.Length; i++)
            {
                if (indexTrans0[i] == i)
                {
                    indexTrans1.Add(i);
                }
            }
            for (var i = 0; i < indexTrans0.Length; i++)
            {
                indexTrans0[i] = indexTrans1.IndexOf(indexTrans0[i]);
            }
            var newSlots = new List<SlotrayIntersection>();
            for (var i = 0; i < mFaces.Length; i++)
            {
                int facePoint0 = indexTrans0[mFaces[i].FacePoint0],
                facePoint1 = indexTrans0[mFaces[i].FacePoint1],
                facePoint2 = indexTrans0[mFaces[i].FacePoint2];
                for (var j = 0; j < SlotrayAdjustments.Length; j++)
                {
                    int[] slotVertIndices = mSlotrayIntersections[j].TrianglePointIndices;
                    if (mFaces[i].FacePoint0 == slotVertIndices[0] && mFaces[i].FacePoint1 == slotVertIndices[1] && mFaces[i].FacePoint2 == slotVertIndices[2])
                    {
                        var temp = new SlotrayIntersection(mSlotrayIntersections[j]);
                        temp.TrianglePointIndices = new int[]
                            {
                                facePoint0,
                                facePoint1,
                                facePoint2
                            };
                        newSlots.Add(temp);
                    }
                }
                mFaces[i] = new Face(facePoint0, facePoint1, facePoint2);
            }
            SlotrayAdjustments = newSlots.ToArray();
            mSlotCount = newSlots.Count;
            var newPositions = new List<Position>();
            var newNormals = new List<Normal>();
            var newUVs = new List<UV>[UVCount];
            for (int i = 0; i < UVCount; i++)
            {
                newUVs[i] = new List<UV>();
            }
            var newBones = new List<Bones>();
            var newTangents = new List<Tangent>();
            var newTagValues = new List<TagValue>();
            var newIDs = new List<int>();
            for (var i = 0; i < indexTrans1.Count; i++)
            {
                newPositions.Add(new Position(mPositions[indexTrans1[i]]));
                newNormals.Add(new Normal(mNormals[indexTrans1[i]]));
                for (var j = 0; j < UVCount; j++)
                {
                    newUVs[j].Add(new UV(mUVs[j][indexTrans1[i]]));
                }
                newBones.Add(new Bones(mBones[indexTrans1[i]]));
                if (HasTangents) newTangents.Add(new Tangent(mTangents[indexTrans1[i]]));
                if (HasTags) newTagValues.Add(new TagValue(mTags[indexTrans1[i]]));
                if (HasVertexIDs) newIDs.Add(mVertexIDs[indexTrans1[i]]);
            }
            mPositions = newPositions.ToArray();
            mNormals = newNormals.ToArray();
            for (var i = 0; i < UVCount; i++)
            {
                mUVs[i] = newUVs[i].ToArray();
            }
            mBones = newBones.ToArray();
            if (HasTangents)
            {
                mTangents = newTangents.ToArray();
            }
            if (HasTags)
            {
                mTags = newTagValues.ToArray();
            }
            if (HasVertexIDs)
            {
                mVertexIDs = newIDs.ToArray();
            }
            mVertexCount = indexTrans1.Count;
            var newSeamStitches = new List<SeamStitch>();
            for (var i = 0; i < indexTrans1.Count; i++)
            {
                for (var j = 0; j < mSeamStitches.Length; j++)
                {
                    if (indexTrans1[i] == mSeamStitches[j].Index)
                    {
                        var seamStitch = new SeamStitch(mSeamStitches[j]);
                        seamStitch.Index = (uint)i;
                        newSeamStitches.Add(seamStitch);
                    }
                }
            }
            mSeamStitches = newSeamStitches.ToArray();
            mSeamStitchCount = newSeamStitches.Count;
            if (HasUVSet(1))
            {
                AutoUV1Stitches();
            }
        }

        public void AutoUV1Stitches()
        {
            var newStitch = new List<UVStitch>();
            for (var i = 0; i < mVertexCount; i++)
            {
                var position = new Vector3(GetPosition(i));
                var stitches = new List<Vector2>();
                for (var j = 0; j < mVertexCount; j++)
                {
                    if (position.PositionMatches(GetPosition(j)))
                    {
                        var tempUV = GetUV(j, 1);
                        tempUV[0] = Math.Abs(tempUV[0]);
                        var temp = new Vector2(tempUV);
                        if (stitches.IndexOf(temp) < 0)
                        {
                            stitches.Add(temp);
                        }
                    }
                }
                if (stitches.Count > 0) newStitch.Add(new UVStitch(i, stitches.ToArray()));
            }
            mUVStitches = newStitch.ToArray();
            mUVStitchCount = mUVStitches.Length;
        }

        public void AutoSeamStitches(Species species, AgeGender age, AgeGender gender, int lod)
        {
            var seamStitches = new List<SeamStitch>();
            for (var i = 0; i < mVertexCount; i++)
            {
                foreach (var seam in Enum.GetValues(typeof(GEOM.SeamType)))
                {
                    var vertices = GetSeamVertexPositions(species, age, gender, lod, (GEOM.SeamType)seam);
                    for (int v = 0; v < vertices.Length; v++)
                    {
                        if (vertices[v].PositionMatches(mPositions[i].Coordinates))
                        {
                            seamStitches.Add(new SeamStitch(i, (GEOM.SeamType)seam, v, mUVs[0]));
                        }
                    }
                }
            }
            mSeamStitches = seamStitches.ToArray();
            mSeamStitchCount = mSeamStitches.Length;
        }

        public static Vector3[] GetSeamVertexPositions(Species species, AgeGender age, AgeGender gender, int lod, GEOM.SeamType seam)
        {
            int ageGenderIndex,
            speciesIndex = (int)species - 1;
            if (species == Species.Human)
            {
                if (age >= AgeGender.Teen)
                {
                    if (gender == AgeGender.Male)
                    {
                        ageGenderIndex = 0;
                    }
                    else
                    {
                        ageGenderIndex = 1;
                    }
                }
                else if (age == AgeGender.Child)
                {
                    ageGenderIndex = 2;
                }
                else
                {
                    ageGenderIndex = 3;
                }
            }
            else if (species == Species.LittleDog && age <= AgeGender.Child)
            {
                speciesIndex = 1;       //little dogs only have adult form so go to dog/child
                ageGenderIndex = 1;
            }
            else ageGenderIndex = age > AgeGender.Child ? 0 : 1;
            return MeshSeamVertices[speciesIndex][ageGenderIndex][lod][(int)seam];
        }

        public static Vector3[][][][][] SetupSeamVertexPositions()
        {
            var meshSeamVertices = new Vector3[4][][][][];   //indices: species, age/gender, lod, seam, verts
            //  dimension 0: 0 = human, 1 = dog, 2 = cat, 3 = little dog
            //  dimension 1: human: 0 = male, 1 = female, 2 = child, 3 = toddler; little dog: 0 = adult; dog/cat: 0 = adult, 1 = child
            meshSeamVertices[0] = new Vector3[4][][][];        //ageGenders
            meshSeamVertices[0][0] = new Vector3[4][][];         //Adult Male
            meshSeamVertices[0][0][0] = new Vector3[7][];         //Adult Male LOD0 seams
            meshSeamVertices[0][0][0][0] = new Vector3[] { new Vector3(0.10318f, 0.16812f, 0.01464f), new Vector3(0.08145f, 0.16812f, 0.006759f), new Vector3(0.12142f, 0.16812f, 0.002309f), new Vector3(0.1301f, 0.16812f, -0.02376f), new Vector3(0.12235f, 0.16812f, -0.04518f), new Vector3(0.10225f, 0.16812f, -0.06237f), new Vector3(0.06959f, 0.16812f, -0.01289f), new Vector3(0.07157f, 0.16812f, -0.03863f), new Vector3(0.08476f, 0.16812f, -0.05846f), new Vector3(-0.10318f, 0.168119f, 0.01464f), new Vector3(-0.08145f, 0.168119f, 0.00676f), new Vector3(-0.12142f, 0.168119f, 0.00231f), new Vector3(-0.1301f, 0.168119f, -0.02376f), new Vector3(-0.12235f, 0.168119f, -0.04518f), new Vector3(-0.10225f, 0.168119f, -0.06237f), new Vector3(-0.06959f, 0.168119f, -0.01289f), new Vector3(-0.07157f, 0.168119f, -0.03863f), new Vector3(-0.08476f, 0.168119f, -0.05846f) };     //Ankles
            meshSeamVertices[0][0][0][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][0][0][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][0][0][3] = new Vector3[] { new Vector3(0.04994f, 1.65732f, -0.04331f), new Vector3(0.05748f, 1.65212f, -0.02185f), new Vector3(0.02016f, 1.62796f, 0.02991f), new Vector3(0f, 1.62329f, 0.03646f), new Vector3(0.02658f, 1.65984f, -0.06291001f), new Vector3(0.04268f, 1.63725f, 0.01346f), new Vector3(0.03073f, 1.63173f, 0.02297f), new Vector3(0.05114f, 1.64436f, -0.00103f), new Vector3(0f, 1.66001f, -0.07078f), new Vector3(-0.04994f, 1.65732f, -0.04331f), new Vector3(-0.05748f, 1.65212f, -0.02185f), new Vector3(-0.02016f, 1.62796f, 0.02991f), new Vector3(-0.02658f, 1.65984f, -0.06291001f), new Vector3(-0.04268f, 1.63725f, 0.01346f), new Vector3(-0.03074f, 1.63173f, 0.02296f), new Vector3(-0.05114f, 1.64436f, -0.00103f) };     //Neck
            meshSeamVertices[0][0][0][4] = new Vector3[0];     //Waist
            meshSeamVertices[0][0][0][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[0][0][0][6] = new Vector3[] { new Vector3(0.13477f, 1.10102f, 0.05168f), new Vector3(0.09531f, 1.09588f, 0.08789f), new Vector3(0.0283f, 1.09001f, 0.11046f), new Vector3(0.06203f, 1.0929f, 0.10109f), new Vector3(0f, 1.08875f, 0.11338f), new Vector3(0.0537f, 1.10483f, -0.07900001f), new Vector3(0.02362f, 1.10363f, -0.08015f), new Vector3(0.12888f, 1.10734f, -0.03361f), new Vector3(0.14252f, 1.10484f, 0.00736f), new Vector3(0.10903f, 1.10822f, -0.05758001f), new Vector3(0.08691f, 1.10752f, -0.0717f), new Vector3(0f, 1.10245f, -0.07855f), new Vector3(-0.13477f, 1.10102f, 0.05168f), new Vector3(-0.09531f, 1.09588f, 0.08789f), new Vector3(-0.0283f, 1.09001f, 0.11046f), new Vector3(-0.06203f, 1.0929f, 0.10109f), new Vector3(-0.0537f, 1.10483f, -0.07900001f), new Vector3(-0.02362f, 1.10363f, -0.08015f), new Vector3(-0.12888f, 1.10734f, -0.03361f), new Vector3(-0.14252f, 1.10484f, 0.00736f), new Vector3(-0.10903f, 1.10822f, -0.05758001f), new Vector3(-0.08691f, 1.10752f, -0.0717f) };     //WaistAdultMale
            meshSeamVertices[0][0][1] = new Vector3[7][];         //Adult Male LOD1 seams
            meshSeamVertices[0][0][1][0] = new Vector3[] { new Vector3(0.10318f, 0.16812f, 0.01464f), new Vector3(0.08145f, 0.16812f, 0.006759f), new Vector3(0.12142f, 0.16812f, 0.002309f), new Vector3(0.1301f, 0.16812f, -0.02376f), new Vector3(0.12235f, 0.16812f, -0.04518f), new Vector3(0.09351f, 0.16812f, -0.06042f), new Vector3(0.06959f, 0.16812f, -0.01289f), new Vector3(0.07157f, 0.16812f, -0.03863f), new Vector3(-0.10318f, 0.168119f, 0.01464f), new Vector3(-0.08145f, 0.168119f, 0.00676f), new Vector3(-0.12142f, 0.168119f, 0.00231f), new Vector3(-0.1301f, 0.168119f, -0.02376f), new Vector3(-0.12235f, 0.168119f, -0.04518f), new Vector3(-0.09351f, 0.168119f, -0.06042f), new Vector3(-0.06959f, 0.168119f, -0.01289f), new Vector3(-0.07157f, 0.168119f, -0.03863f) };     //Ankles
            meshSeamVertices[0][0][1][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][0][1][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][0][1][3] = new Vector3[] { new Vector3(0.04994f, 1.65732f, -0.04331f), new Vector3(0.05748f, 1.65212f, -0.02185f), new Vector3(0.02016f, 1.62796f, 0.02991f), new Vector3(0f, 1.62329f, 0.03646f), new Vector3(0.02658f, 1.65984f, -0.06291001f), new Vector3(0.04268f, 1.63725f, 0.01346f), new Vector3(0.03074f, 1.63173f, 0.02297f), new Vector3(0.05114f, 1.64436f, -0.00103f), new Vector3(0f, 1.66001f, -0.07078f), new Vector3(-0.04994f, 1.65732f, -0.04331f), new Vector3(-0.05748f, 1.65212f, -0.02185f), new Vector3(-0.02016f, 1.62796f, 0.02991f), new Vector3(-0.02658f, 1.65984f, -0.06291001f), new Vector3(-0.04268f, 1.63725f, 0.01346f), new Vector3(-0.03074f, 1.63173f, 0.02296f), new Vector3(-0.05114f, 1.64436f, -0.00103f) };     //Neck
            meshSeamVertices[0][0][1][4] = new Vector3[0];     //Waist
            meshSeamVertices[0][0][1][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[0][0][1][6] = new Vector3[] { new Vector3(0.13477f, 1.10102f, 0.05168f), new Vector3(0.07867001f, 1.09439f, 0.09449001f), new Vector3(0f, 1.08875f, 0.11338f), new Vector3(0.03866f, 1.10423f, -0.07958f), new Vector3(0.12888f, 1.10734f, -0.03361f), new Vector3(0.14252f, 1.10484f, 0.00736f), new Vector3(0.09797f, 1.10787f, -0.06464f), new Vector3(0f, 1.10245f, -0.07855f), new Vector3(-0.13477f, 1.10102f, 0.05168f), new Vector3(-0.07867001f, 1.09439f, 0.09449001f), new Vector3(-0.03866f, 1.10423f, -0.07958f), new Vector3(-0.12888f, 1.10734f, -0.03361f), new Vector3(-0.14252f, 1.10484f, 0.00736f), new Vector3(-0.09797f, 1.10787f, -0.06464f) };     //WaistAdultMale
            meshSeamVertices[0][0][2] = new Vector3[7][];         //Adult Male LOD2 seams
            meshSeamVertices[0][0][2][0] = new Vector3[] { new Vector3(0.1123f, 0.16812f, 0.008479001f), new Vector3(0.08145f, 0.16812f, 0.006759f), new Vector3(0.1301f, 0.16812f, -0.02376f), new Vector3(0.12235f, 0.16812f, -0.04518f), new Vector3(0.08254f, 0.16812f, -0.04952f), new Vector3(0.06959f, 0.16812f, -0.01289f), new Vector3(-0.1123f, 0.168119f, 0.00848f), new Vector3(-0.08145f, 0.168119f, 0.00676f), new Vector3(-0.1301f, 0.168119f, -0.02376f), new Vector3(-0.12235f, 0.168119f, -0.04518f), new Vector3(-0.08254f, 0.168119f, -0.04952f), new Vector3(-0.06959f, 0.168119f, -0.01289f) };     //Ankles
            meshSeamVertices[0][0][2][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][0][2][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][0][2][3] = new Vector3[] { new Vector3(0.04994f, 1.65732f, -0.04331f), new Vector3(0.05748f, 1.65212f, -0.02185f), new Vector3(0.02016f, 1.62796f, 0.02991f), new Vector3(0f, 1.62329f, 0.03646f), new Vector3(0.02658f, 1.65984f, -0.06291001f), new Vector3(0.04268f, 1.63725f, 0.01346f), new Vector3(0.03074f, 1.63173f, 0.02297f), new Vector3(0.05114f, 1.64436f, -0.00103f), new Vector3(0f, 1.66001f, -0.07078f), new Vector3(-0.04994f, 1.65732f, -0.04331f), new Vector3(-0.05748f, 1.65212f, -0.02185f), new Vector3(-0.02016f, 1.62796f, 0.02991f), new Vector3(-0.02658f, 1.65984f, -0.06291001f), new Vector3(-0.04268f, 1.63725f, 0.01346f), new Vector3(-0.03074f, 1.63173f, 0.02296f), new Vector3(-0.05114f, 1.64436f, -0.00103f) };     //Neck
            meshSeamVertices[0][0][2][4] = new Vector3[0];     //Waist
            meshSeamVertices[0][0][2][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[0][0][2][6] = new Vector3[] { new Vector3(0.13477f, 1.10102f, 0.05168f), new Vector3(0.07867001f, 1.09439f, 0.09449001f), new Vector3(0f, 1.08875f, 0.11338f), new Vector3(0.06831001f, 1.10605f, -0.07211f), new Vector3(0.12888f, 1.10734f, -0.03361f), new Vector3(0.14252f, 1.10484f, 0.00736f), new Vector3(0f, 1.10245f, -0.07855f), new Vector3(-0.13477f, 1.10102f, 0.05168f), new Vector3(-0.07867001f, 1.09439f, 0.09449001f), new Vector3(-0.06831001f, 1.10605f, -0.07211f), new Vector3(-0.12888f, 1.10734f, -0.03361f), new Vector3(-0.14252f, 1.10484f, 0.00736f) };     //WaistAdultMale
            meshSeamVertices[0][0][3] = new Vector3[7][];         //Adult Male LOD3 seams
            meshSeamVertices[0][0][3][0] = new Vector3[] { new Vector3(0.10318f, 0.16812f, 0.01464f), new Vector3(0.1301f, 0.16812f, -0.02376f), new Vector3(0.09351f, 0.16812f, -0.06042f), new Vector3(0.06959f, 0.16812f, -0.01289f), new Vector3(-0.10318f, 0.168119f, 0.01464f), new Vector3(-0.1301f, 0.168119f, -0.02376f), new Vector3(-0.09351f, 0.168119f, -0.06042f), new Vector3(-0.06959f, 0.168119f, -0.01289f) };     //Ankles
            meshSeamVertices[0][0][3][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][0][3][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][0][3][3] = new Vector3[] { new Vector3(0.05748f, 1.65212f, -0.02185f), new Vector3(0f, 1.62329f, 0.03646f), new Vector3(0.03826f, 1.65858f, -0.05311f), new Vector3(0.03074f, 1.63173f, 0.02297f), new Vector3(0f, 1.66001f, -0.07078f), new Vector3(-0.05748f, 1.65212f, -0.02185f), new Vector3(-0.03826f, 1.65858f, -0.05311f), new Vector3(-0.03074f, 1.63173f, 0.02296f) };     //Neck
            meshSeamVertices[0][0][3][4] = new Vector3[0];     //Waist
            meshSeamVertices[0][0][3][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[0][0][3][6] = new Vector3[] { new Vector3(0.10672f, 1.09771f, 0.07308f), new Vector3(0f, 1.08875f, 0.11338f), new Vector3(0.0986f, 1.1067f, -0.05286f), new Vector3(0.14252f, 1.10484f, 0.00736f), new Vector3(0f, 1.10245f, -0.07855f), new Vector3(-0.10672f, 1.09771f, 0.07308f), new Vector3(-0.0986f, 1.1067f, -0.05286f), new Vector3(-0.14252f, 1.10484f, 0.00736f) };     //WaistAdultMale
            meshSeamVertices[0][1] = new Vector3[4][][];         //Adult Female
            meshSeamVertices[0][1][0] = new Vector3[7][];         //Adult Female LOD0 seams
            meshSeamVertices[0][1][0][0] = new Vector3[] { new Vector3(0.10061f, 0.17831f, 0.01385f), new Vector3(0.08411001f, 0.17831f, 0.01062f), new Vector3(0.11955f, 0.17831f, -0.00055f), new Vector3(0.12174f, 0.17831f, -0.02315f), new Vector3(0.11393f, 0.17831f, -0.04578f), new Vector3(0.1008f, 0.17831f, -0.05814f), new Vector3(0.07353f, 0.17831f, -0.01651f), new Vector3(0.07809f, 0.17831f, -0.04168f), new Vector3(0.08404f, 0.17831f, -0.05354f), new Vector3(-0.10061f, 0.17831f, 0.01385f), new Vector3(-0.08411001f, 0.17831f, 0.01062f), new Vector3(-0.11955f, 0.17831f, -0.00055f), new Vector3(-0.12174f, 0.17831f, -0.02315f), new Vector3(-0.11393f, 0.17831f, -0.04578f), new Vector3(-0.10078f, 0.17831f, -0.05815f), new Vector3(-0.07353f, 0.17831f, -0.01651f), new Vector3(-0.07809f, 0.17831f, -0.04168f), new Vector3(-0.08404f, 0.17831f, -0.05354f) };     //Ankles
            meshSeamVertices[0][1][0][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][1][0][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][1][0][3] = new Vector3[] { new Vector3(0.04228f, 1.65728f, -0.03741f), new Vector3(0.04676f, 1.65358f, -0.01843f), new Vector3(0.01541f, 1.62769f, 0.02782f), new Vector3(0f, 1.62476f, 0.03136f), new Vector3(0.02362f, 1.65786f, -0.05106f), new Vector3(0.03554f, 1.6385f, 0.013f), new Vector3(0.02586f, 1.6321f, 0.02275f), new Vector3(0.04565f, 1.64751f, -0.00218f), new Vector3(0f, 1.65824f, -0.05823f), new Vector3(-0.04228f, 1.65728f, -0.03741f), new Vector3(-0.04676f, 1.65358f, -0.01843f), new Vector3(-0.01541f, 1.62769f, 0.02782f), new Vector3(-0.02361f, 1.65786f, -0.05106f), new Vector3(-0.03554f, 1.6385f, 0.013f), new Vector3(-0.02586f, 1.6321f, 0.02275f), new Vector3(-0.04565f, 1.64751f, -0.00218f) };     //Neck
            meshSeamVertices[0][1][0][4] = new Vector3[0];     //Waist
            meshSeamVertices[0][1][0][5] = new Vector3[] { new Vector3(0f, 1.16153f, 0.10832f), new Vector3(0f, 1.17486f, -0.05726f), new Vector3(0.11117f, 1.17097f, 0.05975f), new Vector3(0.08326f, 1.16772f, 0.08886f), new Vector3(0.02036f, 1.1624f, 0.10652f), new Vector3(0.05003f, 1.16448f, 0.10046f), new Vector3(0.11914f, 1.17389f, 0.01728f), new Vector3(0.11197f, 1.17412f, -0.01527f), new Vector3(0.09584f, 1.17388f, -0.04124f), new Vector3(0.07065f, 1.1729f, -0.05077f), new Vector3(0.0456f, 1.17315f, -0.05503f), new Vector3(0.01515f, 1.17431f, -0.05672f), new Vector3(-0.11117f, 1.17097f, 0.05975f), new Vector3(-0.08326f, 1.16772f, 0.08886f), new Vector3(-0.02036f, 1.1624f, 0.10652f), new Vector3(-0.05003f, 1.16448f, 0.10046f), new Vector3(-0.11914f, 1.17389f, 0.01728f), new Vector3(-0.11197f, 1.17412f, -0.01527f), new Vector3(-0.09584f, 1.17388f, -0.04124f), new Vector3(-0.07065f, 1.1729f, -0.05077f), new Vector3(-0.0456f, 1.17315f, -0.05503f), new Vector3(-0.01515f, 1.17431f, -0.05672f) };     //WaistAdultFemale
            meshSeamVertices[0][1][0][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[0][1][1] = new Vector3[7][];         //Adult Female LOD1 seams
            meshSeamVertices[0][1][1][0] = new Vector3[] { new Vector3(0.10061f, 0.17831f, 0.01385f), new Vector3(0.08411001f, 0.17831f, 0.01062f), new Vector3(0.11955f, 0.17831f, -0.00055f), new Vector3(0.12174f, 0.17831f, -0.02315f), new Vector3(0.11393f, 0.17831f, -0.04578f), new Vector3(0.09242f, 0.17831f, -0.05584f), new Vector3(0.07353f, 0.17831f, -0.01651f), new Vector3(0.07809f, 0.17831f, -0.04168f), new Vector3(-0.10061f, 0.17831f, 0.01385f), new Vector3(-0.08411001f, 0.17831f, 0.01062f), new Vector3(-0.11955f, 0.17831f, -0.00055f), new Vector3(-0.12174f, 0.17831f, -0.02315f), new Vector3(-0.11393f, 0.17831f, -0.04578f), new Vector3(-0.09241f, 0.17831f, -0.05585f), new Vector3(-0.07353f, 0.17831f, -0.01651f), new Vector3(-0.07809f, 0.17831f, -0.04168f) };     //Ankles
            meshSeamVertices[0][1][1][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][1][1][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][1][1][3] = new Vector3[] { new Vector3(0.04228f, 1.65729f, -0.03741f), new Vector3(0.04676f, 1.65359f, -0.01843f), new Vector3(0.01541f, 1.6277f, 0.02782f), new Vector3(0f, 1.62477f, 0.03136f), new Vector3(0.02362f, 1.65786f, -0.05105f), new Vector3(0.03554f, 1.63851f, 0.013f), new Vector3(0.02586f, 1.63211f, 0.02275f), new Vector3(0.04565f, 1.64751f, -0.00218f), new Vector3(0f, 1.65824f, -0.05822f), new Vector3(-0.04228f, 1.65729f, -0.03741f), new Vector3(-0.04676f, 1.65359f, -0.01843f), new Vector3(-0.01541f, 1.6277f, 0.02782f), new Vector3(-0.02361f, 1.65786f, -0.05105f), new Vector3(-0.03554f, 1.63851f, 0.013f), new Vector3(-0.02586f, 1.63211f, 0.02275f), new Vector3(-0.04565f, 1.64751f, -0.00218f) };     //Neck
            meshSeamVertices[0][1][1][4] = new Vector3[0];     //Waist
            meshSeamVertices[0][1][1][5] = new Vector3[] { new Vector3(0f, 1.16153f, 0.10832f), new Vector3(0f, 1.17486f, -0.05726f), new Vector3(0.11117f, 1.17097f, 0.05975f), new Vector3(0.06664f, 1.1661f, 0.09466001f), new Vector3(0.11914f, 1.17389f, 0.01728f), new Vector3(0.11197f, 1.17412f, -0.01527f), new Vector3(0.08324f, 1.17339f, -0.04601f), new Vector3(0.0456f, 1.17315f, -0.05503f), new Vector3(-0.11117f, 1.17097f, 0.05975f), new Vector3(-0.06664f, 1.1661f, 0.09466001f), new Vector3(-0.11914f, 1.17389f, 0.01728f), new Vector3(-0.11197f, 1.17412f, -0.01527f), new Vector3(-0.08324f, 1.17339f, -0.04601f), new Vector3(-0.0456f, 1.17315f, -0.05503f) };     //WaistAdultFemale
            meshSeamVertices[0][1][1][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[0][1][2] = new Vector3[7][];         //Adult Female LOD2 seams
            meshSeamVertices[0][1][2][0] = new Vector3[] { new Vector3(0.11008f, 0.17831f, 0.00665f), new Vector3(0.08411001f, 0.17831f, 0.01062f), new Vector3(0.12174f, 0.17831f, -0.02315f), new Vector3(0.11393f, 0.17831f, -0.04578f), new Vector3(0.08525001f, 0.17831f, -0.04876f), new Vector3(0.07353f, 0.17831f, -0.01651f), new Vector3(-0.11008f, 0.17831f, 0.00665f), new Vector3(-0.08411001f, 0.17831f, 0.01062f), new Vector3(-0.12174f, 0.17831f, -0.02315f), new Vector3(-0.11393f, 0.17831f, -0.04578f), new Vector3(-0.08525001f, 0.17831f, -0.04876f), new Vector3(-0.07353f, 0.17831f, -0.01651f) };     //Ankles
            meshSeamVertices[0][1][2][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][1][2][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][1][2][3] = new Vector3[] { new Vector3(0.04228f, 1.65729f, -0.03742f), new Vector3(0.04676f, 1.65358f, -0.01844f), new Vector3(0.01541f, 1.6277f, 0.02782f), new Vector3(0f, 1.62477f, 0.03136f), new Vector3(0.02362f, 1.65786f, -0.05105f), new Vector3(0.03554f, 1.6385f, 0.013f), new Vector3(0.02586f, 1.63212f, 0.02275f), new Vector3(0.04565f, 1.64751f, -0.00218f), new Vector3(0f, 1.65824f, -0.05822f), new Vector3(-0.04228f, 1.65729f, -0.03742f), new Vector3(-0.04676f, 1.65358f, -0.01844f), new Vector3(-0.01541f, 1.6277f, 0.02782f), new Vector3(-0.02361f, 1.65786f, -0.05105f), new Vector3(-0.03554f, 1.63851f, 0.013f), new Vector3(-0.02586f, 1.63212f, 0.02275f), new Vector3(-0.04565f, 1.64751f, -0.00218f) };     //Neck
            meshSeamVertices[0][1][2][4] = new Vector3[0];     //Waist
            meshSeamVertices[0][1][2][5] = new Vector3[] { new Vector3(0f, 1.16153f, 0.10832f), new Vector3(0f, 1.17486f, -0.05726f), new Vector3(0.08891001f, 1.16853f, 0.07720001f), new Vector3(0.11914f, 1.17389f, 0.01728f), new Vector3(0.11197f, 1.17412f, -0.01527f), new Vector3(0.06442f, 1.17327f, -0.05052f), new Vector3(-0.08891001f, 1.16853f, 0.07720001f), new Vector3(-0.11914f, 1.17389f, 0.01728f), new Vector3(-0.11197f, 1.17412f, -0.01527f), new Vector3(-0.06442f, 1.17327f, -0.05052f) };     //WaistAdultFemale
            meshSeamVertices[0][1][2][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[0][1][3] = new Vector3[7][];         //Adult Female LOD3 seams
            meshSeamVertices[0][1][3][0] = new Vector3[] { new Vector3(0.10061f, 0.17831f, 0.01385f), new Vector3(0.12174f, 0.17831f, -0.02315f), new Vector3(0.09242f, 0.17831f, -0.05584f), new Vector3(0.07353f, 0.17831f, -0.01651f), new Vector3(-0.10061f, 0.17831f, 0.01385f), new Vector3(-0.12174f, 0.17831f, -0.02315f), new Vector3(-0.09241f, 0.17831f, -0.05585f), new Vector3(-0.07353f, 0.17831f, -0.01651f) };     //Ankles
            meshSeamVertices[0][1][3][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][1][3][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][1][3][3] = new Vector3[] { new Vector3(0.04676f, 1.65358f, -0.01844f), new Vector3(0f, 1.62477f, 0.03136f), new Vector3(0.03295f, 1.65757f, -0.04423f), new Vector3(0.02583f, 1.63211f, 0.02276f), new Vector3(0f, 1.65824f, -0.05822f), new Vector3(-0.04676f, 1.65358f, -0.01844f), new Vector3(-0.03295f, 1.65757f, -0.04423f), new Vector3(-0.02586f, 1.63212f, 0.02275f) };     //Neck
            meshSeamVertices[0][1][3][4] = new Vector3[0];     //Waist
            meshSeamVertices[0][1][3][5] = new Vector3[] { new Vector3(0f, 1.16153f, 0.10832f), new Vector3(0f, 1.17486f, -0.05726f), new Vector3(0.08891001f, 1.16853f, 0.07720001f), new Vector3(0.11914f, 1.17389f, 0.01728f), new Vector3(0.0882f, 1.17369f, -0.03289f), new Vector3(-0.08891001f, 1.16853f, 0.07720001f), new Vector3(-0.11914f, 1.17389f, 0.01728f), new Vector3(-0.0882f, 1.17369f, -0.03289f) };     //WaistAdultFemale
            meshSeamVertices[0][1][3][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[0][2] = new Vector3[4][][];         //Child
            meshSeamVertices[0][2][0] = new Vector3[7][];         //Child LOD0 seams
            meshSeamVertices[0][2][0][0] = new Vector3[] { new Vector3(0.07243f, 0.11592f, 0.01694f), new Vector3(0.05307f, 0.11592f, 0.0112f), new Vector3(0.09189f, 0.11592f, 0.00385f), new Vector3(0.0952f, 0.11592f, -0.01715f), new Vector3(0.08587f, 0.11592f, -0.03915f), new Vector3(0.07431f, 0.11592f, -0.04292f), new Vector3(0.04333f, 0.11592f, -0.00869f), new Vector3(0.04767f, 0.11593f, -0.03418f), new Vector3(0.06141f, 0.11592f, -0.04133f), new Vector3(-0.05307f, 0.11592f, 0.0112f), new Vector3(-0.07243f, 0.11592f, 0.01694f), new Vector3(-0.09189f, 0.11592f, 0.00385f), new Vector3(-0.0952f, 0.11592f, -0.01715f), new Vector3(-0.08587f, 0.11592f, -0.03915f), new Vector3(-0.07431f, 0.11592f, -0.04292f), new Vector3(-0.04333f, 0.11592f, -0.00869f), new Vector3(-0.04767f, 0.11593f, -0.03418f), new Vector3(-0.06141f, 0.11592f, -0.04133f) };     //Ankles
            meshSeamVertices[0][2][0][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][2][0][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][2][0][3] = new Vector3[] { new Vector3(-0.03752f, 1.12267f, -0.03273f), new Vector3(-0.02065f, 1.12468f, -0.04379001f), new Vector3(0f, 1.12563f, -0.04968f), new Vector3(0.02065f, 1.12477f, -0.04379001f), new Vector3(0.03752f, 1.12268f, -0.03273f), new Vector3(-0.04215f, 1.11826f, -0.01802f), new Vector3(-0.03877f, 1.11206f, -0.00206f), new Vector3(-0.03276f, 1.10712f, 0.00977f), new Vector3(-0.01436f, 1.09967f, 0.02605f), new Vector3(0f, 1.09771f, 0.029229f), new Vector3(-0.02296f, 1.10289f, 0.01801f), new Vector3(0.04215f, 1.11824f, -0.01801f), new Vector3(0.03877f, 1.11205f, -0.00205f), new Vector3(0.03276f, 1.10712f, 0.00977f), new Vector3(0.01436f, 1.09965f, 0.02605f), new Vector3(0.02295f, 1.10288f, 0.01802f) };     //Neck
            meshSeamVertices[0][2][0][4] = new Vector3[] { new Vector3(0.0914f, 0.7533001f, 0.03828f), new Vector3(0.06577f, 0.7498001f, 0.06943001f), new Vector3(0.01949f, 0.74579f, 0.08594f), new Vector3(0.04228f, 0.74777f, 0.08012f), new Vector3(0f, 0.74493f, 0.08794f), new Vector3(0.03662f, 0.7558801f, -0.06594001f), new Vector3(0.01616f, 0.7548701f, -0.06698f), new Vector3(0.09703f, 0.75579f, 0.00505f), new Vector3(0.08787f, 0.75756f, -0.02397f), new Vector3(0.07434f, 0.75819f, -0.04569f), new Vector3(0.05926f, 0.75773f, -0.05923f), new Vector3(0f, 0.75421f, -0.06595f), new Vector3(-0.0914f, 0.7533001f, 0.03828f), new Vector3(-0.06577f, 0.7498001f, 0.06944f), new Vector3(-0.04228f, 0.74777f, 0.08014001f), new Vector3(-0.01949f, 0.74579f, 0.08594f), new Vector3(-0.01616f, 0.7548701f, -0.06698f), new Vector3(-0.03662f, 0.7558801f, -0.06594001f), new Vector3(-0.09703f, 0.75579f, 0.00505f), new Vector3(-0.08787f, 0.75756f, -0.02397f), new Vector3(-0.07434f, 0.7582f, -0.04569f), new Vector3(-0.05926f, 0.75773f, -0.05923f) };     //Waist
            meshSeamVertices[0][2][0][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[0][2][0][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[0][2][1] = new Vector3[7][];         //Child LOD1 seams
            meshSeamVertices[0][2][1][0] = new Vector3[] { new Vector3(0.053066f, 0.115924f, 0.011202f), new Vector3(0.043329f, 0.115923f, -0.008687f), new Vector3(0.072431f, 0.115924f, 0.016942f), new Vector3(0.09188901f, 0.115924f, 0.003849f), new Vector3(0.095204f, 0.115924f, -0.017147f), new Vector3(0.08587401f, 0.115924f, -0.039149f), new Vector3(0.067858f, 0.115924f, -0.042117f), new Vector3(0.047673f, 0.115932f, -0.034183f), new Vector3(-0.053066f, 0.115924f, 0.011202f), new Vector3(-0.043329f, 0.115923f, -0.008687f), new Vector3(-0.072431f, 0.115924f, 0.016942f), new Vector3(-0.09188901f, 0.115924f, 0.003849f), new Vector3(-0.095204f, 0.115924f, -0.017147f), new Vector3(-0.08587401f, 0.115924f, -0.039149f), new Vector3(-0.067858f, 0.115924f, -0.042117f), new Vector3(-0.047673f, 0.115932f, -0.034183f) };     //Ankles
            meshSeamVertices[0][2][1][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][2][1][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][2][1][3] = new Vector3[] { new Vector3(-0.03752f, 1.12268f, -0.03273f), new Vector3(-0.02065f, 1.12477f, -0.04379001f), new Vector3(0f, 1.12563f, -0.04968f), new Vector3(0.02065f, 1.12477f, -0.04379001f), new Vector3(0.03752f, 1.12268f, -0.03273f), new Vector3(-0.03276f, 1.10712f, 0.00977f), new Vector3(-0.03877f, 1.11205f, -0.002049f), new Vector3(-0.04215f, 1.11824f, -0.01801f), new Vector3(0.04215f, 1.11824f, -0.01801f), new Vector3(0.03877f, 1.11205f, -0.002049f), new Vector3(0.03276f, 1.10712f, 0.00977f), new Vector3(0f, 1.09771f, 0.02923f), new Vector3(-0.01436f, 1.09965f, 0.02605f), new Vector3(-0.02295f, 1.10288f, 0.01802f), new Vector3(0.01436f, 1.09965f, 0.02605f), new Vector3(0.02295f, 1.10288f, 0.01802f) };     //Neck
            meshSeamVertices[0][2][1][4] = new Vector3[] { new Vector3(0.0914f, 0.7533001f, 0.03828f), new Vector3(0.05402f, 0.74878f, 0.07478f), new Vector3(0f, 0.74493f, 0.08794f), new Vector3(0.09703f, 0.75579f, 0.00505f), new Vector3(0.08787f, 0.75756f, -0.02397f), new Vector3(0.0668f, 0.75796f, -0.05246f), new Vector3(0.02639f, 0.75537f, -0.06646f), new Vector3(0f, 0.75421f, -0.06595f), new Vector3(-0.0914f, 0.7533001f, 0.03828f), new Vector3(-0.05402f, 0.74878f, 0.07478f), new Vector3(-0.09703f, 0.75579f, 0.00505f), new Vector3(-0.08787f, 0.75756f, -0.02397f), new Vector3(-0.0668f, 0.75796f, -0.05246f), new Vector3(-0.02639f, 0.75537f, -0.06646f) };     //Waist
            meshSeamVertices[0][2][1][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[0][2][1][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[0][2][2] = new Vector3[7][];         //Child LOD2 seams
            meshSeamVertices[0][2][2][0] = new Vector3[] { new Vector3(0.053066f, 0.115924f, 0.016333f), new Vector3(), new Vector3(0.043329f, 0.115923f, -0.007374f), new Vector3(0.08216001f, 0.115924f, 0.015517f), new Vector3(0.095204f, 0.115924f, -0.017455f), new Vector3(0.08587401f, 0.115924f, -0.041221f), new Vector3(0.057831f, 0.115938f, -0.038038f), new Vector3(-0.053066f, 0.115924f, 0.016333f), new Vector3(-0.043329f, 0.115923f, -0.007374f), new Vector3(-0.08216001f, 0.115924f, 0.015517f), new Vector3(-0.095204f, 0.115924f, -0.017455f), new Vector3(-0.08587401f, 0.115924f, -0.041221f), new Vector3(-0.057831f, 0.115938f, -0.038038f) };     //Ankles
            meshSeamVertices[0][2][2][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][2][2][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][2][2][3] = new Vector3[] { new Vector3(-0.03752f, 1.12267f, -0.03273f), new Vector3(0f, 1.12563f, -0.04968f), new Vector3(-0.02065f, 1.12468f, -0.04379001f), new Vector3(0.02065f, 1.12468f, -0.04379001f), new Vector3(0.03752f, 1.12267f, -0.03273f), new Vector3(-0.04215f, 1.11826f, -0.01802f), new Vector3(-0.03877f, 1.11206f, -0.002059f), new Vector3(-0.03276f, 1.10712f, 0.00977f), new Vector3(0.03276f, 1.10712f, 0.00977f), new Vector3(0.03877f, 1.11206f, -0.002059f), new Vector3(0.04215f, 1.11826f, -0.01802f), new Vector3(0f, 1.09771f, 0.02923f), new Vector3(-0.01436f, 1.09967f, 0.02605f), new Vector3(-0.02296f, 1.10289f, 0.01801f), new Vector3(0.01436f, 1.09967f, 0.02605f), new Vector3(0.02296f, 1.10289f, 0.01801f) };     //Neck
            meshSeamVertices[0][2][2][4] = new Vector3[] { new Vector3(0.0914f, 0.7533001f, 0.03828f), new Vector3(0.05402f, 0.74878f, 0.07478f), new Vector3(0f, 0.74493f, 0.08794f), new Vector3(0.09703f, 0.75579f, 0.00505f), new Vector3(0.08787f, 0.75756f, -0.02397f), new Vector3(0.04613f, 0.75671f, -0.05946f), new Vector3(0f, 0.75421f, -0.06595f), new Vector3(-0.0914f, 0.7533001f, 0.03828f), new Vector3(-0.05402f, 0.74878f, 0.07479f), new Vector3(-0.09703f, 0.75579f, 0.00505f), new Vector3(-0.08787f, 0.75756f, -0.02397f), new Vector3(-0.04613f, 0.75672f, -0.05946f) };     //Waist
            meshSeamVertices[0][2][2][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[0][2][2][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[0][2][3] = new Vector3[7][];         //Child LOD3 seams
            meshSeamVertices[0][2][3][0] = new Vector3[] { new Vector3(0.07243f, 0.11592f, 0.01694f), new Vector3(0.04333f, 0.11592f, -0.00869f), new Vector3(0.0952f, 0.11592f, -0.01715f), new Vector3(0.06786f, 0.11592f, -0.04212f), new Vector3(-0.07243f, 0.11592f, 0.01694f), new Vector3(-0.04333f, 0.11592f, -0.00869f), new Vector3(-0.0952f, 0.11592f, -0.01715f), new Vector3(-0.06786f, 0.11592f, -0.04212f) };     //Ankles
            meshSeamVertices[0][2][3][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][2][3][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][2][3][3] = new Vector3[] { new Vector3(0.02908f, 1.12368f, -0.03826001f), new Vector3(0f, 1.12563f, -0.04968f), new Vector3(-0.02908f, 1.12368f, -0.03826001f), new Vector3(-0.04215f, 1.11826f, -0.01802f), new Vector3(0.02296f, 1.10289f, 0.01801f), new Vector3(0.04215f, 1.11826f, -0.01802f), new Vector3(0f, 1.09771f, 0.02923f), new Vector3(-0.02296f, 1.10289f, 0.01801f) };     //Neck
            meshSeamVertices[0][2][3][4] = new Vector3[] { new Vector3(0f, 0.74493f, 0.08794f), new Vector3(0.0721f, 0.7513f, 0.05675f), new Vector3(0.09703f, 0.75579f, 0.00505f), new Vector3(0f, 0.75421f, -0.06595f), new Vector3(-0.0721f, 0.7513f, 0.05675f), new Vector3(-0.09703f, 0.75579f, 0.00505f) };     //Waist
            meshSeamVertices[0][2][3][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[0][2][3][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[0][3] = new Vector3[4][][];         //Toddler
            meshSeamVertices[0][3][0] = new Vector3[7][];         //Toddler LOD0 seams
            meshSeamVertices[0][3][0][0] = new Vector3[] { new Vector3(0.0581f, 0.08125f, 0.02318f), new Vector3(0.04031f, 0.08124f, 0.01804f), new Vector3(0.07382f, 0.08125f, 0.01169f), new Vector3(0.07935f, 0.08125f, -0.00576f), new Vector3(0.07126f, 0.08125f, -0.02278f), new Vector3(0.05931f, 0.08124f, -0.02894f), new Vector3(0.03273f, 0.08124f, 0.00192f), new Vector3(0.03487f, 0.08125f, -0.0212f), new Vector3(0.04727f, 0.08125f, -0.02768f), new Vector3(-0.0581f, 0.08125f, 0.02318f), new Vector3(-0.04031f, 0.08124f, 0.01804f), new Vector3(-0.07382f, 0.08125f, 0.01169f), new Vector3(-0.07935f, 0.08125f, -0.00576f), new Vector3(-0.07126f, 0.08125f, -0.02278f), new Vector3(-0.05931f, 0.08124f, -0.02894f), new Vector3(-0.03273f, 0.08124f, 0.00192f), new Vector3(-0.03487f, 0.08125f, -0.0212f), new Vector3(-0.04727f, 0.08125f, -0.02768f) };     //Ankles
            meshSeamVertices[0][3][0][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][3][0][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][3][0][3] = new Vector3[] { new Vector3(0f, 0.72543f, 0.02841f), new Vector3(0f, 0.74598f, -0.04577f), new Vector3(-0.03502f, 0.74528f, -0.0298f), new Vector3(-0.03926f, 0.74341f, -0.0156f), new Vector3(-0.0366f, 0.73923f, -0.00225f), new Vector3(-0.0332f, 0.73407f, 0.01018f), new Vector3(-0.01506f, 0.72707f, 0.02594f), new Vector3(-0.02401f, 0.73009f, 0.01878f), new Vector3(-0.01907f, 0.7453601f, -0.0411f), new Vector3(0.03502f, 0.74528f, -0.0298f), new Vector3(0.03926f, 0.74341f, -0.0156f), new Vector3(0.0366f, 0.73923f, -0.00225f), new Vector3(0.0332f, 0.73407f, 0.01018f), new Vector3(0.01506f, 0.72707f, 0.02594f), new Vector3(0.02401f, 0.73009f, 0.01878f), new Vector3(0.01907f, 0.7453601f, -0.0411f) };     //Neck
            meshSeamVertices[0][3][0][4] = new Vector3[] { new Vector3(0.08580001f, 0.46483f, 0.03673f), new Vector3(0.064f, 0.46369f, 0.06124f), new Vector3(0.01813f, 0.46041f, 0.08473f), new Vector3(0.03817f, 0.46175f, 0.07651f), new Vector3(0f, 0.45983f, 0.08692f), new Vector3(0.03322f, 0.46701f, -0.06998001f), new Vector3(0.01455f, 0.46646f, -0.07039f), new Vector3(0.08139001f, 0.4675f, -0.02652f), new Vector3(0.09089f, 0.46698f, 0.00424f), new Vector3(0.06864001f, 0.46769f, -0.04676f), new Vector3(0.05398f, 0.46762f, -0.05926f), new Vector3(0f, 0.4661f, -0.0696f), new Vector3(-0.08580001f, 0.46483f, 0.0367f), new Vector3(-0.064f, 0.46369f, 0.0613f), new Vector3(-0.01813f, 0.46041f, 0.08473f), new Vector3(-0.03818f, 0.46175f, 0.07676001f), new Vector3(-0.03322f, 0.46701f, -0.06998001f), new Vector3(-0.01455f, 0.46646f, -0.07039f), new Vector3(-0.08139001f, 0.4675f, -0.02652f), new Vector3(-0.09089f, 0.46698f, 0.00424f), new Vector3(-0.06864001f, 0.46769f, -0.04676f), new Vector3(-0.05398f, 0.46762f, -0.05926f) };     //Waist
            meshSeamVertices[0][3][0][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[0][3][0][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[0][3][1] = new Vector3[7][];         //Toddler LOD1 seams
            meshSeamVertices[0][3][1][0] = new Vector3[] { new Vector3(0.058104f, 0.08125f, 0.023184f), new Vector3(0.040309f, 0.08123501f, 0.018042f), new Vector3(0.07382f, 0.08125f, 0.01169f), new Vector3(0.079346f, 0.08125f, -0.005758f), new Vector3(0.065284f, 0.081243f, -0.025861f), new Vector3(0.032729f, 0.081235f, 0.001915f), new Vector3(0.034873f, 0.081249f, -0.021203f), new Vector3(0.047273f, 0.08125f, -0.027681f), new Vector3(-0.058104f, 0.08125f, 0.023184f), new Vector3(-0.040309f, 0.08123501f, 0.018042f), new Vector3(-0.07382f, 0.08125f, 0.01169f), new Vector3(-0.079346f, 0.08125f, -0.005758f), new Vector3(-0.065284f, 0.081243f, -0.025861f), new Vector3(-0.032729f, 0.081235f, 0.001915f), new Vector3(-0.034873f, 0.081249f, -0.021203f), new Vector3(-0.047273f, 0.08125f, -0.027681f) };     //Ankles
            meshSeamVertices[0][3][1][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][3][1][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][3][1][3] = new Vector3[] { new Vector3(0f, 0.725429f, 0.028412f), new Vector3(0f, 0.745978f, -0.045768f), new Vector3(-0.035021f, 0.7452821f, -0.029798f), new Vector3(-0.039258f, 0.743412f, -0.015598f), new Vector3(-0.036595f, 0.739231f, -0.002247f), new Vector3(-0.033196f, 0.7340671f, 0.010182f), new Vector3(-0.024012f, 0.730088f, 0.018779f), new Vector3(-0.019068f, 0.745362f, -0.041102f), new Vector3(-0.015064f, 0.727072f, 0.025942f), new Vector3(0.035021f, 0.7452821f, -0.029798f), new Vector3(0.039258f, 0.743412f, -0.015598f), new Vector3(0.036595f, 0.739231f, -0.002247f), new Vector3(0.033196f, 0.7340671f, 0.010182f), new Vector3(0.024012f, 0.730088f, 0.018779f), new Vector3(0.019068f, 0.745362f, -0.041102f), new Vector3(0.015064f, 0.727072f, 0.025942f) };     //Neck
            meshSeamVertices[0][3][1][4] = new Vector3[] { new Vector3(0.074902f, 0.464262f, 0.048984f), new Vector3(0.038165f, 0.46175f, 0.076512f), new Vector3(0f, 0.459831f, 0.086915f), new Vector3(0.023888f, 0.466736f, -0.070186f), new Vector3(0.090889f, 0.466976f, 0.004244f), new Vector3(0.075017f, 0.467595f, -0.036638f), new Vector3(0.053979f, 0.467616f, -0.05926f), new Vector3(0f, 0.466101f, -0.069595f), new Vector3(-0.074902f, 0.464262f, 0.048999f), new Vector3(-0.038183f, 0.46175f, 0.076757f), new Vector3(-0.023888f, 0.466736f, -0.070186f), new Vector3(-0.090889f, 0.466976f, 0.004244f), new Vector3(-0.075017f, 0.467595f, -0.036638f), new Vector3(-0.053979f, 0.467616f, -0.05926f) };     //Waist
            meshSeamVertices[0][3][1][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[0][3][1][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[0][3][2] = new Vector3[7][];         //Toddler LOD2 seams
            meshSeamVertices[0][3][2][0] = new Vector3[] { new Vector3(0.065962f, 0.08125f, 0.017437f), new Vector3(0.040309f, 0.08123501f, 0.018042f), new Vector3(0.079346f, 0.08125f, -0.005758f), new Vector3(0.065284f, 0.081243f, -0.025861f), new Vector3(0.032729f, 0.081235f, 0.001915f), new Vector3(0.041073f, 0.08125f, -0.024442f), new Vector3(-0.065962f, 0.08125f, 0.017437f), new Vector3(-0.040309f, 0.08123501f, 0.018042f), new Vector3(-0.079346f, 0.08125f, -0.005758f), new Vector3(-0.065284f, 0.081243f, -0.025861f), new Vector3(-0.032729f, 0.081235f, 0.001915f), new Vector3(-0.041073f, 0.08125f, -0.024442f) };     //Ankles
            meshSeamVertices[0][3][2][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][3][2][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][3][2][3] = new Vector3[] { new Vector3(0f, 0.725429f, 0.028412f), new Vector3(0f, 0.745978f, -0.045768f), new Vector3(-0.039258f, 0.743412f, -0.015598f), new Vector3(-0.024012f, 0.730088f, 0.018779f), new Vector3(-0.019068f, 0.745362f, -0.041102f), new Vector3(-0.036595f, 0.739231f, -0.002247f), new Vector3(-0.033196f, 0.7340671f, 0.010182f), new Vector3(-0.035021f, 0.7452821f, -0.029798f), new Vector3(-0.015064f, 0.727072f, 0.025942f), new Vector3(0.039258f, 0.743412f, -0.015598f), new Vector3(0.024012f, 0.730088f, 0.018779f), new Vector3(0.019068f, 0.745362f, -0.041102f), new Vector3(0.036595f, 0.739231f, -0.002247f), new Vector3(0.033196f, 0.7340671f, 0.010182f), new Vector3(0.035021f, 0.7452821f, -0.029798f), new Vector3(0.015064f, 0.727072f, 0.025942f) };     //Neck
            meshSeamVertices[0][3][2][4] = new Vector3[] { new Vector3(0.074902f, 0.464262f, 0.048984f), new Vector3(0.038165f, 0.46175f, 0.076512f), new Vector3(0f, 0.459831f, 0.086915f), new Vector3(0.038933f, 0.467176f, -0.064723f), new Vector3(0.090889f, 0.466976f, 0.004244f), new Vector3(0.075017f, 0.467595f, -0.036638f), new Vector3(0f, 0.466101f, -0.069595f), new Vector3(-0.074902f, 0.464262f, 0.048999f), new Vector3(-0.038183f, 0.46175f, 0.076757f), new Vector3(-0.038933f, 0.467176f, -0.064723f), new Vector3(-0.090889f, 0.466976f, 0.004244f), new Vector3(-0.075017f, 0.467595f, -0.036638f) };     //Waist
            meshSeamVertices[0][3][2][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[0][3][2][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[0][3][3] = new Vector3[7][];         //Toddler LOD3 seams
            meshSeamVertices[0][3][3][0] = new Vector3[] { new Vector3(0.053136f, 0.081243f, 0.017739f), new Vector3(0.079346f, 0.08125f, -0.005758f), new Vector3(0.053179f, 0.081246f, -0.025151f), new Vector3(0.032729f, 0.081235f, 0.001915f), new Vector3(-0.053136f, 0.081243f, 0.017739f), new Vector3(-0.079346f, 0.08125f, -0.005758f), new Vector3(-0.053179f, 0.081246f, -0.025151f), new Vector3(-0.032729f, 0.081235f, 0.001915f) };     //Ankles
            meshSeamVertices[0][3][3][1] = new Vector3[0];     //Tail
            meshSeamVertices[0][3][3][2] = new Vector3[0];     //Ears
            meshSeamVertices[0][3][3][3] = new Vector3[] { new Vector3(0f, 0.725429f, 0.028412f), new Vector3(0f, 0.745978f, -0.045768f) };     //Neck
            meshSeamVertices[0][3][3][4] = new Vector3[] { new Vector3(0.056534f, 0.463006f, 0.062748f), new Vector3(0f, 0.459831f, 0.086915f), new Vector3(0.056975f, 0.467386f, -0.050681f), new Vector3(0.090889f, 0.466976f, 0.004244f), new Vector3(0f, 0.466101f, -0.069595f), new Vector3(-0.056542f, 0.463006f, 0.062878f), new Vector3(-0.056975f, 0.467386f, -0.050681f), new Vector3(-0.090889f, 0.466976f, 0.004244f) };     //Waist
            meshSeamVertices[0][3][3][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[0][3][3][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[1] = new Vector3[4][][][];        //ageSpecies
            meshSeamVertices[1][0] = new Vector3[4][][];         //Adult Dog
            meshSeamVertices[1][0][0] = new Vector3[7][];         //Adult Dog LOD0 seams
            meshSeamVertices[1][0][0][0] = new Vector3[] { new Vector3(0.091319f, 0.110002f, 0.100705f), new Vector3(0.089504f, 0.111499f, 0.110355f), new Vector3(0.083803f, 0.1126f, 0.11691f), new Vector3(0.072392f, 0.112377f, 0.120393f), new Vector3(0.059744f, 0.111575f, 0.116427f), new Vector3(0.052912f, 0.110878f, 0.108974f), new Vector3(0.050505f, 0.109698f, 0.09901001f), new Vector3(0.050042f, 0.107311f, 0.088836f), new Vector3(0.054359f, 0.104351f, 0.079998f), new Vector3(0.06345101f, 0.101799f, 0.071559f), new Vector3(0.077975f, 0.101195f, 0.071651f), new Vector3(0.087592f, 0.104716f, 0.081086f), new Vector3(0.091225f, 0.107681f, 0.090575f), new Vector3(-0.050505f, 0.109698f, 0.09901001f), new Vector3(-0.052912f, 0.110878f, 0.108974f), new Vector3(-0.059744f, 0.111575f, 0.116427f), new Vector3(-0.072392f, 0.112377f, 0.120393f), new Vector3(-0.083803f, 0.1126f, 0.11691f), new Vector3(-0.089504f, 0.111499f, 0.110355f), new Vector3(-0.091319f, 0.110002f, 0.100705f), new Vector3(-0.091225f, 0.107681f, 0.090575f), new Vector3(-0.087592f, 0.104716f, 0.081086f), new Vector3(-0.077975f, 0.101195f, 0.071651f), new Vector3(-0.06345101f, 0.101799f, 0.071559f), new Vector3(-0.054359f, 0.104351f, 0.079998f), new Vector3(-0.050042f, 0.107311f, 0.088836f), new Vector3(0.070713f, 0.101497f, 0.069976f), new Vector3(-0.070713f, 0.101497f, 0.069976f), new Vector3(0.072045f, 0.10763f, -0.502381f), new Vector3(0.07268f, 0.10763f, -0.512439f), new Vector3(0.069921f, 0.107654f, -0.5222141f), new Vector3(0.065669f, 0.107896f, -0.529511f), new Vector3(0.053849f, 0.107928f, -0.531096f), new Vector3(0.041362f, 0.107749f, -0.528099f), new Vector3(0.037166f, 0.107536f, -0.5213591f), new Vector3(0.035074f, 0.10762f, -0.512302f), new Vector3(0.035216f, 0.1077f, -0.5021471f), new Vector3(0.039253f, 0.107855f, -0.491577f), new Vector3(0.046065f, 0.10827f, -0.486399f), new Vector3(0.054622f, 0.108626f, -0.484898f), new Vector3(0.062579f, 0.108652f, -0.485451f), new Vector3(0.068362f, 0.108161f, -0.492384f), new Vector3(-0.041362f, 0.107749f, -0.528099f), new Vector3(-0.037166f, 0.107536f, -0.5213591f), new Vector3(-0.035074f, 0.10762f, -0.512302f), new Vector3(-0.035216f, 0.1077f, -0.5021471f), new Vector3(-0.039253f, 0.107855f, -0.491577f), new Vector3(-0.046065f, 0.10827f, -0.486399f), new Vector3(-0.054622f, 0.108626f, -0.484898f), new Vector3(-0.062579f, 0.108652f, -0.485451f), new Vector3(-0.068362f, 0.108161f, -0.492384f), new Vector3(-0.072045f, 0.10763f, -0.502381f), new Vector3(-0.07268f, 0.10763f, -0.512439f), new Vector3(-0.069921f, 0.107654f, -0.5222141f), new Vector3(-0.065669f, 0.107896f, -0.529511f), new Vector3(-0.053849f, 0.107928f, -0.531096f) };     //Ankles
            meshSeamVertices[1][0][0][1] = new Vector3[] { new Vector3(0.021073f, 0.577363f, -0.43746f), new Vector3(0.02086f, 0.592635f, -0.42631f), new Vector3(0.012646f, 0.600489f, -0.423659f), new Vector3(0f, 0.604671f, -0.423148f), new Vector3(0f, 0.562809f, -0.451407f), new Vector3(0.014413f, 0.569726f, -0.445067f), new Vector3(-0.021073f, 0.577363f, -0.437461f), new Vector3(-0.02086f, 0.592635f, -0.426311f), new Vector3(-0.012646f, 0.600489f, -0.423659f), new Vector3(-0.014413f, 0.569726f, -0.445067f) };     //Tail
            meshSeamVertices[1][0][0][2] = new Vector3[] { new Vector3(-0.067989f, 0.768746f, 0.27049f), new Vector3(-0.063373f, 0.779868f, 0.276023f), new Vector3(-0.068046f, 0.761078f, 0.258345f), new Vector3(-0.066053f, 0.759634f, 0.242935f), new Vector3(-0.06145401f, 0.769566f, 0.230691f), new Vector3(-0.057924f, 0.780925f, 0.229641f), new Vector3(-0.05198f, 0.798391f, 0.236866f), new Vector3(-0.057639f, 0.793634f, 0.27381f), new Vector3(-0.053146f, 0.803787f, 0.266705f), new Vector3(-0.06815f, 0.764306f, 0.265297f), new Vector3(-0.060771f, 0.786525f, 0.276518f), new Vector3(-0.055271f, 0.7991011f, 0.270695f), new Vector3(-0.050504f, 0.808143f, 0.255652f), new Vector3(-0.050653f, 0.803242f, 0.243345f), new Vector3(-0.053795f, 0.7924451f, 0.232829f), new Vector3(-0.066883f, 0.760161f, 0.251542f), new Vector3(0.067989f, 0.768746f, 0.27049f), new Vector3(0.063373f, 0.779868f, 0.276023f), new Vector3(0.068046f, 0.761078f, 0.258345f), new Vector3(0.066053f, 0.759634f, 0.242935f), new Vector3(0.06145401f, 0.769566f, 0.23069f), new Vector3(0.057924f, 0.780925f, 0.229641f), new Vector3(0.05198f, 0.798391f, 0.236866f), new Vector3(0.057638f, 0.793634f, 0.273811f), new Vector3(0.053146f, 0.803787f, 0.266704f), new Vector3(0.06815f, 0.764306f, 0.265296f), new Vector3(0.060771f, 0.786525f, 0.276519f), new Vector3(0.055271f, 0.7991011f, 0.270695f), new Vector3(0.050504f, 0.808143f, 0.255652f), new Vector3(0.050653f, 0.803242f, 0.243345f), new Vector3(0.053795f, 0.7924451f, 0.232828f), new Vector3(0.066883f, 0.760161f, 0.251542f) };     //Ears
            meshSeamVertices[1][0][0][3] = new Vector3[] { new Vector3(0f, 0.65108f, 0.263976f), new Vector3(-0.031086f, 0.663975f, 0.251473f), new Vector3(-0.040149f, 0.6724421f, 0.244208f), new Vector3(-0.052244f, 0.6996421f, 0.220295f), new Vector3(-0.048166f, 0.719901f, 0.202111f), new Vector3(-0.036981f, 0.735922f, 0.186963f), new Vector3(-0.029402f, 0.743723f, 0.179634f), new Vector3(-0.01039f, 0.752611f, 0.171274f), new Vector3(0f, 0.754215f, 0.169697f), new Vector3(-0.012513f, 0.6536471f, 0.261375f), new Vector3(-0.050186f, 0.690092f, 0.229349f), new Vector3(-0.019823f, 0.74883f, 0.174659f), new Vector3(-0.043192f, 0.7280371f, 0.194325f), new Vector3(0.031086f, 0.663975f, 0.251473f), new Vector3(0.040149f, 0.6724421f, 0.244208f), new Vector3(0.052244f, 0.6996421f, 0.220295f), new Vector3(0.048166f, 0.719901f, 0.202111f), new Vector3(0.036981f, 0.735922f, 0.186963f), new Vector3(0.029402f, 0.743723f, 0.179634f), new Vector3(0.01039f, 0.752611f, 0.171274f), new Vector3(0.012513f, 0.6536471f, 0.261375f), new Vector3(0.050186f, 0.690092f, 0.229349f), new Vector3(0.019823f, 0.74883f, 0.174659f), new Vector3(0.043192f, 0.7280371f, 0.194325f), new Vector3(0.051507f, 0.710206f, 0.210656f), new Vector3(0.046314f, 0.680477f, 0.237306f), new Vector3(0.022854f, 0.658232f, 0.256744f), new Vector3(-0.051507f, 0.710206f, 0.210656f), new Vector3(-0.046314f, 0.680477f, 0.237306f), new Vector3(-0.022854f, 0.658232f, 0.256744f) };     //Neck
            meshSeamVertices[1][0][0][4] = new Vector3[0];     //Waist
            meshSeamVertices[1][0][0][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[1][0][0][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[1][0][1] = new Vector3[7][];         //Adult Dog LOD1 seams
            meshSeamVertices[1][0][1][0] = new Vector3[] { new Vector3(0.089504f, 0.111499f, 0.110355f), new Vector3(0.083803f, 0.1126f, 0.11691f), new Vector3(0.072392f, 0.112377f, 0.120393f), new Vector3(0.059744f, 0.111575f, 0.116427f), new Vector3(0.052912f, 0.110878f, 0.108974f), new Vector3(0.050273f, 0.108504f, 0.093923f), new Vector3(0.054359f, 0.104351f, 0.079998f), new Vector3(0.06345101f, 0.101799f, 0.071559f), new Vector3(0.077975f, 0.101195f, 0.071651f), new Vector3(0.087592f, 0.104716f, 0.081086f), new Vector3(0.091272f, 0.108841f, 0.09564f), new Vector3(-0.052912f, 0.110878f, 0.108974f), new Vector3(-0.059744f, 0.111575f, 0.116427f), new Vector3(-0.072392f, 0.112377f, 0.120393f), new Vector3(-0.083803f, 0.1126f, 0.11691f), new Vector3(-0.089504f, 0.111499f, 0.110355f), new Vector3(-0.091272f, 0.108841f, 0.09564f), new Vector3(-0.087592f, 0.104716f, 0.081086f), new Vector3(-0.077975f, 0.101195f, 0.071651f), new Vector3(-0.06345101f, 0.101799f, 0.071559f), new Vector3(-0.054359f, 0.104351f, 0.079998f), new Vector3(-0.050273f, 0.108504f, 0.093923f), new Vector3(0.070713f, 0.101497f, 0.069976f), new Vector3(-0.070713f, 0.101497f, 0.069976f), new Vector3(0.067952f, 0.119676f, -0.490161f), new Vector3(0.071998f, 0.120309f, -0.505297f), new Vector3(0.06767501f, 0.121354f, -0.524048f), new Vector3(0.053809f, 0.121686f, -0.530085f), new Vector3(0.039275f, 0.121442f, -0.522727f), new Vector3(0.035499f, 0.12054f, -0.504781f), new Vector3(0.039851f, 0.1193f, -0.489503f), new Vector3(0.046574f, 0.119315f, -0.484606f), new Vector3(0.054669f, 0.119597f, -0.483415f), new Vector3(0.062031f, 0.119659f, -0.483632f), new Vector3(-0.039275f, 0.121442f, -0.522727f), new Vector3(-0.035499f, 0.12054f, -0.504781f), new Vector3(-0.039851f, 0.1193f, -0.489503f), new Vector3(-0.046574f, 0.119315f, -0.484606f), new Vector3(-0.054669f, 0.119597f, -0.483415f), new Vector3(-0.062031f, 0.119659f, -0.483632f), new Vector3(-0.06767501f, 0.121354f, -0.524048f), new Vector3(-0.053809f, 0.121686f, -0.530085f), new Vector3(-0.067952f, 0.119676f, -0.490161f), new Vector3(-0.071998f, 0.120309f, -0.505297f) };     //Ankles
            meshSeamVertices[1][0][1][1] = new Vector3[] { new Vector3(0.021073f, 0.577363f, -0.43746f), new Vector3(0.016753f, 0.596562f, -0.424984f), new Vector3(0f, 0.604671f, -0.423148f), new Vector3(0f, 0.562809f, -0.451407f), new Vector3(0.014413f, 0.569726f, -0.445067f), new Vector3(-0.021073f, 0.577363f, -0.437461f), new Vector3(-0.016753f, 0.596562f, -0.424985f), new Vector3(-0.014413f, 0.569726f, -0.445067f) };     //Tail
            meshSeamVertices[1][0][1][2] = new Vector3[] { new Vector3(-0.067992f, 0.768754f, 0.270502f), new Vector3(-0.066058f, 0.759642f, 0.242946f), new Vector3(-0.061457f, 0.769576f, 0.230701f), new Vector3(-0.057926f, 0.780935f, 0.229652f), new Vector3(-0.051985f, 0.798399f, 0.236878f), new Vector3(-0.057639f, 0.793635f, 0.27381f), new Vector3(-0.053148f, 0.8037941f, 0.266716f), new Vector3(-0.068101f, 0.7626981f, 0.261832f), new Vector3(-0.060771f, 0.786526f, 0.276518f), new Vector3(-0.05527201f, 0.799109f, 0.270708f), new Vector3(-0.050583f, 0.8057f, 0.249509f), new Vector3(-0.0538f, 0.792453f, 0.232841f), new Vector3(-0.066888f, 0.760171f, 0.251553f), new Vector3(0.067992f, 0.768754f, 0.270502f), new Vector3(0.068101f, 0.7626981f, 0.261832f), new Vector3(0.066058f, 0.759642f, 0.242946f), new Vector3(0.061457f, 0.769576f, 0.230701f), new Vector3(0.057926f, 0.780935f, 0.229652f), new Vector3(0.051985f, 0.798399f, 0.236878f), new Vector3(0.057638f, 0.793635f, 0.27381f), new Vector3(0.053148f, 0.8037941f, 0.266716f), new Vector3(0.060771f, 0.786526f, 0.276518f), new Vector3(0.05527201f, 0.799109f, 0.270708f), new Vector3(0.050583f, 0.8057f, 0.249509f), new Vector3(0.0538f, 0.792453f, 0.232841f), new Vector3(0.066888f, 0.760171f, 0.251553f) };     //Ears
            meshSeamVertices[1][0][1][3] = new Vector3[] { new Vector3(0f, 0.65108f, 0.263976f), new Vector3(-0.031086f, 0.663975f, 0.251473f), new Vector3(-0.047474f, 0.719901f, 0.202111f), new Vector3(-0.039776f, 0.7319791f, 0.190644f), new Vector3(-0.029402f, 0.743723f, 0.179634f), new Vector3(-0.015106f, 0.75072f, 0.172967f), new Vector3(0f, 0.754215f, 0.169697f), new Vector3(-0.050186f, 0.690092f, 0.229349f), new Vector3(0.031086f, 0.663975f, 0.251473f), new Vector3(0.043231f, 0.67646f, 0.240757f), new Vector3(0.051443f, 0.704924f, 0.215476f), new Vector3(0.047474f, 0.719901f, 0.202111f), new Vector3(0.029402f, 0.743723f, 0.179634f), new Vector3(0.017683f, 0.65594f, 0.25906f), new Vector3(0.050186f, 0.690092f, 0.229349f), new Vector3(0.015106f, 0.75072f, 0.172967f), new Vector3(0.039776f, 0.7319791f, 0.190644f), new Vector3(-0.051443f, 0.704924f, 0.215476f), new Vector3(-0.043231f, 0.67646f, 0.240757f), new Vector3(-0.017683f, 0.65594f, 0.25906f) };     //Neck
            meshSeamVertices[1][0][1][4] = new Vector3[0];     //Waist
            meshSeamVertices[1][0][1][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[1][0][1][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[1][0][2] = new Vector3[7][];         //Adult Dog LOD2 seams
            meshSeamVertices[1][0][2][0] = new Vector3[] { new Vector3(0.09131901f, 0.110002f, 0.100705f), new Vector3(0.083803f, 0.1126f, 0.11691f), new Vector3(0.059744f, 0.111575f, 0.116427f), new Vector3(0.050505f, 0.109698f, 0.09901f), new Vector3(0.054359f, 0.104351f, 0.079998f), new Vector3(0.087592f, 0.104716f, 0.081086f), new Vector3(-0.050505f, 0.109698f, 0.09901f), new Vector3(-0.059744f, 0.111575f, 0.116427f), new Vector3(-0.083803f, 0.1126f, 0.11691f), new Vector3(-0.09131901f, 0.110002f, 0.100705f), new Vector3(-0.087592f, 0.104716f, 0.081086f), new Vector3(-0.054359f, 0.104351f, 0.079998f), new Vector3(0.070713f, 0.101497f, 0.069976f), new Vector3(-0.070713f, 0.101497f, 0.069976f), new Vector3(0.071462f, 0.154987f, -0.495282f), new Vector3(0.069497f, 0.159743f, -0.5185031f), new Vector3(0.053665f, 0.162502f, -0.530194f), new Vector3(0.036587f, 0.159017f, -0.515529f), new Vector3(0.035549f, 0.154733f, -0.494399f), new Vector3(0.0472f, 0.151351f, -0.47717f), new Vector3(0.062333f, 0.151765f, -0.476295f), new Vector3(-0.036587f, 0.159017f, -0.515529f), new Vector3(-0.035549f, 0.154733f, -0.494399f), new Vector3(-0.0472f, 0.151351f, -0.47717f), new Vector3(-0.062333f, 0.151765f, -0.476295f), new Vector3(-0.071462f, 0.154987f, -0.495282f), new Vector3(-0.069497f, 0.159743f, -0.5185031f), new Vector3(-0.053665f, 0.162502f, -0.530194f) };     //Ankles
            meshSeamVertices[1][0][2][1] = new Vector3[] { new Vector3(0f, 0.604671f, -0.423148f), new Vector3(0f, 0.562809f, -0.451407f), new Vector3(0.019152f, 0.575161f, -0.439654f), new Vector3(-0.019152f, 0.575161f, -0.439654f) };     //Tail
            meshSeamVertices[1][0][2][2] = new Vector3[] { new Vector3(-0.067992f, 0.768753f, 0.270502f), new Vector3(-0.063376f, 0.779874f, 0.276034f), new Vector3(-0.068048f, 0.761084f, 0.258355f), new Vector3(-0.066058f, 0.759641f, 0.242946f), new Vector3(-0.057926f, 0.780934f, 0.229652f), new Vector3(-0.053148f, 0.8037931f, 0.266716f), new Vector3(-0.056455f, 0.796371f, 0.272259f), new Vector3(-0.050583f, 0.805699f, 0.24951f), new Vector3(-0.052892f, 0.7954251f, 0.23486f), new Vector3(0.067992f, 0.768753f, 0.270503f), new Vector3(0.063376f, 0.779874f, 0.276035f), new Vector3(0.068048f, 0.761084f, 0.258356f), new Vector3(0.066058f, 0.759641f, 0.242947f), new Vector3(0.057926f, 0.780934f, 0.229653f), new Vector3(0.052892f, 0.7954251f, 0.23486f), new Vector3(0.056455f, 0.796371f, 0.27226f), new Vector3(0.053148f, 0.8037931f, 0.266717f), new Vector3(0.050583f, 0.805699f, 0.24951f) };     //Ears
            meshSeamVertices[1][0][2][3] = new Vector3[] { new Vector3(0f, 0.65108f, 0.263976f), new Vector3(-0.040149f, 0.6724421f, 0.244208f), new Vector3(-0.051842f, 0.6996421f, 0.220295f), new Vector3(-0.047474f, 0.719901f, 0.202111f), new Vector3(-0.036981f, 0.735922f, 0.186963f), new Vector3(0f, 0.754215f, 0.169697f), new Vector3(-0.019823f, 0.74883f, 0.174659f), new Vector3(0.040149f, 0.6724421f, 0.244208f), new Vector3(0.051842f, 0.6996421f, 0.220295f), new Vector3(0.047474f, 0.719901f, 0.202111f), new Vector3(0.036981f, 0.735922f, 0.186963f), new Vector3(0.019823f, 0.74883f, 0.174659f), new Vector3(0.022854f, 0.658232f, 0.256744f), new Vector3(-0.022854f, 0.658232f, 0.256744f) };     //Neck
            meshSeamVertices[1][0][2][4] = new Vector3[0];     //Waist
            meshSeamVertices[1][0][2][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[1][0][2][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[1][0][3] = new Vector3[7][];         //Adult Dog LOD3 seams
            meshSeamVertices[1][0][3][0] = new Vector3[] { new Vector3(0.083803f, 0.1126f, 0.11691f), new Vector3(0.059744f, 0.111575f, 0.116427f), new Vector3(0.054359f, 0.104351f, 0.079998f), new Vector3(0.087592f, 0.104716f, 0.081086f), new Vector3(-0.059744f, 0.111575f, 0.116427f), new Vector3(-0.083803f, 0.1126f, 0.11691f), new Vector3(-0.087592f, 0.104716f, 0.081086f), new Vector3(-0.054359f, 0.104351f, 0.079998f), new Vector3(0.070713f, 0.101497f, 0.069976f), new Vector3(-0.070713f, 0.101497f, 0.069976f), new Vector3(0.069497f, 0.159743f, -0.5185031f), new Vector3(0.053665f, 0.162502f, -0.530194f), new Vector3(0.036587f, 0.159017f, -0.515529f), new Vector3(0.0472f, 0.151351f, -0.47717f), new Vector3(0.062333f, 0.151765f, -0.476295f), new Vector3(-0.036587f, 0.159017f, -0.515529f), new Vector3(-0.0472f, 0.151351f, -0.47717f), new Vector3(-0.062333f, 0.151765f, -0.476295f), new Vector3(-0.069497f, 0.159743f, -0.5185031f), new Vector3(-0.053665f, 0.162502f, -0.530194f) };     //Ankles
            meshSeamVertices[1][0][3][1] = new Vector3[] { new Vector3(0f, 0.604671f, -0.423148f), new Vector3(0f, 0.562809f, -0.451407f), new Vector3(0.019152f, 0.575161f, -0.439654f), new Vector3(-0.019152f, 0.575161f, -0.439654f) };     //Tail
            meshSeamVertices[1][0][3][2] = new Vector3[] { new Vector3(-0.065684f, 0.774313f, 0.273268f), new Vector3(-0.06681f, 0.75988f, 0.25055f), new Vector3(-0.057926f, 0.780934f, 0.229652f), new Vector3(-0.054662f, 0.800348f, 0.269728f), new Vector3(-0.050898f, 0.80237f, 0.242183f), new Vector3(0.065684f, 0.774313f, 0.273269f), new Vector3(0.06681f, 0.75988f, 0.25055f), new Vector3(0.057926f, 0.780934f, 0.229653f), new Vector3(0.054662f, 0.800348f, 0.269728f), new Vector3(0.050898f, 0.80237f, 0.242183f) };     //Ears
            meshSeamVertices[1][0][3][3] = new Vector3[] { new Vector3(0f, 0.65108f, 0.263976f), new Vector3(-0.03184f, 0.664723f, 0.250976f), new Vector3(-0.051246f, 0.710773f, 0.211633f), new Vector3(0f, 0.754215f, 0.169697f), new Vector3(-0.029526f, 0.7441601f, 0.180104f), new Vector3(0.051246f, 0.710773f, 0.211633f), new Vector3(0.029526f, 0.7441601f, 0.180104f), new Vector3(0.03184f, 0.664723f, 0.250976f) };     //Neck
            meshSeamVertices[1][0][3][4] = new Vector3[0];     //Waist
            meshSeamVertices[1][0][3][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[1][0][3][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[1][1] = new Vector3[4][][];         //Puppy
            meshSeamVertices[1][1][0] = new Vector3[7][];         //Puppy LOD0 seams
            meshSeamVertices[1][1][0][0] = new Vector3[] { new Vector3(0.036768f, 0.026227f, 0.013628f), new Vector3(0.03593601f, 0.02782f, 0.017807f), new Vector3(0.033462f, 0.028612f, 0.02055f), new Vector3(0.027855f, 0.029023f, 0.022005f), new Vector3(0.022458f, 0.028545f, 0.020437f), new Vector3(0.019409f, 0.027613f, 0.017329f), new Vector3(0.018312f, 0.026146f, 0.013026f), new Vector3(0.018188f, 0.024345f, 0.008862f), new Vector3(0.019965f, 0.022352f, 0.004917f), new Vector3(0.024243f, 0.020768f, 0.001611f), new Vector3(0.03077f, 0.020477f, 0.001638f), new Vector3(0.035116f, 0.022422f, 0.005462f), new Vector3(0.036745f, 0.024428f, 0.009618001f), new Vector3(-0.018312f, 0.026146f, 0.013026f), new Vector3(-0.019409f, 0.027613f, 0.017329f), new Vector3(-0.022458f, 0.028545f, 0.020437f), new Vector3(-0.027855f, 0.029023f, 0.022005f), new Vector3(-0.033462f, 0.028612f, 0.02055f), new Vector3(-0.03593601f, 0.02782f, 0.017807f), new Vector3(-0.036768f, 0.026227f, 0.013628f), new Vector3(-0.036745f, 0.024428f, 0.009618001f), new Vector3(-0.035116f, 0.022422f, 0.005462f), new Vector3(-0.03077f, 0.020477f, 0.001639f), new Vector3(-0.024243f, 0.020768f, 0.001611f), new Vector3(-0.019965f, 0.022352f, 0.004917f), new Vector3(-0.018188f, 0.024345f, 0.008862f), new Vector3(0.027483f, 0.020485f, 0.000959f), new Vector3(-0.027483f, 0.020485f, 0.000958f), new Vector3(0.035483f, 0.026637f, -0.156033f), new Vector3(0.035769f, 0.026894f, -0.160339f), new Vector3(0.034603f, 0.027203f, -0.1644f), new Vector3(0.032792f, 0.027444f, -0.167616f), new Vector3(0.02773f, 0.027534f, -0.16831f), new Vector3(0.022383f, 0.027477f, -0.16705f), new Vector3(0.020598f, 0.027186f, -0.164045f), new Vector3(0.019747f, 0.026911f, -0.160276f), new Vector3(0.019792f, 0.026623f, -0.155934f), new Vector3(0.021326f, 0.026195f, -0.151986f), new Vector3(0.024209f, 0.025911f, -0.149262f), new Vector3(0.02806f, 0.025941f, -0.148507f), new Vector3(0.031656f, 0.026052f, -0.148969f), new Vector3(0.03409901f, 0.026335f, -0.152187f), new Vector3(-0.022383f, 0.027477f, -0.16705f), new Vector3(-0.020598f, 0.027186f, -0.164045f), new Vector3(-0.019747f, 0.026911f, -0.160276f), new Vector3(-0.019792f, 0.026623f, -0.155934f), new Vector3(-0.021326f, 0.026195f, -0.151986f), new Vector3(-0.024209f, 0.025911f, -0.149262f), new Vector3(-0.02806f, 0.025941f, -0.148507f), new Vector3(-0.031656f, 0.026052f, -0.148969f), new Vector3(-0.03409901f, 0.026335f, -0.152187f), new Vector3(-0.035483f, 0.026637f, -0.156033f), new Vector3(-0.035769f, 0.026894f, -0.160339f), new Vector3(-0.034603f, 0.027203f, -0.1644f), new Vector3(-0.032792f, 0.027444f, -0.167616f), new Vector3(-0.02773f, 0.027534f, -0.16831f) };     //Ankles
            meshSeamVertices[1][1][0][1] = new Vector3[] { new Vector3(0.008108f, 0.141443f, -0.133774f), new Vector3(0.008026f, 0.147319f, -0.129635f), new Vector3(0.004865f, 0.150341f, -0.128202f), new Vector3(0f, 0.15195f, -0.127799f), new Vector3(0f, 0.135842f, -0.138938f), new Vector3(0.005545001f, 0.138504f, -0.136499f), new Vector3(-0.008108f, 0.141443f, -0.133774f), new Vector3(-0.008026f, 0.147319f, -0.129635f), new Vector3(-0.004865f, 0.150341f, -0.128202f), new Vector3(-0.005545001f, 0.138504f, -0.136499f) };     //Tail
            meshSeamVertices[1][1][0][2] = new Vector3[] { new Vector3(-0.03149f, 0.202056f, 0.044791f), new Vector3(-0.029873f, 0.206929f, 0.047409f), new Vector3(-0.032279f, 0.198817f, 0.03928f), new Vector3(-0.031583f, 0.198393f, 0.032406f), new Vector3(-0.029422f, 0.202985f, 0.0271f), new Vector3(-0.027024f, 0.208296f, 0.026351f), new Vector3(-0.023789f, 0.217317f, 0.030254f), new Vector3(-0.0268f, 0.213567f, 0.047074f), new Vector3(-0.024298f, 0.218782f, 0.043602f), new Vector3(-0.03232f, 0.200156f, 0.042419f), new Vector3(-0.028384f, 0.210043f, 0.047718f), new Vector3(-0.025254f, 0.216639f, 0.045571f), new Vector3(-0.023084f, 0.220917f, 0.038748f), new Vector3(-0.023264f, 0.219516f, 0.033205f), new Vector3(-0.02485f, 0.213747f, 0.027885f), new Vector3(-0.031973f, 0.198507f, 0.036242f), new Vector3(0.03149f, 0.202056f, 0.044791f), new Vector3(0.029873f, 0.206929f, 0.047409f), new Vector3(0.032279f, 0.198817f, 0.03928f), new Vector3(0.031583f, 0.198393f, 0.032406f), new Vector3(0.029422f, 0.202985f, 0.0271f), new Vector3(0.027024f, 0.208296f, 0.026351f), new Vector3(0.023789f, 0.217317f, 0.030254f), new Vector3(0.0268f, 0.213567f, 0.047074f), new Vector3(0.024298f, 0.218782f, 0.043602f), new Vector3(0.03232f, 0.200156f, 0.042419f), new Vector3(0.028384f, 0.210043f, 0.047718f), new Vector3(0.025254f, 0.216639f, 0.045571f), new Vector3(0.023084f, 0.220917f, 0.038748f), new Vector3(0.023264f, 0.219516f, 0.033205f), new Vector3(0.02485f, 0.213747f, 0.027885f), new Vector3(0.031973f, 0.198507f, 0.036242f) };     //Ears
            meshSeamVertices[1][1][0][3] = new Vector3[] { new Vector3(0f, 0.145662f, 0.05409f), new Vector3(-0.015691f, 0.152265f, 0.04751001f), new Vector3(-0.020114f, 0.156391f, 0.04372f), new Vector3(-0.026019f, 0.171842f, 0.030004f), new Vector3(-0.024575f, 0.181631f, 0.021221f), new Vector3(-0.019194f, 0.18951f, 0.014427f), new Vector3(-0.01529f, 0.192578f, 0.011842f), new Vector3(-0.005277f, 0.19623f, 0.008721f), new Vector3(0f, 0.197183f, 0.007899f), new Vector3(-0.005727f, 0.146799f, 0.052907f), new Vector3(-0.025166f, 0.166557f, 0.034826f), new Vector3(-0.010536f, 0.194739f, 0.009992f), new Vector3(-0.021994f, 0.185906f, 0.017399f), new Vector3(0.015691f, 0.152265f, 0.04751001f), new Vector3(0.020114f, 0.156391f, 0.04372f), new Vector3(0.026019f, 0.171842f, 0.030004f), new Vector3(0.024575f, 0.181631f, 0.021221f), new Vector3(0.019194f, 0.18951f, 0.014427f), new Vector3(0.01529f, 0.192578f, 0.011842f), new Vector3(0.005277f, 0.19623f, 0.008721f), new Vector3(0.005727f, 0.146799f, 0.052907f), new Vector3(0.025166f, 0.166557f, 0.034826f), new Vector3(0.010536f, 0.194739f, 0.009992f), new Vector3(0.021994f, 0.185906f, 0.017399f), new Vector3(0.025781f, 0.177038f, 0.025226f), new Vector3(0.023169f, 0.161847f, 0.038919f), new Vector3(0.010967f, 0.149102f, 0.050558f), new Vector3(-0.025781f, 0.177038f, 0.025226f), new Vector3(-0.023169f, 0.161847f, 0.038919f), new Vector3(-0.010967f, 0.149102f, 0.050558f) };     //Neck
            meshSeamVertices[1][1][0][4] = new Vector3[0];     //Waist
            meshSeamVertices[1][1][0][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[1][1][0][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[1][1][1] = new Vector3[7][];         //Puppy LOD1 seams
            meshSeamVertices[1][1][1][0] = new Vector3[] { new Vector3(0.035936f, 0.027821f, 0.017807f), new Vector3(0.033462f, 0.028613f, 0.020549f), new Vector3(0.027855f, 0.029024f, 0.022004f), new Vector3(0.022458f, 0.028546f, 0.020436f), new Vector3(0.019409f, 0.027614f, 0.017328f), new Vector3(0.01825f, 0.025246f, 0.010943f), new Vector3(0.019965f, 0.022352f, 0.004917f), new Vector3(0.024243f, 0.020769f, 0.00161f), new Vector3(0.03077f, 0.020477f, 0.001638f), new Vector3(0.035116f, 0.022422f, 0.005462f), new Vector3(0.036756f, 0.025328f, 0.011623f), new Vector3(-0.019409f, 0.027614f, 0.017328f), new Vector3(-0.022458f, 0.028546f, 0.020436f), new Vector3(-0.027855f, 0.029024f, 0.022004f), new Vector3(-0.033462f, 0.028613f, 0.020549f), new Vector3(-0.035936f, 0.027821f, 0.017807f), new Vector3(-0.036756f, 0.025328f, 0.011623f), new Vector3(-0.035116f, 0.022422f, 0.005462f), new Vector3(-0.03077f, 0.020477f, 0.001638f), new Vector3(-0.024243f, 0.020769f, 0.00161f), new Vector3(-0.019965f, 0.022352f, 0.004917f), new Vector3(-0.01825f, 0.025246f, 0.010943f), new Vector3(0.027483f, 0.020485f, 0.000958f), new Vector3(-0.027483f, 0.020485f, 0.000958f), new Vector3(0.033966f, 0.02768f, -0.151808f), new Vector3(0.035505f, 0.028593f, -0.157844f), new Vector3(0.03365801f, 0.029292f, -0.165822f), new Vector3(0.02771f, 0.029659f, -0.168411f), new Vector3(0.021542f, 0.029336f, -0.165628f), new Vector3(0.019765f, 0.028546f, -0.157617f), new Vector3(0.021437f, 0.027542f, -0.151612f), new Vector3(0.024334f, 0.027091f, -0.148868f), new Vector3(0.028081f, 0.027075f, -0.148205f), new Vector3(0.031514f, 0.027209f, -0.148604f), new Vector3(-0.021542f, 0.029336f, -0.165628f), new Vector3(-0.019765f, 0.028546f, -0.157617f), new Vector3(-0.021437f, 0.027542f, -0.151612f), new Vector3(-0.024334f, 0.027091f, -0.148868f), new Vector3(-0.028081f, 0.027075f, -0.148205f), new Vector3(-0.031514f, 0.027209f, -0.148604f), new Vector3(-0.03365801f, 0.029292f, -0.165822f), new Vector3(-0.02771f, 0.029659f, -0.168411f), new Vector3(-0.033966f, 0.02768f, -0.151808f), new Vector3(-0.035505f, 0.028593f, -0.157844f) };     //Ankles
            meshSeamVertices[1][1][1][1] = new Vector3[] { new Vector3(0.008108f, 0.141444f, -0.133774f), new Vector3(0.006446f, 0.148831f, -0.128919f), new Vector3(0f, 0.151951f, -0.1278f), new Vector3(0f, 0.135843f, -0.138939f), new Vector3(0.005545f, 0.138505f, -0.136499f), new Vector3(-0.008108f, 0.141444f, -0.133774f), new Vector3(-0.006445f, 0.148831f, -0.128919f), new Vector3(-0.005545f, 0.138505f, -0.136499f) };     //Tail
            meshSeamVertices[1][1][1][2] = new Vector3[] { new Vector3(-0.03149001f, 0.202056f, 0.044792f), new Vector3(-0.031583f, 0.198393f, 0.032406f), new Vector3(-0.029422f, 0.202985f, 0.0271f), new Vector3(-0.027024f, 0.208296f, 0.026351f), new Vector3(-0.023789f, 0.217317f, 0.030254f), new Vector3(-0.0268f, 0.213567f, 0.047074f), new Vector3(-0.024298f, 0.218782f, 0.043602f), new Vector3(-0.032299f, 0.199486f, 0.04085f), new Vector3(-0.028384f, 0.210043f, 0.047718f), new Vector3(-0.025254f, 0.216639f, 0.045571f), new Vector3(-0.023174f, 0.220216f, 0.035976f), new Vector3(-0.02485f, 0.213747f, 0.027885f), new Vector3(-0.031973f, 0.198507f, 0.036242f), new Vector3(0.03149001f, 0.202056f, 0.04479101f), new Vector3(0.032299f, 0.199486f, 0.04085f), new Vector3(0.031583f, 0.198393f, 0.032406f), new Vector3(0.029422f, 0.202985f, 0.0271f), new Vector3(0.027024f, 0.208296f, 0.026351f), new Vector3(0.023789f, 0.217317f, 0.030254f), new Vector3(0.0268f, 0.213567f, 0.047074f), new Vector3(0.024298f, 0.218782f, 0.043602f), new Vector3(0.028384f, 0.210043f, 0.047718f), new Vector3(0.025254f, 0.216639f, 0.045571f), new Vector3(0.023174f, 0.220216f, 0.035976f), new Vector3(0.02485f, 0.213747f, 0.027885f), new Vector3(0.031973f, 0.198507f, 0.036242f) };     //Ears
            meshSeamVertices[1][1][1][3] = new Vector3[] { new Vector3(0f, 0.145662f, 0.05409f), new Vector3(-0.015691f, 0.152266f, 0.04751001f), new Vector3(-0.024575f, 0.181631f, 0.021221f), new Vector3(-0.020594f, 0.187709f, 0.015912f), new Vector3(-0.01529f, 0.192578f, 0.011842f), new Vector3(-0.007906f, 0.195485f, 0.009356001f), new Vector3(0f, 0.197183f, 0.007898f), new Vector3(-0.025166f, 0.166557f, 0.034826f), new Vector3(0.015691f, 0.152266f, 0.04751001f), new Vector3(0.021641f, 0.159119f, 0.04131901f), new Vector3(0.0259f, 0.17444f, 0.027614f), new Vector3(0.024575f, 0.181631f, 0.021221f), new Vector3(0.01529f, 0.192578f, 0.011842f), new Vector3(0.008347f, 0.147951f, 0.051732f), new Vector3(0.025166f, 0.166557f, 0.034826f), new Vector3(0.007906f, 0.195485f, 0.009356001f), new Vector3(0.020594f, 0.187709f, 0.015912f), new Vector3(-0.0259f, 0.17444f, 0.027614f), new Vector3(-0.021641f, 0.159119f, 0.04131901f), new Vector3(-0.008347f, 0.147951f, 0.051732f) };     //Neck
            meshSeamVertices[1][1][1][4] = new Vector3[0];     //Waist
            meshSeamVertices[1][1][1][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[1][1][1][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[1][1][2] = new Vector3[7][];         //Puppy LOD2 seams
            meshSeamVertices[1][1][2][0] = new Vector3[] { new Vector3(0.036768f, 0.026228f, 0.013627f), new Vector3(0.033462f, 0.028613f, 0.020549f), new Vector3(0.022458f, 0.028546f, 0.020436f), new Vector3(0.018312f, 0.026146f, 0.013025f), new Vector3(0.019965f, 0.022352f, 0.004917f), new Vector3(0.035116f, 0.022422f, 0.005462f), new Vector3(-0.018312f, 0.026146f, 0.013025f), new Vector3(-0.022458f, 0.028546f, 0.020436f), new Vector3(-0.033462f, 0.028613f, 0.020549f), new Vector3(-0.036768f, 0.026228f, 0.013627f), new Vector3(-0.035116f, 0.022422f, 0.005462f), new Vector3(-0.019965f, 0.022352f, 0.004917f), new Vector3(0.027483f, 0.020485f, 0.000958f), new Vector3(-0.027483f, 0.020485f, 0.000958f), new Vector3(0.035221f, 0.033182f, -0.154798f), new Vector3(0.03444f, 0.035381f, -0.164102f), new Vector3(0.027588f, 0.036252f, -0.168875f), new Vector3(0.020287f, 0.035234f, -0.163587f), new Vector3(0.01974f, 0.033091f, -0.154437f), new Vector3(0.024645f, 0.031387f, -0.147424f), new Vector3(0.031218f, 0.031604f, -0.147082f), new Vector3(-0.020287f, 0.035234f, -0.163587f), new Vector3(-0.01974f, 0.033091f, -0.154437f), new Vector3(-0.024645f, 0.031387f, -0.147424f), new Vector3(-0.031218f, 0.031604f, -0.147082f), new Vector3(-0.035221f, 0.033182f, -0.154798f), new Vector3(-0.03444f, 0.035381f, -0.164102f), new Vector3(-0.027588f, 0.036252f, -0.168875f) };     //Ankles
            meshSeamVertices[1][1][2][1] = new Vector3[] { new Vector3(0f, 0.151951f, -0.1278f), new Vector3(0f, 0.135843f, -0.138939f), new Vector3(0.007262f, 0.14805f, -0.129288f), new Vector3(-0.007262f, 0.14805f, -0.129289f) };     //Tail
            meshSeamVertices[1][1][2][2] = new Vector3[] { new Vector3(-0.03149001f, 0.202056f, 0.04479101f), new Vector3(-0.029873f, 0.206929f, 0.047409f), new Vector3(-0.032279f, 0.198817f, 0.03928f), new Vector3(-0.031583f, 0.198393f, 0.032406f), new Vector3(-0.027024f, 0.208296f, 0.026351f), new Vector3(-0.024298f, 0.218782f, 0.043602f), new Vector3(-0.026027f, 0.215103f, 0.046323f), new Vector3(-0.023174f, 0.220216f, 0.035976f), new Vector3(-0.02432f, 0.215532f, 0.02907f), new Vector3(0.03149001f, 0.202056f, 0.04479101f), new Vector3(0.029873f, 0.206929f, 0.047409f), new Vector3(0.032279f, 0.198817f, 0.03928f), new Vector3(0.031583f, 0.198393f, 0.032406f), new Vector3(0.027024f, 0.208296f, 0.026351f), new Vector3(0.02432f, 0.215532f, 0.02907f), new Vector3(0.026027f, 0.215103f, 0.046323f), new Vector3(0.024298f, 0.218782f, 0.043602f), new Vector3(0.023174f, 0.220216f, 0.035976f) };     //Ears
            meshSeamVertices[1][1][2][3] = new Vector3[] { new Vector3(0f, 0.145662f, 0.05409f), new Vector3(-0.020114f, 0.156391f, 0.043719f), new Vector3(-0.026019f, 0.171842f, 0.030003f), new Vector3(-0.024575f, 0.181631f, 0.021221f), new Vector3(-0.019194f, 0.18951f, 0.014426f), new Vector3(0f, 0.197183f, 0.007898f), new Vector3(-0.010536f, 0.19474f, 0.009992f), new Vector3(0.020114f, 0.156391f, 0.043719f), new Vector3(0.026019f, 0.171842f, 0.030003f), new Vector3(0.024575f, 0.181631f, 0.021221f), new Vector3(0.019194f, 0.18951f, 0.014426f), new Vector3(0.010536f, 0.19474f, 0.009992f), new Vector3(0.010967f, 0.149103f, 0.050557f), new Vector3(-0.010967f, 0.149103f, 0.050557f) };     //Neck
            meshSeamVertices[1][1][2][4] = new Vector3[0];     //Waist
            meshSeamVertices[1][1][2][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[1][1][2][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[1][1][3] = new Vector3[7][];         //Puppy LOD3 seams
            meshSeamVertices[1][1][3][0] = new Vector3[] { new Vector3(0.033462f, 0.028613f, 0.020549f), new Vector3(0.022458f, 0.028546f, 0.020436f), new Vector3(0.019965f, 0.022352f, 0.004917f), new Vector3(0.035116f, 0.022422f, 0.005462f), new Vector3(-0.022458f, 0.028546f, 0.020436f), new Vector3(-0.033462f, 0.028613f, 0.020549f), new Vector3(-0.035116f, 0.022422f, 0.005462f), new Vector3(-0.019965f, 0.022352f, 0.004917f), new Vector3(0.027483f, 0.020485f, 0.000958f), new Vector3(-0.027483f, 0.020485f, 0.000958f), new Vector3(0.03444f, 0.035381f, -0.164102f), new Vector3(0.027588f, 0.036252f, -0.168875f), new Vector3(0.020287f, 0.035234f, -0.163587f), new Vector3(0.024645f, 0.031387f, -0.147424f), new Vector3(0.031218f, 0.031604f, -0.147082f), new Vector3(-0.020287f, 0.035234f, -0.163587f), new Vector3(-0.024645f, 0.031387f, -0.147424f), new Vector3(-0.031218f, 0.031604f, -0.147082f), new Vector3(-0.03444f, 0.035381f, -0.164102f), new Vector3(-0.027588f, 0.036252f, -0.168875f) };     //Ankles
            meshSeamVertices[1][1][3][1] = new Vector3[] { new Vector3(0f, 0.151951f, -0.1278f), new Vector3(0f, 0.135843f, -0.138939f), new Vector3(0.007824f, 0.147512f, -0.129544f), new Vector3(-0.007824f, 0.147512f, -0.129544f) };     //Tail
            meshSeamVertices[1][1][3][2] = new Vector3[] { new Vector3(-0.030682f, 0.204493f, 0.0461f), new Vector3(-0.031915f, 0.198441f, 0.035844f), new Vector3(-0.027024f, 0.208296f, 0.026351f), new Vector3(-0.024986f, 0.217198f, 0.045089f), new Vector3(-0.023382f, 0.219023f, 0.032543f), new Vector3(0.030682f, 0.204493f, 0.0461f), new Vector3(0.031915f, 0.198441f, 0.035844f), new Vector3(0.027024f, 0.208296f, 0.026351f), new Vector3(0.024986f, 0.217198f, 0.045089f), new Vector3(0.023382f, 0.219023f, 0.032543f) };     //Ears
            meshSeamVertices[1][1][3][3] = new Vector3[] { new Vector3(0f, 0.145662f, 0.05409f), new Vector3(-0.01605f, 0.152601f, 0.047202f), new Vector3(-0.025785f, 0.176954f, 0.025303f), new Vector3(0f, 0.197183f, 0.007898f), new Vector3(-0.0154f, 0.192492f, 0.011914f), new Vector3(0.025785f, 0.176954f, 0.025303f), new Vector3(0.0154f, 0.192492f, 0.011914f), new Vector3(0.01605f, 0.152601f, 0.047202f) };     //Neck
            meshSeamVertices[1][1][3][4] = new Vector3[0];     //Waist
            meshSeamVertices[1][1][3][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[1][1][3][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[2] = new Vector3[4][][][];        //ageSpecies
            meshSeamVertices[2][0] = new Vector3[4][][];         //Adult Cat
            meshSeamVertices[2][0][0] = new Vector3[7][];         //Adult Cat LOD0 seams
            meshSeamVertices[2][0][0][0] = new Vector3[] { new Vector3(0.03187f, 0.04709f, 0.04364f), new Vector3(0.0239f, 0.04833f, 0.05513f), new Vector3(0.02666f, 0.05041f, 0.06838f), new Vector3(0.03171f, 0.05114f, 0.07282f), new Vector3(0.04434f, 0.05157f, 0.07285f), new Vector3(0.04968f, 0.05119f, 0.06906f), new Vector3(0.05158f, 0.04802f, 0.04905f), new Vector3(0.04707f, 0.04709001f, 0.04262f), new Vector3(0.03326f, 0.052579f, -0.23745f), new Vector3(0.02857f, 0.052609f, -0.24304f), new Vector3(0.02676f, 0.052669f, -0.25031f), new Vector3(0.02741f, 0.052829f, -0.25657f), new Vector3(0.02854f, 0.052829f, -0.26258f), new Vector3(0.03167f, 0.052849f, -0.26771f), new Vector3(0.04803f, 0.052849f, -0.26868f), new Vector3(0.05164f, 0.052829f, -0.26331f), new Vector3(0.05357f, 0.052829f, -0.25688f), new Vector3(0.05385f, 0.052829f, -0.25001f), new Vector3(0.05187f, 0.052759f, -0.24285f), new Vector3(0.04686f, 0.052689f, -0.23775f), new Vector3(0.04009f, 0.052619f, -0.23539f), new Vector3(0.04026f, 0.052849f, -0.27037f), new Vector3(0.03794f, 0.05174f, 0.07458f), new Vector3(0.03962f, 0.04686f, 0.04151f), new Vector3(0.02381f, 0.04983f, 0.0623f), new Vector3(0.05228f, 0.05042f, 0.0631f), new Vector3(0.05311f, 0.04933f, 0.05638f), new Vector3(0.02697f, 0.04753f, 0.04869f), new Vector3(-0.03187f, 0.04709f, 0.04364f), new Vector3(-0.0239f, 0.04833f, 0.05513f), new Vector3(-0.02666f, 0.05041f, 0.06838f), new Vector3(-0.03171f, 0.05114f, 0.07282f), new Vector3(-0.04434f, 0.05157f, 0.07285f), new Vector3(-0.04968f, 0.05119f, 0.06906f), new Vector3(-0.05158f, 0.04802f, 0.04905f), new Vector3(-0.04707f, 0.04709001f, 0.04262f), new Vector3(-0.03326f, 0.052579f, -0.23745f), new Vector3(-0.02857f, 0.052609f, -0.24304f), new Vector3(-0.02676f, 0.052669f, -0.25031f), new Vector3(-0.02741f, 0.052829f, -0.25657f), new Vector3(-0.02854f, 0.052829f, -0.26258f), new Vector3(-0.03167f, 0.052849f, -0.26771f), new Vector3(-0.04803f, 0.052849f, -0.26868f), new Vector3(-0.05164f, 0.052829f, -0.26331f), new Vector3(-0.05357f, 0.052829f, -0.25688f), new Vector3(-0.05385f, 0.052829f, -0.25001f), new Vector3(-0.05187f, 0.052759f, -0.24285f), new Vector3(-0.04686f, 0.052689f, -0.23775f), new Vector3(-0.04009f, 0.052619f, -0.23539f), new Vector3(-0.04026f, 0.052849f, -0.27037f), new Vector3(-0.03794f, 0.05174f, 0.07458f), new Vector3(-0.03962f, 0.04686f, 0.04151f), new Vector3(-0.02381f, 0.04983f, 0.0623f), new Vector3(-0.05228f, 0.05042f, 0.0631f), new Vector3(-0.05311f, 0.04933f, 0.05638f), new Vector3(-0.02697f, 0.04753f, 0.04869f) };     //Ankles
            meshSeamVertices[2][0][0][1] = new Vector3[] { new Vector3(0.01765f, 0.26168f, -0.23504f), new Vector3(0f, 0.25412f, -0.23691f), new Vector3(0.0238f, 0.27703f, -0.22732f), new Vector3(0.01977f, 0.28779f, -0.22239f), new Vector3(0.01326f, 0.29217f, -0.2198f), new Vector3(0f, 0.29452f, -0.21842f), new Vector3(-0.01326f, 0.29217f, -0.2198f), new Vector3(-0.01977f, 0.28779f, -0.22239f), new Vector3(-0.0238f, 0.27703f, -0.22732f), new Vector3(-0.01765f, 0.26168f, -0.23504f) };     //Tail
            meshSeamVertices[2][0][0][2] = new Vector3[] { new Vector3(-0.01822f, 0.36544f, 0.157701f), new Vector3(-0.0239f, 0.36349f, 0.158141f), new Vector3(-0.05495f, 0.34042f, 0.133701f), new Vector3(-0.02872f, 0.36095f, 0.113401f), new Vector3(-0.01819f, 0.37046f, 0.146851f), new Vector3(-0.04298f, 0.34484f, 0.113721f), new Vector3(-0.03462f, 0.35361f, 0.109461f), new Vector3(-0.05576f, 0.33682f, 0.130501f), new Vector3(-0.05117f, 0.34445f, 0.137631f), new Vector3(-0.04628f, 0.34964f, 0.138691f), new Vector3(-0.03829f, 0.35765f, 0.144011f), new Vector3(-0.03082f, 0.36234f, 0.150591f), new Vector3(-0.02294f, 0.36797f, 0.122981f), new Vector3(-0.02f, 0.37197f, 0.134161f), new Vector3(-0.05104f, 0.33899f, 0.121241f), new Vector3(0.01822f, 0.36544f, 0.157701f), new Vector3(0.0239f, 0.36349f, 0.158141f), new Vector3(0.05495f, 0.34042f, 0.133701f), new Vector3(0.02872f, 0.36095f, 0.113401f), new Vector3(0.01819f, 0.37046f, 0.146851f), new Vector3(0.04298f, 0.34484f, 0.113721f), new Vector3(0.03462f, 0.35361f, 0.109461f), new Vector3(0.05576f, 0.33682f, 0.130501f), new Vector3(0.05117f, 0.34445f, 0.137631f), new Vector3(0.04628f, 0.34964f, 0.138691f), new Vector3(0.03829f, 0.35765f, 0.144011f), new Vector3(0.03082f, 0.36234f, 0.150591f), new Vector3(0.02294f, 0.36797f, 0.122981f), new Vector3(0.02f, 0.37197f, 0.134161f), new Vector3(0.05104f, 0.33899f, 0.121241f) };     //Ears
            meshSeamVertices[2][0][0][3] = new Vector3[] { new Vector3(0f, 0.27073f, 0.134301f), new Vector3(0f, 0.32871f, 0.075801f), new Vector3(0.042f, 0.31052f, 0.093541f), new Vector3(0.04632f, 0.30462f, 0.100251f), new Vector3(0.04845f, 0.29819f, 0.107681f), new Vector3(0.04489f, 0.29179f, 0.114121f), new Vector3(0.03882f, 0.28568f, 0.120121f), new Vector3(0.03178f, 0.28098f, 0.124551f), new Vector3(0.02408f, 0.27728f, 0.127931f), new Vector3(0.0159f, 0.27415f, 0.130781f), new Vector3(0.007960001f, 0.27215f, 0.132871f), new Vector3(0.01074f, 0.3273f, 0.077111f), new Vector3(0.02059f, 0.32455f, 0.079541f), new Vector3(0.02916f, 0.32087f, 0.083081f), new Vector3(0.03623001f, 0.31613f, 0.087741f), new Vector3(-0.042f, 0.31052f, 0.093541f), new Vector3(-0.04632f, 0.30462f, 0.100251f), new Vector3(-0.04845f, 0.29819f, 0.107681f), new Vector3(-0.04489f, 0.29179f, 0.114121f), new Vector3(-0.03882f, 0.28568f, 0.120121f), new Vector3(-0.03178f, 0.28098f, 0.124551f), new Vector3(-0.02408f, 0.27728f, 0.127931f), new Vector3(-0.0159f, 0.27415f, 0.130781f), new Vector3(-0.007960001f, 0.27215f, 0.132871f), new Vector3(-0.01074f, 0.3273f, 0.077111f), new Vector3(-0.02059f, 0.32455f, 0.079541f), new Vector3(-0.02916f, 0.32087f, 0.083081f), new Vector3(-0.03623001f, 0.31613f, 0.087741f) };     //Neck
            meshSeamVertices[2][0][0][4] = new Vector3[0];     //Waist
            meshSeamVertices[2][0][0][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[2][0][0][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[2][0][1] = new Vector3[7][];         //Adult Cat LOD1 seams
            meshSeamVertices[2][0][1][0] = new Vector3[] { new Vector3(0.03187f, 0.04709f, 0.04364f), new Vector3(0.02666f, 0.05041f, 0.06838f), new Vector3(0.03171f, 0.05114f, 0.07282f), new Vector3(0.04114f, 0.051655f, 0.073715f), new Vector3(0.04968f, 0.05119f, 0.06906f), new Vector3(0.05158f, 0.04802f, 0.04905f), new Vector3(0.04707f, 0.04709001f, 0.04262f), new Vector3(0.03326f, 0.052579f, -0.23745f), new Vector3(0.02857f, 0.052609f, -0.24304f), new Vector3(0.027085f, 0.052749f, -0.25344f), new Vector3(0.03167f, 0.052849f, -0.26771f), new Vector3(0.049835f, 0.052839f, -0.265995f), new Vector3(0.05371f, 0.052829f, -0.253445f), new Vector3(0.049365f, 0.052724f, -0.2403f), new Vector3(0.04009f, 0.052619f, -0.23539f), new Vector3(0.04026f, 0.052849f, -0.27037f), new Vector3(0.03962f, 0.04686f, 0.04151f), new Vector3(0.023855f, 0.04908f, 0.058715f), new Vector3(0.052695f, 0.049875f, 0.05974f), new Vector3(0.02697f, 0.04753f, 0.04869f), new Vector3(-0.03187f, 0.04709f, 0.04364f), new Vector3(-0.02666f, 0.05041f, 0.06838f), new Vector3(-0.03171f, 0.05114f, 0.07282f), new Vector3(-0.04114f, 0.051655f, 0.073715f), new Vector3(-0.04968f, 0.05119f, 0.06906f), new Vector3(-0.05158f, 0.04802f, 0.04905f), new Vector3(-0.04707f, 0.04709001f, 0.04262f), new Vector3(-0.03326f, 0.052579f, -0.23745f), new Vector3(-0.02857f, 0.052609f, -0.24304f), new Vector3(-0.027085f, 0.052749f, -0.25344f), new Vector3(-0.03167f, 0.052849f, -0.26771f), new Vector3(-0.049835f, 0.052839f, -0.265995f), new Vector3(-0.05371f, 0.052829f, -0.253445f), new Vector3(-0.049365f, 0.052724f, -0.2403f), new Vector3(-0.04009f, 0.052619f, -0.23539f), new Vector3(-0.04026f, 0.052849f, -0.27037f), new Vector3(-0.03962f, 0.04686f, 0.04151f), new Vector3(-0.023855f, 0.04908f, 0.058715f), new Vector3(-0.052695f, 0.049875f, 0.05974f), new Vector3(-0.02697f, 0.04753f, 0.04869f) };     //Ankles
            meshSeamVertices[2][0][1][1] = new Vector3[] { new Vector3(0.01765f, 0.26168f, -0.23504f), new Vector3(0f, 0.25412f, -0.23691f), new Vector3(0.0238f, 0.27703f, -0.22732f), new Vector3(0.016515f, 0.28998f, -0.221095f), new Vector3(0f, 0.29452f, -0.21842f), new Vector3(-0.016515f, 0.28998f, -0.221095f), new Vector3(-0.0238f, 0.27703f, -0.22732f), new Vector3(-0.01765f, 0.26168f, -0.23504f) };     //Tail
            meshSeamVertices[2][0][1][2] = new Vector3[] { new Vector3(0.01822f, 0.36544f, 0.157701f), new Vector3(0.0239f, 0.36349f, 0.158141f), new Vector3(0.05495f, 0.34042f, 0.133701f), new Vector3(0.02583f, 0.36446f, 0.118191f), new Vector3(0.01819f, 0.37046f, 0.146851f), new Vector3(0.03462f, 0.35361f, 0.109461f), new Vector3(0.05576f, 0.33682f, 0.130501f), new Vector3(0.05117f, 0.34445f, 0.137631f), new Vector3(0.04628f, 0.34964f, 0.138691f), new Vector3(0.034555f, 0.359995f, 0.147301f), new Vector3(0.02f, 0.37197f, 0.134161f), new Vector3(0.04701f, 0.341915f, 0.117481f), new Vector3(-0.01822f, 0.36544f, 0.157701f), new Vector3(-0.0239f, 0.36349f, 0.158141f), new Vector3(-0.05495f, 0.34042f, 0.133701f), new Vector3(-0.02583f, 0.36446f, 0.118191f), new Vector3(-0.01819f, 0.37046f, 0.146851f), new Vector3(-0.03462f, 0.35361f, 0.109461f), new Vector3(-0.05576f, 0.33682f, 0.130501f), new Vector3(-0.05117f, 0.34445f, 0.137631f), new Vector3(-0.04628f, 0.34964f, 0.138691f), new Vector3(-0.034555f, 0.359995f, 0.147301f), new Vector3(-0.02f, 0.37197f, 0.134161f), new Vector3(-0.04701f, 0.341915f, 0.117481f) };     //Ears
            meshSeamVertices[2][0][1][3] = new Vector3[] { new Vector3(0f, 0.27073f, 0.134301f), new Vector3(0f, 0.32871f, 0.075801f), new Vector3(0.039115f, 0.313325f, 0.09064101f), new Vector3(0.04632f, 0.30462f, 0.100251f), new Vector3(0.04667f, 0.29499f, 0.110901f), new Vector3(0.03882f, 0.28568f, 0.120121f), new Vector3(0.02793f, 0.27913f, 0.126241f), new Vector3(0.01193f, 0.27315f, 0.131826f), new Vector3(0.015665f, 0.325925f, 0.078326f), new Vector3(0.02916f, 0.32087f, 0.083081f), new Vector3(-0.039115f, 0.313325f, 0.09064101f), new Vector3(-0.04632f, 0.30462f, 0.100251f), new Vector3(-0.04667f, 0.29499f, 0.110901f), new Vector3(-0.03882f, 0.28568f, 0.120121f), new Vector3(-0.02793f, 0.27913f, 0.126241f), new Vector3(-0.01193f, 0.27315f, 0.131826f), new Vector3(-0.015665f, 0.325925f, 0.078326f), new Vector3(-0.02916f, 0.32087f, 0.083081f) };     //Neck
            meshSeamVertices[2][0][1][4] = new Vector3[0];     //Waist
            meshSeamVertices[2][0][1][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[2][0][1][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[2][0][2] = new Vector3[7][];         //Adult Cat LOD2 seams
            meshSeamVertices[2][0][2][0] = new Vector3[] { new Vector3(0.03187f, 0.04709f, 0.04364f), new Vector3(0.029185f, 0.050775f, 0.0706f), new Vector3(0.04114f, 0.051655f, 0.073715f), new Vector3(0.04968f, 0.05119f, 0.06906f), new Vector3(0.052137f, 0.048947f, 0.054395f), new Vector3(0.02857f, 0.052609f, -0.24304f), new Vector3(0.027085f, 0.052749f, -0.25344f), new Vector3(0.030105f, 0.052839f, -0.265145f), new Vector3(0.049835f, 0.052839f, -0.265995f), new Vector3(0.05371f, 0.052829f, -0.253445f), new Vector3(0.049365f, 0.052724f, -0.2403f), new Vector3(0.036675f, 0.05259901f, -0.23642f), new Vector3(0.04026f, 0.052849f, -0.27037f), new Vector3(0.043345f, 0.046975f, 0.042065f), new Vector3(0.025412f, 0.048305f, 0.053703f), new Vector3(-0.03187f, 0.04709f, 0.04364f), new Vector3(-0.029185f, 0.050775f, 0.0706f), new Vector3(-0.04114f, 0.051655f, 0.073715f), new Vector3(-0.04968f, 0.05119f, 0.06906f), new Vector3(-0.052137f, 0.048947f, 0.054395f), new Vector3(-0.02857f, 0.052609f, -0.24304f), new Vector3(-0.027085f, 0.052749f, -0.25344f), new Vector3(-0.030105f, 0.052839f, -0.265145f), new Vector3(-0.049835f, 0.052839f, -0.265995f), new Vector3(-0.05371f, 0.052829f, -0.253445f), new Vector3(-0.049365f, 0.052724f, -0.2403f), new Vector3(-0.036675f, 0.05259901f, -0.23642f), new Vector3(-0.04026f, 0.052849f, -0.27037f), new Vector3(-0.043345f, 0.046975f, 0.042065f), new Vector3(-0.025412f, 0.048305f, 0.053703f) };     //Ankles
            meshSeamVertices[2][0][2][1] = new Vector3[] { new Vector3(0.01765f, 0.26168f, -0.23504f), new Vector3(0f, 0.25412f, -0.23691f), new Vector3(0.020157f, 0.283505f, -0.224207f), new Vector3(0f, 0.29452f, -0.21842f), new Vector3(-0.020157f, 0.283505f, -0.224207f), new Vector3(-0.01765f, 0.26168f, -0.23504f) };     //Tail
            meshSeamVertices[2][0][2][2] = new Vector3[] { new Vector3(0.0239f, 0.36349f, 0.158141f), new Vector3(0.05495f, 0.34042f, 0.133701f), new Vector3(0.018205f, 0.36795f, 0.152276f), new Vector3(0.03167f, 0.35728f, 0.111431f), new Vector3(0.05576f, 0.33682f, 0.130501f), new Vector3(0.05117f, 0.34445f, 0.137631f), new Vector3(0.042285f, 0.353645f, 0.141351f), new Vector3(0.02147f, 0.36997f, 0.128571f), new Vector3(0.04701f, 0.341915f, 0.117481f), new Vector3(0.03082f, 0.36234f, 0.150591f), new Vector3(-0.0239f, 0.36349f, 0.158141f), new Vector3(-0.05495f, 0.34042f, 0.133701f), new Vector3(-0.018205f, 0.36795f, 0.152276f), new Vector3(-0.03167f, 0.35728f, 0.111431f), new Vector3(-0.05576f, 0.33682f, 0.130501f), new Vector3(-0.05117f, 0.34445f, 0.137631f), new Vector3(-0.042285f, 0.353645f, 0.141351f), new Vector3(-0.03082f, 0.36234f, 0.150591f), new Vector3(-0.02147f, 0.36997f, 0.128571f), new Vector3(-0.04701f, 0.341915f, 0.117481f) };     //Ears
            meshSeamVertices[2][0][2][3] = new Vector3[] { new Vector3(0f, 0.27073f, 0.134301f), new Vector3(0f, 0.32871f, 0.075801f), new Vector3(0.039115f, 0.313325f, 0.09064101f), new Vector3(0.04632f, 0.30462f, 0.100251f), new Vector3(0.04274501f, 0.290335f, 0.115511f), new Vector3(0.01993f, 0.27614f, 0.129034f), new Vector3(0.022412f, 0.323397f, 0.08070301f), new Vector3(-0.039115f, 0.313325f, 0.09064101f), new Vector3(-0.04632f, 0.30462f, 0.100251f), new Vector3(-0.04274501f, 0.290335f, 0.115511f), new Vector3(-0.01993f, 0.27614f, 0.129034f), new Vector3(-0.022412f, 0.323397f, 0.08070301f) };     //Neck
            meshSeamVertices[2][0][2][4] = new Vector3[0];     //Waist
            meshSeamVertices[2][0][2][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[2][0][2][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[2][0][3] = new Vector3[7][];         //Adult Cat LOD3 seams
            meshSeamVertices[2][0][3][0] = new Vector3[] { new Vector3(0.04114f, 0.051655f, 0.073715f), new Vector3(0.04968f, 0.05119f, 0.06906f), new Vector3(0.052137f, 0.048947f, 0.054395f), new Vector3(0.02857f, 0.052609f, -0.24304f), new Vector3(0.027085f, 0.052749f, -0.25344f), new Vector3(0.030105f, 0.052839f, -0.265145f), new Vector3(0.05371f, 0.052829f, -0.253445f), new Vector3(0.04302f, 0.052661f, -0.23836f), new Vector3(0.045053f, 0.052844f, -0.268183f), new Vector3(0.043345f, 0.046975f, 0.042065f), new Vector3(0.028641f, 0.047697f, 0.048672f), new Vector3(-0.04114f, 0.051655f, 0.073715f), new Vector3(-0.04968f, 0.05119f, 0.06906f), new Vector3(-0.052137f, 0.048947f, 0.054395f), new Vector3(-0.02857f, 0.052609f, -0.24304f), new Vector3(-0.027085f, 0.052749f, -0.25344f), new Vector3(-0.030105f, 0.052839f, -0.265145f), new Vector3(-0.05371f, 0.052829f, -0.253445f), new Vector3(-0.04302f, 0.052661f, -0.23836f), new Vector3(-0.045053f, 0.052844f, -0.268183f), new Vector3(-0.043345f, 0.046975f, 0.042065f), new Vector3(-0.028641f, 0.047697f, 0.048672f) };     //Ankles
            meshSeamVertices[2][0][3][1] = new Vector3[] { new Vector3(0.01765f, 0.26168f, -0.23504f), new Vector3(0f, 0.25412f, -0.23691f), new Vector3(0.020157f, 0.283505f, -0.224207f), new Vector3(-0.020157f, 0.283505f, -0.224207f), new Vector3(-0.01765f, 0.26168f, -0.23504f), new Vector3(0f, 0.29452f, -0.21842f) };     //Tail
            meshSeamVertices[2][0][3][2] = new Vector3[] { new Vector3(0.0239f, 0.36349f, 0.158141f), new Vector3(0.05495f, 0.34042f, 0.133701f), new Vector3(0.018205f, 0.36795f, 0.152276f), new Vector3(0.03167f, 0.35728f, 0.111431f), new Vector3(0.036552f, 0.357993f, 0.145971f), new Vector3(0.021477f, 0.369976f, 0.128572f), new Vector3(0.04701f, 0.341915f, 0.117481f), new Vector3(-0.0239f, 0.36349f, 0.158141f), new Vector3(-0.05495f, 0.34042f, 0.133701f), new Vector3(-0.018205f, 0.36795f, 0.152276f), new Vector3(-0.03167f, 0.35728f, 0.111431f), new Vector3(-0.036552f, 0.357993f, 0.145971f), new Vector3(-0.021477f, 0.369976f, 0.128572f), new Vector3(-0.04701f, 0.341915f, 0.117481f) };     //Ears
            meshSeamVertices[2][0][3][3] = new Vector3[] { new Vector3(0f, 0.27073f, 0.134301f), new Vector3(0f, 0.32871f, 0.075801f), new Vector3(0.042717f, 0.308972f, 0.09544601f), new Vector3(0.04274501f, 0.290335f, 0.115511f), new Vector3(0.01993f, 0.27614f, 0.129034f), new Vector3(0.022412f, 0.323397f, 0.08070301f), new Vector3(-0.042717f, 0.308972f, 0.09544601f), new Vector3(-0.04274501f, 0.290335f, 0.115511f), new Vector3(-0.01993f, 0.27614f, 0.129034f), new Vector3(-0.022412f, 0.323397f, 0.08070301f) };     //Neck
            meshSeamVertices[2][0][3][4] = new Vector3[0];     //Waist
            meshSeamVertices[2][0][3][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[2][0][3][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[2][1] = new Vector3[4][][];         //Kitten
            meshSeamVertices[2][1][0] = new Vector3[7][];         //Kitten LOD0 seams
            meshSeamVertices[2][1][0][0] = new Vector3[] { new Vector3(0.018497f, 0.026379f, 0.002009f), new Vector3(0.012387f, 0.028202f, 0.010199f), new Vector3(0.014981f, 0.030418f, 0.020039f), new Vector3(0.01866f, 0.031186f, 0.023475f), new Vector3(0.027665f, 0.03118f, 0.023433f), new Vector3(0.031687f, 0.030394f, 0.019928f), new Vector3(0.033337f, 0.027245f, 0.00572f), new Vector3(0.030188f, 0.026212f, 0.001113f), new Vector3(0.018861f, 0.029619f, -0.130957f), new Vector3(0.014863f, 0.029804f, -0.135698f), new Vector3(0.013112f, 0.02993f, -0.140976f), new Vector3(0.013336f, 0.030006f, -0.145075f), new Vector3(0.014588f, 0.03012f, -0.149169f), new Vector3(0.017892f, 0.030157f, -0.153162f), new Vector3(0.029672f, 0.030085f, -0.153938f), new Vector3(0.033145f, 0.030043f, -0.150467f), new Vector3(0.034879f, 0.029977f, -0.146234f), new Vector3(0.035254f, 0.02987f, -0.141454f), new Vector3(0.033931f, 0.029635f, -0.134763f), new Vector3(0.030054f, 0.029504f, -0.129609f), new Vector3(0.024389f, 0.029514f, -0.128336f), new Vector3(0.023928f, 0.0301f, -0.154478f), new Vector3(0.023195f, 0.031494f, 0.024832f), new Vector3(0.024602f, 0.02597f, 9.000001E-05f), new Vector3(0.012652f, 0.029413f, 0.015535f), new Vector3(0.033369f, 0.029321f, 0.015074f), new Vector3(0.033974f, 0.028346f, 0.010665f), new Vector3(0.014679f, 0.027247f, 0.005929f), new Vector3(-0.018497f, 0.026379f, 0.002009f), new Vector3(-0.012387f, 0.028202f, 0.010199f), new Vector3(-0.014981f, 0.030418f, 0.020039f), new Vector3(-0.01866f, 0.031186f, 0.023475f), new Vector3(-0.027665f, 0.03118f, 0.023433f), new Vector3(-0.031687f, 0.030394f, 0.019928f), new Vector3(-0.033337f, 0.027245f, 0.00572f), new Vector3(-0.030188f, 0.026212f, 0.001113f), new Vector3(-0.018861f, 0.029619f, -0.130957f), new Vector3(-0.014863f, 0.029804f, -0.135698f), new Vector3(-0.013112f, 0.02993f, -0.140976f), new Vector3(-0.013336f, 0.030006f, -0.145075f), new Vector3(-0.014588f, 0.03012f, -0.149169f), new Vector3(-0.017892f, 0.030157f, -0.153162f), new Vector3(-0.029672f, 0.030085f, -0.153938f), new Vector3(-0.033145f, 0.030043f, -0.150467f), new Vector3(-0.034879f, 0.029977f, -0.146234f), new Vector3(-0.035254f, 0.02987f, -0.141454f), new Vector3(-0.033931f, 0.029635f, -0.134763f), new Vector3(-0.030054f, 0.029504f, -0.129609f), new Vector3(-0.024389f, 0.029514f, -0.128336f), new Vector3(-0.023928f, 0.0301f, -0.154478f), new Vector3(-0.023195f, 0.031494f, 0.024832f), new Vector3(-0.024602f, 0.02597f, 9.000001E-05f), new Vector3(-0.012652f, 0.029413f, 0.015535f), new Vector3(-0.033369f, 0.029321f, 0.015074f), new Vector3(-0.033974f, 0.028346f, 0.010665f), new Vector3(-0.014679f, 0.027247f, 0.005929f) };     //Ankles
            meshSeamVertices[2][1][0][1] = new Vector3[] { new Vector3(0f, 0.149463f, -0.115957f), new Vector3(0f, 0.129288f, -0.128139f), new Vector3(0.010423f, 0.146481f, -0.117671f), new Vector3(0.00681f, 0.14851f, -0.116407f), new Vector3(0.008279f, 0.133257f, -0.127143f), new Vector3(0.012724f, 0.141664f, -0.121088f), new Vector3(-0.010423f, 0.146481f, -0.117671f), new Vector3(-0.00681f, 0.14851f, -0.116407f), new Vector3(-0.008279f, 0.133257f, -0.127143f), new Vector3(-0.012724f, 0.141664f, -0.121088f) };     //Tail
            meshSeamVertices[2][1][0][2] = new Vector3[] { new Vector3(0.013742f, 0.209132f, 0.067178f), new Vector3(0.017547f, 0.207469f, 0.067133f), new Vector3(0.021947f, 0.205727f, 0.062852f), new Vector3(0.027174f, 0.201811f, 0.058545f), new Vector3(0.032633f, 0.197125f, 0.056024f), new Vector3(0.03535f, 0.193716f, 0.056012f), new Vector3(0.037451f, 0.190713f, 0.053592f), new Vector3(0.038267f, 0.187306f, 0.05108f), new Vector3(0.035437f, 0.186991f, 0.04509f), new Vector3(0.029585f, 0.189462f, 0.039499f), new Vector3(0.023855f, 0.193845f, 0.036314f), new Vector3(0.019417f, 0.19881f, 0.038247f), new Vector3(0.015631f, 0.204683f, 0.043522f), new Vector3(0.013788f, 0.208724f, 0.050557f), new Vector3(0.013337f, 0.210111f, 0.059452f), new Vector3(-0.013742f, 0.209132f, 0.067178f), new Vector3(-0.017547f, 0.207469f, 0.067133f), new Vector3(-0.013337f, 0.210111f, 0.059452f), new Vector3(-0.013788f, 0.208724f, 0.050557f), new Vector3(-0.015631f, 0.204683f, 0.043522f), new Vector3(-0.019417f, 0.19881f, 0.038247f), new Vector3(-0.023855f, 0.193845f, 0.036314f), new Vector3(-0.029585f, 0.189462f, 0.039499f), new Vector3(-0.035437f, 0.186991f, 0.04509f), new Vector3(-0.038267f, 0.187306f, 0.05108f), new Vector3(-0.037451f, 0.190713f, 0.053592f), new Vector3(-0.03535f, 0.193716f, 0.056012f), new Vector3(-0.032633f, 0.197125f, 0.056024f), new Vector3(-0.027174f, 0.201811f, 0.058545f), new Vector3(-0.021947f, 0.205727f, 0.062852f) };     //Ears
            meshSeamVertices[2][1][0][3] = new Vector3[] { new Vector3(0f, 0.142284f, 0.059373f), new Vector3(0f, 0.176906f, 0.019761f), new Vector3(0.025839f, 0.16723f, 0.029918f), new Vector3(0.028225f, 0.163564f, 0.034268f), new Vector3(0.029034f, 0.159363f, 0.039308f), new Vector3(0.02783f, 0.155405f, 0.043854f), new Vector3(0.024934f, 0.151424f, 0.048448f), new Vector3(0.020576f, 0.148134f, 0.052331f), new Vector3(0.015405f, 0.145635f, 0.055386f), new Vector3(0.010212f, 0.143867f, 0.057427f), new Vector3(0.004888f, 0.14275f, 0.058746f), new Vector3(0.006122f, 0.176382f, 0.020261f), new Vector3(0.012127f, 0.174964f, 0.021662f), new Vector3(0.017547f, 0.172861f, 0.023772f), new Vector3(0.022247f, 0.170202f, 0.026496f), new Vector3(-0.025839f, 0.16723f, 0.029918f), new Vector3(-0.028225f, 0.163564f, 0.034268f), new Vector3(-0.029034f, 0.159363f, 0.039308f), new Vector3(-0.02783f, 0.155405f, 0.043854f), new Vector3(-0.024934f, 0.151424f, 0.048448f), new Vector3(-0.020576f, 0.148134f, 0.052331f), new Vector3(-0.015405f, 0.145635f, 0.055386f), new Vector3(-0.010212f, 0.143867f, 0.057427f), new Vector3(-0.004888f, 0.14275f, 0.058746f), new Vector3(-0.006122f, 0.176382f, 0.020261f), new Vector3(-0.012127f, 0.174964f, 0.021662f), new Vector3(-0.017547f, 0.172861f, 0.023772f), new Vector3(-0.022247f, 0.170202f, 0.026496f) };     //Neck
            meshSeamVertices[2][1][0][4] = new Vector3[0];     //Waist
            meshSeamVertices[2][1][0][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[2][1][0][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[2][1][1] = new Vector3[7][];         //Kitten LOD1 seams
            meshSeamVertices[2][1][1][0] = new Vector3[] { new Vector3(0.018497f, 0.026379f, 0.002009f), new Vector3(0.014981f, 0.030418f, 0.020039f), new Vector3(0.01866f, 0.031186f, 0.023475f), new Vector3(0.02543f, 0.031337f, 0.024132f), new Vector3(0.031687f, 0.030394f, 0.019928f), new Vector3(0.033337f, 0.027245f, 0.00572f), new Vector3(0.030188f, 0.026212f, 0.001113f), new Vector3(0.018861f, 0.029619f, -0.130957f), new Vector3(0.014863f, 0.029804f, -0.135698f), new Vector3(0.013224f, 0.029968f, -0.143025f), new Vector3(0.017892f, 0.030157f, -0.153162f), new Vector3(0.031409f, 0.030064f, -0.152202f), new Vector3(0.035066f, 0.029924f, -0.143844f), new Vector3(0.031992f, 0.029569f, -0.132186f), new Vector3(0.024389f, 0.029514f, -0.128336f), new Vector3(0.023928f, 0.0301f, -0.154478f), new Vector3(0.024602f, 0.02597f, 9.000001E-05f), new Vector3(0.01252f, 0.028808f, 0.012867f), new Vector3(0.033671f, 0.028833f, 0.01287f), new Vector3(0.014679f, 0.027247f, 0.005929f), new Vector3(-0.018497f, 0.026379f, 0.002009f), new Vector3(-0.014981f, 0.030418f, 0.020039f), new Vector3(-0.01866f, 0.031186f, 0.023475f), new Vector3(-0.02543f, 0.031337f, 0.024132f), new Vector3(-0.031687f, 0.030394f, 0.019928f), new Vector3(-0.033337f, 0.027245f, 0.00572f), new Vector3(-0.030188f, 0.026212f, 0.001113f), new Vector3(-0.018861f, 0.029619f, -0.130957f), new Vector3(-0.014863f, 0.029804f, -0.135698f), new Vector3(-0.013224f, 0.029968f, -0.143025f), new Vector3(-0.017892f, 0.030157f, -0.153162f), new Vector3(-0.031409f, 0.030064f, -0.152202f), new Vector3(-0.035066f, 0.029924f, -0.143844f), new Vector3(-0.031992f, 0.029569f, -0.132186f), new Vector3(-0.024389f, 0.029514f, -0.128336f), new Vector3(-0.023928f, 0.0301f, -0.154478f), new Vector3(-0.024602f, 0.02597f, 9.000001E-05f), new Vector3(-0.01252f, 0.028808f, 0.012867f), new Vector3(-0.033671f, 0.028833f, 0.01287f), new Vector3(-0.014679f, 0.027247f, 0.005929f) };     //Ankles
            meshSeamVertices[2][1][1][1] = new Vector3[] { new Vector3(0f, 0.149463f, -0.115957f), new Vector3(0f, 0.129288f, -0.128139f), new Vector3(0.008616f, 0.147496f, -0.117039f), new Vector3(0.008279f, 0.133257f, -0.127143f), new Vector3(0.012724f, 0.141664f, -0.121088f), new Vector3(-0.008616f, 0.147496f, -0.117039f), new Vector3(-0.008279f, 0.133257f, -0.127143f), new Vector3(-0.012724f, 0.141664f, -0.121088f) };     //Tail
            meshSeamVertices[2][1][1][2] = new Vector3[] { new Vector3(0.013742f, 0.209132f, 0.067178f), new Vector3(0.017547f, 0.207469f, 0.067133f), new Vector3(0.024561f, 0.203769f, 0.060699f), new Vector3(0.032633f, 0.197125f, 0.056024f), new Vector3(0.03535f, 0.193716f, 0.056012f), new Vector3(0.037451f, 0.190713f, 0.053592f), new Vector3(0.038267f, 0.187306f, 0.05108f), new Vector3(0.032511f, 0.188226f, 0.042294f), new Vector3(0.023855f, 0.193845f, 0.036314f), new Vector3(0.017524f, 0.201747f, 0.040885f), new Vector3(0.013788f, 0.208724f, 0.050557f), new Vector3(0.013337f, 0.210111f, 0.059452f), new Vector3(-0.013742f, 0.209132f, 0.067178f), new Vector3(-0.017547f, 0.207469f, 0.067133f), new Vector3(-0.024561f, 0.203769f, 0.060699f), new Vector3(-0.032633f, 0.197125f, 0.056024f), new Vector3(-0.03535f, 0.193716f, 0.056012f), new Vector3(-0.037451f, 0.190713f, 0.053592f), new Vector3(-0.038267f, 0.187306f, 0.05108f), new Vector3(-0.032511f, 0.188226f, 0.042294f), new Vector3(-0.023855f, 0.193845f, 0.036314f), new Vector3(-0.017524f, 0.201747f, 0.040885f), new Vector3(-0.013788f, 0.208724f, 0.050557f), new Vector3(-0.013337f, 0.210111f, 0.059452f) };     //Ears
            meshSeamVertices[2][1][1][3] = new Vector3[] { new Vector3(0f, 0.142284f, 0.059373f), new Vector3(0f, 0.176906f, 0.019761f), new Vector3(0.024043f, 0.168716f, 0.028207f), new Vector3(0.028225f, 0.163564f, 0.034268f), new Vector3(0.028432f, 0.157384f, 0.041581f), new Vector3(0.024934f, 0.151424f, 0.048448f), new Vector3(0.01799f, 0.146885f, 0.053859f), new Vector3(0.00755f, 0.143309f, 0.058087f), new Vector3(0.009124f, 0.175673f, 0.020962f), new Vector3(0.017547f, 0.172861f, 0.023772f), new Vector3(-0.024043f, 0.168716f, 0.028207f), new Vector3(-0.028225f, 0.163564f, 0.034268f), new Vector3(-0.028432f, 0.157384f, 0.041581f), new Vector3(-0.024934f, 0.151424f, 0.048448f), new Vector3(-0.01799f, 0.146885f, 0.053859f), new Vector3(-0.00755f, 0.143309f, 0.058087f), new Vector3(-0.009124f, 0.175673f, 0.020962f), new Vector3(-0.017547f, 0.172861f, 0.023772f) };     //Neck
            meshSeamVertices[2][1][1][4] = new Vector3[0];     //Waist
            meshSeamVertices[2][1][1][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[2][1][1][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[2][1][2] = new Vector3[7][];         //Kitten LOD2 seams
            meshSeamVertices[2][1][2][0] = new Vector3[] { new Vector3(0.018497f, 0.026379f, 0.002009f), new Vector3(0.01682f, 0.030802f, 0.021757f), new Vector3(0.02543f, 0.031337f, 0.024132f), new Vector3(0.031687f, 0.030394f, 0.019928f), new Vector3(0.033811f, 0.028065f, 0.009403f), new Vector3(0.014863f, 0.029804f, -0.135698f), new Vector3(0.013224f, 0.029968f, -0.143025f), new Vector3(0.01624f, 0.030138f, -0.151166f), new Vector3(0.031409f, 0.030064f, -0.152202f), new Vector3(0.035066f, 0.029924f, -0.143844f), new Vector3(0.031992f, 0.029569f, -0.132186f), new Vector3(0.021625f, 0.029566f, -0.129647f), new Vector3(0.023928f, 0.0301f, -0.154478f), new Vector3(0.027395f, 0.026091f, 0.000601f), new Vector3(0.012889f, 0.027994f, 0.009263f), new Vector3(-0.018497f, 0.026379f, 0.002009f), new Vector3(-0.01682f, 0.030802f, 0.021757f), new Vector3(-0.02543f, 0.031337f, 0.024132f), new Vector3(-0.031687f, 0.030394f, 0.019928f), new Vector3(-0.033811f, 0.028065f, 0.009403f), new Vector3(-0.014863f, 0.029804f, -0.135698f), new Vector3(-0.013224f, 0.029968f, -0.143025f), new Vector3(-0.01624f, 0.030138f, -0.151166f), new Vector3(-0.031409f, 0.030064f, -0.152202f), new Vector3(-0.035066f, 0.029924f, -0.143844f), new Vector3(-0.031992f, 0.029569f, -0.132186f), new Vector3(-0.021625f, 0.029566f, -0.129647f), new Vector3(-0.023928f, 0.0301f, -0.154478f), new Vector3(-0.027395f, 0.026091f, 0.000601f), new Vector3(-0.012889f, 0.027994f, 0.009263f) };     //Ankles
            meshSeamVertices[2][1][2][1] = new Vector3[] { new Vector3(0f, 0.149463f, -0.115957f), new Vector3(0f, 0.129288f, -0.128139f), new Vector3(0.011052f, 0.144775f, -0.119061f), new Vector3(0.008279f, 0.133257f, -0.127143f), new Vector3(-0.011052f, 0.144775f, -0.119061f), new Vector3(-0.008279f, 0.133257f, -0.127143f) };     //Tail
            meshSeamVertices[2][1][2][2] = new Vector3[] { new Vector3(0.017547f, 0.207469f, 0.067133f), new Vector3(0.037451f, 0.190713f, 0.053592f), new Vector3(0.013539f, 0.209622f, 0.063315f), new Vector3(0.021636f, 0.196327f, 0.03728f), new Vector3(0.038267f, 0.187306f, 0.05108f), new Vector3(0.03535f, 0.193716f, 0.056012f), new Vector3(0.029904f, 0.199468f, 0.057284f), new Vector3(0.021947f, 0.205727f, 0.06285201f), new Vector3(0.01471f, 0.206704f, 0.047039f), new Vector3(0.032511f, 0.188226f, 0.042294f), new Vector3(-0.017547f, 0.207469f, 0.067133f), new Vector3(-0.037451f, 0.190713f, 0.053592f), new Vector3(-0.013539f, 0.209622f, 0.063315f), new Vector3(-0.021636f, 0.196327f, 0.03728f), new Vector3(-0.038267f, 0.187306f, 0.05108f), new Vector3(-0.03535f, 0.193716f, 0.056012f), new Vector3(-0.029904f, 0.199468f, 0.057284f), new Vector3(-0.021947f, 0.205727f, 0.06285201f), new Vector3(-0.01471f, 0.206704f, 0.047039f), new Vector3(-0.032511f, 0.188226f, 0.042294f) };     //Ears
            meshSeamVertices[2][1][2][3] = new Vector3[] { new Vector3(0f, 0.142284f, 0.059373f), new Vector3(0f, 0.176906f, 0.019761f), new Vector3(0.024043f, 0.168716f, 0.028207f), new Vector3(0.028225f, 0.163564f, 0.034268f), new Vector3(0.027186f, 0.154554f, 0.045188f), new Vector3(0.01322f, 0.144772f, 0.056121f), new Vector3(0.013405f, 0.174547f, 0.022202f), new Vector3(-0.024043f, 0.168716f, 0.028207f), new Vector3(-0.028225f, 0.163564f, 0.034268f), new Vector3(-0.027186f, 0.154554f, 0.045188f), new Vector3(-0.01322f, 0.144772f, 0.056121f), new Vector3(-0.013405f, 0.174547f, 0.022202f) };     //Neck
            meshSeamVertices[2][1][2][4] = new Vector3[0];     //Waist
            meshSeamVertices[2][1][2][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[2][1][2][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[2][1][3] = new Vector3[7][];         //Kitten LOD3 seams
            meshSeamVertices[2][1][3][0] = new Vector3[] { new Vector3(0.01483f, 0.03008f, 0.019832f), new Vector3(0.02543f, 0.031337f, 0.024132f), new Vector3(0.031687f, 0.030394f, 0.019928f), new Vector3(0.033811f, 0.028065f, 0.009403f), new Vector3(0.014863f, 0.029804f, -0.135698f), new Vector3(0.013224f, 0.029968f, -0.143025f), new Vector3(0.01624f, 0.030138f, -0.151166f), new Vector3(0.035066f, 0.029924f, -0.143844f), new Vector3(0.027232f, 0.029509f, -0.128975f), new Vector3(0.028032f, 0.030089f, -0.154092f), new Vector3(0.027395f, 0.026091f, 0.000601f), new Vector3(0.018497f, 0.026379f, 0.002009f), new Vector3(-0.01483f, 0.03008f, 0.019832f), new Vector3(-0.02543f, 0.031337f, 0.024132f), new Vector3(-0.031687f, 0.030394f, 0.019928f), new Vector3(-0.033811f, 0.028065f, 0.009403f), new Vector3(-0.014863f, 0.029804f, -0.135698f), new Vector3(-0.013224f, 0.029968f, -0.143025f), new Vector3(-0.01624f, 0.030138f, -0.151166f), new Vector3(-0.035066f, 0.029924f, -0.143844f), new Vector3(-0.027232f, 0.029509f, -0.128975f), new Vector3(-0.028032f, 0.030089f, -0.154092f), new Vector3(-0.027395f, 0.026091f, 0.000601f), new Vector3(-0.018497f, 0.026379f, 0.002009f) };     //Ankles
            meshSeamVertices[2][1][3][1] = new Vector3[] { new Vector3(0f, 0.149463f, -0.115957f), new Vector3(0f, 0.129288f, -0.128139f), new Vector3(0.011052f, 0.144775f, -0.119061f), new Vector3(0.008279001f, 0.133257f, -0.127143f), new Vector3(-0.011052f, 0.144775f, -0.119061f), new Vector3(-0.008279001f, 0.133257f, -0.127143f) };     //Tail
            meshSeamVertices[2][1][3][2] = new Vector3[] { new Vector3(0.017547f, 0.207469f, 0.067133f), new Vector3(0.037451f, 0.190713f, 0.053592f), new Vector3(0.013539f, 0.209622f, 0.063315f), new Vector3(0.021636f, 0.196327f, 0.03728f), new Vector3(0.025944f, 0.202331f, 0.059947f), new Vector3(0.01471f, 0.206704f, 0.047039f), new Vector3(0.032511f, 0.188226f, 0.042294f), new Vector3(-0.017547f, 0.207469f, 0.067133f), new Vector3(-0.037451f, 0.190713f, 0.053592f), new Vector3(-0.013539f, 0.209622f, 0.063315f), new Vector3(-0.021636f, 0.196327f, 0.03728f), new Vector3(-0.025944f, 0.202331f, 0.059947f), new Vector3(-0.01471f, 0.206704f, 0.047039f), new Vector3(-0.032511f, 0.188226f, 0.042294f) };     //Ears
            meshSeamVertices[2][1][3][3] = new Vector3[] { new Vector3(0f, 0.142284f, 0.059373f), new Vector3(0f, 0.176906f, 0.019761f), new Vector3(0.026663f, 0.166916f, 0.031727f), new Vector3(0.027186f, 0.154554f, 0.045188f), new Vector3(0.01322f, 0.144773f, 0.056121f), new Vector3(0.013405f, 0.174547f, 0.022202f), new Vector3(-0.026663f, 0.166916f, 0.031727f), new Vector3(-0.027186f, 0.154554f, 0.045188f), new Vector3(-0.01322f, 0.144773f, 0.056121f), new Vector3(-0.013405f, 0.174547f, 0.022202f) };     //Neck
            meshSeamVertices[2][1][3][4] = new Vector3[0];     //Waist
            meshSeamVertices[2][1][3][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[2][1][3][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[3] = new Vector3[4][][][];        //ageSpecies
            meshSeamVertices[3][0] = new Vector3[4][][];         //Adult LittleDog
            meshSeamVertices[3][0][0] = new Vector3[7][];         //Adult LittleDog LOD0 seams
            meshSeamVertices[3][0][0][0] = new Vector3[] { new Vector3(0.053266f, 0.05217f, 0.048082f), new Vector3(0.052265f, 0.052938f, 0.05303301f), new Vector3(0.04917501f, 0.053503f, 0.056396f), new Vector3(0.043012f, 0.053388f, 0.058182f), new Vector3(0.036192f, 0.052977f, 0.056148f), new Vector3(0.032485f, 0.052619f, 0.052324f), new Vector3(0.031167f, 0.052014f, 0.047213f), new Vector3(0.031019f, 0.050789f, 0.042328f), new Vector3(0.033381f, 0.049271f, 0.037794f), new Vector3(0.038213f, 0.047961f, 0.03346501f), new Vector3(0.046057f, 0.047652f, 0.033513f), new Vector3(0.05128f, 0.049458f, 0.038353f), new Vector3(0.053238f, 0.050979f, 0.04322f), new Vector3(-0.031168f, 0.052014f, 0.047213f), new Vector3(-0.032486f, 0.052619f, 0.052324f), new Vector3(-0.036193f, 0.052977f, 0.056148f), new Vector3(-0.043012f, 0.053388f, 0.058182f), new Vector3(-0.04917501f, 0.053503f, 0.056396f), new Vector3(-0.052266f, 0.052938f, 0.05303301f), new Vector3(-0.053266f, 0.05217f, 0.048082f), new Vector3(-0.053238f, 0.050979f, 0.04322f), new Vector3(-0.051281f, 0.049458f, 0.038353f), new Vector3(-0.046057f, 0.047652f, 0.033513f), new Vector3(-0.038213f, 0.047961f, 0.03346501f), new Vector3(-0.033382f, 0.049271f, 0.037794f), new Vector3(-0.031019f, 0.050789f, 0.042328f), new Vector3(0.042106f, 0.047806f, 0.032653f), new Vector3(-0.042107f, 0.047806f, 0.032653f), new Vector3(0.043172f, 0.04736301f, -0.243378f), new Vector3(0.043527f, 0.047413f, -0.248714f), new Vector3(0.042082f, 0.047456f, -0.253793f), new Vector3(0.039837f, 0.0475f, -0.257573f), new Vector3(0.033562f, 0.047528f, -0.258435f), new Vector3(0.026935f, 0.047482f, -0.256842f), new Vector3(0.024721f, 0.047435f, -0.253338f), new Vector3(0.023628f, 0.047404f, -0.248632f), new Vector3(0.023723f, 0.047364f, -0.243251f), new Vector3(0.025867f, 0.04733f, -0.237971f), new Vector3(0.02946f, 0.047351f, -0.235291f), new Vector3(0.033971f, 0.047386f, -0.234519f), new Vector3(0.038166f, 0.047399f, -0.234816f), new Vector3(0.041217f, 0.047377f, -0.238406f), new Vector3(-0.026935f, 0.047482f, -0.256842f), new Vector3(-0.024721f, 0.047435f, -0.253338f), new Vector3(-0.023628f, 0.047404f, -0.248632f), new Vector3(-0.023723f, 0.047364f, -0.243251f), new Vector3(-0.025867f, 0.04733f, -0.237971f), new Vector3(-0.02946f, 0.047351f, -0.235291f), new Vector3(-0.033971f, 0.047386f, -0.234519f), new Vector3(-0.038166f, 0.047399f, -0.234816f), new Vector3(-0.041217f, 0.047377f, -0.238406f), new Vector3(-0.043172f, 0.04736301f, -0.243378f), new Vector3(-0.043527f, 0.047413f, -0.248714f), new Vector3(-0.042082f, 0.047456f, -0.253793f), new Vector3(-0.039837f, 0.0475f, -0.257573f), new Vector3(-0.033562f, 0.047528f, -0.258435f) };     //Ankles
            meshSeamVertices[3][0][0][1] = new Vector3[] { new Vector3(0.01081f, 0.278458f, -0.210725f), new Vector3(0.010701f, 0.286293f, -0.205004f), new Vector3(0.006487f, 0.290322f, -0.203094f), new Vector3(0f, 0.292468f, -0.202558f), new Vector3(0f, 0.270991f, -0.21788f), new Vector3(0.007394f, 0.27454f, -0.214627f), new Vector3(-0.01081f, 0.278458f, -0.210725f), new Vector3(-0.010701f, 0.286293f, -0.205005f), new Vector3(-0.006487f, 0.290322f, -0.203094f), new Vector3(-0.007394f, 0.27454f, -0.214627f) };     //Tail
            meshSeamVertices[3][0][0][2] = new Vector3[] { new Vector3(-0.037003f, 0.357684f, 0.114917f), new Vector3(-0.035077f, 0.363509f, 0.118049f), new Vector3(-0.037946f, 0.35381f, 0.108325f), new Vector3(-0.037114f, 0.353302f, 0.100102f), new Vector3(-0.034529f, 0.358795f, 0.09375401f), new Vector3(-0.032545f, 0.364857f, 0.093388f), new Vector3(-0.029207f, 0.374027f, 0.097527f), new Vector3(-0.032384f, 0.370868f, 0.117098f), new Vector3(-0.02986f, 0.376394f, 0.113495f), new Vector3(-0.037995f, 0.355409f, 0.112079f), new Vector3(-0.033635f, 0.36704f, 0.118419f), new Vector3(-0.031054f, 0.373833f, 0.11554f), new Vector3(-0.028377f, 0.378899f, 0.107687f), new Vector3(-0.028461f, 0.376499f, 0.101057f), new Vector3(-0.030227f, 0.370931f, 0.095279f), new Vector3(-0.03758f, 0.353438f, 0.104691f), new Vector3(0.037003f, 0.357684f, 0.114917f), new Vector3(0.035077f, 0.363509f, 0.118049f), new Vector3(0.037946f, 0.35381f, 0.108325f), new Vector3(0.037114f, 0.353302f, 0.100102f), new Vector3(0.034529f, 0.358795f, 0.093755f), new Vector3(0.032545f, 0.364857f, 0.093388f), new Vector3(0.029207f, 0.374027f, 0.097528f), new Vector3(0.032383f, 0.370868f, 0.117098f), new Vector3(0.02986f, 0.376394f, 0.113495f), new Vector3(0.037995f, 0.355409f, 0.11208f), new Vector3(0.033635f, 0.36704f, 0.118419f), new Vector3(0.031054f, 0.373833f, 0.11554f), new Vector3(0.028377f, 0.378899f, 0.107688f), new Vector3(0.028461f, 0.376499f, 0.101057f), new Vector3(0.030227f, 0.370931f, 0.09528001f), new Vector3(0.03758f, 0.353438f, 0.104691f) };     //Ears
            meshSeamVertices[3][0][0][3] = new Vector3[] { new Vector3(0f, 0.295948f, 0.115474f), new Vector3(-0.017193f, 0.303061f, 0.109489f), new Vector3(-0.022039f, 0.307239f, 0.105983f), new Vector3(-0.030306f, 0.322173f, 0.093075f), new Vector3(-0.029397f, 0.332197f, 0.084304f), new Vector3(-0.022961f, 0.341648f, 0.075985f), new Vector3(-0.018291f, 0.345885f, 0.07224901f), new Vector3(-0.006312001f, 0.350373f, 0.068202f), new Vector3(0f, 0.351544f, 0.067248f), new Vector3(-0.006851f, 0.297333f, 0.114314f), new Vector3(-0.028882f, 0.316425f, 0.098026f), new Vector3(-0.012603f, 0.34862f, 0.069776f), new Vector3(-0.02631f, 0.336917f, 0.080184f), new Vector3(0.017193f, 0.303061f, 0.109489f), new Vector3(0.022039f, 0.307239f, 0.105983f), new Vector3(0.030306f, 0.322173f, 0.093075f), new Vector3(0.029397f, 0.332197f, 0.084304f), new Vector3(0.022961f, 0.341648f, 0.075985f), new Vector3(0.018291f, 0.345885f, 0.07224901f), new Vector3(0.006312001f, 0.350373f, 0.068202f), new Vector3(0.006851f, 0.297333f, 0.114314f), new Vector3(0.028882f, 0.316425f, 0.098026f), new Vector3(0.012603f, 0.34862f, 0.069776f), new Vector3(0.02631f, 0.336917f, 0.080184f), new Vector3(0.03084f, 0.3272f, 0.088763f), new Vector3(0.025984f, 0.311629f, 0.1024f), new Vector3(0.012586f, 0.299943f, 0.112173f), new Vector3(-0.03084f, 0.3272f, 0.088763f), new Vector3(-0.025984f, 0.311629f, 0.1024f), new Vector3(-0.012586f, 0.299943f, 0.112173f) };     //Neck
            meshSeamVertices[3][0][0][4] = new Vector3[0];     //Waist
            meshSeamVertices[3][0][0][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[3][0][0][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[3][0][1] = new Vector3[7][];         //Adult LittleDog LOD1 seams
            meshSeamVertices[3][0][1][0] = new Vector3[] { new Vector3(0.052266f, 0.052938f, 0.053031f), new Vector3(0.04917501f, 0.053503f, 0.056394f), new Vector3(0.043012f, 0.05338901f, 0.058181f), new Vector3(0.036192f, 0.052977f, 0.056146f), new Vector3(0.032486f, 0.05262f, 0.052323f), new Vector3(0.031093f, 0.051402f, 0.044769f), new Vector3(0.033381f, 0.049271f, 0.037793f), new Vector3(0.038213f, 0.047962f, 0.033463f), new Vector3(0.046057f, 0.047652f, 0.033511f), new Vector3(0.05128f, 0.049459f, 0.038351f), new Vector3(0.053252f, 0.051575f, 0.045649f), new Vector3(-0.032486f, 0.05262f, 0.052323f), new Vector3(-0.036192f, 0.052977f, 0.056146f), new Vector3(-0.043012f, 0.05338901f, 0.058181f), new Vector3(-0.04917501f, 0.053503f, 0.056394f), new Vector3(-0.052266f, 0.052938f, 0.053031f), new Vector3(-0.053252f, 0.051575f, 0.045649f), new Vector3(-0.05128f, 0.049459f, 0.038351f), new Vector3(-0.046057f, 0.047652f, 0.033511f), new Vector3(-0.038213f, 0.047962f, 0.033463f), new Vector3(-0.033381f, 0.049271f, 0.037793f), new Vector3(-0.031093f, 0.051402f, 0.044769f), new Vector3(0.042107f, 0.047807f, 0.032652f), new Vector3(-0.042107f, 0.047807f, 0.032652f), new Vector3(0.041092f, 0.051627f, -0.237833f), new Vector3(0.043291f, 0.052228f, -0.245455f), new Vector3(0.041029f, 0.052701f, -0.255115f), new Vector3(0.033536f, 0.052772f, -0.258351f), new Vector3(0.025744f, 0.052608f, -0.254548f), new Vector3(0.023714f, 0.052139f, -0.24501f), new Vector3(0.026088f, 0.051489f, -0.237482f), new Vector3(0.02968f, 0.051407f, -0.234561f), new Vector3(0.03399801f, 0.051474f, -0.233948f), new Vector3(0.037926f, 0.051507f, -0.234069f), new Vector3(-0.025744f, 0.052608f, -0.254548f), new Vector3(-0.023714f, 0.052139f, -0.24501f), new Vector3(-0.026088f, 0.051489f, -0.237482f), new Vector3(-0.02968f, 0.051407f, -0.234561f), new Vector3(-0.03399801f, 0.051474f, -0.233948f), new Vector3(-0.037926f, 0.051507f, -0.234069f), new Vector3(-0.041029f, 0.052701f, -0.255115f), new Vector3(-0.033536f, 0.052772f, -0.258351f), new Vector3(-0.041092f, 0.051627f, -0.237833f), new Vector3(-0.043291f, 0.052228f, -0.245455f) };     //Ankles
            meshSeamVertices[3][0][1][1] = new Vector3[] { new Vector3(0.01081f, 0.278458f, -0.210725f), new Vector3(0.008594f, 0.288307f, -0.204049f), new Vector3(0f, 0.292468f, -0.202558f), new Vector3(0f, 0.270991f, -0.21788f), new Vector3(0.007394f, 0.27454f, -0.214627f), new Vector3(-0.01081f, 0.278458f, -0.210725f), new Vector3(-0.008594f, 0.288307f, -0.20405f), new Vector3(-0.007394f, 0.27454f, -0.214627f) };     //Tail
            meshSeamVertices[3][0][1][2] = new Vector3[] { new Vector3(-0.037003f, 0.357684f, 0.114915f), new Vector3(-0.037114f, 0.353302f, 0.1001f), new Vector3(-0.034529f, 0.358794f, 0.093752f), new Vector3(-0.032545f, 0.364857f, 0.09338601f), new Vector3(-0.029207f, 0.374026f, 0.097525f), new Vector3(-0.032383f, 0.370868f, 0.117096f), new Vector3(-0.02986f, 0.376394f, 0.113493f), new Vector3(-0.03797f, 0.354609f, 0.1102f), new Vector3(-0.033635f, 0.36704f, 0.118417f), new Vector3(-0.031054f, 0.373832f, 0.115538f), new Vector3(-0.028419f, 0.377698f, 0.10437f), new Vector3(-0.030226f, 0.37093f, 0.095277f), new Vector3(-0.03758f, 0.353438f, 0.104689f), new Vector3(0.037003f, 0.357684f, 0.114915f), new Vector3(0.03797f, 0.354609f, 0.1102f), new Vector3(0.037114f, 0.353302f, 0.1001f), new Vector3(0.034529f, 0.358794f, 0.093753f), new Vector3(0.032545f, 0.364857f, 0.09338601f), new Vector3(0.029207f, 0.374026f, 0.097526f), new Vector3(0.032383f, 0.370868f, 0.117096f), new Vector3(0.02986f, 0.376394f, 0.113493f), new Vector3(0.033635f, 0.36704f, 0.118417f), new Vector3(0.031054f, 0.373832f, 0.115538f), new Vector3(0.028419f, 0.377698f, 0.10437f), new Vector3(0.030226f, 0.37093f, 0.09527801f), new Vector3(0.03758f, 0.353438f, 0.104689f) };     //Ears
            meshSeamVertices[3][0][1][3] = new Vector3[] { new Vector3(0f, 0.295948f, 0.115472f), new Vector3(-0.017193f, 0.303061f, 0.109487f), new Vector3(-0.029397f, 0.332197f, 0.08430301f), new Vector3(-0.024635f, 0.339282f, 0.078083f), new Vector3(-0.018291f, 0.345885f, 0.072248f), new Vector3(-0.009458f, 0.349496f, 0.06898801f), new Vector3(0f, 0.351544f, 0.067247f), new Vector3(-0.028882f, 0.316425f, 0.098024f), new Vector3(0.017193f, 0.303061f, 0.109487f), new Vector3(0.024012f, 0.309434f, 0.10419f), new Vector3(0.030573f, 0.324687f, 0.09091701f), new Vector3(0.029397f, 0.332197f, 0.08430301f), new Vector3(0.018291f, 0.345885f, 0.072248f), new Vector3(0.009719001f, 0.298638f, 0.113242f), new Vector3(0.028882f, 0.316425f, 0.098024f), new Vector3(0.009458f, 0.349496f, 0.06898801f), new Vector3(0.024635f, 0.339282f, 0.078083f), new Vector3(-0.030573f, 0.324687f, 0.09091701f), new Vector3(-0.024012f, 0.309434f, 0.10419f), new Vector3(-0.009719001f, 0.298638f, 0.113242f) };     //Neck
            meshSeamVertices[3][0][1][4] = new Vector3[0];     //Waist
            meshSeamVertices[3][0][1][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[3][0][1][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[3][0][2] = new Vector3[7][];         //Adult LittleDog LOD2 seams
            meshSeamVertices[3][0][2][0] = new Vector3[] { new Vector3(0.053266f, 0.05217f, 0.04808f), new Vector3(0.049175f, 0.053503f, 0.056394f), new Vector3(0.036192f, 0.052977f, 0.056146f), new Vector3(0.031168f, 0.052014f, 0.047211f), new Vector3(0.033381f, 0.049271f, 0.037793f), new Vector3(0.05128f, 0.049459f, 0.038351f), new Vector3(-0.031168f, 0.052014f, 0.047211f), new Vector3(-0.036192f, 0.052977f, 0.056146f), new Vector3(-0.049175f, 0.053503f, 0.056394f), new Vector3(-0.053266f, 0.05217f, 0.04808f), new Vector3(-0.05128f, 0.049459f, 0.038351f), new Vector3(-0.033381f, 0.049271f, 0.037793f), new Vector3(0.042107f, 0.047807f, 0.032652f), new Vector3(-0.042107f, 0.047807f, 0.032652f), new Vector3(0.043424f, 0.066587f, -0.240959f), new Vector3(0.042368f, 0.068555f, -0.252335f), new Vector3(0.03344f, 0.069764f, -0.258f), new Vector3(0.023818f, 0.06823601f, -0.25111f), new Vector3(0.023287f, 0.066453f, -0.240506f), new Vector3(0.029835f, 0.065184f, -0.231667f), new Vector3(0.038287f, 0.065391f, -0.231218f), new Vector3(-0.023818f, 0.06823601f, -0.25111f), new Vector3(-0.023287f, 0.066453f, -0.240506f), new Vector3(-0.029835f, 0.065184f, -0.231667f), new Vector3(-0.038287f, 0.065391f, -0.231218f), new Vector3(-0.043424f, 0.066587f, -0.240959f), new Vector3(-0.042368f, 0.068555f, -0.252335f), new Vector3(-0.03344f, 0.069764f, -0.258f) };     //Ankles
            meshSeamVertices[3][0][2][1] = new Vector3[] { new Vector3(0f, 0.292468f, -0.202558f), new Vector3(0f, 0.270991f, -0.21788f), new Vector3(0.009682f, 0.287267f, -0.204543f), new Vector3(-0.009682f, 0.287267f, -0.204543f) };     //Tail
            meshSeamVertices[3][0][2][2] = new Vector3[] { new Vector3(-0.037003f, 0.357684f, 0.114915f), new Vector3(-0.035077f, 0.363509f, 0.118047f), new Vector3(-0.037946f, 0.353809f, 0.108323f), new Vector3(-0.037114f, 0.353302f, 0.1001f), new Vector3(-0.032545f, 0.364857f, 0.093386f), new Vector3(-0.02986f, 0.376394f, 0.113493f), new Vector3(-0.031718f, 0.37235f, 0.116317f), new Vector3(-0.028419f, 0.377698f, 0.10437f), new Vector3(-0.029717f, 0.372478f, 0.096401f), new Vector3(0.037003f, 0.357684f, 0.114915f), new Vector3(0.035077f, 0.363509f, 0.118047f), new Vector3(0.037946f, 0.353809f, 0.108323f), new Vector3(0.037114f, 0.353302f, 0.1001f), new Vector3(0.032545f, 0.364857f, 0.093386f), new Vector3(0.029717f, 0.372478f, 0.096402f), new Vector3(0.031718f, 0.37235f, 0.116317f), new Vector3(0.02986f, 0.376394f, 0.113493f), new Vector3(0.028419f, 0.377698f, 0.10437f) };     //Ears
            meshSeamVertices[3][0][2][3] = new Vector3[] { new Vector3(0f, 0.295948f, 0.115472f), new Vector3(-0.022039f, 0.307239f, 0.105982f), new Vector3(-0.030306f, 0.322173f, 0.093073f), new Vector3(-0.029397f, 0.332197f, 0.08430301f), new Vector3(-0.022961f, 0.341648f, 0.075984f), new Vector3(0f, 0.351544f, 0.067247f), new Vector3(-0.012603f, 0.348619f, 0.069774f), new Vector3(0.022039f, 0.307239f, 0.105982f), new Vector3(0.030306f, 0.322173f, 0.093073f), new Vector3(0.029397f, 0.332197f, 0.08430301f), new Vector3(0.022961f, 0.341648f, 0.075984f), new Vector3(0.012603f, 0.348619f, 0.069774f), new Vector3(0.012586f, 0.299942f, 0.112171f), new Vector3(-0.012586f, 0.299942f, 0.112171f) };     //Neck
            meshSeamVertices[3][0][2][4] = new Vector3[0];     //Waist
            meshSeamVertices[3][0][2][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[3][0][2][6] = new Vector3[0];     //WaistAdultMale
            meshSeamVertices[3][0][3] = new Vector3[7][];         //Adult LittleDog LOD3 seams
            meshSeamVertices[3][0][3][0] = new Vector3[] { new Vector3(0.049175f, 0.053503f, 0.056394f), new Vector3(0.036192f, 0.052977f, 0.056146f), new Vector3(0.033381f, 0.049271f, 0.037793f), new Vector3(0.05128f, 0.049459f, 0.038351f), new Vector3(-0.036192f, 0.052977f, 0.056146f), new Vector3(-0.049175f, 0.053503f, 0.056394f), new Vector3(-0.05128f, 0.049459f, 0.038351f), new Vector3(-0.033381f, 0.049271f, 0.037793f), new Vector3(0.042107f, 0.047807f, 0.032652f), new Vector3(-0.042107f, 0.047807f, 0.032652f), new Vector3(0.042368f, 0.068555f, -0.252335f), new Vector3(0.03344f, 0.069764f, -0.258f), new Vector3(0.023818f, 0.06823601f, -0.25111f), new Vector3(0.029835f, 0.065184f, -0.231667f), new Vector3(0.038287f, 0.065391f, -0.231218f), new Vector3(-0.023818f, 0.06823601f, -0.25111f), new Vector3(-0.029835f, 0.065184f, -0.231667f), new Vector3(-0.038287f, 0.065391f, -0.231218f), new Vector3(-0.042368f, 0.068555f, -0.252335f), new Vector3(-0.03344f, 0.069764f, -0.258f) };     //Ankles
            meshSeamVertices[3][0][3][1] = new Vector3[] { new Vector3(0f, 0.292468f, -0.202558f), new Vector3(0f, 0.270991f, -0.21788f), new Vector3(0.010432f, 0.28655f, -0.204883f), new Vector3(-0.010433f, 0.286549f, -0.204883f) };     //Tail
            meshSeamVertices[3][0][3][2] = new Vector3[] { new Vector3(-0.03604f, 0.360596f, 0.116481f), new Vector3(-0.03751f, 0.353353f, 0.104223f), new Vector3(-0.032545f, 0.364857f, 0.093386f), new Vector3(-0.030711f, 0.374507f, 0.115047f), new Vector3(-0.028628f, 0.375944f, 0.100264f), new Vector3(0.03604f, 0.360596f, 0.116481f), new Vector3(0.03751f, 0.353353f, 0.104223f), new Vector3(0.032545f, 0.364857f, 0.093386f), new Vector3(0.030711f, 0.374507f, 0.115047f), new Vector3(0.028628f, 0.375944f, 0.100264f) };     //Ears
            meshSeamVertices[3][0][3][3] = new Vector3[] { new Vector3(0f, 0.295948f, 0.115472f), new Vector3(-0.017586f, 0.3034f, 0.109202f), new Vector3(-0.030831f, 0.327118f, 0.08883201f), new Vector3(0f, 0.351544f, 0.067247f), new Vector3(-0.018422f, 0.345765f, 0.072353f), new Vector3(0.030831f, 0.327118f, 0.08883201f), new Vector3(0.018422f, 0.345765f, 0.072353f), new Vector3(0.017586f, 0.3034f, 0.109202f) };     //Neck
            meshSeamVertices[3][0][3][4] = new Vector3[0];     //Waist
            meshSeamVertices[3][0][3][5] = new Vector3[0];     //WaistAdultFemale
            meshSeamVertices[3][0][3][6] = new Vector3[0];     //WaistAdultMale
            return meshSeamVertices;
        }

        public static bool IsEqual(float x, float y)
        {
            return Math.Abs(x - y) < 1e-4f;
        }

        public int CompareTo(GEOM other)
        {
            if (VertexCount > other.VertexCount)
            {
                return -1;
            }
            else if (VertexCount < other.VertexCount)
            {
                return 1;
            }
            else return 0;
        }
    }
}
