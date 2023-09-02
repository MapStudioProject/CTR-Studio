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
using CtrLibrary.UI;

namespace CtrLibrary
{
    internal class AnimUI
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
                AnimElementAddUI.NewElementDialog(Root, anim);
            }));

            foreach (var group in anim.AnimGroups)
            {
                //Create new group instance if no material anim with the input name exists
                if (anim.H3DAnimation.AnimationType == H3DAnimationType.Skeletal ||
                    anim.H3DAnimation.AnimationType == H3DAnimationType.Visibility ||
                    anim.H3DAnimation.AnimationType == H3DAnimationType.Material)
                {
                    TreeNode elemNode = Root.Children.FirstOrDefault(x => x.Header == group.Name);
                    if (elemNode == null)
                        elemNode = AddGroupTreeNode(Root, anim, group);

                    LoadAnimationGroups(anim, elemNode, (AnimationWrapper.ElementNode)group);
                }
                else
                {
                    LoadAnimationGroups(anim, Root, (AnimationWrapper.ElementNode)group);
                }
            }
            return Root;
        }

        //Adds a group node, which can be a bone/material
        public static TreeNode AddGroupTreeNode(TreeNode Root, AnimationWrapper anim, STAnimGroup group)
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
                AnimElementAddUI.ChildElementDialog(anim, elemNode, group);
            }));
            elemNode.ContextMenus.Add(new MenuItem("Remove", () =>
            {
                var result = TinyFileDialog.MessageBoxInfoYesNo(
                    string.Format("Are you sure you want to remove {0}? This cannot be undone!", elemNode.Header));

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


        //Loads and adds track nodes to the current element group node
        static void LoadAnimationGroups(AnimationWrapper anim, TreeNode elemNode, AnimationWrapper.ElementNode group)
        {
            //Check for track type
            //All possible options
            foreach (var kind in group.SubAnimGroups)
                CreateGroupNode(anim, elemNode, kind,  group);
        }

        //Creates a track node UI
        public static TreeNode CreateGroupNode(AnimationWrapper anim, TreeNode elemNode, STAnimGroup kind, AnimationWrapper.ElementNode group)
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
            else if (kind is AnimationWrapper.TransformGroup)
            {
                trackNode.IsExpanded = false;
                trackNode.Header = "Transform";

                var f = kind as AnimationWrapper.TransformGroup;
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

                trackNode.AddChild(transNode);
                trackNode.AddChild(scaleNode);
                trackNode.AddChild(rotNode);

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
                if (this.Children.Count > 0)
                    ((AnimationTree.TrackNode)this.Children[0]).InsertOrUpdateKeyValue(color.X);
                if (this.Children.Count > 1)
                    ((AnimationTree.TrackNode)this.Children[1]).InsertOrUpdateKeyValue(color.Y);
                if (this.Children.Count > 2)
                    ((AnimationTree.TrackNode)this.Children[2]).InsertOrUpdateKeyValue(color.Z);
                if (this.Children.Count > 3)
                    ((AnimationTree.TrackNode)this.Children[3]).InsertOrUpdateKeyValue(color.W);
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
