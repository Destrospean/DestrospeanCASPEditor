using OpenTK;

namespace Destrospean.DestrospeanCASPEditor.OpenGL
{
    public class Light
    {
        public Vector3 Color = new Vector3(),
        Position;

        public float AmbientIntensity = .1f,
        DiffuseIntensity = 1;

        public Light(Vector3 position, Vector3 color, float diffuseIntensity = 1, float ambientIntensity = 1)
        {
            AmbientIntensity = ambientIntensity;
            Color = color;
            DiffuseIntensity = diffuseIntensity;
            Position = position;
        }
    }
}
