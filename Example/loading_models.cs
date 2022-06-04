using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.Window;
using SFML.System;
using EinEngine;
using System.IO;

namespace TestSMFL
{
    /*
     * ConvexShape - Полигоны
     * CircleShape - Круги
     * RectangleShape - Линии и прямоугольники
     */
    class Program
    {
        static Random RandomArea = new Random();
        static RenderWindow Window = new RenderWindow(new VideoMode(1920, 1080), "TestSFML", Styles.Fullscreen);
        static Camera Canvas = new Camera(Window, new Vector3f(0, 2, -5), new Vector3f(0, 0, 0));
        static EinEngine.Object Terrain = Camera.ImportObject("Assets/Models/Terrain.obj", "Assets/Materials/Terrain", new Vector3f(0, 0, 0), new Vector3f(0, 0, 0), new Vector3f(1, 1, 1));
        static EinEngine.Object Bonfire = Camera.ImportObject("Assets/Models/Bonfire.obj", "Assets/Materials/Bonfire", new Vector3f(3, 0, 3), new Vector3f(0, 0, 0), new Vector3f(1, 1, 1));
        static EinEngine.Object Sky = Camera.ImportObject("Assets/Models/Sky.obj", "Assets/Materials/Sky", new Vector3f(0, 0, 0), new Vector3f(0, 0, 0), new Vector3f(1, 1, 1));

        static EinEngine.Object Fire = new EinEngine.Object(new Vector3f(3, 0.5f, 3), new Vector3f(0, 0, 0), new Vector3f(1, 1, 1),
            new List<Polygon>() { new Polygon(new Color(255, 255, 255), null) }, new Vector3f[] {
                new Vector3f(-0.5f, -0.5f, 0), new Vector3f(-0.5f, 0.5f, 0), new Vector3f(0.5f, 0.5f, 0), new Vector3f(0.5f, -0.5f, 0) },
                new int[][] { new int[] { 0, 1, 2, 3 } });

        static List<Texture> FireTextures = new List<Texture>();
        static float CountFire = 0;

        static int R, G, B;

        static bool ToLeft, ToRight, ToForward, ToBackward, ToUp, ToDown;
        static float Speed = 0.1f; 

        static Vector2i LastMousePosition = new Vector2i();
        static float MouseSensivity = 0.1f;

        static Font GameFont = new Font("Assets/Font/Arial.ttf");
        static Text GameText = new Text();

        static void Main()
        {
            R = Canvas.SpaceLight.R;
            G = Canvas.SpaceLight.G;
            B = Canvas.SpaceLight.B;

            Canvas.Near = 0.1f;
            Canvas.Far = 200;
            Canvas.Lighting = true;
            Window.SetVerticalSyncEnabled(true);
            Window.SetMouseCursorVisible(false);
            Window.Closed += delegate { Window.Close(); };


            Window.MouseMoved += delegate
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

                Canvas.Rotation.Y += DeltaPosition.X;
                Canvas.Rotation.X -= DeltaPosition.Y;
            };

