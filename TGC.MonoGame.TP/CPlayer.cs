using System;
using Assimp.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP
{
    public struct hit_point
    {
        public Vector3 Position;
        public float dw;
        public int bone_id;
    };

    public class CPlayer
    {
        public const int MOUSE_NOT_INIT = 100000;
        public Vector3 Position = new Vector3(0,0,0);
        public Vector3 Direction = new Vector3(0,0,1);
        public Vector3 PrevPosition;
        public float dist = 0;      // dist, recorrida
        public int mouse_x = MOUSE_NOT_INIT;
        public int mouse_y = MOUSE_NOT_INIT;
        public CBspFile scene;
        public bool colission = false;
        public bool on_ground = false;
        public TGCGame game;
        public float Height;
        public float vel_mouse = 0.25f;
        public float vel_lineal = 350f;

        public CPlayer(CBspFile p_scene , TGCGame p_game)
        {
            scene = p_scene;
            game = p_game;
            Height = p_game.soldier_height;
        }
        public virtual void Update(float elapsed_time)
        {
            ProcessInput(elapsed_time);
            if(game.fisica)
                UpdatePhysics(elapsed_time);
        }

        public virtual void ProcessInput(float elapsed_time)
        {

            if(mouse_x==MOUSE_NOT_INIT)
            {
                mouse_x = game.GraphicsDevice.Viewport.Width / 2;
                mouse_y = game.GraphicsDevice.Viewport.Height / 2;
                Mouse.SetPosition(mouse_x, mouse_y);
            }

            MouseState state = Mouse.GetState();
            //if (state.LeftButton == ButtonState.Pressed)
            {
                int dx = state.X - mouse_x;
                int dy = state.Y - mouse_y;
                Direction = Vector3.TransformNormal(Direction, Matrix.CreateRotationY(dx * vel_mouse* elapsed_time));
                Vector3 N = Vector3.Cross(new Vector3(0, 1, 0), Direction);
                Direction = Vector3.TransformNormal(Direction, Matrix.CreateFromAxisAngle(N, dy * vel_mouse * elapsed_time));
                Mouse.SetPosition(mouse_x, mouse_y);
            }

            //mouse_x = state.X;
            //mouse_y = state.Y;



            PrevPosition = Position;

            var keyState = Keyboard.GetState();
            if (!keyState.IsKeyDown(Keys.LeftShift))
            {

                if (keyState.IsKeyDown(Keys.Up)) Position += Direction * vel_lineal*elapsed_time;
                if (keyState.IsKeyDown(Keys.Down)) Position -= Direction * vel_lineal * elapsed_time;
                if (keyState.IsKeyDown(Keys.LeftControl)) Position.Y += vel_lineal * elapsed_time;
                if (keyState.IsKeyDown(Keys.LeftShift)) Position.Y -= vel_lineal * elapsed_time;

            }

            dist += (Position - PrevPosition).Length();

        }


        
        public virtual void UpdatePhysics(float elapsed_time)
        {
            colission = false;
            on_ground = false;
            Vector3 dir = Position - PrevPosition;
            if (dir.LengthSquared() > 0)
            {
                dir.Normalize();
                float s = scene.intersectSegment(PrevPosition, PrevPosition + dir * 50,out ip_data ip0);
                if (s < 1000)
                {
                    Position = PrevPosition;
                    colission = true;
                }
            }

            /*
            // performacne
            float xxx = 0;
            for (var i = 0; i < 2000; ++i)
            {
                xxx += scene.intersectSegment(Position, Position - new Vector3(0, 100, 0));
            }
            */
            float t = scene.intersectSegment(Position, Position - new Vector3(0, 100, 0), out ip_data ip1);
            if (t < 1000)
            {
                // toca piso
                Position.Y -= t * 100 - Height/2;
                on_ground = true;
            }
            else
            {
                // esta en el aire
                Position.Y -= elapsed_time * 250;
            }
        }

        

        public Matrix CalcularMatrizOrientacion(Vector3 pos, Vector3 Dir)
        {
            var matWorld = Matrix.CreateRotationY(MathF.PI);
            Vector3 U = Vector3.Cross(new Vector3(0, 1, 0), Dir);
            U.Normalize();
            Vector3 V = Vector3.Cross(Dir, U);
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


    }


    public class CEnemy : CPlayer
    {
        public CSMDModel model = null;
        public float currentTime = 0;
        public int currentAnimation = 0;
        public float timerIA = 0;
        public bool muerto = false;
        public bool chasing = false;
        // hitpoints
        public int cant_hp = 4;
        public hit_point[] hit_points = new hit_point[5];

        public CEnemy(CBspFile p_scene , TGCGame p_game, CSMDModel p_model) : base(p_scene , p_game)
        {
            model = p_model;
            vel_lineal = model.speed;

            //  0 "ValveBiped.Bip01_Pelvis" -1
            // 13 "ValveBiped.Bip01_Neck1" 12
            // 14 "ValveBiped.Bip01_Head1" 13
            // 45 "ValveBiped.weapon_bone" 9
            int[] ndx_hp = { 0, 13, 14, 45 };
            float[] hp_dw = { 10, 5, 5, 5 };
            for (int i = 0; i < cant_hp; ++i)
            {
                hit_points[i] = new hit_point();
                hit_points[i].Position = new Vector3();
                hit_points[i].bone_id = ndx_hp[i];
                hit_points[i].dw = hp_dw[i];
            }
        }


        public override void Update(float elapsed_time)
        {
            UpdateAnimation(elapsed_time);
            if (muerto)
            {
                return;
            }
            UpdateIA(elapsed_time);
            ProcessInput(elapsed_time);
            UpdatePhysics(elapsed_time);
        }

        public void UpdateIA(float elapsed_time)
        {
            return;

            if (timerIA > 0)
            {
                timerIA -= elapsed_time;
                return;
            }

            if(chasing)
            {
                if (Vector3.Cross(Position - game.player.Position, Direction).Y > 0)
                    Direction = Vector3.TransformNormal(Direction, Matrix.CreateRotationY(0.01f));
                else
                    Direction = Vector3.TransformNormal(Direction, Matrix.CreateRotationY(-0.01f));
            }
            else
            if ((Position - game.player.Position).Length() < 1500)
                chasing = true;
        }

        public override void ProcessInput(float elapsed_time) 
        {
        }

        public virtual void UpdateAnimation(float elapsed_time)
        {
            currentTime += elapsed_time;
        }

        public override void UpdatePhysics(float elapsed_time)
        {

            PrevPosition = Position;
            if(!game.pause)
                Position += Direction * vel_lineal * elapsed_time;

            base.UpdatePhysics(elapsed_time);
            
            if (colission)
            {
                Direction = Vector3.TransformNormal(Direction, Matrix.CreateRotationY(0.1f));
                timerIA = 1;
            }
        }

        public void computeHitpoints()
        {
            // como uso el mismo modelo para todos los enemigos tengo que actualizar el esqueleto 
            model.currentTime = currentTime;
            model.setAnimation(currentAnimation);
            Matrix World = Matrix.CreateRotationY(MathF.PI / 2.0f) * CalcularMatrizOrientacion(Position - new Vector3(0, game.soldier_height / 2, 0), Direction);
            for (int i=0;i<cant_hp;++i)
            {
                hit_points[i].Position = Vector3.Transform(model.bones[hit_points[i].bone_id].Position, model.invMetric*World);
            }
        }

        public void drawHitPoints(GraphicsDevice graphicsDevice, CDebugBox debug_box,Effect Effect, Matrix View,Matrix Proj)
        {
            for (int j = 0; j < cant_hp; ++j)
            {
                var hpt = hit_points[j];
                Vector3 s = new Vector3(1, 1, 1) * hpt.dw*0.5f;
                var p0 = hpt.Position;
                debug_box.Draw(graphicsDevice, p0 - s, p0 + s, Effect, Matrix.Identity, View, Proj);
            }

        }


        public void Draw(GraphicsDevice graphicsDevice, Effect Effect, Matrix View, Matrix Proj, int L = 0)
        {
            if (model == null)
                return;
            // como uso el mismo modelo para todos los enemigos tengo que actualizar el esqueleto antes de dibujar
            // de paso computo los hitpoints
            computeHitpoints();
            // ahora si dibujo            
            Matrix World = Matrix.CreateRotationY(MathF.PI / 2.0f) * CalcularMatrizOrientacion(Position - new Vector3(0, game.soldier_height / 2, 0), Direction);
            model.Draw(graphicsDevice, Effect, World, View, Proj, L);

        }

        // devuelve true si el rayo colisiona con el hitpoint
        public bool colision(Vector3 p0, Vector3 Dir)
        {
            bool rta = false;
            for(int i=0;i<cant_hp && !rta;++i)
            {
                rta = intersectRaySphere(p0, Dir, hit_points[i].Position, hit_points[i].dw);
            }
            return rta;
        }

        public bool intersectRaySphere(Vector3 p, Vector3 d, Vector3 c, float r)
        {
            // p = posicion rayo
            // d = direccion rayo
            // c = centro de la esfera
            // r = radio
            float l = (p - c).Length();
            Vector3 hp = p + d * l;
            return (hp - c).LengthSquared() < r * r;
        }


    }
}
