using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP
{

    public class weapon_reference
    {

        public CSMDModel model;
        // desfasajes para ubicar en el soldado
        public Vector3 desf = new Vector3(-4, -1, -14);
        public Vector3 scale = new Vector3(1, 1, 1);
        public float pitch = 80;
        public float yaw = 0;
        public int cant_tiros = 20;
        public int cant_tiros_max = 20;

        public weapon_reference(string weapon_name, GraphicsDevice p_device, ContentManager p_content, string p_folder,
            string ani_idle, string ani_reload , string ani_shoot , int mazzle_attach=-1)
        {
            model = new CSMDModel(weapon_name, p_device, p_content, p_folder);
            String ani_folder = weapon_name + "_anims";
            model.cargar_ani(ani_folder, ani_idle);
            model.cargar_ani(ani_folder, ani_reload);
            model.cargar_ani(ani_folder, ani_shoot);
            model.w_attachment = mazzle_attach;
            model.anim[1].loop = false;
            model.setAnimation(0);
        }



    };

    public class CWeapon
    {
        public const int W_IDLE = 0;
        public const int W_FIRING = 1;
        public const int W_RELOADING = 2;

        public GraphicsDevice device;
        public ContentManager Content;
        public String cs_folder;
        public TGCGame game;

        public int cur_weapon = 0;
        public int cant_weapons;
        public weapon_reference[] weapons = new weapon_reference[32];


        // estado actual del arma
        public int status = 0;      // 0-> idle, 1->firing, 2->reloading
        public float timer_tiro = 0.1f;

        public CWeapon(TGCGame p_game, GraphicsDevice p_device, ContentManager p_content, string p_folder)
        {
            game = p_game;
            device = p_device;
            Content = p_content;
            cs_folder = p_folder;

            // Arma
            String[] W = {
                "v_rif_ak47" , "ak47_idle","ak47_reload","ak47_fire1","63",
                "v_rif_galil" , "idle","reload","shoot1" , "38",
                "v_357" , "idle01","reload","fire" , "22" ,
                "v_pist_elite","idle","reload","shoot_right1","48",
                "v_rif_famas","idle","reload","shoot1","40",
                "v_rpg","idle1","reload","fire","1",
                "v_shotgun","idle01","reload1","fire","-1",
                "v_stunbaton","idle01","draw","attackcm","-1",
                "v_superphyscannon","idle","chargeup","fire","12" };



            int t = 0;
            while(t<W.Length)
            {
                String weapon_name = "weapons\\"+W[t++];
                weapons[cant_weapons++] = new weapon_reference(weapon_name, device, Content, cs_folder,
                        W[t++], W[t++], W[t++], int.Parse(W[t++]));
            }

            // sobrecargo la ak47 
            weapons[0].pitch = 76;
            weapons[0].yaw = 14;
            weapons[0].desf = new Vector3(-5, -6, -15);
            weapons[0].scale = new Vector3(1, 1, -1);


        }



        public bool firing()
        {
            return status == W_FIRING ? true : false;
        }

        public Matrix getTransform()
        {
            var W = weapons[cur_weapon];
            return 
                Matrix.CreateScale(W.scale)*
                Matrix.CreateRotationY(W.pitch * MathF.PI / 180.0f) *
                Matrix.CreateRotationX(W.yaw * MathF.PI / 180.0f) *
                Matrix.CreateTranslation(W.desf + new Vector3(0, 0, MathF.Sin(game.player.dist * 0.07f))) *
                TGCGame.CalcularMatrizOrientacion(1.0f, game.camPosition, game.player.Direction);

        }
        public void Update(float elapsed_time)
        {
            var W = weapons[cur_weapon];
            var m = W.model;
            float k = m.cur_anim == 2 ? 10 : 2;
            m.update(elapsed_time * k);

            float s = 0.05f;

            var keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.LeftShift))
            {
                if (keyState.IsKeyDown(Keys.Up)) W.desf.Z += s;
                if (keyState.IsKeyDown(Keys.Down)) W.desf.Z -= s;
                if (keyState.IsKeyDown(Keys.Left)) W.desf.X += s;
                if (keyState.IsKeyDown(Keys.Right)) W.desf.X -= s;
                if (keyState.IsKeyDown(Keys.W)) W.yaw += 0.1f;
                if (keyState.IsKeyDown(Keys.S)) W.yaw -= 0.1f;

            }
            else
            {
                if (keyState.IsKeyDown(Keys.W)) W.pitch += 0.1f;
                if (keyState.IsKeyDown(Keys.S)) W.pitch -= 0.1f;

            }
            if (keyState.IsKeyDown(Keys.Q)) W.desf.Y += s;
            if (keyState.IsKeyDown(Keys.A)) W.desf.Y -= s;


            if (keyState.IsKeyDown(Keys.PageDown))
            {
                if (!game.keyDown[(int)Keys.PageDown])
                {
                    cur_weapon = (cur_weapon + 1) % cant_weapons;
                    status = W_RELOADING;
                    m.currentTime = 0;
                    m.setAnimation(1);
                }
                game.keyDown[(int)Keys.PageDown] = true;
            }
            else
                game.keyDown[(int)Keys.PageDown] = false;

            if (keyState.IsKeyDown(Keys.PageUp))
            {
                if (!game.keyDown[(int)Keys.PageUp])
                {
                    cur_weapon = (cur_weapon - 1) % cant_weapons;
                    status = W_RELOADING;
                    m.currentTime = 0;
                    m.setAnimation(1);
                }
                game.keyDown[(int)Keys.PageUp] = true;
            }
            else
                game.keyDown[(int)Keys.PageUp] = false;

            MouseState state = Mouse.GetState();

            var sp = game.scene.sprites[game.scene.spt_muzzle];
            if (status==W_RELOADING)
            {
                // pongo el sprite detras de camara para que no se vea
                sp.origin = game.camPosition - game.player.Direction * 1000;
                if (m.anim[m.cur_anim].finished)
                {
                    status = W_IDLE;
                    m.setAnimation(0);
                    W.cant_tiros = W.cant_tiros_max;
                }
            }
            else
            if (state.LeftButton == ButtonState.Pressed)
            {
                status = W_FIRING;
                m.setAnimation(2);
                timer_tiro -= elapsed_time;
                if (timer_tiro < 0)
                {
                    timer_tiro = 0.1f;
                    W.cant_tiros--;
                    if (W.cant_tiros == 0)
                    {
                        status = W_RELOADING;
                        m.currentTime = 0;
                        m.setAnimation(1);
                    }
                }

                var World= getTransform();

                if(m.w_attachment>0)
                {
                    var p0 = Vector3.Transform(m.bones[m.w_attachment].Position, m.invMetric);
                    sp.origin = Vector3.Transform(p0, World);
                    sp.renderamt = MathF.Abs(MathF.Cos(m.currentTime * 50.0f));
                }
                else
                {
                    sp.origin = game.camPosition - game.player.Direction * 1000;
                }
            }
            else
            {
                status = W_IDLE;
                m.setAnimation(0);
                // pongo el sprite detras de camara para que no se vea
                sp.origin = game.camPosition - game.player.Direction * 1000;
            }

        }

        public void Draw(Effect effect,Matrix View, Matrix Proj)
        {
            var W = weapons[cur_weapon];
            var m = W.model;
            var world = getTransform();
            m.Draw(device, effect, world, View, Proj);
        }

    }
}