using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP
{
	public struct mdl_subset
	{
		public int pos;
		public int cant_items;
		public string image_name;
		public bool traslucido;
	}

	public class CMdlMesh
    {
		public Matrix matWorld;
		public Vector3 p_min;
		public Vector3 p_max;
		public Vector3 size;
		public Vector3 cg;
		public int cant_subsets;
		public mdl_subset[] subset;
		public Texture2D[] texture;
		public string folder;
		public string name;
		public bsp_face[] faces;
		public int cant_faces;


		public GraphicsDevice device;
		public ContentManager Content;
		public VertexBuffer VertexBuffer;

		public static CDebugBox debug_box = null;


		public CMdlMesh(string fname, GraphicsDevice p_device, ContentManager p_content, string p_folder)
		{
			name = fname;
			folder = p_folder;
			device = p_device;
			Content = p_content;
			matWorld = Matrix.Identity;

			if (debug_box == null)
				debug_box = new CDebugBox(p_device);

			var fp = new FileStream(folder+"models//"+fname+".csm", FileMode.Open, FileAccess.Read);
			var arrayByte = new byte[(int)fp.Length];
			fp.Read(arrayByte, 0, (int)fp.Length);
			fp.Close();
			initMeshFromData(arrayByte);

		}

		// helper 
		public string getString(byte[] arrayByte, int offset, int len)
		{
			var s = "";
			for (var j = 0; j < len; ++j)
			{
				s += (char)arrayByte[j+offset];
			}
			return s.Trim('\0');

		}

		public void initMeshFromData(byte[] arrayByte)
		{

			var t = 0;
			int cant_v = BitConverter.ToInt32(arrayByte, t); t += 4;
			float min_x = 1000000;
			float min_y = 1000000;
			float min_z = 1000000;
			float max_x = -1000000;
			float max_y = -1000000;
			float max_z = -1000000;

			VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[cant_v];


			for (var i = 0; i < cant_v; ++i)
			{
				// struct ms_vertex
				/*
				{
					float x, y, z;              // pos
					float nx, ny, nz;               // normal
					float u, v;                 // tex coords
				};
				*/

				var x = vertices[i].Position.X = BitConverter.ToSingle(arrayByte, t); t += 4;
				var z = vertices[i].Position.Z = BitConverter.ToSingle(arrayByte, t); t += 4;
				var y = vertices[i].Position.Y = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].Normal.X = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].Normal.Z = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].Normal.Y = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].TextureCoordinate.X = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].TextureCoordinate.Y = BitConverter.ToSingle(arrayByte, t); t += 4;

				if (x < min_x)
					min_x = x;
				if (y < min_y)
					min_y = y;
				if (z < min_z)
					min_z = z;
				if (x > max_x)
					max_x = x;
				if (y > max_y)
					max_y = y;
				if (z > max_z)
					max_z = z;
			}

			// actualizo el bounding box
			p_min = new Vector3(min_x, min_y, min_z);
			p_max = new Vector3(max_x, max_y, max_z);
			size = new Vector3(max_x - min_x, max_y - min_y, max_z - min_z);
			cg = p_min + size * 0.5f;

			VertexBuffer = new VertexBuffer(device, VertexPositionNormalTexture.VertexDeclaration, cant_v, BufferUsage.WriteOnly);
			VertexBuffer.SetData(vertices);

			// estructura de sub-setes
			cant_subsets = BitConverter.ToInt32(arrayByte, t); t += 4;
			subset = new mdl_subset[cant_subsets];
			for (int i = 0; i < cant_subsets; ++i)
			{
				int cant_items = (int)BitConverter.ToInt32(arrayByte, t); t += 4;
				subset[i].cant_items = cant_items;
				subset[i].image_name = getString(arrayByte, t, 256); t += 256;
				subset[i].traslucido = false;
			}


			// almaceno los faces a los efectos de colision
			faces = new bsp_face[cant_v/3];
			cant_faces = 0;
			var pos = 0;
			for (var i = 0; i < cant_subsets; i++)
			{
				for (int j = 0; j < subset[i].cant_items; ++j)
				{
					var k = pos + 3 * j;
					Vector3 v0 = vertices[k].Position;
					Vector3 v1 = vertices[k + 1].Position;
					Vector3 v2 = vertices[k + 2].Position;
					faces[cant_faces] = new bsp_face();
					faces[cant_faces].v[0] = v0;
					faces[cant_faces].v[1] = v1;
					faces[cant_faces].v[2] = v2;
					cant_faces++;
				}
				pos += subset[i].cant_items * 3;
			}

			initTextures();

			// actualizo la pos de cada subset
			pos = 0;
			for (var i = 0; i < cant_subsets; i++)
			{
				subset[i].pos = pos;
				pos += subset[i].cant_items * 3;
			}

		}

		public void initTextures()
		{
			texture = new Texture2D[cant_subsets];
			for (var i = 0; i < cant_subsets; ++i)
			{
				var name = CBspFile.que_tga_name(subset[i].image_name);
				var tga = CBspFile.tex_folder + name + ".tga";
				texture[i] = CTextureLoader.Load(device, tga);
				// propiedades del material
				var vmt = CBspFile.tex_folder + name + ".vmt";
				if (File.Exists(vmt))
				{
					var content = File.ReadAllText(vmt).ToLower();
					// "$translucent" "1"
					// "$translucent" 1


					var start = content.IndexOf("$translucent");
					if (start >= 0)
					{
						start += 12;
						if (content[start] == '\"')
							++start;
						if (content[start] == ' ')
							++start;
						if (content[start] == '\"')
							++start;
						if (content[start] == '1')
							subset[i].traslucido = true;
					}
				}

			}
		}

		public void Draw(GraphicsDevice graphicsDevice, Effect Effect, Matrix World, Matrix View, Matrix Proj)
		{
			graphicsDevice.SetVertexBuffer(VertexBuffer);
			Effect.Parameters["World"].SetValue(World);
			Effect.Parameters["View"].SetValue(View);
			Effect.Parameters["Projection"].SetValue(Proj);

			for (var L = 0; L < 2; ++L)
			{
				// L ==0  opacos , ==1 traslucidos
				for (var i = 0; i < cant_subsets; i++)
				{
					var cant_items = subset[i].cant_items;
					if (cant_items > 0 && subset[i].traslucido==(L==1))
					{
						var pos = subset[i].pos;

						//gl.bindTexture(gl.TEXTURE_2D, this.texture[i]);
						if (texture[i] != null)
							Effect.Parameters["ModelTexture"].SetValue(texture[i]);

						foreach (var pass in Effect.CurrentTechnique.Passes)
						{
							pass.Apply();
							graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, pos, cant_items);
						}
					}
				}
			}

			// dibujo el aa bb box de debug
			//debug_box.Draw(device, p_min, p_max, Effect, World,View, Proj);
		}


		public void DrawSubset(GraphicsDevice graphicsDevice, Effect Effect, Matrix World, Matrix View, Matrix Proj , int nro_subset)
		{

			var cant_items = subset[nro_subset].cant_items;
			if (cant_items > 0)
			{
				graphicsDevice.SetVertexBuffer(VertexBuffer);
				Effect.Parameters["World"].SetValue(World);
				Effect.Parameters["View"].SetValue(View);
				Effect.Parameters["Projection"].SetValue(Proj);
				var pos = subset[nro_subset].pos;
				if (texture[nro_subset] != null)
					Effect.Parameters["ModelTexture"].SetValue(texture[nro_subset]);
				foreach (var pass in Effect.CurrentTechnique.Passes)
				{
					pass.Apply();
					graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, pos, cant_items);
				}
			}
		}

	}

}
