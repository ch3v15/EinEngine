using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace EinEngine
{
    public class Camera
    {
        private Control Window { set; get; }
        public List<Object> Objects;
        public List<Light> Lights;
        public Color SpaceLight;
        public Vector3 Position;
        public Vector3 Rotation;
        public bool Lighting { set; get; } = false;
        public float GlowIntensity { set; get; }
        public float Near { set; get; } = 1;
        public float Far { set; get; } = 100;
        public float Fov { set; get; } = 2;
        public int TextureResolution { set; get; } = 512;
        public Camera(Control Window, Vector3 Position, Vector3 Rotation)
        {
            Objects = new List<Object>();
            Lights = new List<Light>();
            this.Window = Window;
            this.Position = Position;
            this.Rotation = Rotation;
            this.SpaceLight = Color.FromArgb(100, 149, 237);
            this.GlowIntensity = (SpaceLight.R + SpaceLight.G + SpaceLight.B) / 3;
        }

        public void Display(Graphics Graphics)
        {
            Graphics.Clear(SpaceLight);
            GlowIntensity = (SpaceLight.R + SpaceLight.G + SpaceLight.B) / 3;
            List<Polygon> Polygons = new List<Polygon>();
            foreach (Object Element in Objects)
            {
                for (int IndexPolygon = 0; IndexPolygon < Element.IndexPolygons.Length; IndexPolygon++)
                {
                    Element.Polygons[IndexPolygon].ConvertedVerticies = new List<Vector2>();
                    Element.Polygons[IndexPolygon].SpaceVerticies = new List<Vector3>();

                    int[] ElementPolygon = Element.IndexPolygons[IndexPolygon];
                    Polygon ObjectPolygon = new Polygon();
                    List<Vector3> SortPoints = new List<Vector3>();
                    for (int Vertex = 0; Vertex < ElementPolygon.Length; Vertex++)
                    {
                        Vector2 Vector;

                        float X = Element.Verticies[ElementPolygon[Vertex]].X * Element.Scale.X;
                        float Y = Element.Verticies[ElementPolygon[Vertex]].Y * Element.Scale.Y;
                        float Z = Element.Verticies[ElementPolygon[Vertex]].Z * Element.Scale.Z;

                        Element.Polygons[IndexPolygon].SpaceVerticies.Add(new Vector3(X + Element.Position.X, Y + Element.Position.Y, Z + Element.Position.Z));
                        ObjectPolygon.SpaceVerticies.Add(new Vector3(X + Element.Position.X, Y + Element.Position.Y, Z + Element.Position.Z));

                        Vector = Rotate(new Vector2(X, Z), Element.Rotation.Y);
                        X = Vector.X; Z = Vector.Y;
                        Vector = Rotate(new Vector2(Y, Z), Element.Rotation.X);
                        Y = Vector.X; Z = Vector.Y;
                        Vector = Rotate(new Vector2(X, Y), Element.Rotation.Z);
                        X = Vector.X; Y = Vector.Y;

                        X += Element.Position.X;
                        Y += Element.Position.Y;
                        Z += Element.Position.Z;

                        X -= Position.X;
                        Y -= Position.Y;
                        Z -= Position.Z;

                        Vector = Rotate(new Vector2(X, Z), Rotation.Y);
                        X = Vector.X; Z = Vector.Y;
                        Vector = Rotate(new Vector2(Y, Z), Rotation.X);
                        Y = Vector.X; Z = Vector.Y;
                        Vector = Rotate(new Vector2(X, Y), Rotation.Z);
                        X = Vector.X; Y = Vector.Y;

                        SortPoints.Add(new Vector3(X, Y, Z));
                    }

                    int Index = 0;
                    while (Index < SortPoints.Count)
                    {
                        if (SortPoints[Index].Z < Near)
                        {
                            List<Vector3> PolygonSides = new List<Vector3>();
                            Vector3 Left, Right;
                            if (Index - 1 < 0)
                                Left = SortPoints[SortPoints.Count - 1];
                            else Left = SortPoints[Index - 1];
                            Right = SortPoints[(Index + 1) % SortPoints.Count];

                            if (Left.Z > Near)
                                PolygonSides.Add(GetZCoordinate(SortPoints[Index], Left, Near));
                            if (Right.Z > Near)
                                PolygonSides.Add(GetZCoordinate(SortPoints[Index], Right, Near));
                            List<Vector3> TransitionalVector = new List<Vector3>();
                            for (int IndexVertex = 0; IndexVertex < Index; IndexVertex++)
                                TransitionalVector.Add(SortPoints[IndexVertex]);
                            TransitionalVector.AddRange(PolygonSides);
                            for (int IndexVertex = Index + 1; IndexVertex < SortPoints.Count; IndexVertex++)
                                TransitionalVector.Add(SortPoints[IndexVertex]);
                            SortPoints = TransitionalVector;
                            Index += PolygonSides.Count - 1;
                        }
                        Index++;
                    }

                    if (SortPoints.Count > 2)
                    {
                        for (int i = 0; i < SortPoints.Count; i++)
                        {
                            if (SortPoints[i].Z < Far)
                            {
                                Element.Polygons[IndexPolygon].ConvertedVerticies.Add(new Vector2(Window.Size.Width / 2 + SortPoints[i].X / SortPoints[i].Z * ((Window.Size.Width + Window.Size.Height) / (2 * Fov)), Window.Size.Height / 2 - SortPoints[i].Y / SortPoints[i].Z * ((Window.Size.Width + Window.Size.Height) / (2 * Fov))));
                                ObjectPolygon.ConvertedVerticies.Add(new Vector2(Window.Size.Width / 2 + SortPoints[i].X / SortPoints[i].Z * ((Window.Size.Width + Window.Size.Height) / (2 * Fov)), Window.Size.Height / 2 - SortPoints[i].Y / SortPoints[i].Z * ((Window.Size.Height + Window.Size.Height) / (2 * Fov))));
                            }
                        }

                        if (!Lighting) ObjectPolygon.Material = Element.Polygons[IndexPolygon].Material;
                        else
                        {
                            float SubPixel = GlowIntensity / 255;
                            ObjectPolygon.Material = Color.FromArgb((byte)(SubPixel * Element.Polygons[IndexPolygon].Material.R), (byte)(SubPixel * Element.Polygons[IndexPolygon].Material.G), (byte)(SubPixel * Element.Polygons[IndexPolygon].Material.B));
                        }
                        ObjectPolygon.Texture = Element.Polygons[IndexPolygon].Texture;
                        ObjectPolygon.Distance = DistanceToPolygon(SortPoints);
                        Polygons.Add(ObjectPolygon);
                    }
                }
            }

            Polygons.Sort(new DistancePolygonCompare());
            foreach (Polygon Polygon in Polygons)
            {
                if (Lighting)
                {
                    foreach (Light Light in Lights)
                    {
                        float Distance = DistanceBetweenVectors(Light.Position, ReliableCenterPolygon(Polygon.SpaceVerticies));
                        if (Distance < Light.GlowRadius)
                        {
                            float R, G, B;
                            float SubPixel = (Light.GlowRadius - Distance) / Light.GlowRadius;
                            R = Polygon.Material.R + Light.GlowColor.R * SubPixel;
                            G = Polygon.Material.G + Light.GlowColor.G * SubPixel;
                            B = Polygon.Material.B + Light.GlowColor.B * SubPixel;

                            if (R > 255) R = 255;
                            if (G > 255) G = 255;
                            if (B > 255) B = 255;

                            Polygon.Material = Color.FromArgb((byte)R, (byte)G, (byte)B);
                        }
                    }
                }
                List<PointF> ConvertedVerticies = new List<PointF>();
                foreach (Vector2 Vector in Polygon.ConvertedVerticies)
                    ConvertedVerticies.Add(new PointF(Vector.X, Vector.Y));
                Graphics.FillPolygon(new SolidBrush(Polygon.Material), ConvertedVerticies.ToArray());
            }
        }
        private Vector3 GetZCoordinate(Vector3 FirstVector, Vector3 SecondVector, float Near)
        {
            if (SecondVector.Z == FirstVector.Z || Near < FirstVector.Z || Near > SecondVector.Z)
                return default;
            Vector3 DeltaVector = new Vector3(SecondVector.X - FirstVector.X, SecondVector.Y - FirstVector.Y, SecondVector.Z - FirstVector.Z);
            return new Vector3(FirstVector.X + DeltaVector.X * ((Near - FirstVector.Z) / DeltaVector.Z),
                                FirstVector.Y + DeltaVector.Y * ((Near - FirstVector.Z) / DeltaVector.Z), Near);
        }
        public List<Vector2> ObjectConvertPolygons(Object Example)
        {
            List<Vector2> PointPolygons = new List<Vector2>();
            for (int IndexPolygon = 0; IndexPolygon < Example.IndexPolygons.Length; IndexPolygon++)
            {
                List<Vector3> SortPoints = new List<Vector3>();
                int[] ElementPolygon = Example.IndexPolygons[IndexPolygon];
                for (int Vertex = 0; Vertex < ElementPolygon.Length; Vertex++)
                {
                    Vector2 Vector;

                    float X = Example.Verticies[ElementPolygon[Vertex]].X * Example.Scale.X;
                    float Y = Example.Verticies[ElementPolygon[Vertex]].Y * Example.Scale.Y;
                    float Z = Example.Verticies[ElementPolygon[Vertex]].Z * Example.Scale.Z;

                    Vector = Rotate(new Vector2(X, Z), Example.Rotation.Y);
                    X = Vector.X; Z = Vector.Y;
                    Vector = Rotate(new Vector2(Y, Z), Example.Rotation.X);
                    Y = Vector.X; Z = Vector.Y;
                    Vector = Rotate(new Vector2(X, Y), Example.Rotation.Z);
                    X = Vector.X; Y = Vector.Y;

                    X += Example.Position.X;
                    Y += Example.Position.Y;
                    Z += Example.Position.Z;

                    X -= Position.X;
                    Y -= Position.Y;
                    Z -= Position.Z;

                    Vector = Rotate(new Vector2(X, Z), Rotation.Y);
                    X = Vector.X; Z = Vector.Y;
                    Vector = Rotate(new Vector2(Y, Z), Rotation.X);
                    Y = Vector.X; Z = Vector.Y;
                    Vector = Rotate(new Vector2(X, Y), Rotation.Z);
                    X = Vector.X; Y = Vector.Y;

                    SortPoints.Add(new Vector3(X, Y, Z));
                }

                int Index = 0;
                while (Index < SortPoints.Count)
                {
                    if (SortPoints[Index].Z < Near)
                    {
                        List<Vector3> PolygonSides = new List<Vector3>();
                        Vector3 Left, Right;
                        if (Index - 1 < 0)
                            Left = SortPoints[SortPoints.Count - 1];
                        else Left = SortPoints[Index - 1];
                        Right = SortPoints[(Index + 1) % SortPoints.Count];

                        if (Left.Z > Near)
                            PolygonSides.Add(GetZCoordinate(SortPoints[Index], Left, Near));
                        if (Right.Z > Near)
                            PolygonSides.Add(GetZCoordinate(SortPoints[Index], Right, Near));
                        List<Vector3> TransitionalVector = new List<Vector3>();
                        for (int IndexVertex = 0; IndexVertex < Index; IndexVertex++)
                            TransitionalVector.Add(SortPoints[IndexVertex]);
                        TransitionalVector.AddRange(PolygonSides);
                        for (int IndexVertex = Index + 1; IndexVertex < SortPoints.Count; IndexVertex++)
                            TransitionalVector.Add(SortPoints[IndexVertex]);
                        SortPoints = TransitionalVector;
                        Index += PolygonSides.Count - 1;
                    }
                    Index++;
                }

                for (int i = 0; i < SortPoints.Count; i++)
                    if (SortPoints[i].Z < Far)
                        PointPolygons.Add(new Vector2(Window.Size.Width / 2 + SortPoints[i].X / SortPoints[i].Z * ((Window.Size.Width + Window.Size.Height) / (2 * Fov)), Window.Size.Height / 2 - SortPoints[i].Y / SortPoints[i].Z * ((Window.Size.Width + Window.Size.Height) / (2 * Fov))));
            }
            return PointPolygons;
        }

        public List<Vector2> ConvertPolygon(List<Vector3> Verticies)
        {
            List<Vector2> PointVerticies = new List<Vector2>();
            List<Vector3> SortPoints = new List<Vector3>();
            foreach (Vector3 Vertex in Verticies)
            {
                Vector2 Vector;

                float X = Vertex.X;
                float Y = Vertex.Y;
                float Z = Vertex.Z;

                X -= Position.X;
                Y -= Position.Y;
                Z -= Position.Z;

                Vector = Rotate(new Vector2(X, Z), Rotation.Y);
                X = Vector.X; Z = Vector.Y;
                Vector = Rotate(new Vector2(Y, Z), Rotation.X);
                Y = Vector.X; Z = Vector.Y;
                Vector = Rotate(new Vector2(X, Y), Rotation.Z);
                X = Vector.X; Y = Vector.Y;

                SortPoints.Add(new Vector3(X, Y, Z));
            }

            int Index = 0;
            while (Index < SortPoints.Count)
            {
                if (SortPoints[Index].Z < Near)
                {
                    List<Vector3> PolygonSides = new List<Vector3>();
                    Vector3 Left, Right;
                    if (Index - 1 < 0)
                        Left = SortPoints[SortPoints.Count - 1];
                    else Left = SortPoints[Index - 1];
                    Right = SortPoints[(Index + 1) % SortPoints.Count];

                    if (Left.Z > Near)
                        PolygonSides.Add(GetZCoordinate(SortPoints[Index], Left, Near));
                    if (Right.Z > Near)
                        PolygonSides.Add(GetZCoordinate(SortPoints[Index], Right, Near));
                    List<Vector3> TransitionalVector = new List<Vector3>();
                    for (int IndexVertex = 0; IndexVertex < Index; IndexVertex++)
                        TransitionalVector.Add(SortPoints[IndexVertex]);
                    TransitionalVector.AddRange(PolygonSides);
                    for (int IndexVertex = Index + 1; IndexVertex < SortPoints.Count; IndexVertex++)
                        TransitionalVector.Add(SortPoints[IndexVertex]);
                    SortPoints = TransitionalVector;
                    Index += PolygonSides.Count - 1;
                }
                Index++;
            }

            for (int i = 0; i < SortPoints.Count; i++)
                if (SortPoints[i].Z < Far)
                    PointVerticies.Add(new Vector2(Window.Size.Width / 2 + SortPoints[i].X / SortPoints[i].Z * ((Window.Size.Width + Window.Size.Height) / (2 * Fov)), Window.Size.Height / 2 - SortPoints[i].Y / SortPoints[i].Z * ((Window.Size.Width + Window.Size.Height) / (2 * Fov))));
            return PointVerticies;
        }

        public Vector2 ReliableCenterPolygon(List<Vector2> ConvertedVerticies)
        {
            Vector2 ReliableCenter = new Vector2(0, 0);
            foreach (Vector2 Polygon in ConvertedVerticies)
            {
                ReliableCenter.X += Polygon.X;
                ReliableCenter.Y += Polygon.Y;
            }
            ReliableCenter.X /= ConvertedVerticies.Count;
            ReliableCenter.Y /= ConvertedVerticies.Count;
            return ReliableCenter;
        }

        public Vector3 ReliableCenterPolygon(List<Vector3> SpaceVerticies)
        {
            Vector3 ReliableCenter = new Vector3(0, 0, 0);
            foreach (Vector3 Verticies in SpaceVerticies)
            {
                ReliableCenter.X += Verticies.X;
                ReliableCenter.Y += Verticies.Y;
                ReliableCenter.Z += Verticies.Z;
            }
            ReliableCenter.X /= SpaceVerticies.Count;
            ReliableCenter.Y /= SpaceVerticies.Count;
            ReliableCenter.Z /= SpaceVerticies.Count;
            return ReliableCenter;
        }

        public void DrawJoinVerticies(Graphics Graphics, List<Vector3> Verticies, Color Color)
        {
            List<PointF> Lines = new List<PointF>();
            List<Vector2> ConvertedVerticies = ConvertPolygon(Verticies);
            foreach (Vector2 Vertex in ConvertedVerticies)
                Lines.Add(new PointF(Vertex.X, Vertex.Y));
            Graphics.DrawLines(new Pen(Color),  Lines.ToArray());
        }

        private List<Vector2> ConvertPolygons(Object Example)
        {
            List<Vector2> Polygons = ObjectConvertPolygons(Example);
            Vector2 ReliableCenter = ReliableCenterPolygon(ObjectConvertPolygons(Example));
            float Angle = 180 / (float)Math.PI;
            float Degree = (float)Math.PI / 180;
            List<Vector2> PolarObjectPolygons = new List<Vector2>();
            foreach (Vector2 ObjectPolygon in Polygons)
                PolarObjectPolygons.Add(new Vector2((float)Math.Sqrt(Math.Pow(ObjectPolygon.X - ReliableCenter.X, 2) + Math.Pow(ObjectPolygon.Y - ReliableCenter.Y, 2)), (float)Math.Atan2(ObjectPolygon.Y - ReliableCenter.Y, ObjectPolygon.X - ReliableCenter.X) * Angle));

            PolarObjectPolygons.Sort(new PointCompare());

            Polygons = new List<Vector2>();
            foreach (Vector2 PolarPolygon in PolarObjectPolygons)
                Polygons.Add(new Vector2(PolarPolygon.X * (float)Math.Cos(PolarPolygon.Y * Degree) + ReliableCenter.X, PolarPolygon.X * (float)Math.Sin(PolarPolygon.Y * Degree) + ReliableCenter.Y));

            return Polygons;
        }

        public bool IsPointInObject(Vector2 Point, Object Example)
        {
            bool IsPointInObject = false;
            List<Vector2> ObjectPolygons = ConvertPolygons(Example);
            for (int i = 0, j = ObjectPolygons.Count - 1; i < ObjectPolygons.Count; j = i++)
                if ((
                    (ObjectPolygons[i].Y < ObjectPolygons[j].Y) && (ObjectPolygons[i].Y <= Point.Y) && (Point.Y <= ObjectPolygons[j].Y)
                    && ((ObjectPolygons[j].Y - ObjectPolygons[i].Y) * (Point.X - ObjectPolygons[i].X) > (ObjectPolygons[j].X - ObjectPolygons[i].X) * (Point.Y - ObjectPolygons[i].Y))
                    ) || (
                    (ObjectPolygons[i].Y > ObjectPolygons[j].Y) && (ObjectPolygons[j].Y <= Point.Y) && (Point.Y <= ObjectPolygons[i].Y)
                    && ((ObjectPolygons[j].Y - ObjectPolygons[i].Y) * (Point.X - ObjectPolygons[i].X) < (ObjectPolygons[j].X - ObjectPolygons[i].X) * (Point.Y - ObjectPolygons[i].Y))
                    )) IsPointInObject = !IsPointInObject;
            return IsPointInObject;
        }

        public bool IsPointInPolygon(Vector2 Point, Polygon Example)
        {
            bool IsPointInPolygon = false;
            List<Vector2> ObjectPolygons = Example.ConvertedVerticies;
            for (int i = 0, j = ObjectPolygons.Count - 1; i < ObjectPolygons.Count; j = i++)
                if ((
                    (ObjectPolygons[i].Y < ObjectPolygons[j].Y) && (ObjectPolygons[i].Y <= Point.Y) && (Point.Y <= ObjectPolygons[j].Y)
                    && ((ObjectPolygons[j].Y - ObjectPolygons[i].Y) * (Point.X - ObjectPolygons[i].X) > (ObjectPolygons[j].X - ObjectPolygons[i].X) * (Point.Y - ObjectPolygons[i].Y))
                    ) || (
                    (ObjectPolygons[i].Y > ObjectPolygons[j].Y) && (ObjectPolygons[j].Y <= Point.Y) && (Point.Y <= ObjectPolygons[i].Y)
                    && ((ObjectPolygons[j].Y - ObjectPolygons[i].Y) * (Point.X - ObjectPolygons[i].X) < (ObjectPolygons[j].X - ObjectPolygons[i].X) * (Point.Y - ObjectPolygons[i].Y))
                    )) IsPointInPolygon = !IsPointInPolygon;
            return IsPointInPolygon;
        }

        public Object ReturnObjectAtPoint(Vector2 Point)
        {
            foreach (Object Object in Objects)
                if (IsPointInObject(Point, Object))
                    return Object;
            return default;
        }

        public Polygon ReturnPolygonAtPointInObject(Vector2 Point, Object Object)
        {
            foreach (Polygon Polygon in Object.Polygons)
                if (IsPointInPolygon(Point, Polygon))
                    return Polygon;
            return default;
        }

        public Polygon ReturnPolygonAtPoint(Vector2 Point)
        {
            foreach (Object Object in Objects)
                foreach (Polygon Polygon in Object.Polygons)
                    if (IsPointInPolygon(Point, Polygon))
                        return Polygon;
            return default;
        }

        public Object ImportObject(string PathToObject, Vector3 Position, Vector3 Rotation, Vector3 Scale)
        {
            List<Vector3> Verticies = new List<Vector3>();
            List<int[]> IndexPolygons = new List<int[]>();
            List<Color> PolygonMaterials = new List<Color>();
            List<Bitmap> PolygonTextures = new List<Bitmap>();

            Hashtable PolygonMaterialsHashtable = new Hashtable();
            string[] StringObject = File.ReadAllLines(PathToObject);
            string NameMaterial = null;
            Color ColorMaterial = new Color();
            Bitmap TextureMaterial = null;

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
                            ColorMaterial = Color.FromArgb((byte)(float.Parse(StringMaterial.Split(new char[] { ' ' })[0]) * 255),
                                                           (byte)(float.Parse(StringMaterial.Split(new char[] { ' ' })[1]) * 255),
                                                           (byte)(float.Parse(StringMaterial.Split(new char[] { ' ' })[2]) * 255));
                        }
                        else if (Material.Split(new char[] { ' ' }, 2)[0] == "map_Kd")
                            TextureMaterial = new Bitmap(Material.Split(new char[] { ' ' }, 2)[1]);
                    }

                    PolygonMaterialsHashtable.Add(NameMaterial, new object[] { ColorMaterial, TextureMaterial });
                }

                else if (String.Split(new char[] { ' ' }, 2)[0] == "usemtl")
                {
                    ColorMaterial = (Color)((object[])PolygonMaterialsHashtable[String.Split(new char[] { ' ' }, 2)[1]])[0];
                    TextureMaterial = (Bitmap)((object[])PolygonMaterialsHashtable[String.Split(new char[] { ' ' }, 2)[1]])[1];
                }

                else if (String.Split(new char[] { ' ' }, 2)[0] == "v")
                    Verticies.Add(new Vector3(float.Parse(String.Split(new char[] { ' ' })[1].Replace('.', ',')), float.Parse(String.Split(new char[] { ' ' })[2].Replace('.', ',')), float.Parse(String.Split(new char[] { ' ' })[3].Replace('.', ','))));
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
            return new Object(Position, Rotation, Scale, Polygons, Verticies.ToArray(), IndexPolygons.ToArray());
        }

        public Object ImportObject(string PathToObject, string PathToDirMtl, Vector3 Position, Vector3 Rotation, Vector3 Scale)
        {
            List<Vector3> Verticies = new List<Vector3>();
            List<int[]> IndexPolygons = new List<int[]>();
            List<Color> PolygonMaterials = new List<Color>();
            List<Bitmap> PolygonTextures = new List<Bitmap>();

            Hashtable PolygonMaterialsHashtable = new Hashtable();
            string[] StringObject = File.ReadAllLines(PathToObject);
            string NameMaterial = null;
            Color ColorMaterial = new Color();
            Bitmap TextureMaterial = null;

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
                            ColorMaterial = Color.FromArgb((byte)(float.Parse(StringMaterial.Split(new char[] { ' ' })[0]) * 255),
                                                           (byte)(float.Parse(StringMaterial.Split(new char[] { ' ' })[1]) * 255),
                                                           (byte)(float.Parse(StringMaterial.Split(new char[] { ' ' })[2]) * 255));
                        }
                        else if (Material.Split(new char[] { ' ' }, 2)[0] == "map_Kd")
                            TextureMaterial = new Bitmap(PathToDirMtl + "/" + Material.Split(new char[] { ' ' }, 2)[1]);
                    }

                    PolygonMaterialsHashtable.Add(NameMaterial, new object[] { ColorMaterial, TextureMaterial });
                }

                else if (String.Split(new char[] { ' ' }, 2)[0] == "usemtl")
                {
                    ColorMaterial = (Color)((object[])PolygonMaterialsHashtable[String.Split(new char[] { ' ' }, 2)[1]])[0];
                    TextureMaterial = (Bitmap)((object[])PolygonMaterialsHashtable[String.Split(new char[] { ' ' }, 2)[1]])[1];
                }

                else if (String.Split(new char[] { ' ' }, 2)[0] == "v")
                    Verticies.Add(new Vector3(float.Parse(String.Split(new char[] { ' ' })[1].Replace('.', ',')), float.Parse(String.Split(new char[] { ' ' })[2].Replace('.', ',')), float.Parse(String.Split(new char[] { ' ' })[3].Replace('.', ','))));
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
            return new Object(Position, Rotation, Scale, Polygons, Verticies.ToArray(), IndexPolygons.ToArray());
        }

        public int CountImportObjectPolygons(string Path)
        {
            int Count = 0;
            string[] StringObject = File.ReadAllLines(Path);
            foreach (string String in StringObject)
                if (String.Split(new char[] { ' ' }, 2)[0] == "f")
                    Count++;
            return Count;
        }

        private float DistanceToPolygon(List<Vector3> Polygon)
        {
            float X = 0, Y = 0, Z = 0;
            foreach (Vector3 P in Polygon)
            {
                X += P.X;
                Y += P.Y;
                Z += P.Z;
            }

            return DistanceBetweenVectors(new Vector3(X, Y, Z), Position);
        }

        public Vector2 Rotate(Vector2 Vector, float Angle) =>
            new Vector2(
                Vector.X * (float)Math.Cos(Angle * Math.PI / 180) - Vector.Y * (float)Math.Sin(Angle * Math.PI / 180),
                Vector.Y * (float)Math.Cos(Angle * Math.PI / 180) + Vector.X * (float)Math.Sin(Angle * Math.PI / 180));

        public float DistanceBetweenVectors(Vector3 FirstVector, Vector3 SecondVector) =>
            (float)Math.Sqrt(Math.Pow(FirstVector.X - SecondVector.X, 2) + Math.Pow(FirstVector.Y - SecondVector.Y, 2) + Math.Pow(FirstVector.Z - SecondVector.Z, 2));

        public float DistanceToObject(Object Example) =>
            (float)Math.Sqrt(Math.Pow(Example.Position.X - Position.X, 2) + Math.Pow(Example.Position.Y - Position.Y, 2) + Math.Pow(Example.Position.Z - Position.Z, 2));


        public static float DistanceBetweenObjects(Object First, Object Second) =>
            (float)Math.Sqrt(Math.Pow(First.Position.X - Second.Position.X, 2) + Math.Pow(First.Position.Y - Second.Position.Y, 2) + Math.Pow(First.Position.Z - Second.Position.Z, 2));
    }

    public class Polygon
    {
        public List<Vector2> ConvertedVerticies;
        public List<Vector3> SpaceVerticies;
        public Color Material;
        public Bitmap Texture;
        public float Distance;

        public Polygon()
        {
            ConvertedVerticies = new List<Vector2>();
            SpaceVerticies = new List<Vector3>();
            Material = new Color();
            Texture = null;
            Distance = 0;
        }
        public Polygon(Color Material, Bitmap Texture)
        {
            this.ConvertedVerticies = new List<Vector2>();
            this.SpaceVerticies = new List<Vector3>();
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

    public class Object
    {
        public List<Polygon> Polygons;
        public Vector3[] Verticies;
        public int[][] IndexPolygons;

        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;

        public Object(Vector3 Position, Vector3 Rotation, Vector3 Scale, List<Polygon> Polygons, Vector3[] Verticies, int[][] IndexPolygons)
        {
            this.Position = Position;
            this.Rotation = Rotation;
            this.Scale = Scale;
            this.Polygons = Polygons;
            this.Verticies = Verticies;
            this.IndexPolygons = IndexPolygons;
        }

        public Object(Object Object)
        {
            this.Position = Object.Position;
            this.Rotation = Object.Rotation;
            this.Scale = Object.Scale;
            this.Polygons = Object.Polygons;
            this.Verticies = Object.Verticies;
            this.IndexPolygons = Object.IndexPolygons;
        }
    }

    public class Light
    {
        public Vector3 Position;
        public Color GlowColor;
        public float GlowRadius;
        public Light(Vector3 Position, Color GlowColor, float GlowRadius)
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

    public class DistancePolygonCompare : IComparer<Polygon>
    {
        public int Compare(Polygon FirstPolygon, Polygon SecondPolygon)
        {
            if (FirstPolygon.Distance > SecondPolygon.Distance)
                return -1;
            if (FirstPolygon.Distance < SecondPolygon.Distance)
                return 1;
            else
                return 0;
        }
    }

    public class Vector2
    {
        public float X, Y;
        public Vector2(float X, float Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }

    public class Vector3
    {
        public float X, Y, Z;
        public Vector3(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }
    }

    public class PointCompare : IComparer<Vector2>
    {
        public int Compare(Vector2 FirstPolar, Vector2 SecondPolar)
        {
            if (FirstPolar.Y > SecondPolar.Y)
                return 1;
            else if (FirstPolar.Y < SecondPolar.Y)
                return -1;
            return 0;
        }
    }
}
