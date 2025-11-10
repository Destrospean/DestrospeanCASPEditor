using System;
using OpenTK;

namespace Destrospean.DestrospeanCASPEditor
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
                        X = (float)(Math.Sin((float)Orientation.X) * Math.Cos((float)Orientation.Y)),
                        Y = (float)Math.Sin((float)Orientation.Y),
                        Z = (float)(Math.Cos((float)Orientation.X) * Math.Cos((float)Orientation.Y))
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

        public void Move(float x, float y, float z)
        {
            Vector3 offset = new Vector3(),
            forward = new Vector3((float)Math.Sin((float)Orientation.X), 0, (float)Math.Cos((float)Orientation.X)),
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
