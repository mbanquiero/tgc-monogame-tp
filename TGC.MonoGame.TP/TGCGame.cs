﻿using System;
using System.IO;
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

        public float soldier_height = 77;

        public const string cs_folder = "C:\\Counter-Strike Source\\cstrike\\";
        public const string map_name = "cs_assault";
        public String weapon_name = "weapons\\v_rif_ak47";
        public Vector3 weapon_desf = new Vector3(-4,-1,-14);
        public float weapon_angle = 80;

        public float fieldOfView = MathHelper.PiOver4;
        public float aspectRatio = 1;
        public float nearClipPlane = 5;
        public float farClipPlane = 50000;
        Matrix Projection, View;

        public const int MAX_ENEMIGOS = 10;
        public CPlayer player;
        public CEnemy[] enemigo = new CEnemy[MAX_ENEMIGOS];
        public Vector3 camPosition;

        public Effect EffectMesh;
        public CBspFile scene;

        public Model tgcLogo;

        // tool ver mesh
        public Vector3 LookAt = new Vector3(0, 0, 0), LookFrom = new Vector3(100, 0, 100);

        public SpriteFont font;
        public SpriteBatch spriteBatch;
        public bool[] keyDown = new bool[256];

        public bool fisica = true;

        // grabar gamelay
        public bool recording = false;
        public bool playing = false;
        public const int MAX_FRAMES = 60 * 60 * 5;
        public int cant_frames = 0;
        public int curr_frame = 0;
        public Vector3[] rLookAt = new Vector3[MAX_FRAMES];
        public Vector3[] rLookFrom = new Vector3[MAX_FRAMES];

        // modelos
        public Effect EffectSmd;
        public CSMDModel ak47;
        public CSMDModel soldier;

        // mouse captured
        public int mouse_ox, mouse_oy;

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
            IsMouseVisible = true;

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
               LoadContentGame();
        }
        protected override void Update(GameTime gameTime)
        {
                UpdateGame(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
                DrawGame(gameTime);
        }


        void LoadContentGame()
        {
            font = Content.Load<SpriteFont>("SpriteFonts/Arial");
            spriteBatch = new SpriteBatch(GraphicsDevice);

            EffectMesh = Content.Load<Effect>("Effects/BasicShader");
            EffectSmd = Content.Load<Effect>("Effects/SMDEffect");
            EffectSmd.CurrentTechnique = EffectSmd.Techniques["SkinnedMesh"];

            // Arma
            weapon_name = "weapons\\v_rif_galil";
            String ani_folder = weapon_name + "_anims";
            ak47 = new CSMDModel(weapon_name, GraphicsDevice, Content, cs_folder);
            ak47.debugEffect = Content.Load<Effect>("Effects/BasicShader");
            // cargo las animaciones
            ak47.cargar_ani(ani_folder, "draw");
            ak47.cargar_ani(ani_folder, "idle");
            ak47.cargar_ani(ani_folder, "reload");
            ak47.cargar_ani(ani_folder, "shoot1");
            ak47.cargar_ani(ani_folder, "shoot2");
            ak47.cargar_ani(ani_folder, "shoot3");
            ak47.setAnimation(2);

            // Soldado
            soldier = new CSMDModel("player\\ct_sas", GraphicsDevice, Content, cs_folder);
            soldier.cargar_ani("player\\ct_sas_anims", "ragdoll");
            soldier.cargar_ani("player\\cs_player_shared_anims", "a_WalkN");
            soldier.anim[1].in_site = true;
            soldier.setAnimation(1);
            soldier_height = soldier.size.Y;


            // huevo de pascuas
            tgcLogo = Content.Load<Model>("Models/tgc-logo/tgc-logo");
            var modelEffect = (BasicEffect)tgcLogo.Meshes[0].Effects[0];
            modelEffect.DiffuseColor = Color.DarkBlue.ToVector3();
            modelEffect.EnableDefaultLighting();


            scene = new CBspFile(map_name, GraphicsDevice, Content);
            player = new CPlayer(scene , this);

            Random rnd = new Random();
            for (int i = 0; i < MAX_ENEMIGOS; ++i)
            {
                enemigo[i] = new CEnemy(scene , this);
                enemigo[i].Position = new Vector3(rnd.Next((int)scene.p_min.X, (int)scene.p_max.X), 
                                scene.p_max.Y, rnd.Next((int)scene.p_min.Z, (int)scene.p_max.Z));
                enemigo[i].Position = new Vector3(7242+ rnd.Next(-300,300), -493, 6746+ rnd.Next(-300, 300));

                float an = rnd.Next(0, 360)*MathF.PI/180.0f;
                enemigo[i].Direction = new Vector3(MathF.Cos(an), 0, MathF.Sin(an));

            }
            
            enemigo[0].PrevPosition = enemigo[0].Position = new Vector3(6447, -800, 6276);
            //enemigo[0].PrevPosition = enemigo[0].Position = new Vector3(7242, -400, 5116);
            enemigo[0].Direction = new Vector3(0, 0, -1);
            enemigo[0].vel_lineal = soldier.speed;

            //player.Position = new Vector3(6447, -800, 6376);
            player.Position = scene.cg;
            player.Direction = new Vector3(0,0,1);

            base.LoadContent();
        }


        public void UpdateGame(GameTime gameTime)
        {
            float elapsed_time = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Escape))
                Exit();                     //Salgo del juego.

            float k = ak47.cur_anim >= 3 ? 10 : 1;
            ak47.update(elapsed_time * k);

            for (int i=0;i<MAX_ENEMIGOS;++i)
            {
                soldier.update(elapsed_time);
            }

            MouseState state = Mouse.GetState();
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

            if (keyState.IsKeyDown(Keys.R))
            {
                if (!keyDown[(int)Keys.R])
                    recording = !recording;
                keyDown[(int)Keys.R] = true;
            }
            else
                keyDown[(int)Keys.R] = false;

            if (keyState.IsKeyDown(Keys.P))
            {
                if (!keyDown[(int)Keys.P])
                    playing = !playing;
                keyDown[(int)Keys.P] = true;
                if (playing)
                    curr_frame = 0;
            }
            else
                keyDown[(int)Keys.P] = false;


            if (keyState.IsKeyDown(Keys.S))
            {
                if (!keyDown[(int)Keys.S])
                    scene.usar_smd = !scene.usar_smd;
                keyDown[(int)Keys.S] = true;
            }
            else
                keyDown[(int)Keys.S] = false;

            player.ProcessInput(elapsed_time);

            if (fisica)
            {
                player.UpdatePhysics(elapsed_time);
            }

            for (int i = 0; i < MAX_ENEMIGOS; ++i)
                enemigo[i].UpdatePhysics(elapsed_time);


            if (keyState.IsKeyDown(Keys.LeftShift))
            {
                if (keyState.IsKeyDown(Keys.Up)) weapon_desf.Z += 1.1f;
                if (keyState.IsKeyDown(Keys.Down)) weapon_desf.Z -= 1.1f;
                if (keyState.IsKeyDown(Keys.Left)) weapon_desf.X += 1.1f;
                if (keyState.IsKeyDown(Keys.Right)) weapon_desf.X -= 1.1f;

            }
            if (keyState.IsKeyDown(Keys.Q)) weapon_desf.Y += 1.1f;
            if (keyState.IsKeyDown(Keys.A)) weapon_desf.Y -= 1.1f;
            if (keyState.IsKeyDown(Keys.W)) weapon_angle+= 1;
            if (keyState.IsKeyDown(Keys.S)) weapon_angle-= 1;

            if (state.LeftButton == ButtonState.Pressed)
                ak47.setAnimation(3);
            else
                ak47.setAnimation(1);


            if (playing)
            {
                /*LookAt = rLookAt[curr_frame % cant_frames % MAX_FRAMES];
                LookFrom = rLookFrom[curr_frame % cant_frames % MAX_FRAMES];
                View = Matrix.CreateLookAt(LookFrom, LookAt, new Vector3(0, 1, 0));
                */
            }
            else
            {
                if (recording)
                {
                    rLookFrom[cant_frames % MAX_FRAMES] = player.Position;
                    rLookAt[cant_frames % MAX_FRAMES] = player.Position + player.Direction;
                    ++cant_frames;
                }

                // camara primera persona
                // la pos.Y = pos suelo + soldier_height/2 
                // pos camara = pos.Y + soldier_height/2  - epsilon (en pulgadas)
                Vector3 desf = new Vector3(0, soldier_height/2-5, 0);
                camPosition = player.Position + player.Direction * 0 + desf;
                View = Matrix.CreateLookAt(camPosition, camPosition + player.Direction * 100 , new Vector3(0, 1, 0));
                    
            }


            
            base.Update(gameTime);
        }

        /// <summary>
        ///     Se llama cada vez que hay que refrescar la pantalla.
        ///     Escribir aquí todo el código referido al renderizado.
        /// </summary>
        public void DrawGame(GameTime gameTime)
        {
            if (playing)
            {
                LookAt = rLookAt[curr_frame % cant_frames % MAX_FRAMES];
                LookFrom = rLookFrom[curr_frame % cant_frames % MAX_FRAMES];
                View = Matrix.CreateLookAt(LookFrom, LookAt, new Vector3(0, 1, 0));
            }

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.Clear(Color.Black);

            // escenario
            scene.Draw(Matrix.Identity, View, Projection);

            var world = Matrix.CreateRotationY(weapon_angle * MathF.PI / 180.0f) *
                        Matrix.CreateTranslation(weapon_desf + 
                            new Vector3(0,0,MathF.Sin(player.dist*0.01f)))*
                        CalcularMatrizOrientacion(1.0f, camPosition, player.Direction);
            ak47.Draw(GraphicsDevice, EffectSmd, world, View, Projection);



            for (int i = 0; i < MAX_ENEMIGOS; ++i)
            {
                world = Matrix.CreateRotationY(MathF.PI / 2.0f) *
                    CalcularMatrizOrientacion(1, enemigo[i].Position - new Vector3(0, soldier_height/2, 0), enemigo[i].Direction);
                soldier.Draw(GraphicsDevice, EffectSmd, world, View, Projection);
            }

            tgcLogo.Draw(Matrix.CreateScale(2.0f)*
                Matrix.CreateRotationY(MathHelper.Pi*(float)gameTime.TotalGameTime.TotalSeconds*0.5f)
                    *Matrix.CreateTranslation(3500,665,5900), View, Projection);

            spriteBatch.Begin();
            spriteBatch.DrawString(font, "X:"+weapon_desf.X + "  Y:" + weapon_desf.Y + "  Z:" + weapon_desf.Z +
                "  Angle="+weapon_angle ,new Vector2(10, 50), Color.YellowGreen);
            if (recording)
                spriteBatch.DrawString(font, "R", new Vector2(10, 10), Color.YellowGreen);

            MouseState state = Mouse.GetState();
            var p0 = GraphicsDevice.Viewport.Unproject(new Vector3(state.X, state.Y, 0), Projection, View, Matrix.Identity);
            var p1 = GraphicsDevice.Viewport.Unproject(new Vector3(state.X, state.Y, 1), Projection, View, Matrix.Identity);
            scene.intersectSegment(p0, p1, out ip_data ip);
            if (ip.nro_face != -1)
            {
                var face = scene.g_faces[ip.nro_face];
                if (face.nro_modelo >= 0)
                {
                    var modelo = scene.modelos[face.nro_modelo];
                    var mesh = scene.mesh_pool.meshes[modelo.nro_mesh];
                    /*spriteBatch.DrawString(font,  mesh.name+
                            "  dx=" +mesh.size.X + "dy= " + mesh.size.Y + " dz=" + mesh.size.Z
                        , new Vector2(10, 50), Color.YellowGreen);*/
                }
            }
            int framerate = (int)(1 / gameTime.ElapsedGameTime.TotalSeconds);
            spriteBatch.DrawString(font, "FPS:" + framerate, new Vector2(10, 10), Color.YellowGreen);
            if(!scene.usar_smd)
                spriteBatch.DrawString(font, "usando mesh MDL", new Vector2(10, 10), Color.YellowGreen);
            spriteBatch.DrawString(font, "(" + player.Position.X+ " , "+ player.Position.Y + " ," + player.Position.Z + ")", 
                    new Vector2(10, 100), Color.YellowGreen);
            spriteBatch.End();
 
            base.Draw(gameTime);

            if(playing)
            {
                screenShoot();
                ++curr_frame;
                if (curr_frame >= cant_frames)
                    playing = !playing;
            }
        }


        public Matrix CalcularMatrizOrientacion(float scale, Vector3 pos, Vector3 Dir)
        {
            var matWorld = Matrix.CreateScale(scale , Math.Abs(scale), Math.Abs(scale)) 
                    * Matrix.CreateRotationY(MathF.PI);
            Vector3 U = Vector3.Cross(new Vector3(0, 1, 0), Dir);
            U.Normalize();
            Vector3 V = Vector3.Cross(Dir,U);
            V.Normalize();

            Matrix Orientacion = new Matrix();
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M21 = V.X;
            Orientacion.M22 = V.Y;
            Orientacion.M23 = V.Z;
            Orientacion.M24 = 0;

            Orientacion.M31 = Dir.X;
            Orientacion.M32 = Dir.Y;
            Orientacion.M33 = Dir.Z;
            Orientacion.M34 = 0;

            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            matWorld = matWorld * Orientacion;

            // traslado
            matWorld = matWorld * Matrix.CreateTranslation(pos);
            return matWorld;
        }

        public void screenShoot()
        {
            int w = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int h = GraphicsDevice.PresentationParameters.BackBufferHeight;
            int[] backBuffer = new int[w * h];
            GraphicsDevice.GetBackBufferData(backBuffer);
            //copy into a texture 
            Texture2D texture = new Texture2D(GraphicsDevice, w, h, false, GraphicsDevice.PresentationParameters.BackBufferFormat);
            texture.SetData(backBuffer);
            //save to disk 
            Stream stream = File.OpenWrite("c:\\tmp_counter\\frame"+ curr_frame.ToString("000")+".jpg");
            texture.SaveAsJpeg(stream, w, h);
            stream.Dispose();
            texture.Dispose();
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


