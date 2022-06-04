using SFML.System;
using SFML.Graphics;
using System.Collections.Generic;


namespace EinEngine
{
    public class Polygon
    {
        public List<Vector2f> ConvertedVerticies;
        public List<Vector3f> SpaceVerticies;
        public Color Material;
        public Texture Texture;
        public float Distance;

        public Polygon()
        {
            ConvertedVerticies = new List<Vector2f>();
            SpaceVerticies = new List<Vector3f>();
            Material = new Color();
            Texture = null;
            Distance = 0;
        }
        public Polygon(Color Material, Texture Texture)
        {
            this.ConvertedVerticies = new List<Vector2f>();
            this.SpaceVerticies = new List<Vector3f>();
            this.Material = Material;
            this.Texture = Texture;
            Distance = 0;
        }

        public Polygon(Polygon Polygon)
        {
            this.ConvertedVerticies = Polygon.ConvertedVerticies;
            this.SpaceVerticies = Polygon.SpaceVerticies;
            this.Material = Polygon.Material;
            this.Texture = Polygon.Texture;
            this.Distance = Polygon.Distance;
        }
    }
}
