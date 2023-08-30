using SPICA.Formats.CtrH3D.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIFramework;
using Toolbox.Core.Animations;
using MapStudio.UI;
using ImGuiNET;
using CtrLibrary.Rendering;
using SPICA.Formats.CtrH3D.Texture;
using System.Numerics;
using static OpenTK.Graphics.OpenGL.GL;
using OpenTK.Graphics.OpenGL;
using static SPICA.Formats.CtrGfx.Scene.GfxScene;

namespace CtrLibrary
{
    internal class MaterialAnimUI
    {
        public static TreeNode ReloadTree(TreeNode Root, AnimationWrapper anim, H3DAnimation animation)
        {
            Root.Children.Clear();
            Root.Header = anim.Name;
            Root.CanRename = true;

            Root.ContextMenus.Add(new MenuItem("Rename", () =>
            {
                Root.ActivateRename = true;
            }));

            Root.ContextMenus.Add(new MenuItem($"Add {animation.AnimationType} Element", () =>
            {
                MaterialDialog(Root, anim);
            }));

            foreach (var group in anim.AnimGroups)
            {
                TreeNode elemNode = Root.Children.FirstOrDefault(x => x.Header == group.Name);
                if (elemNode == null)
                    elemNode = AddMaterialTreeNode(Root, anim, group);

                LoadAnimationGroups(anim, elemNode, (AnimationWrapper.ElementNode)group);
            }
            return Root;
        }

        static TreeNode AddMaterialTreeNode(TreeNode Root, AnimationWrapper anim, STAnimGroup group)
        {
            TreeNode elemNode = new TreeNode();
            elemNode.Header = group.Name;
            elemNode.Icon = '\uf5fd'.ToString();
            Root.AddChild(elemNode);
            elemNode.IsExpanded = true;
            elemNode.CanRename = true;
            elemNode.OnHeaderRenamed += delegate
            {
                //Rename all anim nodes that target this material
                var elements = anim.AnimGroups.Where(x => x.Name == group.Name).ToList();
                for (int i = 0; i < elements.Count; i++)
                {
                    //rename group
                    elements[i].Name = elemNode.Header;
                    //Rename element
                    ((AnimationWrapper.ElementNode)elements[i]).Element.Name = elements[i].Name;
                }
                group.Name = elemNode.Header;
            };

            elemNode.ContextMenus.Add(new MenuItem("Rename", () =>
            {
                elemNode.ActivateRename = true;
            }));
            elemNode.ContextMenus.Add(new MenuItem($"Add {anim.H3DAnimation.AnimationType} Element", () =>
            {
                MaterialElementDialog(anim, elemNode, group);
            }));
            elemNode.ContextMenus.Add(new MenuItem("Remove", () =>
            {
                var result = TinyFileDialog.MessageBoxInfoYesNo(
                    string.Format($"Are you sure you want to remove {0}? This cannot be undone!", elemNode.Header));

                if (result != 1)
                    return;

                //reset first
                anim.Reset();

                //Remove from gui
                Root.Children.Remove(elemNode);
                //Remove all elements that use this material
                var groupList = anim.AnimGroups.ToList();
                foreach (AnimationWrapper.ElementNode group in groupList)
                {
                    if (group.Name ==  elemNode.Header)
                        anim.AnimGroups.Remove(group);
                    if (anim.H3DAnimation.Elements.Contains(group.Element))
                        anim.H3DAnimation.Elements.Remove(group.Element);
                }
            }));
            return elemNode;
        }

        static H3DTargetType target = 0;

