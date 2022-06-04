using SFML.Graphics;
using SFML.System;
namespace EinEngine
{
    public class Light
    {
        public Vector3f Position;
        public Color GlowColor;
        public float GlowRadius;
        public Light(Vector3f Position, Color GlowColor, float GlowRadius)
        {
            this.Position = Position;
            this.GlowColor = GlowColor;
            this.GlowRadius = GlowRadius;
        }

        public Light(Light Light)
        {
            this.Position = Light.Position;
            this.GlowColor = Light.GlowColor;
            this.GlowRadius = Light.GlowRadius;
        }
    }
}
