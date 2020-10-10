using System;
using System.Globalization;
using System.IO;
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
		public Vector3 Position;
		public Vector3 Rotation;
	};

	public class smd_frame
	{
		public smd_bone_anim[]bone_animations = new smd_bone_anim[CSMDModel.MAX_BONES];
	};


	public class smd_animation
	{
		public String name;
		public int cant_frames;
		public float frameRate = 30.0f;
		public bool in_site = false;
		public bool loop = true;
		public bool finished = false;
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
		public const int MAX_ANIMATION = 100;
		public const int MAX_BONES = 100;
		public const int MAX_FRAMES = 256;
		public int cant_bones;
		public smd_bone[] bones;
		public int cant_animations = 0;
		public smd_animation []anim = new smd_animation[MAX_ANIMATION];
		public int cur_anim;
		public int cur_frame;
		public Matrix[] matBoneSpace = new Matrix[MAX_BONES];
		public float currentTime = 0f;
		public float speed;
		public int w_attachment = 0;


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

		// metrica
		public Matrix Metric = new Matrix(	0, -1, 0, 0,
											0, 0,  1, 0,
											1, 0, 0, 0,
											0 ,0 , 0,1);
		public Matrix invMetric = new Matrix(	0, 0,  1, 0,
												-1, 0, 0, 0,
												0, 1, 0 , 0,
												0 , 0, 0, 1);


		public CSMDModel(string fname, GraphicsDevice p_device, ContentManager p_content, string p_folder)
		{

			name = fname;
			folder = p_folder + "smd\\";
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
			string smd_name = null;
			string smd_folder = Path.GetDirectoryName(name);
			// cargo el archivo .qc
			var fp = new System.IO.StreamReader(folder+name+".qc");
			while (!fp.EndOfStream)
			{
				var s = fp.ReadLine().TrimStart();
				if(s.StartsWith("$cdmaterials"))
                {
					// $cdmaterials "models\weapons\V_models\rif_fg42\"
					cdmaterials[cant_cdmaterials++] = s.Substring(14).Replace("\"","");
				}
				else
				if (s.StartsWith("studio") && smd_name==null)
				{
					// studio "ak47-1"
					smd_name = smd_folder+"\\"+s.Substring(8).Replace("\"", "");
				}
			}

			var rta = false;
			if(smd_name!=null)
            {
				rta = cargar_smd(folder+smd_name);
            }

			// cargo las animaciones


			return rta;
        }


		public bool cargar_smd(String fname)
		{
			var fp = new System.IO.StreamReader(fname);

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

					Vector3 P = transform(new Vector3(atof(tokens[1]), atof(tokens[2]), atof(tokens[3])), Metric);
					Vector3 N = transform(new Vector3(atof(tokens[4]), atof(tokens[5]), atof(tokens[6])), Metric);

					vertices[cant_v].Position.X = P.X;
					vertices[cant_v].Position.Y = P.Y;
					vertices[cant_v].Position.Z = P.Z;
					vertices[cant_v].Normal.X = N.X;
					vertices[cant_v].Normal.Y = N.Y;
					vertices[cant_v].Normal.Z = N.Z;
					vertices[cant_v].TextureCoordinate.X = atof(tokens[7]);
					vertices[cant_v].TextureCoordinate.Y = -atof(tokens[8]);
					vertices[cant_v].BlendIndices.X = id;
					vertices[cant_v].BlendWeight.X = 1;
					++cant_v;

					if (P.X < min_x)
						min_x = P.X;
					if (P.Y < min_y)
						min_y = P.Y;
					if (P.Z < min_z)
						min_z = P.Z;
					if (P.X > max_x)
						max_x = P.X;
					if (P.Y > max_y)
						max_y = P.Y;
					if (P.Z > max_z)
						max_z = P.Z;

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

		public void cargar_ani(String ani_folder, String ani_name)
		{
			smd_animation p_anim = anim[cur_anim = cant_animations++] = new smd_animation();
			p_anim.name = ani_name;
			p_anim.cant_frames = 0;

			var fp = new System.IO.StreamReader(folder+ ani_folder+ "\\" + ani_name+".smd");
			fp.ReadLine().TrimStart();          // version 1
			fp.ReadLine().TrimStart();          // nodes
			var buffer = fp.ReadLine().TrimStart();
			while (!buffer.StartsWith("skeleton"))
				buffer = fp.ReadLine().TrimStart();


			fp.ReadLine().TrimStart();					// time 0

			smd_frame frame = p_anim.frames[p_anim.cant_frames++] = new smd_frame();

			buffer = fp.ReadLine().TrimStart();          // primer item 
			while (!buffer.StartsWith("end"))
			{
				string[] tokens = buffer.Split(' ');
				int id = int.Parse(tokens[0]);
				if (id >= 0 && id < cant_bones)
				{
					smd_bone_anim bone_anim = frame.bone_animations[id] = new smd_bone_anim();
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
					// creo el nuevo frame
					frame = p_anim.frames[p_anim.cant_frames++] = new smd_frame();
					buffer = fp.ReadLine().TrimStart();
				}
			}

			// completo los frames que faltan (si fue creado con el crawbar no faltara ninguno)
			// para ello hereda los frame anterior
			// de paso computo la distancia total para aprximar la velocidad 
			float dm = 0;
			for (var f=0;f<p_anim.cant_frames;++f)
			{
				for (var i = 0; i < cant_bones; ++i)
				{
					if (p_anim.frames[f].bone_animations[i] == null)
					{
						smd_bone_anim bone_anim = p_anim.frames[f].bone_animations[i] = new smd_bone_anim();
						if (f>0)
						{
							// heredo del anterior
							bone_anim.Position = p_anim.frames[f - 1].bone_animations[i].Position;
							bone_anim.Rotation = p_anim.frames[f - 1].bone_animations[i].Rotation;
						}
						else
						{
							// cero x defecto
							bone_anim.Position = Vector3.Zero;
							bone_anim.Rotation = Vector3.Zero;
						}
					}
				}
				float dist = (p_anim.frames[0].bone_animations[0].Position - p_anim.frames[f].bone_animations[0].Position).Length();
				if (dist > dm)
					dm = dist;

			}

			float time = p_anim.cant_frames / p_anim.frameRate;
			speed = dm / time;

			fp.Close();
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


		public Quaternion toQuaternion(Vector3 rot)
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
			return new Quaternion(x, y, z, w);
		}

		public Matrix toMatrix(Quaternion q)
		{
			var x = q.X;
			var y = q.Y;
			var z = q.Z;
			var w = q.W;
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


		public Matrix rotation(Vector3 rot)
		{
			return toMatrix(toQuaternion(rot));
		}


		public Matrix rotation2(Vector3 rot)
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
		
		public void update(float elapsedTime)
		{
			currentTime += elapsedTime;
			updateSkeleton();
		}
		
		public void setAnimation(int n)
		{
			if(n>=0 && n<cant_animations)
			{
				cur_anim = n;
				smd_animation p_anim = anim[cur_anim];
				if (p_anim == null)
					return;
				updateSkeleton();
			}
		}

		public void updateSkeleton(	)
		{
			smd_animation p_anim = anim[cur_anim];
			if (p_anim == null)
				return;

			float currentFrameF = currentTime * p_anim.frameRate;
			float resto = currentFrameF - MathF.Floor(currentFrameF);
			int frame1 = (int)MathF.Floor(currentFrameF);
			int frame2 = frame1 + 1;
			p_anim.finished = false;

			if (p_anim.loop)
			{
				frame1 = frame1 % p_anim.cant_frames;
				frame2 = frame2 % p_anim.cant_frames;
			}
			else
			{
				if (frame1 > p_anim.cant_frames - 1)
				{
					frame1 = p_anim.cant_frames - 1;
					p_anim.finished = true;
				}
				if (frame2 > p_anim.cant_frames - 1)
					frame2 = p_anim.cant_frames - 1;
			}

			for (int i = 0; i < cant_bones; ++i)
			{
				smd_bone_anim p_bone_anim1 = p_anim.frames[frame1].bone_animations[i];
				smd_bone_anim p_bone_anim2 = p_anim.frames[frame2].bone_animations[i];
				Vector3 Position = i==0 && p_anim.in_site ?
					p_anim.frames[0].bone_animations[0].Position : 
					p_bone_anim1.Position * (1-resto) + p_bone_anim2.Position * resto;
				Quaternion q1 = toQuaternion(p_bone_anim1.Rotation);
				Quaternion q2 = toQuaternion(p_bone_anim2.Rotation);
				Quaternion q = Quaternion.Lerp(q1, q2, resto);
				Matrix T = traslation(Position) * toMatrix(q);
				int k = bones[i].parent;
				if (k != -1)
				{
					T = bones[k].Transform * T;
				}
				bones[i].Transform = T;
				bones[i].Position = transform(Vector3.Zero, T);
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
				matBoneSpace[i] = Matrix.Transpose(Metric * bones[i].Transform * bones[i].matInversePose * invMetric);
			}
		}



		public void Draw(GraphicsDevice graphicsDevice, Effect Effect,Matrix World, Matrix View, Matrix Proj, int L = 0)
		{
			graphicsDevice.SetVertexBuffer(VertexBuffer);
			Effect.Parameters["World"].SetValue(World);
			Effect.Parameters["View"].SetValue(View);
			Effect.Parameters["Projection"].SetValue(Proj);
			smd_animation p_anim = anim[cur_anim];
			if (p_anim!=null)
            {
				// modelo animado
				updateMeshVertices();
				Effect.Parameters["bonesMatWorldArray"].SetValue(matBoneSpace);
			}



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


			/*
			Vector3 s = new Vector3(1, 1, 1) * 0.1f;
			if (debugEffect != null)
				for (int i = 0; i < cant_bones; ++i)
				{
					var p0 = Vector3.Transform(bones[i].Position, invMetric);
					debug_box.Draw(device, p0 - s, p0 + s, debugEffect, World, View, Proj);
					int k = bones[i].parent;
					if (k != -1)
					{
						var p1 = Vector3.Transform(bones[k].Position, invMetric);
						CDebugLine.Draw(graphicsDevice, p0, p1, debugEffect, World, View, Proj);
					}
				}
			*/
			/*
			if (debugEffect != null)
			{
				Vector3 s = new Vector3(1, 1, 1) * 20;
				int cant_hit_points = 2;
				int[] hit_pt = { 0, 12 };
				for (int i = 0; i < cant_hit_points; ++i)
				{
					var p0 = Vector3.Transform(bones[hit_pt[i]].Position, invMetric);
					debug_box.Draw(device, p0 - s, p0 + s, debugEffect, World, View, Proj);
				}
			}*/

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
