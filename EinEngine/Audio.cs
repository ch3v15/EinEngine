using SFML.Audio;
using SFML.System;

namespace EinEngine
{
    public class Audio
    {
        public Vector3f Position;
        public Music Music;
        public float Radius;
        public Audio(Vector3f Position, Music Music, float Radius){
            this.Position = Position;
            this.Music = Music;
            this.Radius = Radius;
        }

        public Audio(Audio Audio)
        {
            this.Position = Audio.Position;
            this.Music = Audio.Music;
            this.Radius = Audio.Radius;
        }
    }
}
