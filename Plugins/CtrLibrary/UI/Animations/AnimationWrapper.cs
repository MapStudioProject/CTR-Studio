using CtrLibrary.Rendering;
using MapStudio.UI;
using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Rendering.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.Animations;
using UIFramework;
using SPICA.Rendering.SPICA_GL;
using SPICA.Math3D;
using SPICA.Formats.Common;

namespace CtrLibrary
{
    internal class AnimationWrapper : STAnimation, IEditableAnimation
    {
        //Root for animation tree in the dope editor
        public TreeNode Root { get; set; }

        public List<string> TextureList = new List<string>();

        public H3DRender Render;

        public H3DAnimation H3DAnimation;

        public AnimationWrapper(H3DAnimation animation)
        {
            Name = animation.Name;
            H3DAnimation = animation;
            FrameCount = animation.FramesCount;
            if (animation is H3DMaterialAnim)
                TextureList = ((H3DMaterialAnim)animation).TextureNames;

            //Create an animation node for loading into the dope sheet
            Root = new AnimationTree.AnimNode(this);
            Root.Header = Name;
            Root.Icon = '\uf0e7'.ToString();

            this.Loop = false;
            if (animation.AnimationFlags.HasFlag(H3DAnimationFlags.IsLooping))
                this.Loop = true;

            foreach (var Elem in animation.Elements)
                AddElement(Elem);

            MaterialAnimUI.ReloadTree(Root, this, animation);
        }

        public void Reload(H3DAnimation animation)
        {
            Name = animation.Name;
            H3DAnimation = animation;
            FrameCount = animation.FramesCount;
            if (animation is H3DMaterialAnim)
                TextureList = ((H3DMaterialAnim)animation).TextureNames;

            //Create an animation node for loading into the dope sheet
            Root = new AnimationTree.AnimNode(this);
            Root.Header = Name;
            Root.Icon = '\uf0e7'.ToString();

            this.Loop = false;
            if (animation.AnimationFlags.HasFlag(H3DAnimationFlags.IsLooping))
                this.Loop = true;

            foreach (var Elem in animation.Elements)
                AddElement(Elem);

            MaterialAnimUI.ReloadTree(Root, this, animation);
        }

        public void ToH3D(H3DAnimation animation)
        {
            //Save generic in tool animation format back into H3D animation data
            animation.FramesCount = FrameCount;
            if (Loop)
                animation.AnimationFlags |= H3DAnimationFlags.IsLooping;
            else
                animation.AnimationFlags &= ~H3DAnimationFlags.IsLooping;

            foreach (ElementNode elementNode in this.AnimGroups)
            {
                foreach (var group in elementNode.SubAnimGroups)
                {
                    //Apply animation group data
                    if (group is Vector2Group)
                    {
                        ((Vector2Group)group).X.Save();
                        ((Vector2Group)group).Y.Save();
                    }
                    else if (group is Vector3Group)
                    {
                        ((Vector3Group)group).X.Save();
                        ((Vector3Group)group).Y.Save();
                        ((Vector3Group)group).Z.Save();
                    }
                    else if (group is FloatGroup)
                    {
                        ((FloatGroup)group).Value.Save();
                    }
                    else if (group is RGBAGroup)
                    {
                        ((RGBAGroup)group).R.Save();
                        ((RGBAGroup)group).G.Save();
                        ((RGBAGroup)group).B.Save();
                        ((RGBAGroup)group).A.Save();
                    }
                    else if (group is TextureGroup)
                    {
                        ((TextureGroup)group).Value.Save();
                    }
                }
            }
        }

