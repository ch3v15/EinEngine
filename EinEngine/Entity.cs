using SFML.System;
using System.IO;
using System;
using SFML.Graphics;
using System.Collections.Generic;
using System.Collections;

namespace EinEngine
{
    public class Entity
    {
        public List<Polygon> Polygons;
        public Vector3f[] Verticies;
        public int[][] IndexPolygons;
        public bool Visible = true;
        public bool SpatialCoordinates = true;

        public Vector3f Position;
        public Vector3f Rotation;
        public Vector3f Scale;

        public Entity(Vector3f Position, Vector3f Rotation, Vector3f Scale, List<Polygon> Polygons, Vector3f[] Verticies, int[][] IndexPolygons)
        {
            this.Position = Position;
            this.Rotation = Rotation;
            this.Scale = Scale;
            this.Polygons = Polygons;
            this.Verticies = Verticies;
            this.IndexPolygons = IndexPolygons;
        }

        public Entity(Entity Object)
        {
            this.Position = Object.Position;
            this.Rotation = Object.Rotation;
            this.Scale = Object.Scale;
            this.Polygons = Object.Polygons;
            this.Verticies = Object.Verticies;
            this.IndexPolygons = Object.IndexPolygons;
            this.SpatialCoordinates = Object.SpatialCoordinates;
            this.Visible = Object.Visible;
        }

