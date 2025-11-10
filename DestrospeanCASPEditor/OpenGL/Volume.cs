using System;
using System.Collections.Generic;
using System.IO;
using OpenTK;

namespace Destrospean.DestrospeanCASPEditor.OpenGL
{
    public abstract class IVolume
    {
        public int TextureID;

        public virtual int NormalCount
        {
            get
            {
                return Normals.Length;
            }
        }

        public virtual int ColorDataCount
        {
            get
            {
                return ColorData.Length;
            }
        }

        public virtual int IndexCount
        {
            get;
            set;
        }

        public virtual int TextureCoordinateCount
        {
            get;
            set;
        }

        public virtual int VertexCount
        {
            get
            {
                return Vertices.Length;
            }
        }

        public virtual Vector3[] ColorData
        {
            get;
            set;
        }

        public virtual Vector3[] Normals
        {
            get;
            set;
        }

        public virtual Vector3[] Vertices
        {
            get;
            set;
        }

        public virtual List<Tuple<int, int, int>> Faces
        {
            get;
            set;
        }

        public bool IsTextured = false;

        public Matrix4 ModelMatrix = Matrix4.Identity,
        ModelViewProjectionMatrix = Matrix4.Identity,
        ViewProjectionMatrix = Matrix4.Identity;

        public Vector3 Position = Vector3.Zero,
        Rotation = Vector3.Zero,
        Scale = Vector3.One;

        public virtual Vector2[] TextureCoordinates
        {
            get;
            set;
        }

        public abstract void CalculateModelMatrix();

        public void CalculateNormals()
        {
            Vector3[] normals = new Vector3[VertexCount],
            vertices = Vertices;
            var indices = GetIndices();
            for (int i = 0; i < IndexCount; i += 3)
            {
                Vector3 a = vertices[indices[i]],
                b = vertices[indices[i + 1]],
                c = vertices[indices[i + 2]];
                normals[indices[i]] += Vector3.Cross(b - a, c - a);
                normals[indices[i + 1]] += Vector3.Cross(b - a, c - a);
                normals[indices[i + 2]] += Vector3.Cross(b - a, c - a);
            }
            for (int i = 0; i < NormalCount; i++)
            {
                normals[i].Normalize();
            }
            Normals = normals;
        }

        public abstract int[] GetIndices(int offset = 0);
    }

    public class Volume : IVolume
    {
        Vector3[] mColorData, mVertices;

        Vector2[] mTextureCoordinates;

        List<Tuple<int, int, int>> mFaces = new List<Tuple<int, int, int>>();

        public override Vector3[] ColorData
        {
            get
            {
                return mColorData;
            }
            set
            {
                mColorData = value;
            }
        }

        public override int ColorDataCount
        {
            get
            {
                return mColorData.Length;
            }
        }

        public override List<Tuple<int, int, int>> Faces
        {
            get
            {
                return mFaces;
            }
            set
            {
                mFaces = value;
            }
        }

        public override int IndexCount
        {
            get
            {
                return mFaces.Count * 3;
            }
        }

        public Material Material = new Material();

        public override int TextureCoordinateCount
        {
            get
            {
                return mTextureCoordinates.Length;
            }
        }

        public override Vector2[] TextureCoordinates
        {
            get
            {
                return mTextureCoordinates;
            }
            set
            {
                mTextureCoordinates = value;
            }
        }

        public override int VertexCount
        {
            get
            {
                return mVertices.Length;
            }
        }

        public override Vector3[] Vertices
        {
            get
            {
                return mVertices;
            }
            set
            {
                mVertices = value;
            }
        }

        public override void CalculateModelMatrix()
        {
            ModelMatrix = Matrix4.Scale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);
        }

        public override int[] GetIndices(int offset = 0)
        {
            var indices = new List<int>();
            foreach (var face in mFaces)
            {
                indices.Add(face.Item1 + offset);
                indices.Add(face.Item2 + offset);
                indices.Add(face.Item3 + offset);
            }
            return indices.ToArray();
        }
    }
}
