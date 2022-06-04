using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SFML.Graphics;
using SFML.System;

namespace EinEngine
{
    public class Space
    {
        public RenderWindow Window;
        public List<Entity> Objects;
        public List<Light> Lights;
        public List<Audio> Audios;

        public Color SpaceLight;
        public Vector3f Position;
        public Vector3f Rotation;
        public bool Lighting { set; get; } = false;
        public float GlowIntensity { set; get; }
        public float Near { set; get; } = 1;
        public float Far { set; get; } = 100;
        public float Fov { set; get; } = 60;
        public int TextureResolution { set; get; } = 512;
        public Space(RenderWindow Window, Vector3f Position, Vector3f Rotation)
        {
            Objects = new List<Entity>();
            Lights = new List<Light>();
            Audios = new List<Audio>();
            this.Window = Window;
            this.Position = Position;
            this.Rotation = Rotation;
            this.SpaceLight = new Color(100, 149, 237);
            this.GlowIntensity = (SpaceLight.R + SpaceLight.G + SpaceLight.B) / 3;
        }

        public void Display()
        {
            Window.Clear(SpaceLight);
            GlowIntensity = (SpaceLight.R + SpaceLight.G + SpaceLight.B) / 3;
            List<Polygon> Polygons = new List<Polygon>();
            foreach (Entity Element in Objects)
            {
                if (Element.Visible)
                {
                    for (int IndexPolygon = 0; IndexPolygon < Element.IndexPolygons.Length; IndexPolygon++)
                    {
                        Element.Polygons[IndexPolygon].ConvertedVerticies = new List<Vector2f>();
                        Element.Polygons[IndexPolygon].SpaceVerticies = new List<Vector3f>();

                        int[] ElementPolygon = Element.IndexPolygons[IndexPolygon];
                        Polygon ObjectPolygon = new Polygon();
                        List<Vector3f> SortPoints = new List<Vector3f>();
                        for (int Vertex = 0; Vertex < ElementPolygon.Length; Vertex++)
                        {
                            Vector2f Vector;

                            float X = -Element.Verticies[ElementPolygon[Vertex]].X * Element.Scale.X;
                            float Y = Element.Verticies[ElementPolygon[Vertex]].Y * Element.Scale.Y;
                            float Z = Element.Verticies[ElementPolygon[Vertex]].Z * Element.Scale.Z;

                            if (Element.SpatialCoordinates)
                            {
                                Element.Polygons[IndexPolygon].SpaceVerticies.Add(new Vector3f(X + Element.Position.X, Y + Element.Position.Y, Z + Element.Position.Z));
                                ObjectPolygon.SpaceVerticies.Add(new Vector3f(X + Element.Position.X, Y + Element.Position.Y, Z + Element.Position.Z));
                            }
                            else
                            {
                                Element.Polygons[IndexPolygon].SpaceVerticies.Add(new Vector3f(X + Element.Position.X + Position.X, Y + Element.Position.Y + Position.Y, Z + Element.Position.Z + Position.Z));
                                ObjectPolygon.SpaceVerticies.Add(new Vector3f(X + Element.Position.X + Position.X, Y + Element.Position.Y + Position.Y, Z + Element.Position.Z + Position.Z));
                            }

                            Vector = Rotate(new Vector2f(X, Z), Element.Rotation.Y);
                            X = Vector.X; Z = Vector.Y;
                            Vector = Rotate(new Vector2f(Y, Z), Element.Rotation.X);
                            Y = Vector.X; Z = Vector.Y;
                            Vector = Rotate(new Vector2f(X, Y), Element.Rotation.Z);
                            X = Vector.X; Y = Vector.Y;

                            X += Element.Position.X;
                            Y += Element.Position.Y;
                            Z += Element.Position.Z;
                            if (Element.SpatialCoordinates)
                            {

                                X -= Position.X;
                                Y -= Position.Y;
                                Z -= Position.Z;

                                Vector = Rotate(new Vector2f(X, Z), Rotation.Y);
                                X = Vector.X; Z = Vector.Y;
                                Vector = Rotate(new Vector2f(Y, Z), Rotation.X);
                                Y = Vector.X; Z = Vector.Y;
                                Vector = Rotate(new Vector2f(X, Y), Rotation.Z);
                                X = Vector.X; Y = Vector.Y;
                            }

                            SortPoints.Add(new Vector3f(X, Y, Z));
                        }

                        int Index = 0;
                        while (Index < SortPoints.Count)
                        {
                            if (SortPoints[Index].Z < Near)
                            {
                                List<Vector3f> PolygonSides = new List<Vector3f>();
                                Vector3f Left, Right;
                                if (Index - 1 < 0)
                                    Left = SortPoints[SortPoints.Count - 1];
                                else Left = SortPoints[Index - 1];
                                Right = SortPoints[(Index + 1) % SortPoints.Count];

                                if (Left.Z > Near)
                                    PolygonSides.Add(GetZCoordinate(SortPoints[Index], Left, Near));
                                if (Right.Z > Near)
                                    PolygonSides.Add(GetZCoordinate(SortPoints[Index], Right, Near));
                                List<Vector3f> TransitionalVector = new List<Vector3f>();
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
                                    Element.Polygons[IndexPolygon].ConvertedVerticies.Add(new Vector2f(Window.Size.X / 2 + SortPoints[i].X / SortPoints[i].Z * ((Window.Size.X + Window.Size.Y) / (2 / (float)Math.Cos(Fov * Math.PI / 180))), Window.Size.Y / 2 - SortPoints[i].Y / SortPoints[i].Z * ((Window.Size.X + Window.Size.Y) / (2 / (float)Math.Cos(Fov * Math.PI / 180)))));
                                    ObjectPolygon.ConvertedVerticies.Add(new Vector2f(Window.Size.X / 2 + SortPoints[i].X / SortPoints[i].Z * ((Window.Size.X + Window.Size.Y) / (2 / (float)Math.Cos(Fov * Math.PI / 180))), Window.Size.Y / 2 - SortPoints[i].Y / SortPoints[i].Z * ((Window.Size.X + Window.Size.Y) / (2 / (float)Math.Cos(Fov * Math.PI / 180)))));
                                }
                            }

                            if (!Lighting) ObjectPolygon.Material = Element.Polygons[IndexPolygon].Material;
                            else
                            {
                                float SubPixel = GlowIntensity / 255;
                                ObjectPolygon.Material = new Color((byte)(SubPixel * Element.Polygons[IndexPolygon].Material.R), (byte)(SubPixel * Element.Polygons[IndexPolygon].Material.G), (byte)(SubPixel * Element.Polygons[IndexPolygon].Material.B));
                            }
                            ObjectPolygon.Texture = Element.Polygons[IndexPolygon].Texture;
                            ObjectPolygon.Distance = DistanceBetweenVectors(ReliableCenterPolygon(ObjectPolygon.SpaceVerticies), Position);
                            Polygons.Add(ObjectPolygon);
                        }
                    }
                }
            }

