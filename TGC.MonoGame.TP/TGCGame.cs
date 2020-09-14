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
        public bool ver_modelo = false;
        public const string SkinnedMeshFolder = "C:\\monogames\\tp\\TGC.MonoGame.TP\\Content\\SkinnedModels\\";
        public const string ContentFolder = "C:\\Counter-Strike Source\\cstrike\\";
        public const string map_name = "cs_assault";
        //"de_mirage_csgo"
        //public String mesh_name = "props\\de_train\\utility_truck";
        //public String mesh_name = "props/cs_assault/MoneyPallet_WasherDryer";
        //public String mesh_name = "props/cs_assault/money";
        //public String mesh_name = "props_junk\\garbage_bag001a";
        //public String mesh_name = "props_borealis\\borealis_door001a";
        public String mesh_name = "combine_soldier";
        public float fieldOfView = MathHelper.PiOver4;
        public float aspectRatio = 1;
        public float nearClipPlane = 5;
        public float farClipPlane = 50000;
        Matrix Projection, View;

        public const int MAX_ENEMIGOS = 20;
        public CPlayer player;
        public CEnemy[] enemigo = new CEnemy[MAX_ENEMIGOS];

        public Effect EffectMesh;
        public CBspFile scene;
        public CMdlMesh mesh;

        // skinned mesh
        public SkinnedModel CharacterMesh;
        public SkinnedModelAnimation AnimationIdle;
        public SkinnedModelInstance []ModelInstance = new SkinnedModelInstance[MAX_ENEMIGOS];
        public Texture2D CharacterTexture;
        public Effect SkinnedModelEffect;

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
            font = Content.Load<SpriteFont>("SpriteFonts/Arial");
            spriteBatch = new SpriteBatch(GraphicsDevice);
            EffectMesh = Content.Load<Effect>("Effects/BasicShader");

            CharacterMesh = new SkinnedModel();
            CharacterMesh.GraphicsDevice = GraphicsDevice;
            CharacterMesh.FilePath = SkinnedMeshFolder + "FBX 2013\\zombiegirl_w_kurniawan.fbx";
            CharacterMesh.Initialize();
            AnimationIdle = new SkinnedModelAnimation();
            AnimationIdle.FilePath = SkinnedMeshFolder + "Female Tough Walk.dae";
            AnimationIdle.Load();

            Random rnd = new Random();
            for (int i = 0; i < MAX_ENEMIGOS; ++i)
            {
                ModelInstance[i] = new SkinnedModelInstance();
                ModelInstance[i].Mesh = CharacterMesh;
                ModelInstance[i].SpeedTransitionSecond = 0.4f;
                ModelInstance[i].Initialize();
                ModelInstance[i].SetAnimation(AnimationIdle);
            }
            CharacterTexture = CTextureLoader.Load(GraphicsDevice, SkinnedMeshFolder+"zombie_diffuse.png");
            SkinnedModelEffect = Content.Load<Effect>("Effects/SkinnedModelEffect");

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
                }

                player.Position = scene.cg;
                player.Direction = new Vector3(0,0,1);

                enemigo[0].Position = new Vector3(7242, -493, 6746);
                enemigo[0].Direction = new Vector3(0, 0, -1);

            }

            base.LoadContent();
        }
        protected override void Update(GameTime gameTime)
        {
            var keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Escape))
                //Salgo del juego.
                Exit();


            if(ver_modelo)
            {
                ModelInstance[0].Transformation = Matrix.CreateScale(3.0f) * Matrix.CreateRotationY(2.5f * (float)gameTime.TotalGameTime.TotalSeconds);
            }

            for(int i=0;i<MAX_ENEMIGOS;++i)
            {
                ModelInstance[i].UpdateBoneAnimations(gameTime);
                ModelInstance[i].UpdateBones(gameTime);
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

                player.ProcessInput(elapsed_time);
                if (fisica)
                {
                    player.UpdatePhysics(elapsed_time);
                }

                for (int i = 0; i < MAX_ENEMIGOS; ++i)
                    enemigo[i].UpdatePhysics(elapsed_time);

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


                // camara primera persona
                View = Matrix.CreateLookAt(player.Position, player.Position + player.Direction, new Vector3(0, 1, 0));

                 //View = Matrix.CreateLookAt(posPlayer-viewDir*400+new Vector3(0,100,0), posPlayer , new Vector3(0, 1, 0));
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

            if (ver_modelo)
            {
                // modelo animado
                SkinnedModelEffect.Parameters["ModelTexture"].SetValue(CharacterTexture);
                DrawSkinnedModel(ModelInstance[0], gameTime);
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

                // modelo animado
                for (int i = 0; i < MAX_ENEMIGOS; ++i)
                {
                    ModelInstance[i].Transformation = CalcularMatrizOrientacion(0.5f, enemigo[i].Position - new Vector3(0, 50, 0), -enemigo[i].Direction);
                    SkinnedModelEffect.Parameters["ModelTexture"].SetValue(CharacterTexture);
                    DrawSkinnedModel(ModelInstance[i], gameTime);
                }
            }

            //float t = scene.intersectSegment(posPlayer, posPlayer - new Vector3(0, 1000, 0))*1000;
            spriteBatch.Begin();
            //spriteBatch.DrawString(font, "Subset:"+scene.current_subset+
            //"  " + scene.subset[scene.current_subset].image_name, new Vector2(10, 10), Color.YellowGreen);

            //spriteBatch.DrawString(font, "tras" + dist, new Vector2(10, 10), Color.YellowGreen);
            
            spriteBatch.End();
 
            base.Draw(gameTime);
        }

        void DrawSkinnedModel(SkinnedModelInstance skinnedModelInstance, GameTime gameTime)
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            SkinnedModelEffect.CurrentTechnique = SkinnedModelEffect.Techniques["BasicColorDrawing"];

            SkinnedModelEffect.Parameters["World"].SetValue(skinnedModelInstance.Transformation);
            SkinnedModelEffect.Parameters["View"].SetValue(View);
            SkinnedModelEffect.Parameters["Projection"].SetValue(Projection);

            foreach (var meshInstance in skinnedModelInstance.MeshInstances)
            {
                SkinnedModelEffect.Parameters["gBonesOffsets"].SetValue(meshInstance.BonesOffsets);
                //SkinnedModelEffect.Parameters["ModelTexture"].SetValue(meshInstance.Mesh.Texture);

                GraphicsDevice.SetVertexBuffer(meshInstance.Mesh.VertexBuffer);
                GraphicsDevice.Indices = meshInstance.Mesh.IndexBuffer;

                foreach (EffectPass pass in SkinnedModelEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, meshInstance.Mesh.FaceCount);
                }

            }
        }

        public Matrix CalcularMatrizOrientacion(float scale, Vector3 pos, Vector3 Dir)
        {
            var matWorld = Matrix.CreateScale(scale) * Matrix.CreateRotationY(MathF.PI);
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