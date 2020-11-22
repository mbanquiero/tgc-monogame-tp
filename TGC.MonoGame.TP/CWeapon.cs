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
        public int muzzle_attachment = 0;
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
                "v_pist_usp","idle","reload","shoot1",
                "v_pist_glock18","glock_idle","glock_reload","glock_firesingle",
                "v_pist_fiveseven","idle","reload","shoot1",
                "v_pist_elite","idle","reload","shoot_left1",
                "v_357","idle01","reload","fire",
                "v_snip_awp","awm_idle","awm_reload","awm_fire",
                "v_smg_mp5","idle","reload1","shoot1",
                "v_smg_mac10","mac10_idle","mac10_reload","mac10_fire",
                "v_shot_m3super90","idle","start_reload","shoot1",
                "v_rif_sg552","idle","reload","shoot1",
                "v_rif_m4a1","idle","reload","shoot1",
                "v_rif_famas","idle","reload","shoot1",
                "v_rif_aug" , "idle","reload","shoot1",
                "v_rif_ak47" , "ak47_idle","ak47_reload","ak47_fire1",
                "v_rif_galil" , "idle","reload","shoot1",
                "v_pist_deagle" , "idle1","reload","shoot1",
                "v_knife_t","idle","draw","stab"
                 };

            int t = 0;
            while(t<W.Length)
            {
                String weapon_name = W[t++];
                String file_name = "weapons\\"+weapon_name+"\\"+weapon_name;
                weapons[cant_weapons++] = new weapon_reference(file_name, device, Content, cs_folder,
                        W[t++], W[t++], W[t++]);
            }


            int k = 0;


            //v_pist_usp
            weapons[k].pitch = 73;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-4.7f, -4, -4);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_attachment = 1;
            weapons[k].muzzle_pos = new Vector3(0,2.75f,6.5f);
            k++;

            // v_pist_glock18
            weapons[k].pitch = 73;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-4.7f, -4, -4);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_attachment = 4;
            weapons[k].muzzle_pos = new Vector3(0,0,6);
            k++;

            // v_pist_fiveseven
            weapons[k].pitch = 73;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-4.7f, -4, -4);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_attachment = 1;
            weapons[k].muzzle_pos = new Vector3(0, 2.2f,7.5f);
            k++;


            // v_pist_elite
            weapons[k].pitch = 73;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-4.7f, -4, -4);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_attachment = 22;
            k++;

            //v_357
            weapons[k].pitch = 73;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-17, -6, -3);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_attachment = 22;
            k++;

            //v_snip_awp
            weapons[k].pitch = 73;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-4.7f, -4, -4);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_pos = new Vector3(0,3.5f,28);
            k++;

            //v_smg_mp5
            weapons[k].pitch = 73;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-4.7f, -4, -4);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_pos = new Vector3(3,6,27.5f);
            k++;

            //v_smg_mac10
            weapons[k].pitch = 73;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-4.7f, -4, -4);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_pos = new Vector3(0,3,5);
            k++;

            // v_shot_m3super90
            weapons[k].pitch = 73;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-4.7f, -4, -4);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_pos = new Vector3(2.724713f,2.405631f,16.455919f) + new Vector3(0,3,18);
            k++;

            //v_rif_sg552
            weapons[k].pitch = 73;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-4.7f, -4, -4);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_pos = new Vector3(3, 5.5f,34);
            k++;

            //v_rif_m4a1
            weapons[k].pitch = 73;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-4.7f, -4, -4);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_pos = new Vector3(0.20f,3.50f,20.00f);
            k++;

            // famas
            weapons[k].pitch = 79;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-11, -3, -9);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_pos = new Vector3(-7.7f,0,12f);
            k++;

            // aug
            weapons[k].pitch = 76;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-5, -6, -15);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_pos = new Vector3(-1.70f, 2.70f, 15.00f);
            k++;

            // ak47 
            weapons[k].pitch = 76;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-5, -6, -15);
            weapons[k].scale = new Vector3(1, 1, -1);
            weapons[k].muzzle_pos = new Vector3(0, 3.50f, 19.00f);
            k++;

            // galil
            weapons[k].pitch = 76;
            weapons[k].yaw = 14;
            weapons[k].desf = new Vector3(-5, -6, -15);
            weapons[k].muzzle_pos = new Vector3(1.6f,3.87f,31);
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


            cur_weapon = 9;
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
                // $attachment "1" "v_weapon.famas1" -0.00 0.00 0.00 rotate 0 0 -0
                // usualmente en el modelo viene un attachment con el lugar donde ubicar el muzzle
                // el attachment puede estar en referencia a otro hueso tambien, con lo cual se usan
                // las 2 cosas, W.muzzle_attachment y W.muzzle_pos
                // en el ejemplo seria
                // W.muzzle_attachment  = "v_weapon.famas1" 
                // W.muzzle_pos = -0.00 0.00
                sp.origin = Vector3.Transform(m.bones[W.muzzle_attachment].Position + W.muzzle_pos, m.invMetric * world);
                sp.renderamt = Math.Max(0, MathF.Cos(m.currentTime * 50.0f));
                sp.scale = 0.075f;

                //sp.renderamt = 1;
                //sp.scale = 0.01f;

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