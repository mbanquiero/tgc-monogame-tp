using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP
{
    /// <summary>
    ///     Esta es la clase principal  del juego.
    ///     Inicialmente puede ser renombrado o copiado para hacer más ejemplos chicos, en el caso de copiar para que se
    ///     ejecute el nuevo ejemplo deben cambiar la clase que ejecuta Program <see cref="Program.Main()" /> linea 10.
    /// </summary>
    public class TGCGame : Game
    {
        public bool ver_mesh = false;
        public const string MyContentFolder = "C:\\Counter-Strike Source\\cstrike\\";
        public const string map_name = "cs_assault";
        //"de_mirage_csgo"
        public String mesh_name = "props\\de_train\\utility_truck";
        //public String mesh_name = "props_junk\\garbage_bag001a";

        public float fieldOfView = MathHelper.PiOver4;
        public float aspectRatio = 1;
        public float nearClipPlane = 5;
        public float farClipPlane = 50000;
        Matrix Projection, View;
        public Vector3 viewDir = new Vector3(0, 0, 1);
        //public Vector3 posPlayer = new Vector3(5120, -577, 4160);
        public Vector3 posPlayer = new Vector3(6631, 0, 4000);
        public Effect EffectMesh;
        public CBspFile scene;
        public CMdlMesh mesh;

        public int mouse_ox = 0, mouse_oy = 0;

        // tool ver mesh
        public Vector3 LookAt = new Vector3(0, 0, 0), LookFrom = new Vector3(100, 0, 100);

        public SpriteFont font;
        public SpriteBatch spriteBatch;
        public bool[] keyDown = new bool[256];

        public bool fisica = false;


        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        public TGCGame()
        {
            Graphics = new GraphicsDeviceManager(this);
            // Descomentar para que el juego sea pantalla completa.
            // Graphics.IsFullScreen = true;
            // Carpeta raiz donde va a estar toda la Media.
            Content.RootDirectory = "Content";
            // Hace que el mouse sea visible.
            IsMouseVisible = false;
        }

        private GraphicsDeviceManager Graphics { get; }

        protected override void Initialize()
        {
            aspectRatio = GraphicsDevice.Viewport.AspectRatio;
            Projection = Matrix.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearClipPlane, farClipPlane);
            Projection.M11 *= -1;           // dif. de convencion con el motor de Source
            Graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 100;
            Graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 100;
            Graphics.ApplyChanges();
            base.Initialize();
            Mouse.SetPosition(mouse_ox = GraphicsDevice.Viewport.Width / 2,mouse_oy = GraphicsDevice.Viewport.Height / 2);
        }

        protected override void LoadContent()
        {
            font = Content.Load<SpriteFont>("SpriteFonts/Arial");
            spriteBatch = new SpriteBatch(GraphicsDevice);
            EffectMesh = Content.Load<Effect>("Effects/BasicShader");

            
            scene = new CBspFile(map_name, GraphicsDevice, Content);
            posPlayer = scene.cg;
            //posPlayer = new Vector3(5300, -700, 5250);
            posPlayer = new Vector3(5631, -600, 4600);


              mesh = new CMdlMesh(mesh_name, GraphicsDevice, Content, "C:\\Counter-Strike Source\\cstrike\\");
            LookAt = mesh.cg;
            LookFrom = LookAt + new Vector3(1, 0, 1) * mesh.size.Length() * 1.1f;


            base.LoadContent();
        }
        protected override void Update(GameTime gameTime)
        {
            // Aca deberiamos poner toda la logica de actualizacion del juego.

            // Capturar Input teclado
            var keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Escape))
                //Salgo del juego.
                Exit();

            if (ver_mesh)
            {
                // tool ver mesh
                // Press Directional Keys to rotate cube
                if (keyState.IsKeyDown(Keys.Up)) LookFrom = Vector3.Transform(LookFrom, Matrix.CreateRotationX(-0.05f));

                if (keyState.IsKeyDown(Keys.Down)) LookFrom = Vector3.Transform(LookFrom, Matrix.CreateRotationX(0.05f));

                if (keyState.IsKeyDown(Keys.Left)) LookFrom = Vector3.Transform(LookFrom, Matrix.CreateRotationY(-0.05f));

                if (keyState.IsKeyDown(Keys.Right)) LookFrom = Vector3.Transform(LookFrom, Matrix.CreateRotationY(0.05f));

                float elapsedTime = gameTime.ElapsedGameTime.Milliseconds;

                View = Matrix.CreateLookAt(LookFrom, LookAt, new Vector3(0, 1, 0));

            }
            else
            {
                float elapsed_time = (float)gameTime.ElapsedGameTime.TotalSeconds;
                MouseState state = Mouse.GetState();
                int dx = state.X - mouse_ox;
                int dy = state.Y - mouse_oy;
                float vel_mouse = elapsed_time * 0.25f;
                viewDir = Vector3.TransformNormal(viewDir, Matrix.CreateRotationY(dx * vel_mouse));
                Vector3 N = Vector3.Cross(new Vector3(0, 1, 0), viewDir);
                viewDir = Vector3.TransformNormal(viewDir, Matrix.CreateFromAxisAngle(N,dy * vel_mouse));
                Mouse.SetPosition(mouse_ox,mouse_oy);


                Vector3 posAnt = posPlayer;
                if (keyState.IsKeyDown(Keys.Up)) posPlayer += viewDir * 10;

                if (keyState.IsKeyDown(Keys.Down)) posPlayer -= viewDir * 10;

                //if (keyState.IsKeyDown(Keys.Left)) viewDir = Vector3.TransformNormal(viewDir, Matrix.CreateRotationY(-0.05f));

                //if (keyState.IsKeyDown(Keys.Right)) viewDir = Vector3.TransformNormal(viewDir, Matrix.CreateRotationY(0.05f));

                if (keyState.IsKeyDown(Keys.LeftControl)) posPlayer.Y += 10;
                if (keyState.IsKeyDown(Keys.LeftShift)) posPlayer.Y -= 10;


                /*
                if (keyState.IsKeyDown(Keys.PageDown))
                {
                    if (!keyDown[(int)Keys.PageDown])
                        scene.current_subset++;
                    keyDown[(int)Keys.PageDown] = true;
                }
                else
                    keyDown[(int)Keys.PageDown] = false;

                if (keyState.IsKeyDown(Keys.PageUp))
                {
                    if (!keyDown[(int)Keys.PageUp])
                        scene.current_subset--;
                    keyDown[(int)Keys.PageUp] = true;
                }
                else
                    keyDown[(int)Keys.PageUp] = false;


                if (scene.current_subset < 0)
                    scene.current_subset = 0;
                else
                if (scene.current_subset > scene.cant_subsets - 1)
                    scene.current_subset = scene.cant_subsets - 1;
                */


                if (keyState.IsKeyDown(Keys.PageDown))
                {
                    if (!keyDown[(int)Keys.PageDown] && scene.current_model < scene.cant_modelos - 1)
                    {
                        scene.current_model++;
                        posPlayer = scene.modelos[scene.current_model].origin - new Vector3(100, 0, 100);
                    }
                    keyDown[(int)Keys.PageDown] = true;
                }
                else
                    keyDown[(int)Keys.PageDown] = false;

                if (keyState.IsKeyDown(Keys.PageUp))
                {
                    if (!keyDown[(int)Keys.PageUp] && scene.current_model > 0)
                    {
                        scene.current_model--;
                        posPlayer = scene.modelos[scene.current_model].origin - new Vector3(100, 0, 100);
                    }
                    keyDown[(int)Keys.PageUp] = true;
                }
                else
                    keyDown[(int)Keys.PageUp] = false;



                if (keyState.IsKeyDown(Keys.Space))
                {
                    if (!keyDown[(int)Keys.Space])
                        fisica = !fisica;
                    keyDown[(int)Keys.Space] = true;
                }
                else
                    keyDown[(int)Keys.Space] = false;

                if (keyState.IsKeyDown(Keys.T))
                {
                    if (!keyDown[(int)Keys.T])
                        scene.mostrar_tools = !scene.mostrar_tools;
                    keyDown[(int)Keys.T] = true;
                }
                else
                    keyDown[(int)Keys.T] = false;

                if (fisica)
                {
                    Vector3 dir = posPlayer - posAnt;
                    if (dir.LengthSquared() > 0)
                    {
                        dir.Normalize();
                        float s = scene.intersectSegment(posAnt, posAnt + dir * 30);
                        if (s < 1000)
                        {
                            posPlayer = posAnt;
                        }
                    }

                    float t = scene.intersectSegment(posPlayer, posPlayer - new Vector3(0, 100, 0));
                    if (t < 1000)
                    {
                        // toca piso
                        posPlayer.Y -= t * 100 - 50;
                    }
                    else
                    {
                        // esta en el aire
                        float et = (float)gameTime.ElapsedGameTime.TotalSeconds;
                        posPlayer.Y -= et * 250;
                    }

                }
                View = Matrix.CreateLookAt(posPlayer, posPlayer + viewDir, new Vector3(0, 1, 0));
            }

            base.Update(gameTime);
        }

        /// <summary>
        ///     Se llama cada vez que hay que refrescar la pantalla.
        ///     Escribir aquí todo el código referido al renderizado.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.Clear(Color.Black);

            /*
			RasterizerState rasterizerState = new RasterizerState();
			rasterizerState.FillMode = FillMode.WireFrame;
			GraphicsDevice.RasterizerState = rasterizerState;
			*/

            if (ver_mesh)
            {
                // mesh
                EffectMesh.CurrentTechnique = EffectMesh.Techniques["TextureDrawing"];
                mesh.Draw(GraphicsDevice, EffectMesh, Matrix.Identity, View, Projection);
            }
            else
            {
                // escenario
                scene.Draw(Matrix.Identity, View, Projection);
            }

            //float t = scene.intersectSegment(posPlayer, posPlayer - new Vector3(0, 1000, 0))*1000;
            spriteBatch.Begin();
            //spriteBatch.DrawString(font, "Subset:"+scene.current_subset+
            //"  " + scene.subset[scene.current_subset].image_name, new Vector2(10, 10), Color.YellowGreen);
            spriteBatch.DrawString(font, "X:" + posPlayer.X + "Y:" + posPlayer.Y     + "  Z:" +posPlayer.Z, new Vector2(10, 10), Color.YellowGreen);
            
            spriteBatch.End();
 
            base.Draw(gameTime);
        }

        /// <summary>
        ///     Libero los recursos que se cargaron en el juego.
        /// </summary>
        protected override void UnloadContent()
        {
            // Libero los recursos.
            Content.Unload();

            base.UnloadContent();
        }
    }
}