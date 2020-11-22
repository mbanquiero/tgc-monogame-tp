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
        public Vector3 muzzle_pos = new Vector3(0, 0, 0);
        public float pitch = 80;
        public float yaw = 0;
        public int cant_tiros = 20;
        public int cant_tiros_max = 20;

        public weapon_reference(string weapon_name, GraphicsDevice p_device, ContentManager p_content, string p_folder,
            string ani_idle, string ani_reload , string ani_shoot)
        {
            model = new CSMDModel(weapon_name, p_device, p_content, p_folder);
            String ani_folder = weapon_name + "_anims";
            model.cargar_ani(ani_folder, ani_idle);
            model.cargar_ani(ani_folder, ani_reload);
            model.cargar_ani(ani_folder, ani_shoot);
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

        public CSprite sp;

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
                "v_rif_famas","idle","reload","shoot1",
                "v_rif_aug" , "idle","reload","shoot1",
                "v_rif_ak47" , "ak47_idle","ak47_reload","ak47_fire1",
                "v_pist_deagle" , "idle1","reload","shoot1",
                "v_knife_t","idle","draw","stab",
                "v_rif_galil" , "idle","reload","shoot1" };


                /*
                 *                 "v_357" , "idle01","reload","fire" , "22" ,
                "v_pist_elite","idle","reload","shoot_right1","48",
                "v_rif_famas","idle","reload","shoot1","40",
                "v_rpg","idle1","reload","fire","1",
                "v_shotgun","idle01","reload1","fire","-1" };
*/
            //                "v_stunbaton","idle01","draw","attackcm","-1",
            //                "v_superphyscannon","idle","chargeup","fire","12" };
            //"v_rif_ak47" , "ak47_idle","ak47_reload","ak47_fire1","63",

            int t = 0;
            while(t<W.Length)
            {
                String weapon_name = W[t++];
                String file_name = "weapons\\"+weapon_name+"\\"+weapon_name;
                weapons[cant_weapons++] = new weapon_reference(file_name, device, Content, cs_folder,
                        W[t++], W[t++], W[t++]);
            }


            int k = 0;

            // famas
            weapons[k].pitch = 79;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-11, -3, -9);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_pos = new Vector3(0,0,0);
            k++;

            // aug
            weapons[k].pitch = 76;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-5, -6, -15);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_pos = new Vector3(-0.20f, 2.70f, 14.00f);
            k++;

            // ak47 
            weapons[k].pitch = 76;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-5, -6, -15);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_pos = new Vector3(0,3.50f,19.00f);
            k++;

            // pist Eagle
            weapons[k].pitch = 71;
            weapons[k].yaw = 10;
            weapons[k].desf = new Vector3(-14, -1.5f, -14);
            weapons[k].muzzle_pos = new Vector3(0, 2.75f, 6.50f);
            //weapons[k].model.anim[2].frameRate = 10;


            // correccion muzzle_pos
            for (int i=0;i<cant_weapons;++i)
                weapons[i].muzzle_pos = new Vector3(weapons[i].muzzle_pos.X, -weapons[i].muzzle_pos.Z, weapons[i].muzzle_pos.Y);

            // otros efectos
            sp = new CSprite("effects\\muzzleflashx", device, Content);
            sp.rendercolor = new Vector3(1, 1, 1);
            sp.scale = 0.1f;
            sp.renderamt = 1;

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
            float k = m.cur_anim == 2 ? 3 : 2;
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

               if (keyState.IsKeyDown(Keys.NumPad1)) W.muzzle_pos.X -= s;
               if (keyState.IsKeyDown(Keys.NumPad2)) W.muzzle_pos.Y -= s;
               if (keyState.IsKeyDown(Keys.NumPad3)) W.muzzle_pos.Z -= s;
                if (keyState.IsKeyDown(Keys.NumPad4)) W.muzzle_pos.X += s;
                if (keyState.IsKeyDown(Keys.NumPad5)) W.muzzle_pos.Y += s;
                if (keyState.IsKeyDown(Keys.NumPad6)) W.muzzle_pos.Z += s;
            
            if (keyState.IsKeyDown(Keys.PageDown))
            {
                if (!game.keyDown[(int)Keys.PageDown])
                {
                    if (++cur_weapon >= cant_weapons)
                        cur_weapon = 0;
                    W = weapons[cur_weapon];
                    m = W.model;
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
                    if (--cur_weapon < 0)
                        cur_weapon = cant_weapons - 1;
                    status = W_RELOADING;
                    W = weapons[cur_weapon];
                    m = W.model;
                    m.currentTime = 0;
                    m.setAnimation(1);
                }
                game.keyDown[(int)Keys.PageUp] = true;
            }
            else
                game.keyDown[(int)Keys.PageUp] = false;

            MouseState state = Mouse.GetState();

            if (status==W_RELOADING)
            {
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
            }
            else
            {
                status = W_IDLE;
                m.setAnimation(0);
            }
        }

        public void Draw(Effect effect, Matrix View, Matrix Proj)
        {
            var W = weapons[cur_weapon];
            var m = W.model;
            var world = getTransform();
            m.Draw(device, effect, world, View, Proj);


            if (status == W_FIRING)
            {
                sp.origin = Vector3.Transform(m.bones[0].Position +W.muzzle_pos , m.invMetric * world);
                sp.renderamt = Math.Max(0, MathF.Cos(m.currentTime * 50.0f));
                sp.scale = 0.1f;

                // env sprites
                var effectSprite = game.scene.EffectMesh;
                effectSprite.CurrentTechnique = effectSprite.Techniques["SpriteDrawing"];
                var ant_blend_state = device.BlendState;
                device.BlendState = BlendState.Additive;
                var ant_z_state = device.DepthStencilState;
                var depthState = new DepthStencilState();
                depthState.DepthBufferEnable = true;
                depthState.DepthBufferWriteEnable = false;
                device.DepthStencilState = depthState;

                effectSprite.Parameters["View"].SetValue(Matrix.Identity);
                effectSprite.Parameters["Projection"].SetValue(Proj);
                // el false es para que no lo tome como un DECAL y no modifique la pos. Z
                sp.Draw(game.scene.spriteVertexBuffer, effectSprite, View , false);
                device.BlendState = ant_blend_state;
                device.DepthStencilState = ant_z_state;

            }

        }

    }
}