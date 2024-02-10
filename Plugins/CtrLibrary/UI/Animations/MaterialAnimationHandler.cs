using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Rendering;
using SPICA.Rendering.Animation;
using SPICA.Rendering.SPICA_GL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CtrLibrary
{
    internal class MaterialAnimationHandler
    {
        public static List<Tuple<MaterialState, H3DMaterial>> GetAnimationStates(Renderer render, string name)
        {
            List<Tuple<MaterialState, H3DMaterial>> states = new List<Tuple<MaterialState, H3DMaterial>>();
            if (render == null) return states;

            foreach (var model in render.Models)
            {
                //Get the animation state and update it
                var state = model.GetState(name);
                var mat = model.GetMaterial(name);
                if (state != null)
                    states.Add(Tuple.Create(state, mat));
            }
            return states;
        }


        public static void SetMaterialState(AnimationWrapper anim, MaterialState State, H3DMaterial mat, AnimationWrapper.ElementNode group)
        {
            State.IsAnimated = true;
            var Params = mat.MaterialParams;

            H3DTextureCoord[] TC = State.TexCoords;

            if (group.Element.PrimitiveType == H3DPrimitiveType.RGBA)
            {
                var RGBA = (AnimationWrapper.RGBAGroup)group.SubAnimGroups[0];

                switch (group.Element.TargetType)
                {
                    case H3DTargetType.MaterialEmission: SetRGBA(anim, RGBA, ref State.Emission); break;
                    case H3DTargetType.MaterialAmbient: SetRGBA(anim, RGBA, ref State.Ambient); break;
                    case H3DTargetType.MaterialDiffuse: SetRGBA(anim, RGBA, ref State.Diffuse); break;
                    case H3DTargetType.MaterialSpecular0: SetRGBA(anim, RGBA, ref State.Specular0); break;
                    case H3DTargetType.MaterialSpecular1: SetRGBA(anim, RGBA, ref State.Specular1); break;
                    case H3DTargetType.MaterialConstant0: SetRGBA(anim, RGBA, ref State.Constant0); break;
                    case H3DTargetType.MaterialConstant1: SetRGBA(anim, RGBA, ref State.Constant1); break;
                    case H3DTargetType.MaterialConstant2: SetRGBA(anim, RGBA, ref State.Constant2); break;
                    case H3DTargetType.MaterialConstant3: SetRGBA(anim, RGBA, ref State.Constant3); break;
                    case H3DTargetType.MaterialConstant4: SetRGBA(anim, RGBA, ref State.Constant4); break;
                    case H3DTargetType.MaterialConstant5: SetRGBA(anim, RGBA, ref State.Constant5); break;
                }
            }
            else if (group.Element.PrimitiveType == H3DPrimitiveType.Float)
            {
                var Float = (AnimationWrapper.FloatGroup)group.SubAnimGroups[0];
                if (!Float.Value.HasKeys)
                    return;

                float Value = Float.Value.GetFrameValue(anim.Frame);

                switch (group.Element.TargetType)
                {
                    case H3DTargetType.MaterialTexCoord0Rot: TC[0].Rotation = Value; break;
                    case H3DTargetType.MaterialTexCoord1Rot: TC[1].Rotation = Value; break;
                    case H3DTargetType.MaterialTexCoord2Rot: TC[2].Rotation = Value; break;
                }
            }
            else if (group.Element.PrimitiveType == H3DPrimitiveType.Vector2D)
            {
                var Vector = (AnimationWrapper.Vector2Group)group.SubAnimGroups[0];

                switch (group.Element.TargetType)
                {
                    case H3DTargetType.MaterialTexCoord0Scale: SetVector2(anim, Vector, ref TC[0].Scale); break;
                    case H3DTargetType.MaterialTexCoord1Scale: SetVector2(anim, Vector, ref TC[1].Scale); break;
                    case H3DTargetType.MaterialTexCoord2Scale: SetVector2(anim, Vector, ref TC[2].Scale); break;

                    case H3DTargetType.MaterialTexCoord0Trans: SetVector2(anim, Vector, ref TC[0].Translation); break;
                    case H3DTargetType.MaterialTexCoord1Trans: SetVector2(anim, Vector, ref TC[1].Translation);  break;
                    case H3DTargetType.MaterialTexCoord2Trans: SetVector2(anim, Vector, ref TC[2].Translation); break;
                }
            }
            else if (group.Element.PrimitiveType == H3DPrimitiveType.Texture)
            {
                var textureGroup = (AnimationWrapper.TextureGroup)group.SubAnimGroups[0];
                if (!textureGroup.Value.HasKeys)
                    return;

                int Value = (int)textureGroup.Value.GetFrameValue(anim.Frame);
                if (Value < anim.TextureList.Count)
                {
                    string Name = anim.TextureList[Value];

                    switch (group.Element.TargetType)
                    {
                        case H3DTargetType.MaterialMapper0Texture: State.Texture0Name = Name; break;
                        case H3DTargetType.MaterialMapper1Texture: State.Texture1Name = Name; break;
                        case H3DTargetType.MaterialMapper2Texture: State.Texture2Name = Name; break;
                    }
                }
            }

            State.Transforms[0] = TC[0].GetTransform().ToMatrix4();
            State.Transforms[1] = TC[1].GetTransform().ToMatrix4();
            State.Transforms[2] = TC[2].GetTransform().ToMatrix4();
        }

        static void SetRGBA(AnimationWrapper anim, AnimationWrapper.RGBAGroup group, ref OpenTK.Graphics.Color4 rgba)
        {
            if (group.R.HasKeys) rgba.R = group.R.GetFrameValue(anim.Frame);
            if (group.G.HasKeys) rgba.G = group.G.GetFrameValue(anim.Frame);
            if (group.B.HasKeys) rgba.B = group.B.GetFrameValue(anim.Frame);
            if (group.A.HasKeys) rgba.A = group.A.GetFrameValue(anim.Frame);
        }

        static void SetVector2(AnimationWrapper anim, AnimationWrapper.Vector2Group group, ref Vector2 vec2)
        {
            if (group.X.HasKeys) vec2.X = group.X.GetFrameValue(anim.Frame);
            if (group.Y.HasKeys) vec2.Y = group.Y.GetFrameValue(anim.Frame);
        }
    }
}
