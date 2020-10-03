using System;
using Assimp.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP
{
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
        public float vel_mouse = 0.2f;
        public float vel_lineal = 200f;

        public CPlayer(CBspFile p_scene , TGCGame p_game)
        {
            scene = p_scene;
            game = p_game;
            Height = p_game.soldier_height;
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
    }


    public class CEnemy : CPlayer
    {
        public CEnemy(CBspFile p_scene , TGCGame p_game) : base(p_scene , p_game)
        {
        }

        public override void ProcessInput(float elapsed_time) 
        {
        }

        public override void UpdatePhysics(float elapsed_time)
        {
            PrevPosition = Position;
            Position += Direction * vel_lineal * elapsed_time;
            base.UpdatePhysics(elapsed_time);

            if(colission)
            {
                Direction = Vector3.TransformNormal(Direction, Matrix.CreateRotationY(0.1f));
            }
        }
    }
}
