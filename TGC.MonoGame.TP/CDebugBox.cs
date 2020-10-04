using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{

    public class CDebugBox
    {
        public GraphicsDevice device;
        public VertexBuffer VertexBuffer;

        public CDebugBox(GraphicsDevice p_device)
        {
            device = p_device;
            Vector3 p_min = new Vector3(-1, -1, -1);
            Vector3 p_max = new Vector3(1, 1, 1);

            Vector3 p0 = new Vector3(p_min.X, p_min.Y, p_min.Z);
            Vector3 p1 = new Vector3(p_max.X, p_min.Y, p_min.Z);
            Vector3 p2 = new Vector3(p_max.X, p_min.Y, p_max.Z);
            Vector3 p3 = new Vector3(p_min.X, p_min.Y, p_max.Z);

            Vector3 p4 = new Vector3(p_min.X, p_max.Y, p_min.Z);
            Vector3 p5 = new Vector3(p_max.X, p_max.Y, p_min.Z);
            Vector3 p6 = new Vector3(p_max.X, p_max.Y, p_max.Z);
            Vector3 p7 = new Vector3(p_min.X, p_max.Y, p_max.Z);

            VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[24];
            vertices[0].Position = p0;
            vertices[1].Position = p1;
            vertices[2].Position = p1;
            vertices[3].Position = p2;
            vertices[4].Position = p2;
            vertices[5].Position = p3;
            vertices[6].Position = p3;
            vertices[7].Position = p0;

            vertices[8].Position = p4;
            vertices[9].Position = p5;
            vertices[10].Position = p5;
            vertices[11].Position = p6;
            vertices[12].Position = p6;
            vertices[13].Position = p7;
            vertices[14].Position = p7;
            vertices[15].Position = p4;

            vertices[16].Position = p0;
            vertices[17].Position = p4;
            vertices[18].Position = p1;
            vertices[19].Position = p5;
            vertices[20].Position = p2;
            vertices[21].Position = p6;
            vertices[22].Position = p3;
            vertices[23].Position = p7;

            VertexBuffer = new VertexBuffer(device, VertexPositionNormalTexture.VertexDeclaration, 24, BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertices);

        }

        public void Draw(GraphicsDevice graphicsDevice, Vector3 p_min, Vector3 p_max, Effect Effect, Matrix World,Matrix View, Matrix Proj)
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            var ant_technique = Effect.CurrentTechnique;
            Effect.CurrentTechnique = Effect.Techniques["ColorDrawing"];

            /*
            var ant_z_state = device.DepthStencilState;
            var depthState = new DepthStencilState();
            depthState.DepthBufferEnable = false;
            depthState.DepthBufferWriteEnable = false;
            device.DepthStencilState = depthState;
            */

            Matrix T = Matrix.CreateScale((p_max - p_min) * 0.5f) * Matrix.CreateTranslation((p_max + p_min) * 0.5f) * World;
            Effect.Parameters["World"].SetValue(T);
            Effect.Parameters["View"].SetValue(View);
            Effect.Parameters["Projection"].SetValue(Proj);
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.LineList, 0, 12);
            }
            Effect.CurrentTechnique = ant_technique;
            
            //device.DepthStencilState = ant_z_state;

        }
    }
}
