using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{
    class CDebugLine
    {

        public static void Draw(GraphicsDevice device, Vector3 p0, Vector3 p1, Effect Effect, Matrix World, Matrix View, Matrix Proj)
        {
            var ant_technique = Effect.CurrentTechnique;
            Effect.CurrentTechnique = Effect.Techniques["ColorDrawing"];
            Effect.Parameters["World"].SetValue(World);
            Effect.Parameters["View"].SetValue(View);
            Effect.Parameters["Projection"].SetValue(Proj);

            VertexPosition[] vertices = new VertexPosition[2];
            vertices[0].Position = p0;
            vertices[1].Position = p1;
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList,vertices, 0,1);
            }
            Effect.CurrentTechnique = ant_technique;

        }

    }
}