        static void MaterialDialog(TreeNode Root, AnimationWrapper anim)
        {
            string type = $"{anim.H3DAnimation.AnimationType}";
            string elementName = $"New{type}";
            target = 0;
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
                    TreeNode elemNode = Root.Children.FirstOrDefault(x => x.Header == elementName);
                    if (elemNode == null)
                        elemNode = AddMaterialTreeNode(Root, anim, group);

                    AddElement(anim, elemNode, group, target);
                }
            });
        }

        static void MaterialElementDialog(AnimationWrapper anim, TreeNode elemNode, STAnimGroup group)
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

        static void DrawElementDialog(AnimationWrapper anim, string materialName)
        {
            var size = ImGui.GetWindowSize();

            ImGui.BeginChild("elementList", new Vector2(size.X, size.Y - 150));

            ImGui.Columns(2);

            ImGui.SetColumnWidth(0, ImGui.GetWindowWidth() - 30);

            void DrawSelect(H3DTargetType type, string name)
            {
                if (anim.H3DAnimation.Elements.Any(x => x.Name == materialName && x.TargetType == type))
                {
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), $"   {materialName}.{name}");
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

        static void LoadAnimationGroups(AnimationWrapper anim, TreeNode elemNode, AnimationWrapper.ElementNode group)
        {
            //Check for track type
            //All possible options
            foreach (var kind in group.SubAnimGroups)
                CreateGroupNode(anim, elemNode, kind,  group);
        }

        static void AddElement(AnimationWrapper animWrapper, TreeNode elemNode, AnimationWrapper.ElementNode group, H3DTargetType target)
        {
            //Create a default element
            var elem = CreateH3DAnimationElement(animWrapper, elemNode.Header, target);
            if (elem == null)
                return;

            animWrapper.H3DAnimation.Elements.Add(elem);

            //Add to animation handler
            var track = animWrapper.AddElement(elem);
            CreateGroupNode(animWrapper, elemNode, track.SubAnimGroups[0], group);

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

        static TreeNode CreateGroupNode(AnimationWrapper anim, TreeNode elemNode, STAnimGroup kind, AnimationWrapper.ElementNode group)
        {
            void RemoveTrack(TreeNode n)
            {
                elemNode.Children.Remove(n);

                anim.Reset();
                //remove element from animation
                anim.AnimGroups.Remove(group);
                group.SubAnimGroups.Remove(kind);

                //Remove from H3D
                var g = kind as AnimationWrapper.H3DGroup;
                if (g != null && anim.H3DAnimation.Elements.Contains(g.Element))
                    anim.H3DAnimation.Elements.Remove(g.Element);
            }

            TreeNode trackNode = new TreeNode();
            trackNode.Icon = '\uf6ff'.ToString();
            trackNode.IsExpanded = true;
            trackNode.ContextMenus.Add(new MenuItem("Remove Property", () =>
            {
                RemoveTrack(trackNode);
            }));

            if (kind is AnimationWrapper.RGBAGroup)
            {
                var rgba = kind as AnimationWrapper.RGBAGroup;
                trackNode = new ColorTreeNode(anim, rgba, group);

                foreach (var track in kind.GetTracks())
                    trackNode.AddChild(CreateTrack(anim, track));
                trackNode.Header = kind.Name;
                elemNode.AddChild(trackNode);

                ((AnimationTree.GroupNode)trackNode).OnGroupRemoved += delegate
                {
                    anim.Reset();
                    //remove element from animation
                    anim.AnimGroups.Remove(group);
                    group.SubAnimGroups.Remove(kind);

                    //Remove from H3D
                    var g = kind as AnimationWrapper.H3DGroup;
                    if (g != null && anim.H3DAnimation.Elements.Contains(g.Element))
                        anim.H3DAnimation.Elements.Remove(g.Element);
                };
            }
            else if (kind is AnimationWrapper.QuatTransformGroup)
            {
                trackNode.IsExpanded = false;

                var f = kind as AnimationWrapper.QuatTransformGroup;
                TreeNode transNode = new TreeNode("Translate");
                transNode.Icon = '\uf6ff'.ToString();

                transNode.AddChild(CreateTrack(anim, f.Translate.X));
                transNode.AddChild(CreateTrack(anim, f.Translate.Y));
                transNode.AddChild(CreateTrack(anim, f.Translate.Z));

                TreeNode scaleNode = new TreeNode("Scale");
                scaleNode.Icon = '\uf6ff'.ToString();

                scaleNode.AddChild(CreateTrack(anim, f.Scale.X));
                scaleNode.AddChild(CreateTrack(anim, f.Scale.Y));
                scaleNode.AddChild(CreateTrack(anim, f.Scale.Z));

                TreeNode rotNode = new TreeNode("Rotation");
                rotNode.Icon = '\uf6ff'.ToString();

                rotNode.AddChild(CreateTrack(anim, f.Rotation.X));
                rotNode.AddChild(CreateTrack(anim, f.Rotation.Y));
                rotNode.AddChild(CreateTrack(anim, f.Rotation.Z));
                rotNode.AddChild(CreateTrack(anim, f.Rotation.W));

                elemNode.AddChild(transNode);
                elemNode.AddChild(scaleNode);
                elemNode.AddChild(rotNode);
            }
            else if (kind is AnimationWrapper.FloatGroup)
            {
                var f = kind as AnimationWrapper.FloatGroup;
                trackNode = CreateTrack(anim, f.Value);
                trackNode.ContextMenus.Add(new MenuItem("Remove Property", () =>
                {
                    RemoveTrack(trackNode);
                }));
                trackNode.Header = kind.Name;
                elemNode.AddChild(trackNode);
            }
            else if (kind is AnimationWrapper.BoolGroup)
            {
                var f = kind as AnimationWrapper.BoolGroup;
                trackNode = new BooleanTreeNode(anim, f.Value);
                trackNode.ContextMenus.Add(new MenuItem("Remove Property", () =>
                {
                    RemoveTrack(trackNode);
                }));
                trackNode.Header = kind.Name;
                elemNode.AddChild(trackNode);
            }
            else if (kind is AnimationWrapper.TextureGroup)
            {
                var f = kind as AnimationWrapper.TextureGroup;
                trackNode = new SamplerTreeTrack(anim, f.Value, group);
                trackNode.ContextMenus.Add(new MenuItem("Remove Property", () =>
                {
                    RemoveTrack(trackNode);
                }));
                trackNode.Header = kind.Name;
                elemNode.AddChild(trackNode);
            }
            else if (kind is AnimationWrapper.Vector2Group)
            {
                var vec2 = kind as AnimationWrapper.Vector2Group;
                trackNode.AddChild(CreateTrack(anim, vec2.X));
                trackNode.AddChild(CreateTrack(anim, vec2.Y));
                trackNode.Header = kind.Name;
                elemNode.AddChild(trackNode);
            }
            else if (kind is AnimationWrapper.Vector3Group)
            {
                var vec3 = kind as AnimationWrapper.Vector3Group;
                trackNode.AddChild(CreateTrack(anim, vec3.X));
                trackNode.AddChild(CreateTrack(anim, vec3.Y));
                trackNode.AddChild(CreateTrack(anim, vec3.Z));
                trackNode.Header = kind.Name;
                elemNode.AddChild(trackNode);
            }
            return trackNode;
        }

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

        static AnimationTree.TrackNode CreateTrack(STAnimation anim, STAnimationTrack track)
        {
            var trackNode = new AnimationTree.TrackNode(anim, track);
            trackNode.Tag = track;
            trackNode.Icon = '\uf1b2'.ToString();
            return trackNode;
        }

        public class BooleanTreeNode : AnimationTree.TrackNodeVisibility
        {
            public BooleanTreeNode(STAnimation anim, STAnimationTrack track) : base(anim, track)
            {
                Icon = '\uf53f'.ToString();
            }

            public override void RenderNode()
            {
                ImGui.Text(this.Header);
                ImGui.NextColumn();

                var color = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
                //Display keyed values differently
                bool isKeyed = Track.KeyFrames.Any(x => x.Frame == Anim.Frame);
                //   if (isKeyed)
                //   color = KEY_COLOR;

                ImGui.PushStyleColor(ImGuiCol.Text, color);

                //Span the whole column
                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 14);

                bool isValue = Track.GetFrameValue(this.Anim.Frame) == 1.0f;

                if (ImGui.Checkbox($"##keyFrame", ref isValue))
                {
                    InsertOrUpdateKeyValue(isValue ? 1.0f : 0.0f);
                }

                ImGui.PopItemWidth();

                ImGui.PopStyleColor();
                ImGui.NextColumn();
            }
        }

        public class ColorTreeNode : AnimationTree.ColorGroupNode
        {
            public ColorTreeNode(STAnimation anim, STAnimGroup group, STAnimGroup parent) : base(anim, group, parent)
            {
                Icon = '\uf53f'.ToString();
            }

            /// <summary>
            /// Sets the track color of the current frame.
            /// </summary>
            public override void SetTrackColor(Vector4 color)
            {
                var group = this.Group as AnimationWrapper.RGBAGroup;
                group.R.Insert(new STKeyFrame(Anim.Frame, color.X));
                group.G.Insert(new STKeyFrame(Anim.Frame, color.Y));
                group.B.Insert(new STKeyFrame(Anim.Frame, color.Z));
                group.A.Insert(new STKeyFrame(Anim.Frame, color.W));
            }
        }

        public class SamplerTreeTrack : AnimationTree.TextureTrackNode
        {
            TextureSelectionDialog TextureSelectionDialog = new TextureSelectionDialog();

            public override List<string> TextureList
            {
                get { return ((AnimationWrapper)Anim).TextureList; }
                set
                {
                    ((AnimationWrapper)Anim).TextureList = value;
                }
            }

            public SamplerTreeTrack(STAnimation anim, STAnimationTrack track, STAnimGroup parent) : base(anim, track)
            {
                Icon = '\uf03e'.ToString();
            }

            bool dialogOpened = false;

            public override void RenderNode()
            {
                ImGui.Text(this.Header);
                ImGui.NextColumn();

                var color = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
                //Display keyed values differently
                bool isKeyed = Track.KeyFrames.Any(x => x.Frame == Anim.Frame);
                //   if (isKeyed)
                //   color = KEY_COLOR;

                ImGui.PushStyleColor(ImGuiCol.Text, color);

                //Span the whole column
                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 14);

                string texture = GetTextureName(Anim.Frame);

                float size = ImGui.GetFrameHeight();
                if (ImGui.Button($"   {MapStudio.UI.IconManager.IMAGE_ICON}   "))
                {
                    dialogOpened = true;

                    var render = GLFrameworkEngine.DataCache.ModelCache.Values.FirstOrDefault();

                    TextureSelectionDialog.Textures.Clear();
                    foreach (var tex in H3DRender.TextureCache)
                        TextureSelectionDialog.Textures.Add(tex.Key);
                }
                ImGui.SameLine();


                int ID = IconManager.GetTextureIcon("TEXTURE");
                if (IconManager.HasIcon(texture))
                    ID = IconManager.GetTextureIcon(texture);

                IconManager.DrawTexture(texture, ID);

                ImGui.SameLine();
                if (ImGui.InputText("##texSelect", ref texture, 0x200))
                {

                }

                if (dialogOpened)
                {
                    if (TextureSelectionDialog.Render(texture, ref dialogOpened))
                    {
                        var input = TextureSelectionDialog.OutputName;
                        if (TextureList.IndexOf(input) == -1)
                            TextureList.Add(input);

                        InsertOrUpdateKeyValue(TextureList.IndexOf(input));
                    }
                }

                ImGui.PopItemWidth();

                ImGui.PopStyleColor();
                ImGui.NextColumn();
            }

            public H3DTexture GetImage(string name)
            {
                if (H3DRender.TextureCache.ContainsKey(name))
                    return H3DRender.TextureCache[name];

                return null;
            }

            public string GetTextureName(float frame)
            {
                int index = (int)Track.GetFrameValue(frame);
                if (index >= TextureList.Count)
                    return "";
                return TextureList[index];
            }
        }
    }
}