            Window.KeyPressed += delegate (object sender, KeyEventArgs e)
            {
                switch (e.Code)
                {
                    case Keyboard.Key.W: ToForward = true; break;
                    case Keyboard.Key.S: ToBackward = true; break;
                    case Keyboard.Key.A: ToLeft = true; break;
                    case Keyboard.Key.D: ToRight = true; break;
                    case Keyboard.Key.Space: ToUp = true; break;
                    case Keyboard.Key.LShift: ToDown = true; break;
                    case Keyboard.Key.Q: Canvas.Fov += 1; break;
                    case Keyboard.Key.E: Canvas.Fov -= 1; break;
                    case Keyboard.Key.X: 
                        R += 1;
                        G += 1;
                        B += 1;

                        if (R > 255) R = 255;else if (R < 0) R = 0;
                        if (G > 255) G = 255;else if (G < 0) G = 0;
                        if (B > 255) B = 255;else if (B < 0) B = 0;

                        break;
                    case Keyboard.Key.Z:
                        R -= 1;
                        G -= 1;
                        B -= 1;

                        if (R > 255) R = 255;else if (R < 0) R = 0;
                        if (G > 255) G = 255;else if (G < 0) G = 0;
                        if (B > 255) B = 255;else if (B < 0) B = 0;
                        break;
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

            Canvas.Lights = new List<Light>() { new Light(new Vector3f(3, 1, 3), new Color(255, 255, 255), 10) };

            foreach (string TexturePath in Directory.GetFiles("Assets/Materials/Bonfire/Fire"))
                FireTextures.Add(new Texture(TexturePath));

            Canvas.Objects = new List<EinEngine.Object>() { Terrain, Bonfire, Sky };
            while (Window.IsOpen)
            {

                Movement();
                Window.DispatchEvents();
                Sky.Position = Canvas.Position;
                Canvas.SpaceLight = new Color((byte)R, (byte)G, (byte)B);
                Canvas.Display();

                Fire.Rotation.Y = -Canvas.Rotation.Y;
                Fire.Polygons[0].Texture = FireTextures[(int)CountFire];
                if (CountFire > FireTextures.Count - 1)
                    CountFire = 0;
                else CountFire += 0.25f;

                GameText = new Text("X: " + Canvas.Position.X + " Y: " + Canvas.Position.Y + " Z: " + Canvas.Position.Z, GameFont);
                GameText.FillColor = Color.Black;
                GameText.Scale = new Vector2f(0.5f, 0.5f);
                GameText.Position = new Vector2f(0, 0);
                Window.Draw(GameText);
                Window.Display();
            }
        }

        static void Movement()
        {
            if (ToForward)
            {
                Canvas.Position.X += Speed * (float)Math.Sin(Canvas.Rotation.Y * Math.PI / 180);
                Canvas.Position.Z += Speed * (float)Math.Cos(Canvas.Rotation.Y * Math.PI / 180);
            }
            else if (ToBackward)
            {
                Canvas.Position.X -= Speed * (float)Math.Sin(Canvas.Rotation.Y * Math.PI / 180);
                Canvas.Position.Z -= Speed * (float)Math.Cos(Canvas.Rotation.Y * Math.PI / 180);
            }

            if (ToLeft)
            {
                Canvas.Position.X -= Speed * (float)Math.Cos(Canvas.Rotation.Y * Math.PI / 180);
                Canvas.Position.Z += Speed * (float)Math.Sin(Canvas.Rotation.Y * Math.PI / 180);
            }
            else if (ToRight)
            {
                Canvas.Position.X += Speed * (float)Math.Cos(Canvas.Rotation.Y * Math.PI / 180);
                Canvas.Position.Z -= Speed * (float)Math.Sin(Canvas.Rotation.Y * Math.PI / 180);
            }

            if (ToUp)
                Canvas.Position.Y += Speed;
            else if (ToDown)
                Canvas.Position.Y -= Speed;


            if (Canvas.Position.Y < 0)
                Canvas.Position.Y = 0;
            else if (Canvas.Position.Y > 20)
                Canvas.Position.Y = 20;

            if (Canvas.Position.X < -20)
                Canvas.Position.X = -20;
            else if (Canvas.Position.X > 20)
                Canvas.Position.X = 20;

            if (Canvas.Position.Z < -20)
                Canvas.Position.Z = -20;
            else if (Canvas.Position.Z > 20)
                Canvas.Position.Z = 20;
        }

        public static Color Material(Color Material, byte Area)
        {
            byte R, G, B;
            R = (byte)(Material.R + RandomArea.Next(-Area, Area));
            G = (byte)(Material.G + RandomArea.Next(-Area, Area));
            B = (byte)(Material.B + RandomArea.Next(-Area, Area));

            if (R > 255) R = 255;else if (R < 0) R = 0;
            if (G > 255) G = 255;else if (G < 0) G = 0;
            if (B > 255) B = 255;else if (B < 0) B = 0;

            Material = new Color(R, G, B);
            return Material;
        }
    }
}
