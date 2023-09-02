using CtrLibrary.Rendering;
using ImGuiNET;
using MapStudio.UI;
using SPICA.Formats.CtrH3D.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.Animations;
using UIFramework;

namespace CtrLibrary.UI
{
    public class AnimElementAddUI
    {

        static H3DTargetType target = 0;

        public static void NewElementDialog(TreeNode Root, AnimationWrapper anim)
        {
            string type = $"{anim.H3DAnimation.AnimationType}";
            string elementName = $"New{type}";
            target = 0;

            var selector = new StringListSelectionDialog();

            //Get a list of selectable elements in the scene that may be added
            foreach (var render in H3DRender.H3DRenderCache)
            {
                switch (anim.H3DAnimation.AnimationType)
                {
                    case H3DAnimationType.Material:
                        foreach (var model in render.Scene.Models)
                        {
                            foreach (var mat in model.Materials)
                                selector.Strings.Add(mat.Name);
                        }
                        break;
                    case H3DAnimationType.Skeletal:
                        foreach (var model in render.Scene.Models)
                        {
                            foreach (var b in model.Skeleton)
                                selector.Strings.Add(b.Name);
                        }
                        break;
                    case H3DAnimationType.Light:
                        foreach (var light in render.Scene.Lights)
                            selector.Strings.Add(light.Name);
                        break;
                    case H3DAnimationType.Camera:
                        foreach (var cam in render.Scene.Cameras)
                            selector.Strings.Add(cam.Name);
                        break;
                    case H3DAnimationType.Fog:
                        foreach (var fog in render.Scene.Fogs)
                            selector.Strings.Add(fog.Name);
                        break;
                    case H3DAnimationType.Visibility:
                        foreach (var model in render.Scene.Models)
                        {
                            foreach (var mesh in model.MeshNodesTree)
                                selector.Strings.Add(mesh);
                        }
                        break;
                }
            }

            bool show_dialog = false;

            DialogHandler.Show($"{type} Elements", 350, 500, () =>
            {
                if (ImGui.CollapsingHeader($"{type}", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.BeginColumns($"{type}Dialog", 2);
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(type);
                    ImGui.NextColumn();
                    ImGui.InputText($"##{type}", ref elementName, 0x50);
                    ImGui.NextColumn();
                    ImGui.EndColumns();

                    ImGui.SameLine();

                    if (ImGui.Button($"   {IconManager.EDIT_ICON}    "))
                    {
                        show_dialog = true;
                    }

                    if (show_dialog)
                    {
                        if (selector.Render(elementName, ref show_dialog))
                            elementName = selector.Output;
                    }
                }
                if (ImGui.CollapsingHeader("Element", ImGuiTreeNodeFlags.DefaultOpen))
                    DrawElementDialog(anim, elementName);

            }, (o) =>
            {
                if (o)
                {
                    var group = new AnimationWrapper.ElementNode(new H3DAnimationElement() { Name = elementName });
                    group.Name = elementName;

                    //Create new group instance if no material anim with the input name exists
                    if (anim.H3DAnimation.AnimationType == H3DAnimationType.Skeletal ||
                        anim.H3DAnimation.AnimationType == H3DAnimationType.Visibility ||
                        anim.H3DAnimation.AnimationType == H3DAnimationType.Material)
                    {
                        TreeNode elemNode = Root.Children.FirstOrDefault(x => x.Header == elementName);
                        if (elemNode == null)
                            elemNode = AnimUI.AddGroupTreeNode(Root, anim, group);

                        AddElement(anim, elemNode, group, target);
                    }
                    else
                        AddElement(anim, Root, group, target);
                }
            });
        }

        public static void ChildElementDialog(AnimationWrapper anim, TreeNode elemNode, STAnimGroup group)
        {
            target = 0;
            DialogHandler.Show($"{anim.H3DAnimation.AnimationType} Elements", 350, 500, () =>
            {
                DrawElementDialog(anim, group.Name);
            }, (o) =>
            {
                if (o)
                {
                    AddElement(anim, elemNode, (AnimationWrapper.ElementNode)group, target);
                }
            });
        }

        static void DrawElementDialog(AnimationWrapper anim, string elementName)
        {
            bool useElementName = anim.H3DAnimation.AnimationType == H3DAnimationType.Material ||
                    anim.H3DAnimation.AnimationType == H3DAnimationType.Visibility ||
                    anim.H3DAnimation.AnimationType == H3DAnimationType.Skeletal;

            var size = ImGui.GetWindowSize();

            ImGui.BeginChild("elementList", new Vector2(size.X, size.Y - 150));

            ImGui.Columns(2);

            ImGui.SetColumnWidth(0, ImGui.GetWindowWidth() - 30);

            void DrawSelect(H3DTargetType type, string name)
            {
                //Check if an element with the group name and target exists, disable it if so
                if (anim.H3DAnimation.Elements.Any(x => x.Name == elementName && x.TargetType == type))
                {
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), $"   {name}");
                    ImGui.NextColumn();
                    ImGui.NextColumn();
                } //Check if an element with just the target exists, disable it if so
                else if (!useElementName && anim.H3DAnimation.Elements.Any(x => x.TargetType == type))
                {
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), $"   {name}");
                    ImGui.NextColumn();
                    ImGui.NextColumn();
                }
                else
                {
                    if (ImGui.Selectable($"   {name}"))
                    {
                        target = type;
                        DialogHandler.ClosePopup(true);
                    }
                    ImGui.NextColumn();
                    ImGui.Text($"{IconManager.ADD_ICON}");
                    ImGui.NextColumn();
                }
            }

            if (anim.H3DAnimation.AnimationType == H3DAnimationType.Material)
            {
                DrawSelect(H3DTargetType.MaterialMapper0Texture, "Texture Map 0");
                DrawSelect(H3DTargetType.MaterialMapper1Texture, "Texture Map 1");
                DrawSelect(H3DTargetType.MaterialMapper2Texture, "Texture Map 2");

                DrawSelect(H3DTargetType.MaterialTexCoord0Trans, "Texture Coord 0 Translate");
                DrawSelect(H3DTargetType.MaterialTexCoord0Scale, "Texture Coord 0 Scale");
                DrawSelect(H3DTargetType.MaterialTexCoord0Rot, "Texture Coord 0 Rotate");

                DrawSelect(H3DTargetType.MaterialTexCoord1Trans, "Texture Coord 1 Translate");
                DrawSelect(H3DTargetType.MaterialTexCoord1Scale, "Texture Coord 1 Scale");
                DrawSelect(H3DTargetType.MaterialTexCoord1Rot, "Texture Coord 1 Rotate");

                DrawSelect(H3DTargetType.MaterialTexCoord2Trans, "Texture Coord 2 Translate");
                DrawSelect(H3DTargetType.MaterialTexCoord2Scale, "Texture Coord 2 Scale");
                DrawSelect(H3DTargetType.MaterialTexCoord2Rot, "Texture Coord 2 Rotate");

                DrawSelect(H3DTargetType.MaterialDiffuse, "Diffuse Color");
                DrawSelect(H3DTargetType.MaterialEmission, "Emission Color");
                DrawSelect(H3DTargetType.MaterialSpecular0, "Specular 0 Color");
                DrawSelect(H3DTargetType.MaterialSpecular1, "Specular 1 Color");

                DrawSelect(H3DTargetType.MaterialConstant0, "Constant 0 Color");
                DrawSelect(H3DTargetType.MaterialConstant1, "Constant 1 Color");
                DrawSelect(H3DTargetType.MaterialConstant2, "Constant 2 Color");
                DrawSelect(H3DTargetType.MaterialConstant3, "Constant 3 Color");
                DrawSelect(H3DTargetType.MaterialConstant4, "Constant 4 Color");
                DrawSelect(H3DTargetType.MaterialConstant5, "Constant 5 Color");

                DrawSelect(H3DTargetType.MaterialBlendColor, "Blend Color");

                DrawSelect(H3DTargetType.MaterialMapper0BorderCol, "Texture 0 Border Color");
                DrawSelect(H3DTargetType.MaterialMapper1BorderCol, "Texture 1 Border Color");
                DrawSelect(H3DTargetType.MaterialMapper2BorderCol, "Texture 2 Border Color");
            }
            else if (anim.H3DAnimation.AnimationType == H3DAnimationType.Visibility)
            {
                DrawSelect(H3DTargetType.ModelVisibility, "Model Visibility");
                DrawSelect(H3DTargetType.MeshNodeVisibility, "Mesh Visibility");
            }
            else if (anim.H3DAnimation.AnimationType == H3DAnimationType.Fog)
            {
                DrawSelect(H3DTargetType.FogColor, "Fog Color");
            }
            else if (anim.H3DAnimation.AnimationType == H3DAnimationType.Skeletal)
            {
                DrawSelect(H3DTargetType.Bone, "Bone");
            }
            else if (anim.H3DAnimation.AnimationType == H3DAnimationType.Camera)
            {
                ImGuiHelper.BoldText("Camera");

                DrawSelect(H3DTargetType.CameraTransform, "Transform");

                ImGuiHelper.BoldText("View Rotation");

                DrawSelect(H3DTargetType.CameraViewRotation, "View Rotation");

                ImGuiHelper.BoldText("LookAt Rotation");

                DrawSelect(H3DTargetType.CameraTargetPos, "TargetPos");

                ImGuiHelper.BoldText("Aim Rotation");

                DrawSelect(H3DTargetType.CameraUpVector, "Up Vector");
                DrawSelect(H3DTargetType.CameraTwist, "Twist");

                ImGuiHelper.BoldText("Projection");

                DrawSelect(H3DTargetType.CameraZNear, "Z Near");
                DrawSelect(H3DTargetType.CameraZNear, "Z Far");
                DrawSelect(H3DTargetType.CameraAspectRatio, "Aspect");
                DrawSelect(H3DTargetType.CameraHeight, "Height");
            }
            else if (anim.H3DAnimation.AnimationType == H3DAnimationType.Light)
            {
                ImGuiHelper.BoldText("Lighting");

                DrawSelect(H3DTargetType.LightEnabled, "Enabled");
                DrawSelect(H3DTargetType.LightTransform, "Transform");

                ImGuiHelper.BoldText("Color");

                DrawSelect(H3DTargetType.LightDiffuse, "Diffuse Color");
                DrawSelect(H3DTargetType.LightAmbient, "Ambient Color");
                DrawSelect(H3DTargetType.LightSpecular0, "Specular 0 Color");
                DrawSelect(H3DTargetType.LightSpecular1, "Specular 1 Color");

                ImGuiHelper.BoldText("Hemi Lighting");

                DrawSelect(H3DTargetType.LightGround, "Ground Color");
                DrawSelect(H3DTargetType.LightSky, "Sky Color");
                DrawSelect(H3DTargetType.LightInterpolationFactor, "Hemi Interpolation");

                ImGuiHelper.BoldText("Attenuation");

                DrawSelect(H3DTargetType.LightAttenuationStart, "Attenuation Start");
                DrawSelect(H3DTargetType.LightAttenuationEnd, "Attenuation End");
            }

            ImGui.Columns(1);

            ImGui.EndChild();

            DialogHandler.DrawCancelOk();
        }

        //Creates and added an element to the UI
        static void AddElement(AnimationWrapper animWrapper, TreeNode elemNode, AnimationWrapper.ElementNode group, H3DTargetType target)
        {
            //Create a default element
            var elem = CreateH3DAnimationElement(animWrapper, elemNode.Header, target);
            if (elem == null)
                return;

            animWrapper.H3DAnimation.Elements.Add(elem);

            //Add to animation handler
            var track = animWrapper.AddElement(elem);
            AnimUI.CreateGroupNode(animWrapper, elemNode, track.SubAnimGroups[0], group);

            //insert key defaults
            var anim = track.SubAnimGroups[0];
            if (anim is AnimationWrapper.RGBAGroup)
            {
                ((AnimationWrapper.RGBAGroup)anim).R.Insert(new STKeyFrame(0, 1));
                ((AnimationWrapper.RGBAGroup)anim).G.Insert(new STKeyFrame(0, 1));
                ((AnimationWrapper.RGBAGroup)anim).B.Insert(new STKeyFrame(0, 1));
                ((AnimationWrapper.RGBAGroup)anim).A.Insert(new STKeyFrame(0, 1));
            }
            else if (anim is AnimationWrapper.FloatGroup)
            {
                ((AnimationWrapper.FloatGroup)anim).Value.Insert(new STKeyFrame(0, 0));
            }
            else if (anim is AnimationWrapper.TextureGroup)
            {
                ((AnimationWrapper.TextureGroup)anim).Value.Insert(new STKeyFrame(0, 0));
            }
            else if (anim is AnimationWrapper.Vector2Group)
            {
                if (target == H3DTargetType.MaterialTexCoord0Scale ||
                    target == H3DTargetType.MaterialTexCoord1Scale ||
                    target == H3DTargetType.MaterialTexCoord2Scale)
                {
                    ((AnimationWrapper.Vector2Group)anim).X.Insert(new STKeyFrame(0, 1));
                    ((AnimationWrapper.Vector2Group)anim).Y.Insert(new STKeyFrame(0, 1));
                }
                else
                {
                    ((AnimationWrapper.Vector2Group)anim).X.Insert(new STKeyFrame(0, 0));
                    ((AnimationWrapper.Vector2Group)anim).Y.Insert(new STKeyFrame(0, 0));
                }
            }
            else if (anim is AnimationWrapper.Vector3Group)
            {
                ((AnimationWrapper.Vector3Group)anim).X.Insert(new STKeyFrame(0, 0));
                ((AnimationWrapper.Vector3Group)anim).Y.Insert(new STKeyFrame(0, 0));
                ((AnimationWrapper.Vector3Group)anim).Y.Insert(new STKeyFrame(0, 0));
            }
        }

        //Creates an H3D element instance to use
        static H3DAnimationElement CreateH3DAnimationElement(AnimationWrapper ani, string matName, H3DTargetType target)
        {
            if (ani.H3DAnimation.Elements.Any(x => x.Name == matName && x.TargetType == target))
                return null;

            object content = new H3DAnimFloat();
            H3DPrimitiveType type = H3DPrimitiveType.Float;

            switch (target)
            {
                case H3DTargetType.MaterialMapper0Texture:
                case H3DTargetType.MaterialMapper1Texture:
                case H3DTargetType.MaterialMapper2Texture:
                    content = new H3DAnimFloat();
                    type = H3DPrimitiveType.Texture;
                    ((H3DAnimFloat)content).Value.InterpolationType = H3DInterpolationType.Step;
                    break;
                case H3DTargetType.MaterialTexCoord0Rot:
                case H3DTargetType.MaterialTexCoord1Rot:
                case H3DTargetType.MaterialTexCoord2Rot:
                    content = new H3DAnimFloat();
                    type = H3DPrimitiveType.Float;
                    ((H3DAnimFloat)content).Value.InterpolationType = H3DInterpolationType.Linear;
                    break;
                case H3DTargetType.MaterialTexCoord0Trans:
                case H3DTargetType.MaterialTexCoord1Trans:
                case H3DTargetType.MaterialTexCoord2Trans:
                    content = new H3DAnimVector2D();
                    type = H3DPrimitiveType.Vector2D;
                    ((H3DAnimVector2D)content).X.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimVector2D)content).Y.InterpolationType = H3DInterpolationType.Linear;
                    break;
                case H3DTargetType.MaterialTexCoord0Scale:
                case H3DTargetType.MaterialTexCoord1Scale:
                case H3DTargetType.MaterialTexCoord2Scale:
                    content = new H3DAnimVector2D();
                    type = H3DPrimitiveType.Vector2D;
                    ((H3DAnimVector2D)content).X.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimVector2D)content).Y.InterpolationType = H3DInterpolationType.Linear;
                    break;
                case H3DTargetType.CameraTargetPos:
                case H3DTargetType.CameraUpVector:
                case H3DTargetType.CameraViewRotation:
                    content = new H3DAnimVector3D();
                    type = H3DPrimitiveType.Vector3D;
                    ((H3DAnimVector3D)content).X.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimVector3D)content).Y.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimVector3D)content).Z.InterpolationType = H3DInterpolationType.Linear;
                    break;
                case H3DTargetType.MaterialConstant0:
                case H3DTargetType.MaterialConstant1:
                case H3DTargetType.MaterialConstant2:
                case H3DTargetType.MaterialConstant3:
                case H3DTargetType.MaterialConstant4:
                case H3DTargetType.MaterialConstant5:
                case H3DTargetType.MaterialDiffuse:
                case H3DTargetType.MaterialAmbient:
                case H3DTargetType.MaterialEmission:
                case H3DTargetType.MaterialSpecular0:
                case H3DTargetType.MaterialSpecular1:
                case H3DTargetType.MaterialBlendColor:
                case H3DTargetType.MaterialMapper0BorderCol:
                case H3DTargetType.MaterialMapper1BorderCol:
                case H3DTargetType.MaterialMapper2BorderCol:
                    content = new H3DAnimRGBA();
                    ((H3DAnimRGBA)content).R.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimRGBA)content).G.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimRGBA)content).B.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimRGBA)content).A.InterpolationType = H3DInterpolationType.Linear;
                    type = H3DPrimitiveType.RGBA;
                    break;
                case H3DTargetType.LightSky:
                case H3DTargetType.LightGround:
                case H3DTargetType.LightDiffuse:
                case H3DTargetType.LightAmbient:
                case H3DTargetType.LightSpecular0:
                case H3DTargetType.LightSpecular1:
                case H3DTargetType.FogColor:
                    content = new H3DAnimRGBA();
                    ((H3DAnimRGBA)content).R.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimRGBA)content).G.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimRGBA)content).B.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimRGBA)content).A.InterpolationType = H3DInterpolationType.Linear;
                    type = H3DPrimitiveType.RGBA;
                    break;
                case H3DTargetType.LightAttenuationStart:
                case H3DTargetType.LightAttenuationEnd:
                case H3DTargetType.LightInterpolationFactor:
                case H3DTargetType.CameraAspectRatio:
                case H3DTargetType.CameraHeight:
                case H3DTargetType.CameraZFar:
                case H3DTargetType.CameraZNear:
                case H3DTargetType.CameraTwist:
                    content = new H3DAnimFloat();
                    type = H3DPrimitiveType.Float;
                    ((H3DAnimFloat)content).Value.InterpolationType = H3DInterpolationType.Linear;
                    break;
                case H3DTargetType.MeshNodeVisibility:
                case H3DTargetType.ModelVisibility:
                case H3DTargetType.LightEnabled:
                    content = new H3DAnimBoolean();
                    type = H3DPrimitiveType.Boolean;
                    break;
                case H3DTargetType.CameraTransform:
                case H3DTargetType.LightTransform:
                case H3DTargetType.Bone:
                    content = new H3DAnimTransform();
                    ((H3DAnimTransform)content).TranslationX.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimTransform)content).TranslationY.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimTransform)content).TranslationZ.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimTransform)content).ScaleX.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimTransform)content).ScaleY.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimTransform)content).ScaleZ.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimTransform)content).RotationX.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimTransform)content).RotationY.InterpolationType = H3DInterpolationType.Linear;
                    ((H3DAnimTransform)content).RotationZ.InterpolationType = H3DInterpolationType.Linear;
                    type = H3DPrimitiveType.Transform;
                    break;
                default:
                    throw new Exception($"Unsupported element target to create {target}!");
            }

            return new H3DAnimationElement()
            {
                Name = matName,
                Content = content,
                PrimitiveType = type,
                TargetType = target
            };
        }
    }
}