        public ElementNode AddElement(H3DAnimationElement Elem)
        {
            ElementNode elemNode = new ElementNode(Elem);
            this.AnimGroups.Add(elemNode);

            string targetName = Elem.TargetType.ToString();

            Dictionary<H3DTargetType, string> targets = new Dictionary<H3DTargetType, string>()
            {
                { H3DTargetType.MaterialTexCoord0Trans, "TexCoord0_Translate" },
                { H3DTargetType.MaterialTexCoord1Trans, "TexCoord1_Translate" },
                { H3DTargetType.MaterialTexCoord2Trans, "TexCoord2_Translate" },

                { H3DTargetType.MaterialTexCoord0Rot, "TexCoord0_Rotate" },
                { H3DTargetType.MaterialTexCoord1Rot, "TexCoord1_Rotate" },
                { H3DTargetType.MaterialTexCoord2Rot, "TexCoord2_Rotate" },

                { H3DTargetType.MaterialTexCoord0Scale, "TexCoord0_Scale" },
                { H3DTargetType.MaterialTexCoord1Scale, "TexCoord1_Scale" },
                { H3DTargetType.MaterialTexCoord2Scale, "TexCoord2_Scale" },

                { H3DTargetType.MaterialMapper0Texture, "Texture0" },
                { H3DTargetType.MaterialMapper1Texture, "Texture1" },
                { H3DTargetType.MaterialMapper2Texture, "Texture2" },
            };

            if (targets.ContainsKey(Elem.TargetType))
                targetName = targets[Elem.TargetType];

            switch (Elem.PrimitiveType)
            {
                case H3DPrimitiveType.Float:
                    {
                        var v = Elem.Content as H3DAnimFloat;

                        var g = new FloatGroup();
                        g.Name = targetName;
                        g.Value.Load(v.Value);
                        elemNode.SubAnimGroups.Add(g);
                    } 
                    break;
                case H3DPrimitiveType.Boolean:
                    {
                        var v = Elem.Content as H3DAnimBoolean;

                        var g = new BoolGroup();
                        g.Name = targetName;
                        g.Value.Load(v);
                        elemNode.SubAnimGroups.Add(g);
                    }
                    break;
                case H3DPrimitiveType.Texture:
                    {
                        var v = Elem.Content as H3DAnimFloat;

                        var g = new TextureGroup();
                        g.Name = targetName;
                        g.Value.Load(v.Value);
                        elemNode.SubAnimGroups.Add(g);
                    }
                    break;
                case H3DPrimitiveType.MtxTransform:
                    {
                        var v = Elem.Content as H3DAnimMtxTransform;

                    }
                    break;
                case H3DPrimitiveType.Vector2D:
                    {
                        var vec2 = Elem.Content as H3DAnimVector2D;

                        var g = new Vector2Group();
                        g.Name = targetName;
                        g.X.Load(vec2.X);
                        g.Y.Load(vec2.Y);
                        elemNode.SubAnimGroups.Add(g);
                    }
                    break;
                case H3DPrimitiveType.Vector3D:
                    {
                        var vec2 = Elem.Content as H3DAnimVector3D;

                        var g = new Vector3Group();
                        g.Name = targetName;
                        g.X.Load(vec2.X);
                        g.Y.Load(vec2.Y);
                        g.Y.Load(vec2.Z);
                        elemNode.SubAnimGroups.Add(g);
                    }
                    break;
                case H3DPrimitiveType.QuatTransform:
                    {
                        var quatTransform = Elem.Content as H3DAnimQuatTransform;

                        var g = new QuatTransformGroup();
                        g.Name = targetName;
                        for (int i = 0; i < quatTransform.Translations.Count; i++)
                        {
                            g.Translate.X.Load(i, quatTransform.Translations[i].X);
                            g.Translate.Y.Load(i, quatTransform.Translations[i].X);
                            g.Translate.Z.Load(i, quatTransform.Translations[i].X);
                        }
                        for (int i = 0; i < quatTransform.Scales.Count; i++)
                        {
                            g.Scale.X.Load(i, quatTransform.Scales[i].X);
                            g.Scale.Y.Load(i, quatTransform.Scales[i].X);
                            g.Scale.Z.Load(i, quatTransform.Scales[i].X);
                        }
                        for (int i = 0; i < quatTransform.Rotations.Count; i++)
                        {
                            g.Rotation.X.Load(i, quatTransform.Rotations[i].X);
                            g.Rotation.Y.Load(i, quatTransform.Rotations[i].Y);
                            g.Rotation.Z.Load(i, quatTransform.Rotations[i].Z);
                            g.Rotation.W.Load(i, quatTransform.Rotations[i].W);
                        }
                        elemNode.SubAnimGroups.Add(g);
                    }
                    break;
                case H3DPrimitiveType.RGBA:
                    {
                        var rgba = Elem.Content as H3DAnimRGBA;

                        var g = new RGBAGroup();
                        g.Name = targetName;
                        g.R.Load(rgba.R);
                        g.G.Load(rgba.G);
                        g.B.Load(rgba.B);
                        g.A.Load(rgba.A);
                        elemNode.SubAnimGroups.Add(g);
                    }
                    break;
            }
            return elemNode;
        }

