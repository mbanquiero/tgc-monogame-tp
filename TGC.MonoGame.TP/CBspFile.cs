﻿using Microsoft.Xna.Framework;
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
using System.Xml;

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


	public class bsp_face
	{
		public Vector3[] v = new Vector3[3];
		// auxiliares
		public Vector3 pmin;
		public Vector3 pmax;
		// precomputed data: interseccion ray - tri
		public Vector3 e1;
		public Vector3 e2;
		// info adicional
		public int nro_modelo;
	
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
		public int flags;

		public Matrix world()
        {
			Matrix pitch = Matrix.CreateRotationZ(-angles.X);
			Matrix yaw = Matrix.CreateRotationY(-angles.Y);
			Matrix roll = Matrix.CreateRotationX(-angles.Z);
			Matrix mat_world = pitch *yaw * roll * Matrix.CreateTranslation(origin);
			return mat_world;
		}

	}

	public class CMeshPool
    {
		public const int MAX_MESHES = 1024;
		public int cant_meshes;
		public CSMDModel[] meshes;

		public CMeshPool()
		{
			cant_meshes = 0;
			meshes = new CSMDModel[MAX_MESHES];
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
			{
				meshes[rta = cant_meshes++] = new CSMDModel(model, device, Content, cs_folder);
			}
			return rta;

		}
	}

	public class info_decal
    {
		public Vector3 origin;
		public Texture2D texture;
		public bool blood = false;

		public info_decal(CBspFile scene,Vector3 p , string image_name , GraphicsDevice device, bool p_sangre=false)
        {
			origin = p;
			texture = scene.decals_tx_pool.insert(image_name, device);
			blood = p_sangre;
		}
	};

	public class info_hostage
	{
		public Vector3 origin;
		public Vector3 angles;
	};

	public class CBspFile
	{

		public const int PROP_DOOR_ROTATING = 1;

		public Vector3 p_min;
		public Vector3 p_max;
		public Vector3 size;
		public Vector3 cg;
		public Vector3 info_player_start_pos = new Vector3(0, 0, 0);
		public Vector3 info_player_start_angles = new Vector3(0, 0, 0);
		public int cant_subsets;
		public bsp_subset[] subset;
		public Texture2D[] texture;
		public Texture2D texture_default;
		public Texture2D lightmap;
		public string folder_textures;
		public info_hostage[] hostages = new info_hostage[256];
		public int cant_hostages = 0;

		public GraphicsDevice device;
		public ContentManager Content;
		public VertexBuffer VertexBuffer;
		public Effect Effect;
		public Effect EffectMesh;
		public VertexBuffer spriteVertexBuffer;
		public VertexBuffer bbVertexBuffer;
		public int cant_debug_bb = 0;

		public int current_subset = 6;
		public bool mostrar_tools = false;
		public int current_model = 0;


		// geometria
		public int cant_faces;
		public bsp_face[] faces;
		// globales escena + todos los meshes
		public int g_cant_faces;
		public bsp_face[] g_faces;

		public KDTree kd_tree;

		// meshes
		public CMeshPool mesh_pool = new CMeshPool();

		// sprites
		public const int MAX_SPRITES = 4096;
		public int cant_sprites = 0;
		public CSprite[] sprites = new CSprite[MAX_SPRITES];

		//gui
		public const int MAX_IMAGES = 4096;
		public int cant_images = 0;
		public Texture2D[] images = new Texture2D[MAX_IMAGES];


		// modelos
		public const int MAX_MODELOS = 4096;
		public int cant_modelos;
		public bsp_model[] modelos = new bsp_model[MAX_MODELOS];

		public static string tex_folder = "C:\\Counter-Strike Source\\cstrike\\materials\\";
		public static string map_folder = "C:\\Counter-Strike Source\\cstrike\\maps\\";
		public static string cs_folder = "C:\\Counter-Strike Source\\cstrike\\";

		// decals
		public const int MAX_DECALS = 4096;
		public int cant_decals = 0;
		public info_decal[] decals = new info_decal[MAX_DECALS];
		public VertexBuffer decalsVertexBuffer;
		public CTexturePool decals_tx_pool = new CTexturePool();


		public CBspFile(string fname, GraphicsDevice p_device, ContentManager p_content)
		{

			device = p_device;
			Content = p_content;
			Effect = Content.Load<Effect>("Effects/PhongShader");
			EffectMesh = Content.Load<Effect>("Effects/BasicShader");

			var fp = new FileStream(map_folder + fname + ".tgc", FileMode.Open, FileAccess.Read);
			var arrayByte = new byte[(int)fp.Length];
			fp.Read(arrayByte, 0, (int)fp.Length);
			fp.Close();
			initMeshFromData(arrayByte);
			texture_default = Content.Load<Texture2D>("Textures/barrier");
			// cargo las entidades
			cargarEntidades(fname);
			createSpriteQuad();
			
			// experimento kdtree con toda la escena + los mesh
			g_cant_faces = cant_faces;
			for (int i = 0; i < cant_modelos; ++i)
			{
				g_cant_faces += mesh_pool.meshes[modelos[i].nro_mesh].cant_faces;
			}
			g_faces = new bsp_face[g_cant_faces];
			g_cant_faces = 0;
			for (var i = 0; i < cant_faces; ++i)
				g_faces[g_cant_faces++] = faces[i];


			VertexPosition[] bb_vertices = new VertexPosition[cant_modelos * 36];
			var t = 0;

			for (int i = 0; i < cant_modelos; ++i)
			{
				var m = mesh_pool.meshes[modelos[i].nro_mesh];
				// TODO: determino si me conviene usar todo el mesh a nivel triangulos
				// o solo el bounding box
				/*var dx = m.p_max.X - m.p_min.X;
				var dy = m.p_max.Y - m.p_min.Y;
				var dz = m.p_max.Z - m.p_min.Z;
				*/
				if (true)
				{
					// todos los triangulos
					for (var j = 0; j < m.cant_faces; ++j)
					{
						var face = g_faces[g_cant_faces++] = new bsp_face();
						for (var k = 0; k < 3; ++k)
						{
							face.v[k] = Vector3.Transform(m.faces[j].v[k], modelos[i].world());
							face.nro_modelo = i;
						}
					}
				}
                else
                {
					// solo el bounding box oobb
					var x0 = m.p_min.X;
					var y0 = m.p_min.Y;
					var z0 = m.p_min.Z;
					var x1 = m.p_max.X;
					var y1 = m.p_max.Y;
					var z1 = m.p_max.Z;

					Vector3[] p = new Vector3[8];
					p[0] = new Vector3(x0, y0, z0);
					p[1] = new Vector3(x1, y0, z0);
					p[2] = new Vector3(x1, y1, z0);
					p[3] = new Vector3(x0, y1, z0);

					p[4] = new Vector3(x0, y0, z1);
					p[5] = new Vector3(x1, y0, z1);
					p[6] = new Vector3(x1, y1, z1);
					p[7] = new Vector3(x0, y1, z1);

					for(var j=0;j<8;++j)
						p[j] = Vector3.Transform(p[j], modelos[i].world());


					int[] ndx = {
							0,1,2,0,2,3,			// abajo
							4,5,6,4,6,7,			// arriba
							1,2,6,1,6,5,			// derecha
							0,3,7,0,7,4,			// izquierda
							3,2,6,3,6,7,			// adelante
							0,1,5,0,5,4				// atras
						};

					for(int j=0;j<36;++j)
					{
						bb_vertices[t++] = new VertexPosition(p[ndx[j]]);
					}
				}

			}

			cant_debug_bb = t;
			if (cant_debug_bb>0)
			{
				bbVertexBuffer = new VertexBuffer(device, VertexPosition.VertexDeclaration, t, BufferUsage.WriteOnly);
				bbVertexBuffer.SetData(bb_vertices,0,t);
			}

			// armo el kdtree
			//kd_tree = new KDTree(cant_faces, faces);
			kd_tree = new KDTree(g_cant_faces, g_faces);
			kd_tree.createKDTree();


			// imagenes
			images[cant_images++] = cargarImagen("sprites\\redglow4");
			images[cant_images++] = cargarImagen("sprites\\dot");
			images[cant_images++] = cargarImagen("sprites\\orangecore1");
			images[cant_images++] = cargarImagen("sprites\\laserdot");


			// decals
			createDecals();

		}


		public Texture2D cargarImagen(string image_name)
        {
			var name = que_tga_name(image_name);
			var tga = tex_folder + name + ".tga";
			return CTextureLoader.Load(device, tga);
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
			faces = new bsp_face[cant_tri];
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
						faces[cant_faces] = new bsp_face();
						faces[cant_faces].v[0] = v0;
						faces[cant_faces].v[1] = v1;
						faces[cant_faces].v[2] = v2;
						faces[cant_faces].nro_modelo = -1-i;		// escenario
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
		}

		public void createSpriteQuad()
        {
			// crea un quad unitario para 
			VertexPositionTexture[] vertices = new VertexPositionTexture[6];
			vertices[0].Position.X = -1;
			vertices[0].Position.Y = -1;
			vertices[0].Position.Z =  0.5f;
			vertices[0].TextureCoordinate.X = 0;
			vertices[0].TextureCoordinate.Y = 0;

			vertices[1].Position.X = -1;
			vertices[1].Position.Y = 1;
			vertices[1].Position.Z = 0.5f;
			vertices[1].TextureCoordinate.X = 0;
			vertices[1].TextureCoordinate.Y = 1;

			vertices[2].Position.X = 1;
			vertices[2].Position.Y = -1;
			vertices[2].Position.Z = 0.5f;
			vertices[2].TextureCoordinate.X = 1;
			vertices[2].TextureCoordinate.Y = 0;

			vertices[3] = vertices[1];
			vertices[4] = vertices[2];

			vertices[5].Position.X = 1;
			vertices[5].Position.Y = 1;
			vertices[5].Position.Z = 0.5f;
			vertices[5].TextureCoordinate.X = 1;
			vertices[5].TextureCoordinate.Y = 1;

			spriteVertexBuffer = new VertexBuffer(device, VertexPositionTexture.VertexDeclaration, 6, BufferUsage.WriteOnly);
			spriteVertexBuffer.SetData(vertices);


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

		public Vector3 parseColor(string s)
		{
			// "245 255 236"
			Vector3 rta = new Vector3(0, 0, 0);
			string[] n = s.Split(' ');
			if (n.Length == 3)
			{
				rta.X = float.Parse(n[0], CultureInfo.InvariantCulture.NumberFormat);
				rta.Y = float.Parse(n[1], CultureInfo.InvariantCulture.NumberFormat);
				rta.Z = float.Parse(n[2], CultureInfo.InvariantCulture.NumberFormat);
			}
			return rta * (1/255.0f);
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
			Vector3 rendercolor = new Vector3(1, 1, 1);
			float renderamt = 1;
			float scale = 0;
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
								if (key == "model" || key== "RopeMaterial" || key=="texture")
									model = value;
								else
								if (key == "classname")
									classname = value;
								else
								if (key == "angles")
									angles = parseOrient(value);
								else
								if (key == "scale")
									scale = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
								else
								if (key == "rendercolor")
									rendercolor = parseColor(value);
								else
								if (key == "renderamt")
									renderamt = float.Parse(value, CultureInfo.InvariantCulture.NumberFormat) / 255.0f;
								break;
						}
						break;

					case '}':
						state = 0;
						// fin de entidad
						if (model.Contains(".mdl") && cant_modelos < MAX_MODELOS - 1)
						{

							model = model.Replace("models/", "").Replace(".mdl", "");
							var p = modelos[cant_modelos++] = new bsp_model();
							p.origin = origin;
							p.angles = angles;
							p.nro_mesh = mesh_pool.insert(model, device, Content, cs_folder);

							if (classname == "prop_door_rotating")
							{
								// caso particular: es una puerta que gira
								p.flags |= PROP_DOOR_ROTATING;
							}



							Debug.WriteLine(model + " " + origin + "  " + angles);

						}
						else
						if ((classname == "env_sprite" || classname == "xkeyframe_rope") 
								&& cant_sprites < MAX_SPRITES- 1)
						{
							if(classname == "keyframe_rope")
                            {
								rendercolor = new Vector3(1, 1, 1);
								renderamt = 1;
							}
							if(classname == "infodecal")
                            {
								rendercolor = new Vector3(1, 1, 1);
								renderamt = 1;
                            }
							model = model.Replace("materials/", "").Replace(".vmt", "");
							var p = sprites[cant_sprites++] = new CSprite(model,device,Content);
							p.origin = origin;
							//p.scale = scale;
							p.renderamt = renderamt;
							p.rendercolor = rendercolor;
							Debug.WriteLine(model + " " + origin + "  " + angles);

						}
						else
						if (classname == "infodecal" && cant_decals < MAX_DECALS- 1)
						{
							model = model.Replace("materials/", "").Replace(".vmt", "");
							decals[cant_decals++] = new info_decal(this,origin,model,device);
						}
						else
						if(classname == "info_player_start")
                        {
							info_player_start_pos = origin;
							info_player_start_angles = angles;
						}
						else
						if (classname == "hostage_entity")
						{
							var h = hostages[cant_hostages++] = new info_hostage();
							h.origin = origin;
							h.angles = angles;
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

		public int que_face(Vector3 p)
        {
			int rta = -1;
			for(int i=0;i<cant_faces && rta==-1 && faces[i].nro_modelo<0;++i)
            {
				// verifico si el punto vive en la cara i
				if (TgcCollisionUtils.testPointInTriangle(p, faces[i].v[0], faces[i].v[1], faces[i].v[2]))
					rta = i;
            }
			return rta;
		}
		public void createDecals()
		{
			VertexPositionTexture []vertices = new VertexPositionTexture[MAX_DECALS*4];
			int t = 0;
			for (int i = 0; i < cant_decals; ++i)
			{

				// determino en que cara vive el decal
				int f = que_face(decals[i].origin);
				if (f != -1)
				{
					Vector3 n = Vector3.Cross(faces[f].v[2] - faces[f].v[0], faces[f].v[1] - faces[f].v[0]);
					n.Normalize();
					Vector3 up;
					if( MathF.Abs(n.X)<= MathF.Abs(n.Y) && MathF.Abs(n.X) <= MathF.Abs(n.Z))
						up = new Vector3(1, 0, 0);
					else
					if (MathF.Abs(n.Y) <= MathF.Abs(n.X) && MathF.Abs(n.Y) <= MathF.Abs(n.Z))
						up = new Vector3(0, 1, 0);
					else
						up = new Vector3(0, 0 , 1);

					Vector3 v = Vector3.Cross(n, up);
					v.Normalize();
					Vector3 u = Vector3.Cross(n, v);
					u.Normalize();

					float du = 50;
					float dv = 50;

					Vector3 p = decals[i].origin - u * du*0.5f - v * dv * 0.5f - n*5;
					vertices[t++] = new VertexPositionTexture(p, new Vector2(0, 0));
					vertices[t++] = new VertexPositionTexture(p + u * du, new Vector2(0, 1));
					vertices[t++] = new VertexPositionTexture(p + v * dv, new Vector2(-1, 0));
					vertices[t++] = new VertexPositionTexture(p + u * du + v * dv, new Vector2(-1, 1));
				}
			}
			decalsVertexBuffer = new VertexBuffer(device, VertexPositionTexture.VertexDeclaration, MAX_DECALS, BufferUsage.WriteOnly);
			if(t>0)
				decalsVertexBuffer.SetData(vertices, 0, t);

		}

		public void addDecal(Vector3 origin, int nro_face , string model , bool sangre)
		{
			// verifico si esta suficientemente alejado del anterior, para evitar 
			// una secuencia de tiros muy cerca
			if (cant_decals>0 && (origin - decals[cant_decals - 1].origin).LengthSquared() < 50)
				return;


			Vector3 []face_v = g_faces[nro_face].v;

			Vector3 n = Vector3.Cross(face_v[2] - face_v[0], face_v[1] - face_v[0]);
			n.Normalize();
			Vector3 up;
			if (MathF.Abs(n.X) <= MathF.Abs(n.Y) && MathF.Abs(n.X) <= MathF.Abs(n.Z))
				up = new Vector3(1, 0, 0);
			else
			if (MathF.Abs(n.Y) <= MathF.Abs(n.X) && MathF.Abs(n.Y) <= MathF.Abs(n.Z))
				up = new Vector3(0, 1, 0);
			else
				up = new Vector3(0, 0, 1);

			Vector3 v = Vector3.Cross(n, up);
			v.Normalize();
			Vector3 u = Vector3.Cross(n, v);
			u.Normalize();

			float du = sangre ? 25 : 5;
			float dv = sangre ? 25 : 5;
			float dn = g_faces[nro_face].nro_modelo > 0 ? 0.5f : -5f;

			Vector3 p = origin - u * du * 0.5f - v * dv * 0.5f + n * dn;
			VertexPositionTexture[] vertices = new VertexPositionTexture[4];
			vertices[0] = new VertexPositionTexture(p, new Vector2(0, 0));
			vertices[1] = new VertexPositionTexture(p + u * du, new Vector2(0, 1));
			vertices[2] = new VertexPositionTexture(p + v * dv, new Vector2(-1, 0));
			vertices[3] = new VertexPositionTexture(p + u * du + v * dv, new Vector2(-1, 1));

			decalsVertexBuffer.SetData(cant_decals * 4 * 20, vertices, 0, 4, 20);
			decals[cant_decals++] = new info_decal(this, origin, model, device,sangre);
		}


		public void Draw(Matrix World, Matrix View, Matrix Proj)
		{
			device.SetVertexBuffer(VertexBuffer);
			Effect.CurrentTechnique = Effect.Techniques["Phong"];
			Effect.Parameters["World"].SetValue(World);
			Effect.Parameters["View"].SetValue(View);
			Effect.Parameters["Projection"].SetValue(Proj);
			Effect.Parameters["Lightmap"].SetValue(lightmap);

			//device.BlendState = BlendState.AlphaBlend;
			device.BlendState = BlendState.Opaque;

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
			// primero layers opacos luego transparentes
			EffectMesh.CurrentTechnique = EffectMesh.Techniques["TextureDrawing"];
			device.BlendState = BlendState.Opaque;
			for (var L = 0; L < 2; ++L)
			{
				for (var i = 0; i < cant_modelos; ++i)
				{
					Matrix world = modelos[i].world();
					CSMDModel p_mesh = mesh_pool.meshes[modelos[i].nro_mesh];
					p_mesh.Draw(device, EffectMesh, world, View, Proj, L);
				}

				// paso al layer transparente
				// activo el Blend y desactivo el zwrite
				device.BlendState = BlendState.AlphaBlend;
				// mejor lo activo, porque como no puedo dibujar en orden correcto, crea muchos artifacts
				//device.DepthStencilState = DepthStencilState.DepthRead;
			}
			//device.DepthStencilState = DepthStencilState.Default;

			// decals
			Effect.CurrentTechnique = Effect.Techniques["Phong"];
			device.BlendState = BlendState.NonPremultiplied;
			device.DepthStencilState = DepthStencilState.DepthRead;
			device.SetVertexBuffer(decalsVertexBuffer);
			for (int i = 0; i < cant_decals; ++i)
			if(!decals[i].blood)
			{
				Effect.Parameters["ModelTexture"].SetValue(decals[i].texture != null ? decals[i].texture : texture_default);
				foreach (var pass in Effect.CurrentTechnique.Passes)
				{
					pass.Apply();
					device.DrawPrimitives(PrimitiveType.TriangleStrip, i * 4, 2);
				}
			}

			// sangre
			Effect.CurrentTechnique = Effect.Techniques["Blood"];
			for (int i = 0; i < cant_decals; ++i)
				if (decals[i].blood)
				{
					Effect.Parameters["ModelTexture"].SetValue(decals[i].texture != null ? decals[i].texture : texture_default);
					foreach (var pass in Effect.CurrentTechnique.Passes)
					{
						pass.Apply();
						device.DrawPrimitives(PrimitiveType.TriangleStrip, i * 4, 2);
					}
				}

			device.DepthStencilState = DepthStencilState.Default;

			// debug bbb
			if (cant_debug_bb > 0)
			{
				device.SetVertexBuffer(bbVertexBuffer);
				Effect.CurrentTechnique = Effect.Techniques["DebugBB"];
				foreach (var pass in Effect.CurrentTechnique.Passes)
				{
					pass.Apply();
					device.DrawPrimitives(PrimitiveType.TriangleList, 0, cant_debug_bb / 3);
				}
			}

			// env sprites
			EffectMesh.CurrentTechnique = EffectMesh.Techniques["SpriteDrawing"];
			var ant_blend_state = device.BlendState;
			device.BlendState = BlendState.Additive;
			var ant_z_state = device.DepthStencilState;
			var depthState = new DepthStencilState();
			depthState.DepthBufferEnable = true;
			depthState.DepthBufferWriteEnable = false;
			device.DepthStencilState = depthState;

			EffectMesh.Parameters["View"].SetValue(Matrix.Identity);
			EffectMesh.Parameters["Projection"].SetValue(Proj);
			for (var i=0;i<cant_sprites;++i)
            {
				sprites[i].Draw(spriteVertexBuffer , EffectMesh , View);
            }


			device.BlendState = ant_blend_state;
			device.DepthStencilState = ant_z_state;


		}

		public void DrawMap(CPlayer player, CEnemy []enemigo)
		{

			Vector3 Position = player.Position;
			Vector3 Direction = player.Direction;

		    Viewport ant_vp = device.Viewport;
			var vp = new Viewport();
			vp.X = 10;
			vp.Y = 50;
			float aspect = (float)ant_vp.Width / (float)ant_vp.Height;
			vp.Width = vp.Height = 200;
			vp.MinDepth = 0;
			vp.MaxDepth = 1;
			device.Viewport = vp;
			device.RasterizerState = RasterizerState.CullNone;

			device.DepthStencilState = DepthStencilState.None;
			Effect.CurrentTechnique = Effect.Techniques["ClearScreen"];
			Effect.Parameters["World"].SetValue(Matrix.Identity);
			Effect.Parameters["View"].SetValue(Matrix.Identity);
			Effect.Parameters["Projection"].SetValue(Matrix.Identity);
			device.SetVertexBuffer(spriteVertexBuffer);
			foreach (var pass in Effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
			}
			device.Clear(ClearOptions.DepthBuffer, Color.White, 1.0f, 0);

			Direction.Y = 0;
			Direction.Normalize();
			Matrix View = Matrix.CreateLookAt(Position, Position + Direction * 100, new Vector3(0, 1, 0));
			Matrix World = Matrix.Identity;
			float kx = 1.0f / size.X;
			float ky = 1.0f / size.Y;
			float kz = 1.0f / size.Z;

			kx *= 5*aspect;
			kz *= 5;
			Matrix S = new Matrix(	-kx, 0, 0, 0,
									0, 0, ky, 0,
									0, -kz, 0, 0,
									0, 0, 0.5f, 1);

			Matrix Proj = S ;

			device.SetVertexBuffer(VertexBuffer);
			Effect.CurrentTechnique = Effect.Techniques["Map"];
			Effect.Parameters["World"].SetValue(World);
			Effect.Parameters["View"].SetValue(View);
			Effect.Parameters["Projection"].SetValue(Proj);
			Effect.Parameters["Lightmap"].SetValue(lightmap);

			device.BlendState = BlendState.AlphaBlend;

			// escenario estatico
			var pos = 0;
			for (var i = 0; i < cant_subsets; i++)
			{
				var cant_tri = subset[i].cant_items;
				var cant_items = cant_tri * 3;
				if (cant_items > 0)
				{

					if (mostrar_tools || !subset[i].image_name.StartsWith("TOOLS"))
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
			EffectMesh.CurrentTechnique = EffectMesh.Techniques["Map"];
			for (var L = 0; L < 1; ++L)
			{
				for (var i = 0; i < cant_modelos; ++i)
				{
					Matrix world = modelos[i].world();
					CSMDModel p_mesh = mesh_pool.meshes[modelos[i].nro_mesh];
					p_mesh.Draw(device, EffectMesh, world, View, Proj, L);
				}
			}


			BeginDrawImage();
			int W = device.Viewport.Width;
			int H = device.Viewport.Height;
			// dibujo la posicion del cero 
			device.BlendState = BlendState.Additive;
			DrawImage(0,W/2, H/2,20,20);

			// dibujo la pos. de los enemigos
			Matrix T = World * View * Proj;
			for (int i = 0; i < enemigo.Length; ++i)
            {
				Vector4 v = Vector4.Transform(enemigo[i].Position, T);
				if (v.W > 0)
				{
					float x = v.X / v.W;
					float y = v.Y / v.W;
					float d = x*x + y*y;
					if (d < 0.9f)
					{
						int xs = (int)(W * (0.5f + x / 2));
						int ys = (int)(H * (0.5 - y / 2));
						DrawImage(2, xs - 5, ys - 5, 10, 10);
					}
				}
			}
			device.Viewport = ant_vp;
			device.DepthStencilState = DepthStencilState.Default;


		}

		public void BeginDrawImage()
        {
			device.SetVertexBuffer(spriteVertexBuffer);
			Effect.CurrentTechnique = Effect.Techniques["DrawImage"];
		}

		public void DrawImage(int nro_imagen,int x,int y,int dx,int dy)
        {
			if (images[nro_imagen] == null)
				return;
			
			// por un tema de performance ya tienen que estar definidos de antes
			// device.SetVertexBuffer(spriteVertexBuffer); 
			// Effect.CurrentTechnique = Effect.Techniques["DrawImage"];

			int W = device.Viewport.Width;
			int H = device.Viewport.Height;
			float ex = 1.0f / (float)W;
			float ey = 1.0f / (float)H;
			Matrix world = new Matrix(
						dx*ex, 0, 0, 0,
						0, -dy*ey, 0, 0,
						0, 0, 1, 0,
						x*ex*2-1, 1-y*ey*2, 0, 1);
			
			Effect.Parameters["World"].SetValue(world);
			Effect.Parameters["ModelTexture"].SetValue(images[nro_imagen]);
			foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 6);
            }
		}

		// experimento ray - tracing
		public float intersectSegment(Vector3 p, Vector3 q , out ip_data hitinfo)
		{
			hitinfo = new ip_data();
			hitinfo.nro_face = -1;
			/*
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
			*/


			// test kd tree
			float min_t = 10000;
			Vector3 dir = q - p;
			float dist = dir.Length();
			dir.Normalize();
			if(kd_tree.ray_intersects(p, dir,out ip_data Ip))
            {
				float t = Ip.t / dist;
				if (t <= 1)
				{
					min_t = t;
					hitinfo = Ip;
				}
            }

			return min_t;
		}


	}
}
