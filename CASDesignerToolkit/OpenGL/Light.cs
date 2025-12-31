using OpenTK;

namespace Destrospean.CASDesignerToolkit.OpenGL
{
    public enum LightType
    {
        Directional,
        Point,
        Spot
    }

    public class Light
    {
        public Vector3 Color, Direction, Position;

        public float AmbientIntensity, ConeAngle, DiffuseIntensity, LinearAttenuation, QuadraticAttenuation;

        public LightType Type;

        public Light(Vector3 position, Vector3 color, float diffuseIntensity = 1, float ambientIntensity = 1)
        {
            AmbientIntensity = ambientIntensity;
            DiffuseIntensity = diffuseIntensity;
            Direction = new Vector3(0, 0, 1);
            Color = color;
            ConeAngle = 15;
            Position = position;
            Type = LightType.Point;
        }
    }
}