        public override void Reset()
        {
            if (H3DAnimation.AnimationType == H3DAnimationType.Skeletal)
            {
                foreach (var render in H3DRender.RenderCache)
                {
                    foreach (var model in render.Models)
                    {
                        model.SkeletalAnim.SetAnimations(new List<H3DAnimation>());
                        model.SkeletalAnim.Stop();
                    }
                }
            }
            if (H3DAnimation.AnimationType == H3DAnimationType.Material)
            {
                foreach (ElementNode group in this.AnimGroups)
                {
                    foreach (var render in H3DRender.RenderCache)
                    {
                        foreach (var state in MaterialAnimationHandler.GetAnimationStates(render, group.Name))
                        {
                            state.Item1.Reset(state.Item2);
                            state.Item1.IsAnimated = false;
                        }
                    }
                }
            }
            base.Reset();
        }

        public void AnimationSet()
        {
            if (H3DAnimation.AnimationType == H3DAnimationType.Skeletal)
            {
                foreach (var render in H3DRender.RenderCache)
                {
                    foreach (var model in render.Models)
                    {
                        model.SkeletalAnim.SetAnimations(new List<H3DAnimation>() { H3DAnimation });
                        model.SkeletalAnim.Play();
                    }
                }
            }
            if (H3DAnimation.AnimationType == H3DAnimationType.Material)
            {
                foreach (ElementNode group in this.AnimGroups)
                {
                    foreach (var render in H3DRender.RenderCache)
                    {
                        foreach (var state in MaterialAnimationHandler.GetAnimationStates(render, group.Name))
                        {
                            state.Item1.Reset(state.Item2);
                            state.Item1.IsAnimated = true;
                        }
                    }
                }
            }
        }

        public override void NextFrame()
        {
            var frame = this.Frame;
            if (frame == 0)
            {

            }

            if (H3DAnimation.AnimationType == H3DAnimationType.Skeletal)
            {
                foreach (var render in H3DRender.RenderCache)
                {
                    foreach (var model in render.Models)
                    {
                        model.SkeletalAnim.Frame = this.Frame;
                        model.UpdateAnimationTransforms();
                    }
                }
            }

            foreach (ElementNode group in this.AnimGroups)
            {
                switch (H3DAnimation.AnimationType)
                {
                    case H3DAnimationType.Material:
                        {
                            foreach (var render in H3DRender.RenderCache)
                            {
                                foreach (var state in MaterialAnimationHandler.GetAnimationStates(render, group.Name))
                                    MaterialAnimationHandler.SetMaterialState(this, state.Item1, state.Item2, group);
                            }
                        }
                        break;
                    case H3DAnimationType.Skeletal:
                        {

                        }
                        break;
                    case H3DAnimationType.Visibility:
                        {
                            //Index based
                            if (group.Name.StartsWith("Meshes["))
                            {

                            }
                        }
                        break;
                }
            }
        }

        public class ElementNode : STAnimGroup
        {
            public H3DAnimationElement Element;

            public ElementNode(H3DAnimationElement element)
            {
                Element = element;
                Name = element.Name;
            }

            public void Save()
            {
                if (Element is H3DFloatKeyFrameGroup)
                {

                }

                foreach (var group in this.SubAnimGroups)
                { 
                }
            }
        }

        public class BoolGroup : STAnimGroup
        {
            public H3DTrack Value = new H3DTrack("Value");
        }

        public class FloatGroup : STAnimGroup
        {
            public H3DTrack Value = new H3DTrack("Value");
        }

        public class TextureGroup : STAnimGroup
        {
            public H3DTrack Value = new H3DTrack("Value");
        }

        public class Vector2Group : STAnimGroup
        {
            public H3DTrack X = new H3DTrack("X");
            public H3DTrack Y = new H3DTrack("Y");
        }

        public class Vector3Group : STAnimGroup
        {
            public H3DTrack X = new H3DTrack("X");
            public H3DTrack Y = new H3DTrack("Y");
            public H3DTrack Z = new H3DTrack("Z");
        }

        public class Vector4Group : STAnimGroup
        {
            public H3DTrack X = new H3DTrack("X");
            public H3DTrack Y = new H3DTrack("Y");
            public H3DTrack Z = new H3DTrack("Z");
            public H3DTrack W = new H3DTrack("W");
        }

        public class QuatTransformGroup : STAnimGroup
        {
            public Vector3Group Translate = new Vector3Group() { Name = "Translate" };
            public Vector3Group Scale = new Vector3Group() { Name = "Scale" };
            public Vector4Group Rotation = new Vector4Group() { Name = "Quat" };

            public QuatTransformGroup()
            {
                this.SubAnimGroups.Add(Translate);
                this.SubAnimGroups.Add(Scale);
                this.SubAnimGroups.Add(Rotation);
            }
        }

