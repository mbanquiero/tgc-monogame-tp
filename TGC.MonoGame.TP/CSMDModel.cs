using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{
	public class smd_bone
	{
		public int id;
		public int parent;
		public String name;
		public Vector3 startPosition;
		public Vector3 startRotation;
		public Matrix matInversePose;
		public Vector3 Position;
		public Matrix Transform;
	};

	public class smd_bone_anim
	{
		public int id_bone;
		public Vector3 Position;
		public Vector3 Rotation;
	};

	public class smd_frame
	{
		public int cant_bone_animations;
		public smd_bone_anim[]bone_animations = new smd_bone_anim[CSMDModel.MAX_BONES];
	};


	public class smd_animation
	{
		public String name;
		public int cant_frames;
		public smd_frame[]frames = new smd_frame[CSMDModel.MAX_FRAMES];
	};

	public struct smd_vertex
	{
		public Vector3 Position;
		public Vector2 TextureCoordinate;
		public Vector3 Normal;
		public Vector3 Tangent;
		public Vector3 Binormal;
		public Vector4 BlendWeight;
		public Vector4 BlendIndices;

		public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement[]
			{
						new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
						new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate,0),
						new VertexElement(20, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
						new VertexElement(32, VertexElementFormat.Vector3, VertexElementUsage.Tangent,0),
						new VertexElement(44, VertexElementFormat.Vector3, VertexElementUsage.Binormal,0),
						new VertexElement(56, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight,0),
						new VertexElement(72, VertexElementFormat.Vector4, VertexElementUsage.BlendIndices,0)
			});


	};


	public class smd_subset
	{
		public string name;
		public int pos;
		public int cant_items;
		public string image_name;
		public bool traslucido;

		public smd_subset(string s)
        {
			name = s;
        }
	};


	public class CSMDModel
	{
		public const int MAX_BONES = 100;
		public const int MAX_FRAMES = 256;
		public int cant_bones;
		public smd_bone[] bones;
		public smd_animation anim1;
		public smd_animation p_anim;
		public int cur_frame;
		public Matrix[] matBoneSpace = new Matrix[MAX_BONES];

		public string smd_folder;
		public int cant_cdmaterials;
		public String[] cdmaterials;

		public Matrix matWorld;
		public Vector3 p_min;
		public Vector3 p_max;
		public Vector3 size;
		public Vector3 cg;
		public int cant_subsets;
		public smd_subset[] subset;
		public Texture2D[] texture;
		public string folder;
		public string name;
		public bsp_face[] faces;
		public int cant_faces;


		public GraphicsDevice device;
		public ContentManager Content;
		public VertexBuffer VertexBuffer;

		public static CDebugBox debug_box = null;
		public Effect debugEffect;


		public CSMDModel(string fname, GraphicsDevice p_device, ContentManager p_content, string p_folder)
		{

			name = fname;
			folder = p_folder;
			device = p_device;
			Content = p_content;
			matWorld = Matrix.Identity;

			if (debug_box == null)
				debug_box = new CDebugBox(p_device);

			cant_bones = 0;
			bones = new smd_bone[MAX_BONES];
			cant_cdmaterials = 0;
			cdmaterials = new string[10];

			cargar(fname);
		}


		public bool cargar(String name)
        {
			String smd_name = null;
			smd_folder = Path.GetDirectoryName(name)+"\\";
			// cargo el archivo .qc
			var fp = new System.IO.StreamReader(name);
			while (!fp.EndOfStream)
			{
				var s = fp.ReadLine().TrimStart();
				if(s.StartsWith("$cdmaterials"))
                {
					// $cdmaterials "models\weapons\V_models\rif_fg42\"
					cdmaterials[cant_cdmaterials++] = s.Substring(14).Replace("\"","");
				}
				else
				if (s.StartsWith("studio"))
				{
					// studio "ak47-1"
					smd_name = s.Substring(8).Replace("\"", "");
				}
			}

			var rta = false;
			if(smd_name!=null)
            {
				rta = cargar_smd(smd_folder+smd_name);
            }

			return rta;
        }


		public bool cargar_smd(String name)
		{
			var fp = new System.IO.StreamReader(name);

			var buffer = fp.ReadLine().TrimStart();
			while (!buffer.StartsWith("nodes"))
				buffer = fp.ReadLine().TrimStart();
	
			// huesos
			buffer = fp.ReadLine().TrimStart();
			while(!buffer.StartsWith("end"))
			{
				// 0 "L_Armdummy" -1
				string[] tokens = buffer.Split(' ');
				bones[cant_bones] = new smd_bone();
				bones[cant_bones].id = int.Parse(tokens[0]);
				bones[cant_bones].name = tokens[1];
				bones[cant_bones].parent = int.Parse(tokens[2]);
				++cant_bones;
				buffer = fp.ReadLine().TrimStart();
			}

			// skeleton
			fp.ReadLine().TrimStart();
			// time 0
			fp.ReadLine().TrimStart();
			buffer = fp.ReadLine().TrimStart();
			while (!buffer.StartsWith("end"))
			{
				string[] tokens = buffer.Split(' ');
				int id = int.Parse(tokens[0]);
				if (id >= 0 && id < cant_bones)
				{
					bones[id].startPosition.X = atof(tokens[1]);
					bones[id].startPosition.Y = atof(tokens[2]);
					bones[id].startPosition.Z = atof(tokens[3]);
					bones[id].startRotation.X = atof(tokens[4]);
					bones[id].startRotation.Y = atof(tokens[5]);
					bones[id].startRotation.Z = atof(tokens[6]);
				}
				buffer = fp.ReadLine().TrimStart();
			}

			// triangles y subsets
			cant_subsets = 0;
			subset = new smd_subset[100];
			smd_vertex[] vertices = new smd_vertex[65535];
			int cant_v = 0;
			float min_x = 1000000;
			float min_y = 1000000;
			float min_z = 1000000;
			float max_x = -1000000;
			float max_y = -1000000;
			float max_z = -1000000;

			fp.ReadLine().TrimStart();
			buffer = fp.ReadLine().TrimStart();
			smd_subset cur_subset = null;
			while (!buffer.StartsWith("end"))
			{
				// material
				var mat = Path.GetFileNameWithoutExtension(buffer);
				if(que_subset(mat)==-1)
                {
					// agrego el subset
					cur_subset = subset[cant_subsets++] = new smd_subset(mat);
				}


				// 3 vertices
				// 35 4.564999 -13.692499 -4.852700 -0.237700 0.289800 0.927100 0.498400 0.279000
				for (int i = 0; i < 3; ++i)
				{
					buffer = fp.ReadLine().TrimStart();
					string[] tokens = buffer.Split(' ');
					int id = int.Parse(tokens[0]);

					var x = vertices[cant_v].Position.X = atof(tokens[1]);
					var y = vertices[cant_v].Position.Y = atof(tokens[2]);
					var z = vertices[cant_v].Position.Z = atof(tokens[3]);
					vertices[cant_v].Normal.X = atof(tokens[4]);
					vertices[cant_v].Normal.Y = atof(tokens[5]);
					vertices[cant_v].Normal.Z = atof(tokens[6]);
					vertices[cant_v].TextureCoordinate.X = atof(tokens[7]);
					vertices[cant_v].TextureCoordinate.Y = atof(tokens[8]);
					vertices[cant_v].BlendIndices.X = id;
					vertices[cant_v].BlendWeight.X = 1;
					++cant_v;

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
				cur_subset.cant_items++;
				buffer = fp.ReadLine().TrimStart();
			}
			fp.Close();
			setupBones();

			// actualizo el bounding box
			p_min = new Vector3(min_x, min_y, min_z);
			p_max = new Vector3(max_x, max_y, max_z);
			size = new Vector3(max_x - min_x, max_y - min_y, max_z - min_z);
			cg = p_min + size * 0.5f;

			// Creo el vertex buffer
			VertexBuffer = new VertexBuffer(device, smd_vertex.VertexDeclaration, cant_v, BufferUsage.WriteOnly);
			VertexBuffer.SetData(vertices,0,cant_v);

			// almaceno los faces a los efectos de colision
			faces = new bsp_face[cant_v / 3];
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
			return true;
		}

		public smd_animation cargar_ani(String name)
		{
			var anim = new smd_animation();
			anim.name = name;
			anim.cant_frames = 0;

			var fp = new System.IO.StreamReader(name);
			fp.ReadLine().TrimStart();          // version 1
			fp.ReadLine().TrimStart();          // nodes
			var buffer = fp.ReadLine().TrimStart();
			while (!buffer.StartsWith("skeleton"))
				buffer = fp.ReadLine().TrimStart();


			fp.ReadLine().TrimStart();					// time 0

			smd_frame frame = anim.frames[anim.cant_frames++] = new smd_frame();
			frame.cant_bone_animations = 0;

			buffer = fp.ReadLine().TrimStart();          // primer item 
			while (!buffer.StartsWith("end"))
			{
				string[] tokens = buffer.Split(' ');
				int id = int.Parse(tokens[0]);
				if (id >= 0 && id < cant_bones)
				{
					smd_bone_anim bone_anim = frame.bone_animations[frame.cant_bone_animations++] = new smd_bone_anim();
					bone_anim.id_bone = id;
					bone_anim.Position.X = atof(tokens[1]);
					bone_anim.Position.Y = atof(tokens[2]);
					bone_anim.Position.Z = atof(tokens[3]);
					bone_anim.Rotation.X = atof(tokens[4]);
					bone_anim.Rotation.Y = atof(tokens[5]);
					bone_anim.Rotation.Z = atof(tokens[6]);
				}
				buffer = fp.ReadLine().TrimStart();
				if (buffer.StartsWith("time"))
				{
					frame = anim.frames[anim.cant_frames++] = new smd_frame();
					frame.cant_bone_animations = 0;
					buffer = fp.ReadLine().TrimStart();
				}
			}
			fp.Close();
			return anim;
		}


		public Matrix traslation(Vector3 t)
        {
			Matrix C;
			C.M11 = 1; C.M12 = 0; C.M13 = 0; C.M14 = t.X;
			C.M21 = 0; C.M22 = 1; C.M23 = 0; C.M24 = t.Y;
			C.M31 = 0; C.M32 = 0; C.M33 = 1; C.M34 = t.Z;
			C.M41 = 0; C.M42 = 0; C.M43 = 0; C.M44 = 1;
			return C;
		}

		public Matrix rotation(Vector3 rot)
		{
			// lo paso a quaternion
			float an_x = rot.Y * 0.5f;
			float an_y = rot.Z * 0.5f;
			float an_z = rot.X * 0.5f;
			float sy = MathF.Sin(an_y);
			float cy = MathF.Cos(an_y);
			float sp = MathF.Sin(an_x);
			float cp = MathF.Cos(an_x);
			float sr = MathF.Sin(an_z);
			float cr = MathF.Cos(an_z);
			float srXcp = sr * cp, crXsp = cr * sp;
			float x = srXcp * cy - crXsp * sy; // X
			float y = crXsp * cy + srXcp * sy; // Y
			float crXcp = cr * cp, srXsp = sr * sp;
			float z = crXcp * sy - srXsp * cy; // Z
			float w = crXcp * cy + srXsp * sy; // W (real component)

			Matrix C;
			C.M11 = 1.0f - 2.0f * y * y - 2.0f * z * z;
			C.M21 = 2.0f * x * y + 2.0f * w * z;
			C.M31 = 2.0f * x * z - 2.0f * w * y;

			C.M12 = 2.0f * x * y - 2.0f * w * z;
			C.M22 = 1.0f - 2.0f * x * x - 2.0f * z * z;
			C.M32 = 2.0f * y * z + 2.0f * w * x;

			C.M13 = 2.0f * x * z + 2.0f * w * y;
			C.M23 = 2.0f * y * z - 2.0f * w * x;
			C.M33 = 1.0f - 2.0f * x * x - 2.0f * y * y;


			C.M41 = 0; C.M42 = 0; C.M43 = 0; C.M44 = 1;
			C.M14 = 0; C.M24 = 0; C.M34 = 0;

			return C;
		}


		Vector3 transform(Vector3 p, Matrix T)
		{
			Vector3 r;
			r.X = T.M11 * p.X + T.M12 * p.Y + T.M13 * p.Z + T.M14;
			r.Y = T.M21 * p.X + T.M22 * p.Y + T.M23 * p.Z + T.M24;
			r.Z = T.M31 * p.X + T.M32 * p.Y + T.M33 * p.Z + T.M34;
			return r;
		}


		public void setupBones()
		{
			for (int i = 0; i < cant_bones; ++i)
			{
				Matrix T = traslation(bones[i].startPosition) * rotation(bones[i].startRotation);
				int k = bones[i].parent;
				if (k != -1)
				{
					T = bones[k].Transform * T;
				}
				bones[i].Transform = T;
				bones[i].Position = transform(Vector3.Zero, T);

				// esta inversa permite llevar del word al espacio del hueso
				bones[i].matInversePose = Matrix.Invert(T);
			}
		}

		public void setAnimation(smd_animation anim)
		{
			p_anim = anim;
			updatesBones(0);
		}

		public void updatesBones(int frame)
		{
			cur_frame = frame = frame % p_anim.cant_frames;
			for (int i = 0; i < p_anim.frames[frame].cant_bone_animations; ++i)
			{
				smd_bone_anim p_bone_anim = p_anim.frames[frame].bone_animations[i];
				int id = p_bone_anim.id_bone;
				Matrix T = traslation(p_bone_anim.Position) * rotation(p_bone_anim.Rotation);
				int k = bones[id].parent;
				if (k != -1)
				{
					T = bones[k].Transform * T;
				}
				bones[id].Transform = T;
				bones[id].Position = transform(Vector3.Zero, T);
			}

		}


		public void initTextures()
		{
			texture = new Texture2D[cant_subsets];
			for (var i = 0; i < cant_subsets; ++i)
			{

				// busco en que directorio puede esta la textura
				bool found = false;
				for(var j=0;j<cant_cdmaterials && !found;++j)
                {
					string fname = CBspFile.tex_folder + cdmaterials[j] + subset[i].name + ".vmt";
					if(File.Exists(fname))
                    {
						found = true;
						subset[i].image_name = cdmaterials[j] + subset[i].name;
                    }
                }

				if (!found)
					continue;

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

		public void updateMeshVertices()
		{
			//Precalcular la multiplicación para llevar a un vertice a Bone-Space y luego transformarlo segun el hueso
			for (var i = 0; i < cant_bones; i++)
			{
				matBoneSpace[i] = Matrix.Transpose(bones[i].Transform * bones[i].matInversePose);
			}
		}


		public void Draw(GraphicsDevice graphicsDevice, Effect Effect,Matrix World, Matrix View, Matrix Proj, int L = 0)
		{
			graphicsDevice.SetVertexBuffer(VertexBuffer);
			Effect.Parameters["World"].SetValue(World);
			Effect.Parameters["View"].SetValue(View);
			Effect.Parameters["Projection"].SetValue(Proj);

			updateMeshVertices();
			Effect.Parameters["bonesMatWorldArray"].SetValue(matBoneSpace);

			/*
			Vector3 s = new Vector3(1, 1, 1) * 0.1f;
			for (int i = 0; i < cant_bones; ++i)
			{
				debug_box.Draw(device, bones[i].Position - s, bones[i].Position + s, debugEffect, World, View, Proj);
				int k = bones[i].parent;
				if (k != -1)
				{
					CDebugLine.Draw(graphicsDevice, bones[k].Position, bones[i].Position, debugEffect, World, View, Proj);
				}
			}*/

			// L ==0  opacos , ==1 traslucidos
			for (var i = 0; i < cant_subsets; i++)
			{
				var cant_items = subset[i].cant_items;
				if (cant_items > 0 && subset[i].traslucido == (L == 1))
				{
					var pos = subset[i].pos;
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


		// helper
		public float atof(string s)
		{
			return float.Parse(s, CultureInfo.InvariantCulture.NumberFormat);
		}

		public int que_subset(string s)
        {
			int rta = -1;
			for (int i = 0; i < cant_subsets && rta == -1; ++i)
				if (subset[i].name == s)
					rta = i;
			return rta;
        }

	}
}
