using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{
    public class CSkybox
    {
        public GraphicsDevice device;
        public VertexBuffer VertexBuffer;
        public Texture2D[] texture = new Texture2D[6];


        public CSkybox(GraphicsDevice p_device)
        {
            device = p_device;
            Vector3 p_min = new Vector3(-1, -1, -1);
            Vector3 p_max = new Vector3(1, 1, 1);


            VertexPositionTexture[] vertices = new VertexPositionTexture[36];

            // abajo
            vertices[0].Position = new Vector3(p_min.X, p_min.Y, p_min.Z);
            vertices[0].TextureCoordinate = new Vector2(0, 0);
            vertices[1].Position = new Vector3(p_max.X, p_min.Y, p_min.Z);
            vertices[1].TextureCoordinate = new Vector2(0, 1);
            vertices[2].Position = new Vector3(p_max.X, p_min.Y, p_max.Z);
            vertices[2].TextureCoordinate = new Vector2(1, 1);
            vertices[3].Position = new Vector3(p_min.X, p_min.Y, p_min.Z);
            vertices[3].TextureCoordinate = new Vector2(0, 0);
            vertices[4].Position = new Vector3(p_max.X, p_min.Y, p_max.Z);
            vertices[4].TextureCoordinate = new Vector2(1, 1);
            vertices[5].Position = new Vector3(p_min.X, p_min.Y, p_max.Z);
            vertices[5].TextureCoordinate = new Vector2(1, 0);

            // arriba
            for (int i = 0; i < 6; ++i)
            {
                vertices[6 + i].Position = vertices[i].Position;
                vertices[6 + i].Position.Y = p_max.Y;
                vertices[6 + i].TextureCoordinate = vertices[i].TextureCoordinate;
            }


            //izquierda
            vertices[12].Position = new Vector3(p_min.X, p_min.Y, p_min.Z);
            vertices[12].TextureCoordinate = new Vector2(1, 1);
            vertices[13].Position = new Vector3(p_min.X, p_min.Y, p_max.Z);
            vertices[13].TextureCoordinate = new Vector2(0, 1);
            vertices[14].Position = new Vector3(p_min.X, p_max.Y, p_max.Z);
            vertices[14].TextureCoordinate = new Vector2(0, 0);
            vertices[15].Position = new Vector3(p_min.X, p_min.Y, p_min.Z);
            vertices[15].TextureCoordinate = new Vector2(1, 1);
            vertices[16].Position = new Vector3(p_min.X, p_max.Y, p_max.Z);
            vertices[16].TextureCoordinate = new Vector2(0, 0);
            vertices[17].Position = new Vector3(p_min.X, p_max.Y, p_min.Z);
            vertices[17].TextureCoordinate = new Vector2(1, 0);

            //derecha
            for (int i = 0; i < 6; ++i)
            {
                vertices[18 + i].Position = vertices[12+i].Position;
                vertices[18 + i].Position.X = p_max.X;
                vertices[18 + i].TextureCoordinate = vertices[12 + i].TextureCoordinate;
                vertices[18 + i].TextureCoordinate.X = 1 - vertices[18 + i].TextureCoordinate.X;
            }

            //adelante
            vertices[24].Position = new Vector3(p_min.X, p_min.Y, p_max.Z);
            vertices[24].TextureCoordinate = new Vector2(1, 1);
            vertices[25].Position = new Vector3(p_max.X, p_min.Y, p_max.Z);
            vertices[25].TextureCoordinate = new Vector2(0, 1);
            vertices[26].Position = new Vector3(p_min.X, p_max.Y, p_max.Z);
            vertices[26].TextureCoordinate = new Vector2(1, 0);
            vertices[27].Position = new Vector3(p_max.X, p_min.Y, p_max.Z);
            vertices[27].TextureCoordinate = new Vector2(0, 1);
            vertices[28].Position = new Vector3(p_min.X, p_max.Y, p_max.Z);
            vertices[28].TextureCoordinate = new Vector2(1, 0);
            vertices[29].Position = new Vector3(p_max.X, p_max.Y, p_max.Z);
            vertices[29].TextureCoordinate = new Vector2(0, 0);

            //atras
            for (int i = 0; i < 6; ++i)
            {
                vertices[30 + i].Position = vertices[24 + i].Position;
                vertices[30 + i].Position.Z = p_min.Z;
                vertices[30 + i].TextureCoordinate = vertices[24 + i].TextureCoordinate;
                vertices[30 + i].TextureCoordinate.X = 1 - vertices[24 + i].TextureCoordinate.X;
            }

            VertexBuffer = new VertexBuffer(device, VertexPositionTexture.VertexDeclaration, 36, BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertices);

            texture[0] = CTextureLoader.Load(device, CBspFile.tex_folder + "skybox\\assaultdn.tga");
            texture[1] = CTextureLoader.Load(device, CBspFile.tex_folder + "skybox\\assaultup.tga");
            texture[2] = CTextureLoader.Load(device, CBspFile.tex_folder + "skybox\\assaultlf.tga");
            texture[3] = CTextureLoader.Load(device, CBspFile.tex_folder + "skybox\\assaultrt.tga");
            texture[4] = CTextureLoader.Load(device, CBspFile.tex_folder + "skybox\\assaultft.tga");
            texture[5] = CTextureLoader.Load(device, CBspFile.tex_folder + "skybox\\assaultbk.tga");



        }

        public void Draw(GraphicsDevice graphicsDevice, Vector3 p_min, Vector3 p_max, Effect Effect, Matrix View, Matrix Proj)
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            var ant_technique = Effect.CurrentTechnique;
            Effect.CurrentTechnique = Effect.Techniques["SkyboxDrawing"];

            Matrix T = Matrix.CreateScale((p_max - p_min) * 0.5f) * Matrix.CreateTranslation((p_max + p_min) * 0.5f);
            Effect.Parameters["World"].SetValue(T);
            Effect.Parameters["View"].SetValue(View);
            Effect.Parameters["Projection"].SetValue(Proj);

            for (int face = 0; face < 6; ++face)
            {
                Effect.Parameters["SkyboxTexture"].SetValue(texture[face]);
                foreach (var pass in Effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawPrimitives(PrimitiveType.TriangleList, face * 6, 2);
                }
            }
            Effect.CurrentTechnique = ant_technique;
        }
    }
}
