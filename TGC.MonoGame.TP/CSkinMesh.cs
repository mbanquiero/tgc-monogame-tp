using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP
{
	public class sm_skeletal_bone
	{
		public int id = -1;
		public string name = "";
		public int parentId = -1;
		public Vector3 startPosition = new Vector3(0);
		public Quaternion startRotation = new Quaternion();
		public Matrix matLocal = Matrix.Identity;
		public Matrix matFinal = Matrix.Identity;
		public Matrix matInversePose = Matrix.Identity;

	};


	public struct sm_vertex
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

	public struct sm_subset
	{
		public int cant_items;
		public string image_name;
	}


	public class sm_bone_animation_frame
	{
		public int nro_frame;
		public Vector3 Position;
		public Quaternion Rotation;
	};

	public class sm_bone_animation
	{

		public int cant_frames;
		public sm_bone_animation_frame[] frame;
	}

	public class sm_animacion
	{
		public string name;
		public int bone_id;
		public int cant_frames;
		public int frame_rate;
		public int cant_bones;
		public sm_bone_animation[] bone_animation;
	}


	public class CSkinMesh
    {
		public static string content_folder = "C:\\monogames\\tp\\TGC.MonoGame.TP\\Content\\";
		public const int MAX_BONES = 26;
		public const int MAX_FRAMES_X_BONE = 30;
		public const int MAX_ANIMATION_X_MESH = 30;

		public Matrix matWorld;
		public int cant_bones;
		public sm_skeletal_bone[] bones;
		public Vector3 p_min;
		public Vector3 p_max;
		public Vector3 size;
		public Vector3 cg;
		public int cant_subsets;
		public sm_subset[] subset;
		public int cant_animaciones;
		public sm_animacion[] animacion;
		public Matrix[] matBoneSpace;
		public Texture2D[] texture;



		public bool animating;
		public int currentAnimation;
		public bool playLoop;
		public float currentTime;
		public int currentFrame;
		public int frameRate;
		public float animationTimeLenght;



		public VertexBuffer VertexBuffer;
		public GraphicsDevice device;
		public Effect Effect;
		public ContentManager Content;


		public CSkinMesh(string fname, GraphicsDevice p_device, ContentManager p_content)
		{
			device = p_device;
			Content = p_content;
			matWorld = Matrix.Identity;

			Effect = Content.Load<Effect>("Effects/SkinMesh");

			var fp = new FileStream(content_folder+ "skmesh\\" + fname, FileMode.Open, FileAccess.Read);
			var arrayByte = new byte[(int)fp.Length];
			fp.Read(arrayByte, 0, (int)fp.Length);
			fp.Close();

			initMeshFromData(arrayByte);

		}


		// Setup inicial del esqueleto
		public void setupSkeleton()
		{
			//Actualizar jerarquia
			for (var i = 0; i < cant_bones; i++)
			{
				//Debug.WriteLine("Hueso" + bones[i].name);
				//Debug.WriteLine("------");

				var bone = this.bones[i];
				var parent_id = bone.parentId;
				if (parent_id == -1)
					bone.matFinal = bone.matLocal * Matrix.Identity;
				else
					bone.matFinal = bone.matLocal * bones[parent_id].matFinal;
				//Almacenar la inversa de la posicion original del hueso, para la referencia inicial de los vertices
				bone.matInversePose = Matrix.Invert(bone.matFinal);
			}
		}

		// helper 
		public string getString(byte[] arrayByte, int offset, int len)
		{
			var s = "";
			for (var j = 0; j < len; ++j)
			{
				int code = (int)BitConverter.ToSingle(arrayByte, offset);
				offset += 4;
				s += (char)code;
			}
			return s.Trim('\0');

		}
		public void initMeshFromData(byte[] arrayByte)
		{

			var t = 0;
			int cant_v = (int)BitConverter.ToSingle(arrayByte, t); t += 4;
			float min_x = 1000000;
			float min_y = 1000000;
			float min_z = 1000000;
			float max_x = -1000000;
			float max_y = -1000000;
			float max_z = -1000000;

			sm_vertex[] vertices = new sm_vertex[cant_v];


			for (var i = 0; i < cant_v; ++i)
			{
				var x = vertices[i].Position.X = BitConverter.ToSingle(arrayByte, t); t += 4;
				var y = vertices[i].Position.Y = BitConverter.ToSingle(arrayByte, t); t += 4;
				var z = vertices[i].Position.Z = BitConverter.ToSingle(arrayByte, t); t += 4;

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

			for (int i = 0; i < cant_v; ++i)
			{
				vertices[i].Normal.X = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].Normal.Y = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].Normal.Z = BitConverter.ToSingle(arrayByte, t); t += 4;
			}

			for (int i = 0; i < cant_v; ++i)
			{
				vertices[i].Tangent.X = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].Tangent.Y = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].Tangent.Z = BitConverter.ToSingle(arrayByte, t); t += 4;
			}

			for (int i = 0; i < cant_v; ++i)
			{
				vertices[i].Binormal.X = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].Binormal.Y = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].Binormal.Z = BitConverter.ToSingle(arrayByte, t); t += 4;
			}

			for (int i = 0; i < cant_v; ++i)
			{
				vertices[i].TextureCoordinate.X = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].TextureCoordinate.Y = BitConverter.ToSingle(arrayByte, t); t += 4;
			}

			for (int i = 0; i < cant_v; ++i)
			{
				vertices[i].BlendWeight.X = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].BlendWeight.Y = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].BlendWeight.Z = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].BlendWeight.W = BitConverter.ToSingle(arrayByte, t); t += 4;
			}

			for (int i = 0; i < cant_v; ++i)
			{
				vertices[i].BlendIndices.X = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].BlendIndices.Y = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].BlendIndices.Z = BitConverter.ToSingle(arrayByte, t); t += 4;
				vertices[i].BlendIndices.W = BitConverter.ToSingle(arrayByte, t); t += 4;
			}


			VertexBuffer = new VertexBuffer(device, sm_vertex.VertexDeclaration, cant_v, BufferUsage.WriteOnly);
			VertexBuffer.SetData(vertices);


			// estructura de sub-setes
			cant_subsets = (int)BitConverter.ToSingle(arrayByte, t); t += 4;
			subset = new sm_subset[cant_subsets];
			for (int i = 0; i < cant_subsets; ++i)
			{
				int cant_items = (int)BitConverter.ToSingle(arrayByte, t); t += 4;
				subset[i].cant_items = cant_items / 3;
				subset[i].image_name = getString(arrayByte, t, 256); t += 4 * 256;
			}

			// estructura de huesos
			cant_bones = (int)BitConverter.ToSingle(arrayByte, t); t += 4;
			bones = new sm_skeletal_bone[cant_bones];
			for (int i = 0; i < cant_bones; ++i)
			{
				bones[i] = new sm_skeletal_bone();
				bones[i].id = (int)BitConverter.ToSingle(arrayByte, t); t += 4;
				bones[i].parentId = (int)BitConverter.ToSingle(arrayByte, t); t += 4;
				float X = BitConverter.ToSingle(arrayByte, t); t += 4;
				float Y = BitConverter.ToSingle(arrayByte, t); t += 4;
				float Z = BitConverter.ToSingle(arrayByte, t); t += 4;
				bones[i].startPosition = new Vector3(X, Y, Z);
				X = BitConverter.ToSingle(arrayByte, t); t += 4;
				Y = BitConverter.ToSingle(arrayByte, t); t += 4;
				Z = BitConverter.ToSingle(arrayByte, t); t += 4;
				float W = BitConverter.ToSingle(arrayByte, t); t += 4;
				bones[i].startRotation = new Quaternion(X, Y, Z, W);
				bones[i].name = getString(arrayByte, t, 32); t += 4 * 32;
				// Computo la matriz local en base a la orientacion del cuaternion y la traslacion
				bones[i].matLocal = Matrix.CreateFromQuaternion(bones[i].startRotation) * Matrix.CreateTranslation(bones[i].startPosition);
			}

			// ANIMACIONES
			cant_animaciones = (int)BitConverter.ToSingle(arrayByte, t); t += 4;
			animacion = new sm_animacion[cant_animaciones];
			for (int i = 0; i < cant_animaciones; ++i)
			{
				animacion[i] = new sm_animacion();
				animacion[i].bone_id = (int)BitConverter.ToSingle(arrayByte, t); t += 4;
				animacion[i].cant_frames = (int)BitConverter.ToSingle(arrayByte, t); t += 4;
				animacion[i].frame_rate = (int)BitConverter.ToSingle(arrayByte, t); t += 4;
				animacion[i].cant_bones = (int)BitConverter.ToSingle(arrayByte, t); t += 4;
				animacion[i].bone_animation = new sm_bone_animation[MAX_BONES];
				for (var j = 0; j < MAX_BONES; ++j)
				{
					animacion[i].bone_animation[j] = new sm_bone_animation();
					var bone_animation = animacion[i].bone_animation[j];
					bone_animation.cant_frames = (int)BitConverter.ToSingle(arrayByte, t); t += 4;
					bone_animation.frame = new sm_bone_animation_frame[MAX_FRAMES_X_BONE];
					for (var k = 0; k < MAX_FRAMES_X_BONE; ++k)
					{
						var frame = bone_animation.frame[k] = new sm_bone_animation_frame();
						frame.nro_frame = (int)BitConverter.ToSingle(arrayByte, t); t += 4;
						float X = BitConverter.ToSingle(arrayByte, t); t += 4;
						float Y = BitConverter.ToSingle(arrayByte, t); t += 4;
						float Z = BitConverter.ToSingle(arrayByte, t); t += 4;
						frame.Position = new Vector3(X, Y, Z);
						X = BitConverter.ToSingle(arrayByte, t); t += 4;
						Y = BitConverter.ToSingle(arrayByte, t); t += 4;
						Z = BitConverter.ToSingle(arrayByte, t); t += 4;
						float W = BitConverter.ToSingle(arrayByte, t); t += 4;
						frame.Rotation = new Quaternion(X, Y, Z, W);
					}
				}
				animacion[i].name = getString(arrayByte, t, 32); t += 4 * 32;

			}

			//Matrices final de transformacion de cada ueso
			matBoneSpace = new Matrix[MAX_BONES];
			for (var i = 0; i < MAX_BONES; ++i)
				matBoneSpace[i] = Matrix.Identity;

			initTextures();

			// Setup inicial del esqueleto
			setupSkeleton();

			// prendo la primer animacion
			initAnimation(8, true, 30);

		}


		public void initAnimation(int nro_animacion, bool con_loop, int userFrameRate)
		{
			animating = true;
			currentAnimation = nro_animacion;
			var p_animacion = animacion[nro_animacion];
			playLoop = con_loop;
			currentTime = 0;
			currentFrame = 0;
			frameRate = userFrameRate > 0 ? userFrameRate : p_animacion.frame_rate;
			animationTimeLenght = (float)(p_animacion.cant_frames - 1) / (float)frameRate;

			//Configurar posicion inicial de los huesos
			for (var i = 0; i < this.cant_bones; i++)
			{
				//Determinar matriz local inicial
				var frame0 = p_animacion.bone_animation[i].frame[0];
				bones[i].matLocal = Matrix.CreateFromQuaternion(frame0.Rotation) * Matrix.CreateTranslation(frame0.Position);
				//Multiplicar por matriz del padre, si tiene
				var parent_id = bones[i].parentId;
				if (parent_id != -1)
					bones[i].matFinal = bones[i].matLocal * bones[parent_id].matFinal;
				else
					bones[i].matFinal = bones[i].matLocal;
			}

			//Ajustar vertices a posicion inicial del esqueleto
			updateMeshVertices();
		}

		// si la animacion es la misma que ya esta no hace nada
		public void setCurrentAnimation(int nro_animacion)
		{
			if (currentAnimation != nro_animacion)
				initAnimation(nro_animacion, true, 30);

		}

		// Actualizar los vertices de la malla segun las posiciones del los huesos del esqueleto
		public void updateMeshVertices()
		{

			//Precalcular la multiplicación para llevar a un vertice a Bone-Space y luego transformarlo segun el hueso
			//Estas matrices se envian luego al Vertex Shader para hacer skinning en GPU
			for (var i = 0; i < cant_bones; i++)
			{
				matBoneSpace[i] = bones[i].matInversePose * bones[i].matFinal;

			}
		}

		// Actualiza el cuadro actual de la animacion.
		public void updateAnimation(float elapsed_time)
		{

			//Sumo el tiempo transcurrido
			currentTime += elapsed_time / 1000.0f;

			//Se termino la animacion
			if (currentTime > animationTimeLenght)
			{
				//Ver si hacer loop
				if (playLoop)
				{
					//Dejar el remanente de tiempo transcurrido para el proximo loop
					currentTime = currentTime % animationTimeLenght;
					//setSkleletonLastPose();
					//updateMeshVertices();
				}
				else
				{

					//TODO: Puede ser que haya que quitar este stopAnimation() y solo llamar al Listener (sin cargar isAnimating = false)
					//stopAnimation();
				}
			}

			//La animacion continua
			else
			{
				//Actualizar esqueleto y malla
				updateSkeleton();
				updateMeshVertices();
			}
		}


		protected void updateSkeleton()
		{
			var p_animacion = this.animacion[currentAnimation];

			for (var i = 0; i < cant_bones; i++)
			{
				//Tomar el frame actual para este hueso
				var boneFrames = p_animacion.bone_animation[i];
				if (boneFrames.cant_frames == 1)
					continue;       //Solo hay un frame, no hacer nada, ya se hizo en el init de la animacion

				//Obtener cuadro actual segun el tiempo transcurrido
				var currentFrameF = currentTime * frameRate;
				//Ve a que KeyFrame le corresponde
				var keyFrameIdx = getCurrentFrameBone(boneFrames, currentFrameF);
				currentFrame = keyFrameIdx;

				//Armar un intervalo entre el proximo KeyFrame y el anterior
				var p_frame1 = boneFrames.frame[keyFrameIdx - 1];
				var p_frame2 = boneFrames.frame[keyFrameIdx];

				//Calcular la cantidad que hay interpolar en base al la diferencia entre cuadros
				var framesDiff = p_frame2.nro_frame - p_frame1.nro_frame;
				var interpolationValue = (currentFrameF - p_frame1.nro_frame) / framesDiff;


				//Interpolar traslacion
				var frameTranslation = (p_frame2.Position - p_frame1.Position) * interpolationValue + p_frame1.Position;

				//Interpolar rotacion con SLERP
				var quatFrameRotation = Quaternion.Slerp(p_frame1.Rotation, p_frame2.Rotation, interpolationValue);

				//Unir ambas transformaciones de este frame
				var frameMatrix = Matrix.CreateFromQuaternion(quatFrameRotation) * Matrix.CreateTranslation(frameTranslation);

				//Multiplicar por la matriz del padre, si tiene
				var parent_id = bones[i].parentId;
				if (parent_id != -1)
					bones[i].matFinal = frameMatrix * bones[parent_id].matFinal;
				else
					bones[i].matFinal = frameMatrix;
			}
		}

		// Obtener el KeyFrame correspondiente a cada hueso segun el tiempo transcurrido
		public int getCurrentFrameBone(sm_bone_animation boneFrames, float currentFrame)
		{
			for (var i = 0; i < boneFrames.cant_frames; i++)
			{
				if (currentFrame < boneFrames.frame[i].nro_frame)
				{
					return i;
				}
			}
			return boneFrames.cant_frames - 1;
		}

		public void initTextures()
		{
			texture = new Texture2D[cant_subsets];
			for (var i = 0; i < cant_subsets; ++i)
			{
				// Textures/Robot_uvw
				var src = subset[i].image_name.TrimEnd();
				var image_name = content_folder+"skmesh\\" + src;
				texture[i] = CTextureLoader.Load(device, image_name);
			}
		}



		public void Draw(GraphicsDevice graphicsDevice, Matrix World, Matrix View, Matrix Proj)
		{
			graphicsDevice.SetVertexBuffer(VertexBuffer);
			Effect.Parameters["World"].SetValue(World);
			Effect.Parameters["View"].SetValue(View);
			Effect.Parameters["Projection"].SetValue(Proj);
			Effect.Parameters["bonesMatWorldArray"].SetValue(matBoneSpace);

			var pos = 0;
			for (var i = 0; i < cant_subsets; i++)
			{
				var cant_items = subset[i].cant_items;
				if (cant_items > 0)
				{
					//gl.bindTexture(gl.TEXTURE_2D, this.texture[i]);
					Effect.Parameters["ModelTexture"].SetValue(texture[i]);

					foreach (var pass in Effect.CurrentTechnique.Passes)
					{
						pass.Apply();
						graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, pos, cant_items);
					}
					pos += cant_items * 3;
				}
			}

		}


	}
	
}