            //Calculate Audio Client
            foreach (Audio Audio in Audios)
                if (DistanceBetweenVectors(Audio.Position, Position) <= Audio.Radius)
                    Audio.Music.Volume = (int)(100 - DistanceBetweenVectors(Audio.Position, Position) * 100 / Audio.Radius);
                else
                    Audio.Music.Volume = 0;


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

                            Polygon.Material = new Color((byte)R, (byte)G, (byte)B);
                        }
                    }
                }

                ConvexShape Shape = new ConvexShape((uint)Polygon.ConvertedVerticies.Count);
                for (int i = 0; i < Polygon.ConvertedVerticies.Count; i++)
                    Shape.SetPoint((uint)i, Polygon.ConvertedVerticies[i]);

                Shape.FillColor = Polygon.Material;
                if (Polygon.Texture != null)
                {
                    Shape.Texture = Polygon.Texture;
                    Shape.TextureRect = new IntRect(0, 0, TextureResolution, TextureResolution);
                }
                Window.Draw(Shape);
            }
        }
        private static Vector3f GetZCoordinate(Vector3f FirstVector, Vector3f SecondVector, float Near)
        {
            if (SecondVector.Z == FirstVector.Z || Near < FirstVector.Z || Near > SecondVector.Z)
                return default;
            Vector3f DeltaVector = new Vector3f(SecondVector.X - FirstVector.X, SecondVector.Y - FirstVector.Y, SecondVector.Z - FirstVector.Z);
            return new Vector3f(FirstVector.X + DeltaVector.X * ((Near - FirstVector.Z) / DeltaVector.Z),
                                FirstVector.Y + DeltaVector.Y * ((Near - FirstVector.Z) / DeltaVector.Z), Near);
        }
        public List<Vector2f> ObjectConvertPolygons(Entity Example)
        {
            List<Vector2f> PointPolygons = new List<Vector2f>();
            for (int IndexPolygon = 0; IndexPolygon < Example.IndexPolygons.Length; IndexPolygon++)
            {
                List<Vector3f> SortPoints = new List<Vector3f>();
                int[] ElementPolygon = Example.IndexPolygons[IndexPolygon];
                for (int Vertex = 0; Vertex < ElementPolygon.Length; Vertex++)
                {
                    Vector2f Vector;

                    float X = Example.Verticies[ElementPolygon[Vertex]].X * Example.Scale.X;
                    float Y = Example.Verticies[ElementPolygon[Vertex]].Y * Example.Scale.Y;
                    float Z = Example.Verticies[ElementPolygon[Vertex]].Z * Example.Scale.Z;

                    Vector = Rotate(new Vector2f(X, Z), Example.Rotation.Y);
                    X = Vector.X; Z = Vector.Y;
                    Vector = Rotate(new Vector2f(Y, Z), Example.Rotation.X);
                    Y = Vector.X; Z = Vector.Y;
                    Vector = Rotate(new Vector2f(X, Y), Example.Rotation.Z);
                    X = Vector.X; Y = Vector.Y;

                    X += Example.Position.X;
                    Y += Example.Position.Y;
                    Z += Example.Position.Z;

                    if (Example.SpatialCoordinates)
                    {
                        X -= Position.X;
                        Y -= Position.Y;
                        Z -= Position.Z;

                        Vector = Rotate(new Vector2f(X, Z), Rotation.Y);
                        X = Vector.X; Z = Vector.Y;
                        Vector = Rotate(new Vector2f(Y, Z), Rotation.X);
                        Y = Vector.X; Z = Vector.Y;
                        Vector = Rotate(new Vector2f(X, Y), Rotation.Z);
                        X = Vector.X; Y = Vector.Y;
                    }

                    SortPoints.Add(new Vector3f(X, Y, Z));
                }

                int Index = 0;
                while (Index < SortPoints.Count)
                {
                    if (SortPoints[Index].Z < Near)
                    {
                        List<Vector3f> PolygonSides = new List<Vector3f>();
                        Vector3f Left, Right;
                        if (Index - 1 < 0)
                            Left = SortPoints[SortPoints.Count - 1];
                        else Left = SortPoints[Index - 1];
                        Right = SortPoints[(Index + 1) % SortPoints.Count];

                        if (Left.Z > Near)
                            PolygonSides.Add(GetZCoordinate(SortPoints[Index], Left, Near));
                        if (Right.Z > Near)
                            PolygonSides.Add(GetZCoordinate(SortPoints[Index], Right, Near));
                        List<Vector3f> TransitionalVector = new List<Vector3f>();
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
                        PointPolygons.Add(new Vector2f(Window.Size.X / 2 + SortPoints[i].X / SortPoints[i].Z * ((Window.Size.X + Window.Size.Y) / (2 / (float)Math.Cos(Fov * Math.PI / 180))), Window.Size.Y / 2 - SortPoints[i].Y / SortPoints[i].Z * ((Window.Size.X + Window.Size.Y) / (2 / (float)Math.Cos(Fov * Math.PI / 180)))));
            }
            return PointPolygons;
        }

        public List<Vector2f> ConvertPolygon(List<Vector3f> Verticies, bool SpatialCoordinates = true)
        {
            List<Vector2f> PointVerticies = new List<Vector2f>();
            List<Vector3f> SortPoints = new List<Vector3f>();
            foreach (Vector3f Vertex in Verticies) {
                Vector2f Vector;

                float X = Vertex.X;
                float Y = Vertex.Y;
                float Z = Vertex.Z;

                if (SpatialCoordinates) { 
                    X -= Position.X;
                    Y -= Position.Y;
                    Z -= Position.Z;

                    Vector = Rotate(new Vector2f(X, Z), Rotation.Y);
                    X = Vector.X; Z = Vector.Y;
                    Vector = Rotate(new Vector2f(Y, Z), Rotation.X);
                    Y = Vector.X; Z = Vector.Y;
                    Vector = Rotate(new Vector2f(X, Y), Rotation.Z);
                    X = Vector.X; Y = Vector.Y;
                }
                    
                SortPoints.Add(new Vector3f(X, Y, Z));
            }

            int Index = 0;
            while (Index < SortPoints.Count)
            {
                if (SortPoints[Index].Z < Near)
                {
                    List<Vector3f> PolygonSides = new List<Vector3f>();
                    Vector3f Left, Right;
                    if (Index - 1 < 0)
                        Left = SortPoints[SortPoints.Count - 1];
                    else Left = SortPoints[Index - 1];
                    Right = SortPoints[(Index + 1) % SortPoints.Count];

                    if (Left.Z > Near)
                        PolygonSides.Add(GetZCoordinate(SortPoints[Index], Left, Near));
                    if (Right.Z > Near)
                        PolygonSides.Add(GetZCoordinate(SortPoints[Index], Right, Near));
                    List<Vector3f> TransitionalVector = new List<Vector3f>();
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
                    PointVerticies.Add(new Vector2f(Window.Size.X / 2 + SortPoints[i].X / SortPoints[i].Z * ((Window.Size.X + Window.Size.Y) / (2 / (float)Math.Cos(Fov * Math.PI / 180))), Window.Size.Y / 2 - SortPoints[i].Y / SortPoints[i].Z * ((Window.Size.X + Window.Size.Y) / (2 / (float)Math.Cos(Fov * Math.PI / 180)))));
            return PointVerticies;
        }

        public static Vector2f ReliableCenterPolygon(List<Vector2f> ConvertedVerticies)
        {
            Vector2f ReliableCenter = new Vector2f(0, 0);
            foreach (Vector2f Polygon in ConvertedVerticies)
            {
                ReliableCenter.X += Polygon.X;
                ReliableCenter.Y += Polygon.Y;
            }
            ReliableCenter.X /= ConvertedVerticies.Count;
            ReliableCenter.Y /= ConvertedVerticies.Count;
            return ReliableCenter;
        }

        public static Vector3f ReliableCenterPolygon(List<Vector3f> SpaceVerticies)
        {
            Vector3f ReliableCenter = new Vector3f(0, 0, 0);
            foreach (Vector3f Verticies in SpaceVerticies)
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

        public void DrawJoinVerticies(List<Vector3f> Verticies, Color Color, bool SpatialCoordinates = true)
        {
            List<Vertex> Lines = new List<Vertex>();
            List<Vector2f> ConvertedVerticies = ConvertPolygon(Verticies, SpatialCoordinates);
            foreach (Vector2f Vertex in ConvertedVerticies)
                Lines.Add(new Vertex(Vertex, Color));
            Window.Draw(Lines.ToArray(), PrimitiveType.LinesStrip);
        }

        public List<Vector2f> ConvertPolygons(Entity Example)
        {
            List<Vector2f> Polygons = ObjectConvertPolygons(Example);
            Vector2f ReliableCenter = ReliableCenterPolygon(ObjectConvertPolygons(Example));
            float Angle = 180 / (float)Math.PI;
            float Degree = (float)Math.PI / 180;
            List<Vector2f> PolarObjectPolygons = new List<Vector2f>();
            foreach (Vector2f ObjectPolygon in Polygons)
                PolarObjectPolygons.Add(new Vector2f((float)Math.Sqrt(Math.Pow(ObjectPolygon.X - ReliableCenter.X, 2) + Math.Pow(ObjectPolygon.Y - ReliableCenter.Y, 2)), (float)Math.Atan2(ObjectPolygon.Y - ReliableCenter.Y, ObjectPolygon.X - ReliableCenter.X) * Angle));

            PolarObjectPolygons.Sort(new PointCompare());

            Polygons = new List<Vector2f>();
            foreach (Vector2f PolarPolygon in PolarObjectPolygons)
                Polygons.Add(new Vector2f(PolarPolygon.X * (float)Math.Cos(PolarPolygon.Y * Degree) + ReliableCenter.X, PolarPolygon.X * (float)Math.Sin(PolarPolygon.Y * Degree) + ReliableCenter.Y));

            return Polygons;
        }

        public bool IsPointInObject(Vector2f Point, Entity Example)
        {
            bool IsPointInObject = false;
            List<Vector2f> ObjectPolygons = ConvertPolygons(Example);
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

        public bool IsPointInPolygon(Vector2f Point, Polygon Example)
        {
            bool IsPointInPolygon = false;
            List<Vector2f> ObjectPolygons = Example.ConvertedVerticies;
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

        public Entity ReturnObjectAtPoint(Vector2f Point)
        {
            foreach (Entity Object in Objects)
                if (IsPointInObject(Point, Object) && Object.Visible)
                    return Object;
            return default;
        }

        public Polygon ReturnPolygonAtPointInObject(Vector2f Point, Entity Object)
        {
            foreach (Polygon Polygon in Object.Polygons)
                if (IsPointInPolygon(Point, Polygon) && Object.Visible)
                    return Polygon;
            return default;
        }

        public Polygon ReturnPolygonAtPoint(Vector2f Point)
        {
            foreach (Entity Object in Objects)
                foreach (Polygon Polygon in Object.Polygons)
                    if (IsPointInPolygon(Point, Polygon) && Object.Visible)
                        return Polygon;
            return default;
        }

        //Not Released
        public static float FunctionPolygon(Polygon Polygon, Vector2f Vector)
        {
            Vector3f First = Polygon.SpaceVerticies[0];
            Vector3f Second = Polygon.SpaceVerticies[1];
            Vector3f Third = Polygon.SpaceVerticies[2];

            float A, B, C, D;
            A = First.Y * (Second.Z - Third.Z) + Second.Y * (Third.Z - First.Z) + Third.Y * (First.Z - Second.Z);
            B = First.Z * (Second.X - Third.X) + Second.Z * (Third.X - First.X) + Third.Z * (First.X - Second.X);
            C = First.X * (Second.Y - Third.Y) + Second.X * (Third.Y - First.Y) + Second.X * (First.Y - Second.Y);
            D = -(First.X * (Second.Y * Third.Z - Third.Y * Second.Z) + Second.X * (Third.Y * First.Z - First.Y * Third.Z) + Third.X * (First.Y * Second.Z - Second.Y * First.Z));

            return -(Vector.X * A + Vector.Y * C + D) / B;
        }

        public static Vector2f Rotate(Vector2f Vector, float Angle) =>
            new Vector2f(
                Vector.X * (float)Math.Cos(Angle * Math.PI / 180) - Vector.Y * (float)Math.Sin(Angle * Math.PI / 180),
                Vector.Y * (float)Math.Cos(Angle * Math.PI / 180) + Vector.X * (float)Math.Sin(Angle * Math.PI / 180));

        public static float DistanceBetweenVectors(Vector3f FirstVector, Vector3f SecondVector) =>
            (float)Math.Sqrt(Math.Pow(FirstVector.X - SecondVector.X, 2) + Math.Pow(FirstVector.Y - SecondVector.Y, 2) + Math.Pow(FirstVector.Z - SecondVector.Z, 2));

        public float DistanceToObject(Entity Example) =>
            (float)Math.Sqrt(Math.Pow(Example.Position.X - Position.X, 2) + Math.Pow(Example.Position.Y - Position.Y, 2) + Math.Pow(Example.Position.Z - Position.Z, 2));


        public static float DistanceBetweenObjects(Entity First, Entity Second) =>
            (float)Math.Sqrt(Math.Pow(First.Position.X - Second.Position.X, 2) + Math.Pow(First.Position.Y - Second.Position.Y, 2) + Math.Pow(First.Position.Z - Second.Position.Z, 2));
    }

    class DistancePolygonCompare : IComparer<Polygon>
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

    class PointCompare : IComparer<Vector2f>
    {
        public int Compare(Vector2f FirstPolar, Vector2f SecondPolar)
        {
            if (FirstPolar.Y > SecondPolar.Y)
                return 1;
            else if (FirstPolar.Y < SecondPolar.Y)
                return -1;
            return 0;
        }
    }
}
