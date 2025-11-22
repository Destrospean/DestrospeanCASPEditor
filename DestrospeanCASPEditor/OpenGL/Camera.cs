using System;
using OpenTK;

namespace Destrospean.DestrospeanCASPEditor.OpenGL
{
    public class Camera
    {
        public float MouseSensitivity = .0025f,
        MoveSpeed = .2f;

        public Vector3 Orientation = new Vector3((float)Math.PI, 0, 0),
        Position = Vector3.Zero;

        public Matrix4 ViewMatrix
        {
            get
            {
                return Matrix4.LookAt(Position, Position + new Vector3
                    {
                        X = (float)(Math.Sin(Orientation.X) * Math.Cos(Orientation.Y)),
                        Y = (float)Math.Sin(Orientation.Y),
                        Z = (float)(Math.Cos(Orientation.X) * Math.Cos(Orientation.Y))
                    }, Vector3.UnitY);
            }
        }

        public void AddRotation(float x, float y)
        {
            x *= MouseSensitivity;
            y *= MouseSensitivity;
            Orientation.X = (Orientation.X + x) % ((float)Math.PI * 2);
            Orientation.Y = Math.Max(Math.Min(Orientation.Y + y, (float)Math.PI / 2 - .1f), (float)-Math.PI / 2 + .1f);
        }

        public void AddTranslation(float x, float y, float z)
        {
            x *= MouseSensitivity;
            y *= MouseSensitivity;
            z *= MouseSensitivity;
            Position.X += x;
            Position.Y -= y;
            Position.Z += z;
        }

        public void Move(float x, float y, float z)
        {
            Vector3 offset = new Vector3(),
            forward = new Vector3((float)Math.Sin(Orientation.X), 0, (float)Math.Cos(Orientation.X)),
            right = new Vector3(-forward.Z, 0, forward.X);
            offset += x * right;
            offset += y * forward;
            offset.Y += z;
            offset.NormalizeFast();
            offset = Vector3.Multiply(offset, MoveSpeed);
            Position += offset;
        }
    }
}
