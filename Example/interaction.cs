using System;
using System.Collections.Generic;
using EinEngine;
using SFML.System;
using SFML.Graphics;
using SFML.Window;
using SFML.Audio;
namespace BuildOwnCity
{
    class Program
    {
        static RenderWindow Window = new RenderWindow(new VideoMode(1920, 1080), "BuildOwnCity", Styles.Fullscreen);
        static Random RandomArea = new Random();
        static Camera GameCamera = new Camera(Window, new Vector3f(10, 10, -10), new Vector3f(0, 0, 0));
        static Music GameMusic = new Music("Assets/Sounds/background_music.wav");
        static string PathToModels = "Assets/Models/", PathToMaterials = "Assets/Materials/";

        //Objects
        static EinEngine.Object Plane = Camera.ImportObject(PathToModels + "Terrain.obj", PathToMaterials, new Vector3f(0, 0, 0), new Vector3f(0,0,0), new Vector3f(1,1,1));
        static EinEngine.Object Build = Camera.ImportObject(PathToModels + "Build.obj", PathToMaterials, new Vector3f(0, 0, 0), new Vector3f(0, 0, 0), new Vector3f(1, 1, 1));

        static Polygon SelectedPolygon;
        static bool ToLeft, ToRight, ToForward, ToBackward, ToUp, ToDown;

        static Vector2i LastMousePosition = new Vector2i();
        static bool Moving = false;
        static float MouseSensivity = 0.1f, Speed = 0.1f;
        static void Main()
        {
            Events();
            GameCamera.Objects.Add(Plane);
            GameCamera.Near = 0.1f;
            GameCamera.Far = 200f;
            GameCamera.Lighting = true;
            GameCamera.TextureResolution = 512;
            GameCamera.SpaceLight = new Color(100, 149, 237);
            Window.Closed += delegate { Window.Close(); };

            GameCamera.Lights = new List<Light>() { new Light(new Vector3f(0,0,0), Color.White, 50) };
            GameCamera.Rotation = new Vector3f(-45, -45,0);

            GameMusic.Play();
            while (Window.IsOpen)
            {
                Movement();
                Window.DispatchEvents();
                SelectedPolygon = GameCamera.ReturnPolygonAtPointInObject(new Vector2f(Mouse.GetPosition(Window).X, Mouse.GetPosition(Window).Y), Plane);
                foreach (Polygon Polygon in Plane.Polygons)
                    Polygon.Material = new Color(93, 161, 148);
                if (SelectedPolygon != null)
                    SelectedPolygon.Material = Color.Green;

                GameCamera.Display();
                GameCamera.DrawJoinVerticies(new List<Vector3f>() { new Vector3f(-100, 0, 0), new Vector3f(100, 0, 0) }, Color.Red);
                GameCamera.DrawJoinVerticies(new List<Vector3f>() { new Vector3f(0, -100, 0), new Vector3f(0, 100, 0) }, Color.Green);
                GameCamera.DrawJoinVerticies(new List<Vector3f>() { new Vector3f(0, 0, -100), new Vector3f(0, 0, 100) }, Color.Blue);
                Window.Display();
            }
        }

        static void Movement()
        {
            if (ToForward)
            {
                GameCamera.Position.X += Speed * (float)Math.Sin(GameCamera.Rotation.Y * Math.PI / 180);
                GameCamera.Position.Z += Speed * (float)Math.Cos(GameCamera.Rotation.Y * Math.PI / 180);
            }
            else if (ToBackward)
            {
                GameCamera.Position.X -= Speed * (float)Math.Sin(GameCamera.Rotation.Y * Math.PI / 180);
                GameCamera.Position.Z -= Speed * (float)Math.Cos(GameCamera.Rotation.Y * Math.PI / 180);
            }

            if (ToLeft)
            {
                GameCamera.Position.X -= Speed * (float)Math.Cos(GameCamera.Rotation.Y * Math.PI / 180);
                GameCamera.Position.Z += Speed * (float)Math.Sin(GameCamera.Rotation.Y * Math.PI / 180);
            }
            else if (ToRight)
            {
                GameCamera.Position.X += Speed * (float)Math.Cos(GameCamera.Rotation.Y * Math.PI / 180);
                GameCamera.Position.Z -= Speed * (float)Math.Sin(GameCamera.Rotation.Y * Math.PI / 180);
            }

            if (ToUp)
                GameCamera.Position.Y += Speed;
            else if (ToDown)
                GameCamera.Position.Y -= Speed;


            if (GameCamera.Position.Y < 1)
                GameCamera.Position.Y = 1;
            else if (GameCamera.Position.Y > 100)
                GameCamera.Position.Y = 100;

            if (GameCamera.Position.X < -100)
                GameCamera.Position.X = -100;
            else if (GameCamera.Position.X > 100)
                GameCamera.Position.X = 100;

            if (GameCamera.Position.Z < -100)
                GameCamera.Position.Z = -100;
            else if (GameCamera.Position.Z > 100)
                GameCamera.Position.Z = 100;
        }

