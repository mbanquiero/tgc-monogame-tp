using Assimp.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TGC.MonoGame.TP
{
    public class SkinnedModelInstance
    {
        public const uint MaxBones = 100;
        public const uint FramePerSecond = 30;

        public SkinnedModel Mesh;
        public Matrix Transformation;
        public SkinnedModelAnimation Animation;
        public List<MeshInstance> MeshInstances;
        public List<BoneAnimationInstance> BoneAnimationInstances;
        public SkinnedModelAnimation PreviousAnimation;
        public List<BoneAnimationInstance> PreviousBoneAnimationInstances;

        public float SpeedTransitionSecond { get; set; } = 1.0f;

        public float TimeAnimationChanged;
        public float Time = 0;

        public struct BoneInstance
        {
            public BoneAnimationInstance BoneAnimationInstance { get; set; }
            public SkinnedModel.Bone Bone { get; set; }
        }

        public class MeshInstance
        {
            public SkinnedModel.Mesh Mesh { get; set; }
            public List<BoneInstance> BoneInstances { get; set; }
            public Matrix[] BonesOffsets { get; set; }
        }

        public class BoneAnimationInstance
        {
            public SkinnedModelAnimation.BoneAnimation BoneAnimation { get; set; }
            public BoneAnimationInstance Parent { get; set; }
            public BoneAnimationInstance PreviousBoneAnimationInstance { get; set; }
            public Matrix AdditionalTransform { get; set; }
            public Matrix Transform { get; set; }
            public bool Updated { get; set; }
        }

        public SkinnedModelInstance()
        {

        }

        public void Initialize()
        {
            BoneAnimationInstances = new List<BoneAnimationInstance>();
            MeshInstances = new List<MeshInstance>();
            PreviousBoneAnimationInstances = new List<BoneAnimationInstance>();

            foreach (var skinnedMesh in Mesh.Meshes)
            {
                MeshInstance meshInstance = new MeshInstance();
                meshInstance.Mesh = skinnedMesh;
                meshInstance.BoneInstances = new List<BoneInstance>();
                meshInstance.BonesOffsets = new Matrix[MaxBones];
                for (int i = 0; i < meshInstance.BonesOffsets.Length; i++)
                {
                    meshInstance.BonesOffsets[i] = Matrix.Identity;
                }

                MeshInstances.Add(meshInstance);
            }
        }

        public Matrix GetBoneAnimationTransform(SkinnedModelAnimation.BoneAnimation boneAnimation, float time)
        {
            if (!boneAnimation.IsAnimate && boneAnimation.Parent != null)
            {
                return boneAnimation.Transformation;
            }

            Matrix transform = Matrix.Identity;
            if (boneAnimation.Scales.Any())
            {
                int frameIndex = (int)(time * FramePerSecond) % boneAnimation.Scales.Count;
                transform *= Matrix.CreateScale(boneAnimation.Scales[frameIndex]);
            }
            if (boneAnimation.Rotations.Any())
            {
                int frameIndex = (int)(time * FramePerSecond) % boneAnimation.Rotations.Count;
                transform *= Matrix.CreateFromQuaternion(boneAnimation.Rotations[frameIndex]);
            }
            if (boneAnimation.Positions.Any())
            {
                int frameIndex = (int)(time * FramePerSecond) % boneAnimation.Positions.Count;
                transform *= Matrix.CreateTranslation(boneAnimation.Positions[frameIndex]);
            }

            return transform;
        }

        void UpdateBoneAnimationInstance(BoneAnimationInstance boneAnimationInstance)
        {
            if(boneAnimationInstance.Updated)
            {
                return;
            }

            Matrix parentTransform = Matrix.Identity;
            if(boneAnimationInstance.Parent != null)
            {
                UpdateBoneAnimationInstance(boneAnimationInstance.Parent);
                parentTransform = boneAnimationInstance.Parent.Transform;
            }
            boneAnimationInstance.Transform = GetBoneAnimationTransform(boneAnimationInstance.BoneAnimation, Time) * boneAnimationInstance.AdditionalTransform * parentTransform;
        }

        public void UpdateBoneAnimations()
        {
            foreach (var boneAnimationInstance in BoneAnimationInstances)
            {
                boneAnimationInstance.Updated = false;
            }

            foreach (var boneAnimationInstance in BoneAnimationInstances)
            {
                UpdateBoneAnimationInstance(boneAnimationInstance);
            }

            foreach (var boneAnimationInstance in PreviousBoneAnimationInstances)
            {
                UpdateBoneAnimationInstance(boneAnimationInstance);
            }
        }

        public void UpdateBones()
        {
            foreach (var meshInstance in MeshInstances)
            {
                // meto un parche para que si no encuentra el boneanimationinstance, al menos use el anterior
                // no nulo, eso sucede cuando el esqueleto no coincide exacto o le faltan algunas partes de la animacion
                BoneAnimationInstance temp = null;
                foreach (var boneInstances in meshInstance.BoneInstances)
                {
                    Matrix transform = boneInstances.BoneAnimationInstance != null ? 
                            boneInstances.BoneAnimationInstance.Transform : temp!=null ? temp.Transform : Matrix.Identity;
                    if (boneInstances.BoneAnimationInstance != null && 
                        boneInstances.BoneAnimationInstance.PreviousBoneAnimationInstance != null)
                    {
                        float transition = (float)(Time - TimeAnimationChanged);
                        if(transition < SpeedTransitionSecond)
                        {
                            transform = Matrix.Lerp(boneInstances.BoneAnimationInstance.PreviousBoneAnimationInstance.Transform, boneInstances.BoneAnimationInstance.Transform, transition / SpeedTransitionSecond);
                        }
                    }
                    meshInstance.BonesOffsets[boneInstances.Bone.Index] = boneInstances.Bone.Offset * transform;
                    if(boneInstances.BoneAnimationInstance != null)
                        temp = boneInstances.BoneAnimationInstance;
                }
            }
        }

        public void Update(float elapsed_time)
        {
            Time += elapsed_time;
            UpdateBoneAnimations();
            UpdateBones();
        }

        public BoneAnimationInstance GetBoneAnimationInstance(string name)
        {
            return BoneAnimationInstances.FirstOrDefault(ni => ni.BoneAnimation.Name == name);
        }

        public Matrix GetTransform(BoneAnimationInstance boneAnimationInstance, float time)
        {
            Matrix transform = boneAnimationInstance.Transform;
            if (boneAnimationInstance.PreviousBoneAnimationInstance != null)
            {
                float transition = (float)(time - TimeAnimationChanged);
                if (transition < SpeedTransitionSecond)
                {
                    transform = Matrix.Lerp(boneAnimationInstance.PreviousBoneAnimationInstance.Transform, boneAnimationInstance.Transform, transition / SpeedTransitionSecond);
                }
            }
            return transform;
        }

        public string LN(string name)
        {
            return name.Replace("mixamorig", "").Replace("_", "").Replace(":", "");
        }

        public void SetAnimation(SkinnedModelAnimation animation, float time = 0)
        {
            TimeAnimationChanged = time;
            
            PreviousAnimation = animation;
            PreviousBoneAnimationInstances.Clear();
            PreviousBoneAnimationInstances.AddRange(BoneAnimationInstances);

            Animation = animation;

            BoneAnimationInstances.Clear();
            foreach(var boneAnimation in animation.BoneAnimations)
            {
                BoneAnimationInstance boneAnimationInstance = new BoneAnimationInstance();
                boneAnimationInstance.BoneAnimation = boneAnimation;
                boneAnimationInstance.Updated = false;
                boneAnimationInstance.AdditionalTransform = Matrix.Identity;
                boneAnimationInstance.PreviousBoneAnimationInstance = PreviousBoneAnimationInstances.FirstOrDefault(ni => ni.BoneAnimation.Name == boneAnimation.Name);
                BoneAnimationInstances.Add(boneAnimationInstance);
            }

            foreach (var boneAnimationInstance in BoneAnimationInstances)
            {
                boneAnimationInstance.Parent = BoneAnimationInstances.FirstOrDefault(ni => ni.BoneAnimation == boneAnimationInstance.BoneAnimation.Parent);
            }

            foreach( var bi in BoneAnimationInstances)
            {
                Debug.WriteLine(bi.BoneAnimation.Name);
            }

            foreach (var meshInstance in MeshInstances)
            {
                meshInstance.BoneInstances.Clear();
                foreach(var bone in meshInstance.Mesh.Bones)
                {
                    var s = BoneAnimationInstances.FirstOrDefault(
                            ni => LN(ni.BoneAnimation.Name) == LN(bone.Name));
                    var boneInstance = new BoneInstance();
                    boneInstance.Bone = bone;
                    boneInstance.BoneAnimationInstance = s;
                    meshInstance.BoneInstances.Add(boneInstance);
                }
            }
        }


        public void Draw(GraphicsDevice device, Effect effect, Matrix View , Matrix Projection)
        {
            device.DepthStencilState = DepthStencilState.Default;
            effect.CurrentTechnique = effect.Techniques["BasicColorDrawing"];

            effect.Parameters["World"].SetValue(Transformation);
            effect.Parameters["View"].SetValue(View);
            effect.Parameters["Projection"].SetValue(Projection);

            foreach (var meshInstance in MeshInstances)
            {
                effect.Parameters["gBonesOffsets"].SetValue(meshInstance.BonesOffsets);
                if(meshInstance.Mesh.Texture!=null)
                    effect.Parameters["ModelTexture"].SetValue(meshInstance.Mesh.Texture);

                device.SetVertexBuffer(meshInstance.Mesh.VertexBuffer);
                device.Indices = meshInstance.Mesh.IndexBuffer;

                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, meshInstance.Mesh.FaceCount);
                }
            }
        }
    }
}