        public static int CountImportObjectPolygons(string Path)
        {
            int Count = 0;
            string[] StringObject = File.ReadAllLines(Path);
            foreach (string String in StringObject)
                if (String.Split(new char[] { ' ' }, 2)[0] == "f")
                    Count++;
            return Count;
        }
        public static Entity ImportObject(string PathToObject, Vector3f Position, Vector3f Rotation, Vector3f Scale)
        {
            List<Vector3f> Verticies = new List<Vector3f>();
            List<int[]> IndexPolygons = new List<int[]>();
            List<Color> PolygonMaterials = new List<Color>();
            List<Texture> PolygonTextures = new List<Texture>();

            Hashtable PolygonMaterialsHashtable = new Hashtable();
            string[] StringObject = File.ReadAllLines(PathToObject);
            string NameMaterial = null;
            Color ColorMaterial = new Color();
            Texture TextureMaterial = null;

            foreach (string String in StringObject)
            {
                if (String.Split(new char[] { ' ' }, 2)[0] == "mtllib")
                {
                    string Mtllib = String.Split(new char[] { ' ' }, 2)[1];
                    string[] StringMaterials = File.ReadAllLines(Mtllib);


                    foreach (string Material in StringMaterials)
                    {
                        if (Material.Split(new char[] { ' ' }, 2)[0] == "newmtl")
                        {
                            if (NameMaterial != Material.Split(new char[] { ' ' }, 2)[1] && NameMaterial != null)
                            {
                                PolygonMaterialsHashtable.Add(NameMaterial, new object[] { ColorMaterial, TextureMaterial });
                                ColorMaterial = new Color();
                                TextureMaterial = null;
                            }
                            NameMaterial = Material.Split(new char[] { ' ' }, 2)[1];

                        }
                        else if (Material.Split(new char[] { ' ' }, 2)[0] == "Kd")
                        {
                            string StringMaterial = Material.Split(new char[] { ' ' }, 2)[1].Replace('.', ',');
                            ColorMaterial = new Color((byte)(float.Parse(StringMaterial.Split(new char[] { ' ' })[0]) * 255),
                                                           (byte)(float.Parse(StringMaterial.Split(new char[] { ' ' })[1]) * 255),
                                                           (byte)(float.Parse(StringMaterial.Split(new char[] { ' ' })[2]) * 255));
                        }
                        else if (Material.Split(new char[] { ' ' }, 2)[0] == "map_Kd")
                            TextureMaterial = new Texture(Material.Split(new char[] { ' ' }, 2)[1]);
                    }

                    PolygonMaterialsHashtable.Add(NameMaterial, new object[] { ColorMaterial, TextureMaterial });
                }

                else if (String.Split(new char[] { ' ' }, 2)[0] == "usemtl")
                {
                    ColorMaterial = (Color)((object[])PolygonMaterialsHashtable[String.Split(new char[] { ' ' }, 2)[1]])[0];
                    TextureMaterial = (Texture)((object[])PolygonMaterialsHashtable[String.Split(new char[] { ' ' }, 2)[1]])[1];
                }

                else if (String.Split(new char[] { ' ' }, 2)[0] == "v")
                    Verticies.Add(new Vector3f(float.Parse(String.Split(new char[] { ' ' })[1].Replace('.', ',')), float.Parse(String.Split(new char[] { ' ' })[2].Replace('.', ',')), float.Parse(String.Split(new char[] { ' ' })[3].Replace('.', ','))));
                else if (String.Split(new char[] { ' ' }, 2)[0] == "f")
                {
                    string StringPolygon = String.Split(new char[] { ' ' }, 2)[1].Replace("//", "/");
                    List<int> Polygon = new List<int>();
                    for (int i = 0; i < StringPolygon.Split(new char[] { ' ' }).Length; i++)
                        Polygon.Add(int.Parse(StringPolygon.Split(new char[] { ' ' })[i].Split(new char[] { '/' })[0]) - 1);
                    IndexPolygons.Add(Polygon.ToArray());
                    PolygonMaterials.Add(ColorMaterial);
                    PolygonTextures.Add(TextureMaterial);
                }
            }

            List<Polygon> Polygons = new List<Polygon>();
            for (int i = 0; i < CountImportObjectPolygons(PathToObject); i++)
                Polygons.Add(new Polygon(PolygonMaterials[i], PolygonTextures[i]));
            return new Entity(Position, Rotation, Scale, Polygons, Verticies.ToArray(), IndexPolygons.ToArray());
        }
        public static Entity ImportObject(string PathToObject, string PathToDirMtl, Vector3f Position, Vector3f Rotation, Vector3f Scale)
        {
            List<Vector3f> Verticies = new List<Vector3f>();
            List<int[]> IndexPolygons = new List<int[]>();
            List<Color> PolygonMaterials = new List<Color>();
            List<Texture> PolygonTextures = new List<Texture>();

            Hashtable PolygonMaterialsHashtable = new Hashtable();
            string[] StringObject = File.ReadAllLines(PathToObject);
            string NameMaterial = null;
            Color ColorMaterial = new Color();
            Texture TextureMaterial = null;

            foreach (string String in StringObject)
            {
                if (String.Split(new char[] { ' ' }, 2)[0] == "mtllib")
                {
                    string Mtllib = PathToDirMtl + "/" + String.Split(new char[] { ' ' }, 2)[1];
                    string[] StringMaterials = File.ReadAllLines(Mtllib);


                    foreach (string Material in StringMaterials)
                    {
                        if (Material.Split(new char[] { ' ' }, 2)[0] == "newmtl")
                        {
                            if (NameMaterial != Material.Split(new char[] { ' ' }, 2)[1] && NameMaterial != null)
                            {
                                PolygonMaterialsHashtable.Add(NameMaterial, new object[] { ColorMaterial, TextureMaterial });
                                ColorMaterial = new Color();
                                TextureMaterial = null;
                            }
                            NameMaterial = Material.Split(new char[] { ' ' }, 2)[1];

                        }
                        else if (Material.Split(new char[] { ' ' }, 2)[0] == "Kd")
                        {
                            string StringMaterial = Material.Split(new char[] { ' ' }, 2)[1].Replace('.', ',');
                            ColorMaterial = new Color((byte)(float.Parse(StringMaterial.Split(new char[] { ' ' })[0]) * 255),
                                                           (byte)(float.Parse(StringMaterial.Split(new char[] { ' ' })[1]) * 255),
                                                           (byte)(float.Parse(StringMaterial.Split(new char[] { ' ' })[2]) * 255));
                        }
                        else if (Material.Split(new char[] { ' ' }, 2)[0] == "map_Kd")
                            TextureMaterial = new Texture(PathToDirMtl + "/" + Material.Split(new char[] { ' ' }, 2)[1]);
                    }

                    PolygonMaterialsHashtable.Add(NameMaterial, new object[] { ColorMaterial, TextureMaterial });
                }

                else if (String.Split(new char[] { ' ' }, 2)[0] == "usemtl")
                {
                    ColorMaterial = (Color)((object[])PolygonMaterialsHashtable[String.Split(new char[] { ' ' }, 2)[1]])[0];
                    TextureMaterial = (Texture)((object[])PolygonMaterialsHashtable[String.Split(new char[] { ' ' }, 2)[1]])[1];
                }

                else if (String.Split(new char[] { ' ' }, 2)[0] == "v")
                    Verticies.Add(new Vector3f(float.Parse(String.Split(new char[] { ' ' })[1].Replace('.', ',')), float.Parse(String.Split(new char[] { ' ' })[2].Replace('.', ',')), float.Parse(String.Split(new char[] { ' ' })[3].Replace('.', ','))));
                else if (String.Split(new char[] { ' ' }, 2)[0] == "f")
                {
                    string StringPolygon = String.Split(new char[] { ' ' }, 2)[1].Replace("//", "/");
                    List<int> Polygon = new List<int>();
                    for (int i = 0; i < StringPolygon.Split(new char[] { ' ' }).Length; i++)
                        Polygon.Add(int.Parse(StringPolygon.Split(new char[] { ' ' })[i].Split(new char[] { '/' })[0]) - 1);
                    IndexPolygons.Add(Polygon.ToArray());
                    PolygonMaterials.Add(ColorMaterial);
                    PolygonTextures.Add(TextureMaterial);
                }
            }

            List<Polygon> Polygons = new List<Polygon>();
            for (int i = 0; i < CountImportObjectPolygons(PathToObject); i++)
                Polygons.Add(new Polygon(PolygonMaterials[i], PolygonTextures[i]));
            return new Entity(Position, Rotation, Scale, Polygons, Verticies.ToArray(), IndexPolygons.ToArray());
        }
    }
}