        static void Events()
        {
            Window.MouseButtonPressed += delegate (object sender, MouseButtonEventArgs e)
            {
                if (e.Button == Mouse.Button.Right)
                {
                    Moving = true;
                    Mouse.SetPosition(new Vector2i((int)Window.Size.X / 2, (int)Window.Size.Y / 2));
                    Window.SetMouseCursorVisible(false);
                }
                else if(e.Button == Mouse.Button.Left)
                {
                    if(SelectedPolygon != null)
                    {
                        Build.Position = Camera.ReliableCenterPolygon(SelectedPolygon.SpaceVerticies);
                        GameCamera.Objects.Add(new EinEngine.Object(Build));
                    }
                }
            };

            Window.MouseButtonReleased += delegate (object sender, MouseButtonEventArgs e)
            {
                if (e.Button == Mouse.Button.Right)
                {
                    Moving = false;
                    Window.SetMouseCursorVisible(true);
                }
            };


            Window.MouseMoved += delegate
            {
                if (Moving)
                {
                    Vector2f DeltaPosition = new Vector2f(Mouse.GetPosition().X - LastMousePosition.X, Mouse.GetPosition().Y - LastMousePosition.Y);
                    DeltaPosition *= MouseSensivity;

                    float MaxMousePositionRadius = Math.Min(Window.Size.X, Window.Size.Y) / 3;
                    Vector2i WindowCenter = new Vector2i((int)Window.Size.X / 2, (int)Window.Size.Y / 2);

                    if (Math.Sqrt(Math.Pow(Mouse.GetPosition().X - WindowCenter.X, 2) + Math.Pow(Mouse.GetPosition().Y - WindowCenter.Y, 2)) > MaxMousePositionRadius)
                    {
                        Mouse.SetPosition(new Vector2i((int)WindowCenter.X, (int)WindowCenter.Y));
                        LastMousePosition = WindowCenter;
                    }
                    else
                        LastMousePosition = Mouse.GetPosition();

                    GameCamera.Rotation.Y += DeltaPosition.X;
                    GameCamera.Rotation.X -= DeltaPosition.Y;
                }
            };


            Window.KeyPressed += delegate (object sender, KeyEventArgs e)
            {
                if (Moving)
                {
                    switch (e.Code)
                    {
                        case Keyboard.Key.W: ToForward = true; break;
                        case Keyboard.Key.S: ToBackward = true; break;
                        case Keyboard.Key.A: ToLeft = true; break;
                        case Keyboard.Key.D: ToRight = true; break;
                        case Keyboard.Key.Space: ToUp = true; break;
                        case Keyboard.Key.LShift: ToDown = true; break;
                        case Keyboard.Key.Escape:
                            Window.Close();
                            break;
                    }
                }
            };


            Window.KeyReleased += delegate (object sender, KeyEventArgs e)
            {
                switch (e.Code)
                {
                    case Keyboard.Key.W: ToForward = false; break;
                    case Keyboard.Key.S: ToBackward = false; break;
                    case Keyboard.Key.A: ToLeft = false; break;
                    case Keyboard.Key.D: ToRight = false; break;
                    case Keyboard.Key.Space: ToUp = false; break;
                    case Keyboard.Key.LShift: ToDown = false; break;
                }
            };
        }

        static Color Material(Color Color, int Area)
        {
            int R = RandomArea.Next(-Area, Area);
            int G = RandomArea.Next(-Area, Area);
            int B = RandomArea.Next(-Area, Area);
            if (Color.R + R > 255)
                Color.R = 255;
            else if (Color.R + R < 0)
                Color.R = 0;
            else Color.R += (byte)R;

            if (Color.G + G > 255)
                Color.G = 255;
            else if (Color.G + G < 0)
                Color.G = 0;
            else Color.G += (byte)G;

            if (Color.B + B > 255)
                Color.B = 255;
            else if (Color.B + B < 0)
                Color.B = 0;
            else Color.B += (byte)B;

            return Color;
        }
    }
}
