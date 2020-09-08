using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace TGC.MonoGame.TP
{
	public struct ValveVertex
	{
		public Vector3 Position;
		public Vector2 TextureCoordinate;
		public Vector3 Normal;
		public Vector2 LightmapCoordinate;

		public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement[]
			{
						new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
						new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate,0),
						new VertexElement(20, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
						new VertexElement(32, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate,1)
			});
	};


	public class bsp_triangle
	{
		public Vector3[] v = new Vector3[3];
	}

	public struct bsp_subset
	{
		public int cant_items;
		public string image_name;
	}




	public class bsp_model
    {
		public int nro_mesh;		
		public Vector3 origin;
		public Vector3 angles;

		public Matrix world()
        {
			Matrix T = new Matrix( new Vector4(1, 0, 0, 0),
								   new Vector4(0, 0, 1, 0),
								   new Vector4(0, 1, 0, 0),
								   new Vector4(0, 0, 0, 1));
		
			Matrix pitch = Matrix.CreateRotationX(-angles.X);
			Matrix yaw = Matrix.CreateRotationZ(angles.Y);
			Matrix roll = Matrix.CreateRotationY(-angles.Z);

			Matrix mat_world = T * pitch * yaw * roll * T * Matrix.CreateTranslation(origin);
			return mat_world;
		}

	}

	public class CMeshPool
    {
		public const int MAX_MESHES = 1024;
		public int cant_meshes;
		public CMdlMesh[] meshes;

		public CMeshPool()
		{
			cant_meshes = 0;
			meshes = new CMdlMesh[MAX_MESHES];
		}

		public int que_mesh(string model)
		{
			// busco si esta en la lista
			int rta = -1;
			for (int i = 0; i < cant_meshes && rta == -1; ++i)
				if (meshes[i].name == model)
					rta = i;
			return rta;
		}


		public int insert(string model , GraphicsDevice device, ContentManager Content,string cs_folder)
        {
			int rta = que_mesh(model);
			if (rta == -1)
				meshes[rta = cant_meshes++] = new CMdlMesh(model, device, Content, cs_folder);
			return rta;

		}
	}


	public class CBspFile
    {
		public Vector3 p_min;
		public Vector3 p_max;
		public Vector3 size;
		public Vector3 cg;
		public int cant_subsets;
		public bsp_subset[] subset;
		public Texture2D[] texture;
		public Texture2D texture_default;
		public Texture2D lightmap;
		public string folder_textures;

		public GraphicsDevice device;
		public ContentManager Content;
		public VertexBuffer VertexBuffer;
		public Effect Effect;
		public Effect EffectMesh;


		public int current_subset = 6;
		public bool mostrar_tools = false;
		public int current_model = 0;


		// geometria
		public int cant_faces;
		public bsp_triangle[] faces;

		// meshes
		public CMeshPool mesh_pool = new CMeshPool();

		// modelos
		public const int MAX_MODELOS = 4096;
		public int cant_modelos;
		public bsp_model[] modelos = new bsp_model[MAX_MODELOS];

		public static string tex_folder = "C:\\Counter-Strike Source\\cstrike\\materials\\";
		public static string map_folder = "C:\\Counter-Strike Source\\cstrike\\maps\\";
		public static string cs_folder = "C:\\Counter-Strike Source\\cstrike\\";




		public CBspFile(string fname, GraphicsDevice p_device, ContentManager p_content)
		{

			device = p_device;
			Content = p_content;
			Effect = Content.Load<Effect>("Effects/PhongShader");
			EffectMesh = Content.Load<Effect>("Effects/BasicShader");

			var fp = new FileStream(map_folder+fname+".tgc", FileMode.Open, FileAccess.Read);
			var arrayByte = new byte[(int)fp.Length];
			fp.Read(arrayByte, 0, (int)fp.Length);
			fp.Close();
			initMeshFromData(arrayByte);
			texture_default = Content.Load<Texture2D>("Textures/barrier");
			// cargo las entidades
			cargarEntidades(fname);


		}

		public string getString(byte[] arrayByte, int offset, int len)
		{
			var s = "";
			for (var j = 0; j < len; ++j)
				s += (char)arrayByte[offset++];
			return s.Trim('\0');

		}

		// helper
		public static bool ispot2(int n)
		{
			return (n != 0) && ((n & (n - 1)) == 0);
		}

		// helper para obtener el nombre de la textura
		public static string que_tga_name(string name)
        {
			var vmt = tex_folder + name + ".vmt";
			var tga = tex_folder + name + ".tga";
			// si no encuentra el archivo tga, pero esta el vmt, tengo que buscar el nombre de la textura en el vmt
			if (!File.Exists(tga) && File.Exists(vmt))
			{
				var content = File.ReadAllText(vmt).ToLower();

				if (content.StartsWith("\"patch\""))
				{
					var j = content.IndexOf("include\"");
					if (j >= 0)
					{
						j = content.IndexOf("materials", j + 8);
						if (j > 0)
						{
							var fin = content.IndexOf("\"", j);
							var include = content.Substring(j + 10, fin - j - 10);
							content = File.ReadAllText(tex_folder + include).ToLower();
						}
					}
				}

				// puede venir con o sin comilllas
				// 	"$basetexture"		"de_mirage/base/base_mid_ver1_diffuse"
				// 	$basetexture	"decals\hpe_plaster_decal_decay_brick_03"

				var start = content.IndexOf("$basetexture");
				if (start >= 0)
				{
					start += 12;
					if (content[start] == '\"')
						++start;
					// primer comilla
					start = content.IndexOf("\"", start);
					start++;
					// ultima comilla
					var fin = content.IndexOf("\"", start);
					name = content.Substring(start, fin - start);
				}
			}

			//tga = tex_folder + name + ".tga";
			return name;

		}



		public void initMeshFromData(byte[] arrayByte)
		{

			var t = 0;
			int cant_v = (int)BitConverter.ToInt32(arrayByte, t); t += 4;
			float min_x = 1000000;
			float min_y = 1000000;
			float min_z = 1000000;
			float max_x = -1000000;
			float max_y = -1000000;
			float max_z = -1000000;


			ValveVertex[] vertices = new ValveVertex[cant_v];
			for (var i = 0; i < cant_v; ++i)
			{
				var x = BitConverter.ToSingle(arrayByte, t); t += 4;
				var z = BitConverter.ToSingle(arrayByte, t); t += 4;
				var y = BitConverter.ToSingle(arrayByte, t); t += 4;

				vertices[i].Position.X = x;
				vertices[i].Position.Y = y;
				vertices[i].Position.Z = z;

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

				vertices[i].TextureCoordinate.X = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].TextureCoordinate.Y = BitConverter.ToSingle(arrayByte, t); t += 4;

				vertices[i].LightmapCoordinate.X = -BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].LightmapCoordinate.Y = BitConverter.ToSingle(arrayByte, t); t += 4;


			}

			// actualizo el bounding box
			p_min = new Vector3(min_x, min_y, min_z);
			p_max = new Vector3(max_x, max_y, max_z);
			size = new Vector3(max_x - min_x, max_y - min_y, max_z - min_z);
			cg = p_min + size * 0.5f;


			// computo la normal y de paso actualizo la geoemtria
			int cant_tri = cant_v / 3;



			for (int i = 0; i < cant_tri; ++i)
			{
				Vector3 v0 = vertices[3 * i].Position;
				Vector3 v1 = vertices[3 * i + 1].Position;
				Vector3 v2 = vertices[3 * i + 2].Position;

				Vector3 N = Vector3.Cross(v1 - v0, v2 - v0);
				N.Normalize();
				vertices[3 * i].Normal = vertices[3 * i + 1].Normal = vertices[3 * i + 2].Normal = N;

				/*
				faces[i] = new bsp_triangle();
				faces[i].v[0] = v0;
				faces[i].v[1] = v1;
				faces[i].v[2] = v2;
				*/

			}


			VertexBuffer = new VertexBuffer(device, ValveVertex.VertexDeclaration, cant_v, BufferUsage.WriteOnly);
			VertexBuffer.SetData(vertices);


			// estructura de subsets
			cant_subsets = (int)BitConverter.ToInt32(arrayByte, t); t += 4;
			subset = new bsp_subset[cant_subsets];
			texture = new Texture2D[cant_subsets];

			for (int i = 0; i < cant_subsets; ++i)
			{
				subset[i] = new bsp_subset();
				subset[i].cant_items = (int)BitConverter.ToInt32(arrayByte, t); t += 4;
				string name = getString(arrayByte, t, 256); t += 256;
				name = name.ToUpper();
				subset[i].image_name = name = que_tga_name(name);
				var tga = tex_folder + name + ".tga";
				texture[i] = null;
				if (File.Exists(tga))
					texture[i] = CTextureLoader.Load(device,tga);

				if(texture[i]==null)
				{
					Debug.WriteLine(i + "-" + subset[i].image_name);
				}
			}

			// como mucho hay cant_tri faces a los efectos de colision, anulo las que son TOOLS
			faces = new bsp_triangle[cant_tri];
			cant_faces = 0;
			var pos = 0;
			for (var i = 0; i < cant_subsets; i++)
			{
				if (!subset[i].image_name.StartsWith("TOOLS"))
                {

					for(int j=0;j<subset[i].cant_items;++j)
                    {
						var k = pos + 3 * j;
						Vector3 v0 = vertices[k].Position;
						Vector3 v1 = vertices[k+1].Position;
						Vector3 v2 = vertices[k+2].Position;
						faces[cant_faces] = new bsp_triangle();
						faces[cant_faces].v[0] = v0;
						faces[cant_faces].v[1] = v1;
						faces[cant_faces].v[2] = v2;
						cant_faces++;
					}
				}
				pos += subset[i].cant_items * 3;
			}


			// leo el lightmap
			lightmap = new Texture2D(device, 2048, 2048);
			int lm_size = 2048 * 2048;
			Color[] data = new Color[lm_size];
			for (int pixel = 0; pixel < lm_size; pixel++)
			{
				data[pixel].R = arrayByte[t++];
				data[pixel].G = arrayByte[t++];
				data[pixel].B = arrayByte[t++];
				data[pixel].A = (byte)(arrayByte[t++] + 128);
			}
			lightmap.SetData(data);


			// modelos
			/*
			cant_modelos = 1;
			modelos = new bsp_model[cant_modelos];
			modelos[0] = new bsp_model();
			modelos[0].mesh = new CMdlMesh("props_junk\\TrashBin01a", device, Content, cs_folder);
			modelos[0].origin = new Vector3(5330, -840 , 7182);
			modelos[0].angles = new Vector3(0, 0, 0);
			*/
		}

		public Vector3 parseVector3(string s)
        {
			// 5620.04 4026.82 -482.691
			Vector3 rta = new Vector3(0, 0, 0);
			string[] n = s.Split(' ');
			if (n.Length == 3)
			{
				// ojo, el z y el y estan invertidos 
				rta.X = float.Parse(n[0], CultureInfo.InvariantCulture.NumberFormat);
				rta.Z = float.Parse(n[1], CultureInfo.InvariantCulture.NumberFormat);
				rta.Y = float.Parse(n[2], CultureInfo.InvariantCulture.NumberFormat);
			}
			return rta;
		}

		public Vector3 parseOrient(string s)
		{
			Vector3 rta = new Vector3(0, 0, 0);
			string[] n = s.Split(' ');
			if (n.Length == 3)
			{
				rta.X = float.Parse(n[0], CultureInfo.InvariantCulture.NumberFormat);
				rta.Y = float.Parse(n[1], CultureInfo.InvariantCulture.NumberFormat);
				rta.Z = float.Parse(n[2], CultureInfo.InvariantCulture.NumberFormat);
			}
			return rta * MathF.PI/180.0f;
		}

		public void cargarEntidades(string fname)
        {
			string lumpname = map_folder + fname + ".ent";
			if (!File.Exists(lumpname))
				return;

			StreamReader reader;
			int state = 0;
			string key="", value="";
			Vector3 origin = new Vector3(0, 0, 0);
			Vector3 angles = new Vector3(0, 0, 0);
			string model = "" , classname="";
			reader = new StreamReader(lumpname);
			do
			{
				char ch = (char)reader.Read();
				switch (ch)
				{
					case '{':
						state = 1;
						break;

					case '"':
						switch(state)
                        {
							case 1:
							case 5:
								key = "";
								state = 2;      // input key
								break;
							case 2:
								state = 3;
								break;
							case 3:
								state = 4;
								value = "";     // input value
								break;
							case 4:
								state = 5;
								// fin de entrada key , value
								if (key == "origin")
									origin = parseVector3(value);
								else
								if (key == "model")
									model = value;
								else
								if (key == "classname")
									classname = value;
								else
								if (key == "angles")
									angles = parseOrient(value);
								break;
						}
						break;

					case '}':
						state = 0;
						// fin de entidad
						if(model.Contains(".mdl") && cant_modelos< MAX_MODELOS-1)
                        {
							
							model = model.Replace("models/", "").Replace(".mdl", "");
							var p = modelos[cant_modelos++] = new bsp_model();
							p.origin = origin;
							p.angles = angles;
							p.nro_mesh = mesh_pool.insert(model, device, Content, cs_folder);
							Debug.WriteLine(model + " " + origin + "  " + angles);

						}
						break;

					default:
						switch (state)
						{
							case 2:
								key += ch;
								break;
							case 4:
								value += ch;
								break;
						}
						break;
				}

			} while (!reader.EndOfStream);
			reader.Close();
			reader.Dispose();
		}


		public void Draw(Matrix World, Matrix View, Matrix Proj)
		{
			device.SetVertexBuffer(VertexBuffer);
			Effect.CurrentTechnique = Effect.Techniques["Phong"];
			Effect.Parameters["World"].SetValue(World);
			Effect.Parameters["View"].SetValue(View);
			Effect.Parameters["Projection"].SetValue(Proj);
			Effect.Parameters["Lightmap"].SetValue(lightmap);

			// escenario estatico
			var pos = 0;
			for (var i = 0; i < cant_subsets; i++)
			{
				var cant_tri = subset[i].cant_items;
				var cant_items = cant_tri * 3;
				if (cant_items > 0)
				{

					if (mostrar_tools || !subset[i].image_name.StartsWith("TOOLS"))
					//if (i == current_subset)
					{
						Effect.Parameters["ModelTexture"].SetValue(texture[i] != null ? texture[i] : texture_default);
						foreach (var pass in Effect.CurrentTechnique.Passes)
						{
							pass.Apply();
							device.DrawPrimitives(PrimitiveType.TriangleList, pos, cant_tri);
						}
					}
					pos += cant_items;
				}
			}

			// modelos estaticos
			EffectMesh.CurrentTechnique = EffectMesh.Techniques["TextureDrawing"];
			for (var i = 0; i < cant_modelos; ++i)
			{
				Matrix world = modelos[i].world();
				CMdlMesh p_mesh = mesh_pool.meshes[modelos[i].nro_mesh];
				p_mesh.Draw(device, EffectMesh, world, View, Proj);
			}
		}


		// experimento ray - tracing
		public float intersectSegment(Vector3 p, Vector3 q)
		{
			float min_t = 10000;
			Vector3 uvw = new Vector3();
			Vector3 col = new Vector3();
			float t = 0;
			for (int i = 0; i < cant_faces; ++i)
			{
				if (TgcCollisionUtils.intersectSegmentTriangle(p, q, faces[i].v[0], faces[i].v[1], faces[i].v[2], out uvw, out t, out col))
				{
					if (t < min_t)
						min_t = t;
				}
			}
			return min_t;
		}


	}
}
