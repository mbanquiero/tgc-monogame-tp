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
        public int mouse_x = MOUSE_NOT_INIT;
        public int mouse_y = MOUSE_NOT_INIT;
        public CBspFile scene;
        public bool colission = false;
        public bool on_ground = false;

        public CPlayer(CBspFile p_scene)
        {
            scene = p_scene;
        }

        public virtual void ProcessInput(float elapsed_time)
        {

            MouseState state = Mouse.GetState();
            if (state.LeftButton == ButtonState.Pressed && mouse_x!= MOUSE_NOT_INIT)
            {
                int dx = state.X - mouse_x;
                int dy = state.Y - mouse_y;
                float vel_mouse = elapsed_time * 0.25f;
                Direction = Vector3.TransformNormal(Direction, Matrix.CreateRotationY(dx * vel_mouse));
                Vector3 N = Vector3.Cross(new Vector3(0, 1, 0), Direction);
                Direction = Vector3.TransformNormal(Direction, Matrix.CreateFromAxisAngle(N, dy * vel_mouse));
                //                    Mouse.SetPosition(mouse_ox, mouse_oy);
            }

            mouse_x = state.X;
            mouse_y = state.Y;

            PrevPosition = Position;
            var keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Up)) Position+= Direction * 10;
            if (keyState.IsKeyDown(Keys.Down)) Position-= Direction * 10;
            if (keyState.IsKeyDown(Keys.LeftControl)) Position.Y += 10;
            if (keyState.IsKeyDown(Keys.LeftShift)) Position.Y -= 10;
        }

        public virtual void UpdatePhysics(float elapsed_time)
        {
            colission = false;
            on_ground = false;
            Vector3 dir = Position - PrevPosition;
            if (dir.LengthSquared() > 0)
            {
                dir.Normalize();
                float s = scene.intersectSegment(PrevPosition, PrevPosition + dir * 30);
                if (s < 1000)
                {
                    Position = PrevPosition;
                    colission = true;
                }
            }

            float t = scene.intersectSegment(Position, Position - new Vector3(0, 100, 0));
            if (t < 1000)
            {
                // toca piso
                Position.Y -= t * 100 - 50;
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
        public float speed = 70;
        public CEnemy(CBspFile p_scene) : base(p_scene)
        {
        }

        public override void ProcessInput(float elapsed_time)
        {

        }

        public override void UpdatePhysics(float elapsed_time)
        {
            PrevPosition = Position;
            Position += Direction * speed * elapsed_time;
            base.UpdatePhysics(elapsed_time);

            if(colission)
            {
                Direction = Vector3.TransformNormal(Direction, Matrix.CreateRotationY(0.1f));
            }
        }
    }
}
