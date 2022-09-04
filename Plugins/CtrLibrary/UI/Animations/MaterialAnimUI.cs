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

namespace CtrLibrary
{
    internal class MaterialAnimUI
    {
        public static TreeNode ReloadTree(TreeNode Root, AnimationWrapper anim, H3DAnimation animation)
        {
            Root.Children.Clear();
            Root.Header = anim.Name;

            foreach (var group in anim.AnimGroups)
            {
                TreeNode elemNode = Root.Children.FirstOrDefault(x => x.Header == group.Name);
                if (elemNode == null)
                {
                    elemNode = new TreeNode();
                    elemNode.Header = group.Name;
                    elemNode.Icon = '\uf5fd'.ToString();
                    Root.AddChild(elemNode);
                    elemNode.IsExpanded = true;

                    elemNode.ContextMenus.Add(new MenuItem("Add Material Element", () =>
                    {
                        H3DTargetType target = 0;
                        DialogHandler.Show("Material Elements", 250, 400, () =>
                        {
                            void DrawSelect(H3DTargetType type)
                            {
                                if (ImGui.Selectable($"   {type}    {IconManager.ADD_ICON}"))
                                {
                                    target = type;
                                    DialogHandler.ClosePopup(true);
                                }
                            }

                            DrawSelect(H3DTargetType.MaterialMapper0Texture);
                            DrawSelect(H3DTargetType.MaterialMapper1Texture);
                            DrawSelect(H3DTargetType.MaterialMapper2Texture);

                            DialogHandler.DrawCancelOk();
                        }, (o) =>
                        {
                            if (o)
                            {
                                AddElement(anim, elemNode, (AnimationWrapper.ElementNode)group, target);
                            }
                        });
                    }));
                }
                LoadAnimationGroups(anim, elemNode, (AnimationWrapper.ElementNode)group);
            }
            return Root;
        }

        static void LoadAnimationGroups(STAnimation anim, TreeNode elemNode, AnimationWrapper.ElementNode group)
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

            //Add to animation handler
            var track = animWrapper.AddElement(elem);
            //Add to the gui
            CreateGroupNode(animWrapper, elemNode, track.SubAnimGroups[0], group);
        }

        static void CreateGroupNode(STAnimation anim, TreeNode elemNode, STAnimGroup kind, AnimationWrapper.ElementNode group)
        {
            TreeNode trackNode = new TreeNode();
            trackNode.Icon = '\uf6ff'.ToString();
            trackNode.IsExpanded = true;

            if (kind is AnimationWrapper.RGBAGroup)
            {
                var rgba = kind as AnimationWrapper.RGBAGroup;
                trackNode = new ColorTreeNode(anim, rgba, group);

                foreach (var track in kind.GetTracks())
                    trackNode.AddChild(CreateTrack(anim, track));
                trackNode.Header = kind.Name;
                elemNode.AddChild(trackNode);
            }
            else if (kind is AnimationWrapper.QuatTransformGroup)
            {
                trackNode.IsExpanded = false;

                var f = kind as AnimationWrapper.QuatTransformGroup;
                TreeNode transNode = new TreeNode("Translate");
                transNode.Icon = '\uf6ff'.ToString();

                transNode.AddChild(CreateTrack(anim, f.Translate.X));
                transNode.AddChild(CreateTrack(anim, f.Translate.Y));
                transNode.AddChild(CreateTrack(anim, f.Translate.Y));

                TreeNode scaleNode = new TreeNode("Scale");
                scaleNode.Icon = '\uf6ff'.ToString();

                scaleNode.AddChild(CreateTrack(anim, f.Scale.X));
                scaleNode.AddChild(CreateTrack(anim, f.Scale.Y));
                scaleNode.AddChild(CreateTrack(anim, f.Scale.Y));

                TreeNode rotNode = new TreeNode("Rotation");
                rotNode.Icon = '\uf6ff'.ToString();

                rotNode.AddChild(CreateTrack(anim, f.Rotation.X));
                rotNode.AddChild(CreateTrack(anim, f.Rotation.Y));
                rotNode.AddChild(CreateTrack(anim, f.Rotation.Y));
                rotNode.AddChild(CreateTrack(anim, f.Rotation.W));

                elemNode.AddChild(transNode);
                elemNode.AddChild(scaleNode);
                elemNode.AddChild(rotNode);
            }
            else if (kind is AnimationWrapper.FloatGroup)
            {
                var f = kind as AnimationWrapper.FloatGroup;
                trackNode = CreateTrack(anim, f.Value);
                trackNode.Header = kind.Name;
                elemNode.AddChild(trackNode);
            }
            else if (kind is AnimationWrapper.BoolGroup)
            {
                var f = kind as AnimationWrapper.BoolGroup;
                trackNode = new BooleanTreeNode(anim, f.Value);
                trackNode.Header = kind.Name;
                elemNode.AddChild(trackNode);
            }
            else if (kind is AnimationWrapper.TextureGroup)
            {
                var f = kind as AnimationWrapper.TextureGroup;
                trackNode = new SamplerTreeTrack(anim, f.Value, group);
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
                    break;
                case H3DTargetType.MaterialTexCoord0Rot:
                case H3DTargetType.MaterialTexCoord1Rot:
                case H3DTargetType.MaterialTexCoord2Rot:
                    content = new H3DAnimFloat();
                    type = H3DPrimitiveType.Float;
                    break;
                case H3DTargetType.MaterialTexCoord0Scale:
                case H3DTargetType.MaterialTexCoord1Scale:
                case H3DTargetType.MaterialTexCoord2Scale:
                    content = new H3DAnimVector2D();
                    type = H3DPrimitiveType.Vector2D;
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
                    type = H3DPrimitiveType.RGBA;
                    break;
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

        public class BooleanTreeNode : AnimationTree.TrackNode
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
                    TextureSelectionDialog.Textures = TextureList;
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
