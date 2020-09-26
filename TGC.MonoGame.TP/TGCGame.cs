using System;
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
        public bool ver_smd = true;
        public bool ver_mesh = false;
        public bool ver_modelo = false;
        public const string SkinnedMeshFolder = "C:\\monogames\\tp\\TGC.MonoGame.TP\\Content\\SkinnedModels\\";
        public const string ContentFolder = "C:\\Counter-Strike Source\\cstrike\\";
        public const string map_name = "cs_assault";
        //"de_mirage_csgo"
        //public String mesh_name = "props\\de_train\\utility_truck";
        public String mesh_name = "props_c17/fence02a";
        //public String mesh_name = "props/cs_assault/money";
        //public String mesh_name = "props_junk\\garbage_bag001a";
        //public String mesh_name = "props_borealis\\borealis_door001a";
        //public String mesh_name = "props_wasteland\\exterior_fence002d";
        public String weapon_name = "weapons\\w_rif_ak47";
        //w_snip_sg550
        public Vector3 weapon_desf = new Vector3(-65.7f, 139.8f, -12.1f);
        public float weapon_angle = -88;

        public float fieldOfView = MathHelper.PiOver4;
        public float aspectRatio = 1;
        public float nearClipPlane = 5;
        public float farClipPlane = 50000;
        Matrix Projection, View;

        public const int MAX_ENEMIGOS = 1;
        public CPlayer player;
        public CEnemy[] enemigo = new CEnemy[MAX_ENEMIGOS];


        public Effect EffectMesh;
        public CBspFile scene;
        public CMdlMesh mesh;
        public CMdlMesh rifle;

        // skinned mesh
        public SkinnedModel []CharacterMesh = new SkinnedModel[4];
        public SkinnedModelAnimation []Animations = new SkinnedModelAnimation[5];
        public SkinnedModelInstance []ModelInstance = new SkinnedModelInstance[MAX_ENEMIGOS];
        public Effect SkinnedModelEffect;

        public Model tgcLogo;


        // tool ver mesh
        public Vector3 LookAt = new Vector3(0, 0, 0), LookFrom = new Vector3(100, 0, 100);

        public SpriteFont font;
        public SpriteBatch spriteBatch;
        public bool[] keyDown = new bool[256];

        public bool fisica = false;

        // grabar gamelay
        public bool recording = false;
        public bool playing = false;
        public const int MAX_FRAMES = 60 * 60 * 5;
        public int cant_frames = 0;
        public int curr_frame = 0;
        public Vector3[] rLookAt = new Vector3[MAX_FRAMES];
        public Vector3[] rLookFrom = new Vector3[MAX_FRAMES];



        // experimento smd
        public Effect SMDEffect;
        public CSMDModel ak47;

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
            //Mouse.SetPosition(mouse_ox = GraphicsDevice.Viewport.Width / 2,mouse_oy = GraphicsDevice.Viewport.Height / 2);
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


            void LoadContentSMD()
        {
            font = Content.Load<SpriteFont>("SpriteFonts/Arial");
            spriteBatch = new SpriteBatch(GraphicsDevice);
            SMDEffect = Content.Load<Effect>("Effects/SMDEffect");

            ak47 = new CSMDModel("C:\\smd\\props\\cs_assault\\dryer_box.qc", GraphicsDevice, Content, "");
            ak47.debugEffect = Content.Load<Effect>("Effects/BasicShader");
            ak47.anim1 = ak47.cargar_ani("C:\\smd\\props\\cs_assault\\dryer_box_anims\\idle.smd");

        
            ak47.setAnimation(ak47.anim1);
            LookAt = ak47.cg;
            LookFrom = LookAt + new Vector3(1, 0, 1) * ak47.size.Length() * 1.01f;
            base.LoadContent();
        }

        void UpdateSMD(GameTime gameTime)
        {
            var keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Escape))
                //Salgo del juego.
                Exit();
            if (keyState.IsKeyDown(Keys.Up)) LookFrom = Vector3.Transform(LookFrom, Matrix.CreateRotationX(-0.05f));
            if (keyState.IsKeyDown(Keys.Down)) LookFrom = Vector3.Transform(LookFrom, Matrix.CreateRotationX(0.05f));
            if (keyState.IsKeyDown(Keys.Left)) LookFrom = Vector3.Transform(LookFrom, Matrix.CreateRotationY(-0.05f));
            if (keyState.IsKeyDown(Keys.Right)) LookFrom = Vector3.Transform(LookFrom, Matrix.CreateRotationY(0.05f));
            View = Matrix.CreateLookAt(LookFrom, LookAt, new Vector3(0, 1, 0));

            ak47.updatesBones(ak47.cur_frame + 1);
            

            base.Update(gameTime);
        }

        void DrawSMD(GameTime gameTime)
        {

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.Clear(Color.Black);

            ak47.Draw(GraphicsDevice , SMDEffect, Matrix.Identity , View , Projection);

            spriteBatch.Begin();
            spriteBatch.DrawString(font, "frame:"+ ak47.cur_frame , new Vector2(10, 10), Color.YellowGreen);
            spriteBatch.End();
            base.Draw(gameTime);
        }




        void LoadContentGame()
        {
            font = Content.Load<SpriteFont>("SpriteFonts/Arial");
            spriteBatch = new SpriteBatch(GraphicsDevice);
            EffectMesh = Content.Load<Effect>("Effects/BasicShader");

            String[] st_characters = {
                        "swat",
                        "zombiegirl_w_kurniawan" ,
                        "Zombie 1" ,
                        "Zombie 2" ,
                        "Zombie 3" };

            for (int i = 0; i < 4; ++i)
            {
                CharacterMesh[i] = new SkinnedModel(!ver_modelo);
                CharacterMesh[i].GraphicsDevice = GraphicsDevice;
                CharacterMesh[i].FilePath = SkinnedMeshFolder + "FBX 2013\\" + st_characters[i] + ".fbx";
                CharacterMesh[i].TexturePath = SkinnedMeshFolder + st_characters[i] + ".fbm\\";
                CharacterMesh[i].Initialize();
            }

            String[] st_animations = {
                        "Idle" ,
                        "Female Tough Walk" ,
                        "Zombie Running" ,
                        "Firing Rifle",
                        "Zombie Walk"};

            for (int i = 0; i < 4; ++i)
            {
                Animations[i] = new SkinnedModelAnimation();
                Animations[i].FilePath = SkinnedMeshFolder + st_animations[i] + ".dae";
                Animations[i].Load();
            }

            Random rnd = new Random();
            for (int i = 0; i < MAX_ENEMIGOS; ++i)
            {
                ModelInstance[i] = new SkinnedModelInstance();
                ModelInstance[i].Mesh = CharacterMesh[0];           // rnd.Next(0,3)
                ModelInstance[i].SpeedTransitionSecond = 0.4f;
                ModelInstance[i].Initialize();
                ModelInstance[i].SetAnimation(Animations[rnd.Next(0, 0)]);
                ModelInstance[i].Time = (float)rnd.Next(200) / 100.0f;
            }
            //CharacterTexture = CTextureLoader.Load(GraphicsDevice, SkinnedMeshFolder+"zombie_diffuse.png");
            //CharacterTexture = CTextureLoader.Load(GraphicsDevice, SkinnedMeshFolder + "Zombie 2.fbm\\Yakuzombie_diffuse.png"); 

            //
            SkinnedModelEffect = Content.Load<Effect>("Effects/SkinnedModelEffect");

            // armas
            rifle = new CMdlMesh(weapon_name, GraphicsDevice, Content, "C:\\Counter-Strike Source\\cstrike\\");

            // huevo de pascuas
            tgcLogo = Content.Load<Model>("Models/tgc-logo/tgc-logo");
            var modelEffect = (BasicEffect)tgcLogo.Meshes[0].Effects[0];
            modelEffect.DiffuseColor = Color.DarkBlue.ToVector3();
            modelEffect.EnableDefaultLighting();


            if (ver_modelo)
            {
                LookAt = new Vector3(0, 200, 0);
                LookFrom = new Vector3(900, 550, 0);
            }
            else
            if(ver_mesh)
            {
                mesh = new CMdlMesh(mesh_name, GraphicsDevice, Content, "C:\\Counter-Strike Source\\cstrike\\");
                LookAt = mesh.cg;
                LookFrom = LookAt + new Vector3(1, 0, 1) * mesh.size.Length() * 1.1f;
            }
            else
            {
                scene = new CBspFile(map_name, GraphicsDevice, Content);
                player = new CPlayer(scene);

                for (int i = 0; i < MAX_ENEMIGOS; ++i)
                {
                    enemigo[i] = new CEnemy(scene);
                    enemigo[i].Position = new Vector3(rnd.Next((int)scene.p_min.X, (int)scene.p_max.X), 
                                    scene.p_max.Y, rnd.Next((int)scene.p_min.Z, (int)scene.p_max.Z));
                    enemigo[i].Position = new Vector3(7242+ rnd.Next(-300,300), -493, 6746+ rnd.Next(-300, 300));

                    float an = rnd.Next(0, 360)*MathF.PI/180.0f;
                    enemigo[i].Direction = new Vector3(MathF.Cos(an), 0, MathF.Sin(an));


                    //enemigo[i].Position = new Vector3(7242-i*20, -493, 6746);
                    //enemigo[i].Direction = new Vector3(0, 0, -1);

                }

                player.Position = scene.cg;
                player.Direction = new Vector3(0,0,1);

                enemigo[0].Position = new Vector3(7242, -493, 6746);
                enemigo[0].Direction = new Vector3(0, 0, -1);

            }

            base.LoadContent();
        }
        public void UpdateGame(GameTime gameTime)
        {
            var keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Escape))
                //Salgo del juego.
                Exit();


            if(ver_modelo)
            {
                ModelInstance[0].Transformation = Matrix.CreateScale(-3.0f, 3.0f, 3.0f);
                //* Matrix.CreateRotationY(2.5f * (float)gameTime.TotalGameTime.TotalSeconds);
                
            }

            for (int i=0;i<MAX_ENEMIGOS;++i)
            {
                ModelInstance[i].Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            if (ver_mesh || ver_modelo)
            {
                // tool ver mesh
                // Press Directional Keys to rotate cube
                if (keyState.IsKeyDown(Keys.Up)) LookFrom = Vector3.Transform(LookFrom, Matrix.CreateRotationX(-0.05f));
                if (keyState.IsKeyDown(Keys.Down)) LookFrom = Vector3.Transform(LookFrom, Matrix.CreateRotationX(0.05f));
                if (keyState.IsKeyDown(Keys.Left)) LookFrom = Vector3.Transform(LookFrom, Matrix.CreateRotationY(-0.05f));
                if (keyState.IsKeyDown(Keys.Right)) LookFrom = Vector3.Transform(LookFrom, Matrix.CreateRotationY(0.05f));
                View = Matrix.CreateLookAt(LookFrom, LookAt, new Vector3(0, 1, 0));

            }
            else
            {
                float elapsed_time = (float)gameTime.ElapsedGameTime.TotalSeconds;
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



                // animo al jugador
                /*
                if ((posPlayer-posAnt).LengthSquared()>0)
                {
                    model.setCurrentAnimation(5);
                }
                else
                {
                    model.setCurrentAnimation(6);
                }
                */

                if (playing)
                {
                    /*LookAt = rLookAt[curr_frame % cant_frames % MAX_FRAMES];
                    LookFrom = rLookFrom[curr_frame % cant_frames % MAX_FRAMES];
                    View = Matrix.CreateLookAt(LookFrom, LookAt, new Vector3(0, 1, 0));
                    */
                }
                else
                {
                    // camara primera persona
                    Vector3 desf = new Vector3(0, 20, 0);
                    View = Matrix.CreateLookAt(player.Position+ desf, player.Position + desf + player.Direction, new Vector3(0, 1, 0));
                    if (recording)
                    {
                        rLookFrom[cant_frames % MAX_FRAMES] = player.Position;
                        rLookAt[cant_frames % MAX_FRAMES] = player.Position + player.Direction;
                        ++cant_frames;
                    }

                    // primera persona
                    var camPos = player.Position + player.Direction * 0 + desf;
                    //View = Matrix.CreateLookAt(camPos, camPos + player.Direction * 1000 , new Vector3(0, 1, 0));
                    View = Matrix.CreateLookAt(player.Position - player.Direction * 50 + desf, player.Position + player.Direction * 10 + desf, new Vector3(0, 1, 0));

                    // 3era persona
                    //View = Matrix.CreateLookAt(player.Position - player.Direction * 250 + desf, player.Position + player.Direction * 10 + desf, new Vector3(0, 1, 0));
                }
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

            /*
			RasterizerState rasterizerState = new RasterizerState();
			rasterizerState.FillMode = FillMode.WireFrame;
			GraphicsDevice.RasterizerState = rasterizerState;
			*/

            if (ver_modelo)
            {
                // modelo animado
                //SkinnedModelEffect.Parameters["ModelTexture"].SetValue(CharacterTexture);
                ModelInstance[0].Draw(GraphicsDevice, SkinnedModelEffect, View, Projection);
            }
            else
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

                /*
                // modelo animado
                for (int i = 0; i < MAX_ENEMIGOS; ++i)
                {
                    ModelInstance[i].Transformation = CalcularMatrizOrientacion(0.5f, enemigo[i].Position - new Vector3(0, 50, 0), -enemigo[i].Direction);
                    ModelInstance[i].Draw(GraphicsDevice, SkinnedModelEffect, View, Projection);
                }*/

                /*
                // dibujo al jugador 
                ModelInstance[0].Transformation = CalcularMatrizOrientacion(-0.45f, player.Position - new Vector3(0, 50, 0), -player.Direction);
                ModelInstance[0].Draw(GraphicsDevice, SkinnedModelEffect, View, Projection);
                // y el arma
                EffectMesh.CurrentTechnique = EffectMesh.Techniques["TextureDrawing"];
                // Matrix.CreateRotationY(weapon_angle*MathF.PI/180.0f)* 
                var world = Matrix.CreateRotationY(weapon_angle * MathF.PI / 180.0f) * Matrix.CreateRotationX(MathF.PI / 2) *
                        Matrix.CreateScale(-1 , 1, 1 ) * Matrix.CreateTranslation(weapon_desf)*
                    ModelInstance[0].MeshInstances[0].BonesOffsets[27] * 
                    ModelInstance[0].Transformation;
                rifle.Draw(GraphicsDevice, EffectMesh,Matrix.CreateScale(3)*world, View, Projection);
                */
                tgcLogo.Draw(Matrix.CreateScale(2.0f)*
                    Matrix.CreateRotationY(MathHelper.Pi*(float)gameTime.TotalGameTime.TotalSeconds*0.5f)
                        *Matrix.CreateTranslation(3500,665,5900), View, Projection);

            }

            //float t = scene.intersectSegment(posPlayer, posPlayer - new Vector3(0, 1000, 0))*1000;
            spriteBatch.Begin();
            //spriteBatch.DrawString(font, "Subset:"+scene.current_subset+
            //"  " + scene.subset[scene.current_subset].image_name, new Vector2(10, 10), Color.YellowGreen);
            //spriteBatch.DrawString(font, "X:"+weapon_desf.X + "  Y:" + weapon_desf.Y + "  Z:" + weapon_desf.Z +
            //    "  Angle="+weapon_angle ,new Vector2(10, 10), Color.YellowGreen);
            if (recording)
                spriteBatch.DrawString(font, "R", new Vector2(10, 10), Color.YellowGreen);

            if (!ver_mesh && !ver_modelo)
            {
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
                        spriteBatch.DrawString(font,  mesh.name, new Vector2(10, 50), Color.YellowGreen);
                    }
                }
                int framerate = (int)(1 / gameTime.ElapsedGameTime.TotalSeconds);
                spriteBatch.DrawString(font, "FPS:" + framerate, new Vector2(10, 10), Color.YellowGreen);
                spriteBatch.DrawString(font, "(" + player.Position.X+ " , "+ player.Position.Y + " ," + player.Position.Z + ")", 
                        new Vector2(10, 100), Color.YellowGreen);
            }

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