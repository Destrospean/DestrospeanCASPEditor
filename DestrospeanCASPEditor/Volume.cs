using System;
using System.Collections.Generic;
using System.IO;
using OpenTK;

namespace Destrospean.DestrospeanCASPEditor
{
    public abstract class IVolume
    {
        public int ColorDataCount, IndexCount, VertexCount;

        public Vector3[] ColorData, Vertices;

        public List<Tuple<int, int, int>> Faces;

        public Matrix4 ModelMatrix = Matrix4.Identity,
        ModelViewProjectionMatrix = Matrix4.Identity,
        ViewProjectionMatrix = Matrix4.Identity;

        public Vector3 Position = Vector3.Zero,
        Rotation = Vector3.Zero,
        Scale = Vector3.One;

        public Vector2[] TextureCoordinates;

        public abstract void CalculateModelMatrix();

        public abstract int[] GetIndices(int offset = 0);
    }

    public class Volume : IVolume
    {
        Vector3[] mColorData, mVertices;

        List<Tuple<int, int, int>> mFaces = new List<Tuple<int, int, int>>();

        Vector2[] mTextureCoordinates;

        public new Vector3[] ColorData
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

        public new int ColorDataCount
        {
            get
            {
                return mColorData.Length;
            }
        }

        public new List<Tuple<int, int, int>> Faces
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

        public new int IndexCount
        {
            get
            {
                return mFaces.Count * 3;
            }
        }

        public new Vector2[] TextureCoordinates
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

        public new int VertexCount
        {
            get
            {
                return mVertices.Length;
            }
        }

        public new Vector3[] Vertices
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

    public class OBJ
    {
        public static Volume LoadFromFile(string filename)
        {
            var volume = new Volume();
            try
            {
                using (StreamReader reader = new StreamReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
                {
                    volume = LoadFromString(reader.ReadToEnd());
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File not found: {0}", filename);
            }
            catch (Exception)
            {
                Console.WriteLine("Error loading file: {0}", filename);
            }
            return volume;
        }

        public static Volume LoadFromString(string obj)
        {
            List<Vector3> colors = new List<Vector3>(),
            vertices = new List<Vector3>();
            var faces = new List<Tuple<int, int, int>>();
            var lines = new List<string>(obj.Split('\n'));
            var textureCoordinates = new List<Vector2>();
            foreach (String line in lines)
            {
                if (line.StartsWith("v "))
                {
                    var temp = line.Substring(2);
                    var vector = new Vector3();
                    if (new List<char>(temp.ToCharArray()).FindAll(c => c == ' ').Count == 2)
                    {
                        var vertexParts = temp.Split(' ');
                        var success = float.TryParse(vertexParts[0], out vector.X);
                        success &= float.TryParse(vertexParts[1], out vector.Y);
                        success &= float.TryParse(vertexParts[2], out vector.Z);
                        colors.Add(new Vector3((float) Math.Sin(vector.Z), (float) Math.Sin(vector.Z), (float) Math.Sin(vector.Z)));
                        textureCoordinates.Add(new Vector2((float) Math.Sin(vector.Z), (float) Math.Sin(vector.Z)));
                        if (!success)
                        {
                            Console.WriteLine("Error parsing vertex: {0}", line);
                        }
                    }
                    vertices.Add(vector);
                }
                else if (line.StartsWith("f "))
                {
                    var temp = line.Substring(2);
                    var face = new Tuple<int, int, int>(0, 0, 0);
                    if (new List<char>(temp.ToCharArray()).FindAll(c => c == ' ').Count == 2)
                    {
                        var faceParts = temp.Split(' ');
                        int x, y, z;
                        var success = int.TryParse(faceParts[0], out x);
                        success &= int.TryParse(faceParts[1], out y);
                        success &= int.TryParse(faceParts[2], out z);
                        if (success)
                        {
                            face = new Tuple<int, int, int>(x - 1, y - 1, z - 1);
                            faces.Add(face);
                        }
                        else
                        {
                            Console.WriteLine("Error parsing face: {0}", line);
                        }
                    }
                }
            }
            Volume volume = new Volume();
            volume.Faces = new List<Tuple<int, int, int>>(faces);
            volume.ColorData = colors.ToArray();
            volume.TextureCoordinates = textureCoordinates.ToArray();
            volume.Vertices = vertices.ToArray();
            return volume;
        }
    }
}