        public class RGBAGroup : STAnimGroup
        {
            public H3DTrack R = new H3DTrack("R");
            public H3DTrack G = new H3DTrack("G");
            public H3DTrack B = new H3DTrack("B");
            public H3DTrack A = new H3DTrack("A");

            public override List<STAnimationTrack> GetTracks()
            {
                return new List<STAnimationTrack>() { R, G, B, A };
            }
        }

        public class H3DTrack : STAnimationTrack
        {
            H3DFloatKeyFrameGroup KeyData;
            H3DAnimBoolean KeyBoolData;

            private int Hash;

            public H3DTrack(string name) { Name = name; }

            public void Load(int frame, float value)
            {
                //Baked types
                InterpolationType = STInterpoaltionType.Linear;
                KeyFrames.Add(new STKeyFrame()
                {
                    Frame = frame,
                    Value = value,
                });
            }

            public void Load(H3DAnimBoolean group)
            {
                KeyBoolData = group;

                InterpolationType = STInterpoaltionType.Step;
                if (group.PostRepeat == H3DLoopType.Repeat)
                    this.WrapMode = STLoopMode.Repeat;

                if (group.PostRepeat == H3DLoopType.MirroredRepeat)
                    this.WrapMode = STLoopMode.Mirror;

                if (KeyBoolData.Values.Count > 0)
                {
                    //Max to load as one frame as end frame can be empty for constants
                    for (float i = group.StartFrame; i < Math.Max(group.EndFrame, 1.0f); i++)
                    {
                        bool value = KeyBoolData.GetFrameValue((int)i);
                        KeyFrames.Add(new STKeyFrame()
                        {
                            Frame = i,
                            Value = value ? 1 : 0,
                        });
                    }
                }
                else
                {
                    KeyFrames.Add(new STKeyFrame()
                    {
                        Frame = 0,
                        Value = 0,
                    });
                }
                Hash = CalculateHash();
            }

            public void Load(H3DFloatKeyFrameGroup group)
            {
                KeyData = group;
                InterpolationType = STInterpoaltionType.Linear;

                if (group.InterpolationType == H3DInterpolationType.Step)
                    this.InterpolationType = STInterpoaltionType.Step;
                if (group.InterpolationType == H3DInterpolationType.Hermite)
                    this.InterpolationType = STInterpoaltionType.Hermite;

                if (group.PostRepeat == H3DLoopType.Repeat)
                    this.WrapMode = STLoopMode.Repeat;

                if (group.PostRepeat == H3DLoopType.MirroredRepeat)
                    this.WrapMode = STLoopMode.Mirror;

                foreach (var key in group.KeyFrames)
                {
                    switch (group.InterpolationType)
                    {
                        case H3DInterpolationType.Hermite:
                            KeyFrames.Add(new STHermiteKeyFrame()
                            {
                                Frame = key.Frame,
                                Value = key.Value,
                                TangentIn = key.InSlope,
                                TangentOut = key.OutSlope,
                            });
                            break;
                        case H3DInterpolationType.Linear:
                        case H3DInterpolationType.Step:
                            KeyFrames.Add(new STKeyFrame()
                            {
                                Frame = key.Frame,
                                Value = key.Value,
                            });
                            break;
                    }
                }
                Hash = CalculateHash();
            }

            private int CalculateHash()
            {
                int hash = 0;
                hash += this.InterpolationType.GetHashCode();
                for (int i = 0; i < KeyFrames.Count; i++)
                {
                    hash += KeyFrames[i].Frame.GetHashCode();
                    hash += KeyFrames[i].Value.GetHashCode();
                }
                return hash;
            }

            //Convert data back into H3D key data
            public void Save() 
            {
                //Check if keys have been edited or not
                int hash = CalculateHash();
                if (Hash == hash)
                    return;

                //Update hash with resave
                Hash = hash;

                //Set expected end frame.
                KeyData.EndFrame = this.KeyFrames.Max(x => x.Frame);

                //Convert key data back
                KeyData.KeyFrames.Clear();
                foreach (var key in this.KeyFrames)
                {
                    KeyFrame kf = new KeyFrame() { Frame = key.Frame, Value = key.Value, };
                    KeyData.KeyFrames.Add(kf);

                    if (key is STHermiteKeyFrame)
                    {
                        kf.InSlope = ((STHermiteKeyFrame)key).TangentIn;
                        kf.OutSlope = ((STHermiteKeyFrame)key).TangentOut;
                    }
                }
            }
        }
    }
}
