using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{

    public class CTexturePool
    {
        public const int MAX_TEXTURAS = 1024;
        public int cant_texturas;
        public Texture2D[] texturas;
        public string[] tx_name;

        public CTexturePool()
        {
            cant_texturas = 0;
            texturas = new Texture2D[MAX_TEXTURAS];
            tx_name = new string[MAX_TEXTURAS];
        }

        public int que_textura(string name)
        {
            // busco si esta en la lista
            int rta = -1;
            for (int i = 0; i < cant_texturas && rta == -1; ++i)
                if (tx_name[i] == name)
                    rta = i;
            return rta;
        }


        public Texture2D insert(string image_name , GraphicsDevice device)
        {
            int n = que_textura(image_name);
            if (n == -1 && cant_texturas<MAX_TEXTURAS-1)
            {
                
                var name = CBspFile.que_tga_name(image_name);
                var tga = CBspFile.tex_folder + name + ".tga";
                texturas[cant_texturas] = CTextureLoader.Load(device, tga);
                tx_name[cant_texturas] = image_name;
                n = cant_texturas++;
            }
            return n!=-1 ? texturas[n] : null;
        }
    }

    public class CSprite
    {

        public static CTexturePool tx_pool = new CTexturePool();


        public Vector3 origin = new Vector3();
        public float scale = 1.0f;
        public Vector3 rendercolor = new Vector3();
        public float renderamt = 1.0f;
        public Texture2D texture;

        public GraphicsDevice device;
        public ContentManager Content;


        public CSprite(string image_name, GraphicsDevice p_device, ContentManager p_content)
        {
            device = p_device;
            Content = p_content;

            /*
            var name = CBspFile.que_tga_name(image_name);
            var tga = CBspFile.tex_folder + name + ".tga";
            texture = CTextureLoader.Load(device, tga);
            */
            texture = tx_pool.insert(image_name, device);
        }

        public void Draw(VertexBuffer vertexBuffer, Effect Effect , Matrix View)
        {
            // El quad esta siempre orientado a la camara, para ello
            // 1- pongo la matrix de view en identiy (eso lo hace el scene una sola vez antes de tirar todos los sprites
            // 2 -la traslacion se calcula en el espacio de la camara, (o sea multiplico x View)
            device.SetVertexBuffer(vertexBuffer);
            Vector3 pos = Vector3.Transform(origin, View);
            if(texture!=null)
                pos.Z += texture.Width * 0.1f;
            
            Matrix world = Matrix.CreateScale(texture.Width*0.5f) * Matrix.CreateTranslation(pos);
            Effect.Parameters["World"].SetValue(world);
            if (texture != null)
                Effect.Parameters["ModelTexture"].SetValue(texture);
            Effect.Parameters["renderamt"].SetValue(renderamt);
            Effect.Parameters["rendercolor"].SetValue(rendercolor);

            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 6);
            }
        }

    }
}
