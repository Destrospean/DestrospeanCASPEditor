using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace Destrospean.DestrospeanCASPEditor
{
    public class ShaderProgram
    {
        public int AttributeCount = 0,
        FShaderID = -1,
        ProgramID = -1,
        UniformCount = 0,
        VShaderID = -1;

        public readonly Dictionary<string, AttributeInfo> Attributes = new Dictionary<string, AttributeInfo>();

        public readonly Dictionary<string, uint> Buffers = new Dictionary<string, uint>();

        public readonly Dictionary<string, UniformInfo> Uniforms = new Dictionary<string, UniformInfo>();

        public class AttributeInfo
        {
            public int Address = -1,
            Size = 0;

            public string Name = "";

            public ActiveAttribType Type;
        }

        public class UniformInfo
        {
            public int Address = -1,
            Size = 0;

            public string Name = "";

            public ActiveUniformType Type;
        }

        public ShaderProgram()
        {
            ProgramID = GL.CreateProgram();
        }

        public ShaderProgram(string vertexShader, string fragmentShader, bool fromFile = false)
        {
            ProgramID = GL.CreateProgram();
            if (fromFile)
            {
                LoadShaderFromFile(vertexShader, ShaderType.VertexShader);
                LoadShaderFromFile(fragmentShader, ShaderType.FragmentShader);
            }
            else
            {
                LoadShaderFromString(vertexShader, ShaderType.VertexShader);
                LoadShaderFromString(fragmentShader, ShaderType.FragmentShader);
            }
            Link();
            GenBuffers();
        }

        void LoadShader(string code, ShaderType type, out int address)
        {
            address = GL.CreateShader(type);
            GL.ShaderSource(address, code);
            GL.CompileShader(address);
            GL.AttachShader(ProgramID, address);
            System.Console.WriteLine(GL.GetShaderInfoLog(address));
        }

        public void DisableVertexAttribArrays()
        {
            for (var i = 0; i < Attributes.Count; i++)
            {
                GL.DisableVertexAttribArray(new List<AttributeInfo>(Attributes.Values)[i].Address);
            }
        }

        public void EnableVertexAttribArrays()
        {
            for (var i = 0; i < Attributes.Count; i++)
            {
                GL.EnableVertexAttribArray(new List<AttributeInfo>(Attributes.Values)[i].Address);
            }
        }

        public void GenBuffers()
        {
            for (var i = 0; i < Attributes.Count; i++)
            {
                uint buffer = 0;
                GL.GenBuffers(1, out buffer);
                Buffers.Add(new List<AttributeInfo>(Attributes.Values)[i].Name, buffer);
            }
            for (var i = 0; i < Uniforms.Count; i++)
            {
                uint buffer = 0;
                GL.GenBuffers(1, out buffer);
                Buffers.Add(new List<UniformInfo>(Uniforms.Values)[i].Name, buffer);
            }
        }

        public int GetAttribute(string name)
        {
            if (Attributes.ContainsKey(name))
            {
                return Attributes[name].Address;
            }
            else
            {
                return -1;
            }
        }

        public uint GetBuffer(string name)
        {
            if (Buffers.ContainsKey(name))
            {
                return Buffers[name];
            }
            else
            {
                return 0;
            }
        }

        public int GetUniform(string name)
        {
            if (Uniforms.ContainsKey(name))
            {
                return Uniforms[name].Address;
            }
            else
            {
                return -1;
            }
        }

        public void Link()
        {
            GL.LinkProgram(ProgramID);
            System.Console.WriteLine(GL.GetProgramInfoLog(ProgramID));
            GL.GetProgram(ProgramID, ProgramParameter.ActiveAttributes, out AttributeCount);
            GL.GetProgram(ProgramID, ProgramParameter.ActiveUniforms, out UniformCount);
            for (var i = 0; i < AttributeCount; i++)
            {
                var info = new AttributeInfo();
                var length = 0;
                var name = new System.Text.StringBuilder();
                GL.GetActiveAttrib(ProgramID, i, 256, out length, out info.Size, out info.Type, name);
                info.Name = name.ToString();
                info.Address = GL.GetAttribLocation(ProgramID, info.Name);
                Attributes.Add(name.ToString(), info);
            }
            for (var i = 0; i < UniformCount; i++)
            {
                var info = new UniformInfo();
                var length = 0;
                var name = new System.Text.StringBuilder();
                GL.GetActiveUniform(ProgramID, i, 256, out length, out info.Size, out info.Type, name);
                info.Name = name.ToString();
                Uniforms.Add(name.ToString(), info);
                info.Address = GL.GetUniformLocation(ProgramID, info.Name);
            }
        }

        public void LoadShaderFromFile(string filename, ShaderType type)
        {
            using (var streamReader = new System.IO.StreamReader(filename))
            {
                if (type == ShaderType.VertexShader)
                {
                    LoadShader(streamReader.ReadToEnd(), type, out VShaderID);
                }
                else if (type == ShaderType.FragmentShader)
                {
                    LoadShader(streamReader.ReadToEnd(), type, out FShaderID);
                }
            }
        }

        public void LoadShaderFromString(string code, ShaderType type)
        {
            if (type == ShaderType.VertexShader)
            {
                LoadShader(code, type, out VShaderID);
            }
            else if (type == ShaderType.FragmentShader)
            {
                LoadShader(code, type, out FShaderID);
            }
        }
    }
}

