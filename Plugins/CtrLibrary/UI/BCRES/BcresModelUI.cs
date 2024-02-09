using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrGfx.Model.Mesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using GLFrameworkEngine;
using MapStudio.UI;
using OpenTK;
using Newtonsoft.Json;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrGfx.AnimGroup;
using Discord;

namespace CtrLibrary.Bcres
{
    internal class BcresModelUI
    {
        private GfxModel GfxModel;
        private GfxMesh GfxMesh;

        private int selectedMeshID = 0;
        private int hoveredMeshID = -1;

        private CMDL ModelNode;

        public void Init(CMDL modelNode, GfxModel model)
        {
            ModelNode = modelNode;
            GfxModel = model;
        }

        public void Render()
        {
            ImGui.BeginTabBar("modelTabbar");

            if (ImguiCustomWidgets.BeginTab("modelTabbar", "Model Info"))
            {
                DrawModelInfo();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("modelTabbar", "Animation Binds"))
            {
                DrawAnimInfo();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("modelTabbar", "User Data"))
            {
                UserDataInfoEditor.Render(GfxModel.MetaData);
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("modelTabbar", "Stats"))
            {
                DrawStats();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("modelTabbar", "Animation Groups (Advanced)"))
            {
                DrawAnimGroups();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        private void DrawAnimGroups()
        {
            if (ImGui.Button("Export Anim Groups"))
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.FileName = $"{ModelNode.Header}_AnimGroups.json";
                dlg.SaveDialog = true;
                if (dlg.ShowDialog())
                {
                    var json = JsonConvert.SerializeObject(ModelNode.Model.AnimationsGroup, Formatting.Indented);
                    File.WriteAllText(dlg.FilePath, json);
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Replace Anim Groups"))
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                if (dlg.ShowDialog())
                {
                    var json = File.ReadAllText(dlg.FilePath);
                    ModelNode.Model.AnimationsGroup = JsonConvert.DeserializeObject<GfxDict<GfxAnimGroup>>(json);
                }
            }
            if (ImGui.Button("Regenerate Bone Anim Groups"))
            {
                ModelNode.GenerateSkeletalAnimGroups();
            }

            foreach (var anim in ModelNode.Model.AnimationsGroup)
            {
                if (ImGui.CollapsingHeader(anim.Name, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    foreach (var elem in anim.Elements)
                    {
                        ImGui.Text(elem.Name);
                    }
                }
            }
        }

        private void DrawAnimInfo()
        {
            bool checkAll = ModelNode.AnimGroupSettings.MaterialTypes.All(x => x.Value == true);
            if (ImGui.Checkbox("Toggle All", ref checkAll))
            {
                foreach (var item in ModelNode.AnimGroupSettings.MaterialTypes)
                    ModelNode.AnimGroupSettings.MaterialTypes[item.Key] = checkAll;
                ModelNode.GenerateMaterialAnimGroups();
            }

            int index = 0;
            foreach (var item in ModelNode.AnimGroupSettings.MaterialTypes)
            {
                string name = AnimGroupHelper.MatAnimTypes[index++];

                bool hasType = item.Value;
                if (ImGui.Checkbox($"Animate {name}", ref hasType))
                {
                    ModelNode.AnimGroupSettings.MaterialTypes[item.Key] = hasType;
                    ModelNode.GenerateMaterialAnimGroups();
                }
            }
        }

        private void DrawModelInfo()
        {
            void UpdateTransform()
            {
                Matrix4 scale = Matrix4.CreateScale(GfxModel.TransformScale.X, GfxModel.TransformScale.Y, GfxModel.TransformScale.Z);
                Matrix4 rotation = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(GfxModel.TransformRotation.X)) *
                                    Matrix4.CreateRotationY(MathHelper.DegreesToRadians(GfxModel.TransformRotation.Y)) *
                                    Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(GfxModel.TransformRotation.Z));

                Matrix4 pos = Matrix4.CreateTranslation(GfxModel.TransformTranslation.X, GfxModel.TransformTranslation.Y, GfxModel.TransformTranslation.Z);
                var m = scale * rotation * pos;
                GfxModel.WorldTransform = new SPICA.Math3D.Matrix3x4(
                                            m.M11, m.M12, m.M13,
                                            m.M21, m.M22, m.M23,
                                            m.M31, m.M32, m.M33,
                                            m.M41, m.M42, m.M43);
                GfxModel.H3DModel.WorldTransform = GfxModel.WorldTransform;
                GLContext.ActiveContext.UpdateViewport = true;
            }

            if (ImGui.CollapsingHeader("Model Settings", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.DragFloat3("Scale", ref GfxModel.TransformScale))
                {
                    UpdateTransform();
                }
                if (ImGui.DragFloat3("Rotate", ref GfxModel.TransformRotation))
                {
                    UpdateTransform();
                }
                if (ImGui.DragFloat3("Translation", ref GfxModel.TransformTranslation))
                {
                    UpdateTransform();
                }
            }
            if (ImGui.CollapsingHeader("Mesh Rendering", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var mesh = GfxModel.Meshes[selectedMeshID];
                var mat = GfxModel.Materials[mesh.MaterialIndex].H3DMaterial;

                var transparentLayer = mat.MaterialParams.RenderLayer.ToString();
                if (ImguiCustomWidgets.ComboScrollable("Render Layer", RenderLayer[mat.MaterialParams.RenderLayer], ref transparentLayer, RenderLayer))
                {
                    mat.MaterialParams.RenderLayer = int.Parse(transparentLayer[0].ToString());
                }

                var renderPriority = (int)GfxModel.Meshes[selectedMeshID].RenderPriority;
                if (ImGui.SliderInt("Priority", ref renderPriority, 0, 255))
                {
                    GfxModel.Meshes[selectedMeshID].RenderPriority = (byte)renderPriority;
                    GfxModel.Meshes[selectedMeshID].H3DMesh.Priority = (byte)renderPriority;
                    GLContext.ActiveContext.UpdateViewport = true;
                }

                ImGui.BeginColumns("meshListHeader", 5);
                ImGuiHelper.BoldText("Show");
                ImGui.NextColumn();
                ImGuiHelper.BoldText("Mesh");
                ImGui.NextColumn();
                ImGuiHelper.BoldText("Material");
                ImGui.NextColumn();
                ImGuiHelper.BoldText("Layer");
                ImGui.NextColumn();
                ImGuiHelper.BoldText("Priority");
                ImGui.NextColumn();

                ImGui.SetColumnWidth(0, 60);

                ImGui.EndColumns();

                ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeHandler.Theme.FrameBg);
                if (ImGui.BeginChild("modelList", new System.Numerics.Vector2(ImGui.GetWindowWidth() - 2, 160)))
                {
                    ImGui.BeginColumns("meshListHeader", 5);
                    ImGui.SetColumnWidth(0, 60);

                    //Set the same header colors as hovered and active. This makes nav scrolling more seamless looking
                    var active = ImGui.GetStyle().Colors[(int)ImGuiCol.Header];
                    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, active);
                    ImGui.PushStyleColor(ImGuiCol.NavHighlight, new System.Numerics.Vector4(0));

                    for (int i = 0; i < GfxModel.Meshes.Count; i++)
                        DrawMeshItem(GfxModel.Meshes[i], i);

                    ImGui.PopStyleColor(2);

                    ImGui.EndColumns();
                }
                ImGui.EndChild();

                ImGui.PopStyleColor();
            }
            if (ImGui.CollapsingHeader("Vis Nodes", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.BeginColumns("meshVisHeader", 2);
                ImGui.SetColumnWidth(0, 60);

                ImGuiHelper.BoldText("Show");
                ImGui.NextColumn();
                ImGuiHelper.BoldText("Mesh");
                ImGui.NextColumn();

                ImGui.EndColumns();

                ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeHandler.Theme.FrameBg);
                if (ImGui.BeginChild("modelVisList"))
                {
                    ImGui.BeginColumns("meshVisHeader", 2);
                    ImGui.SetColumnWidth(0, 60);

                    for (int i = 0; i < GfxModel.MeshNodeVisibilities?.Count; i++)
                    {
                        var value = GfxModel.MeshNodeVisibilities[i];
                        var name = value.Name;

                        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(2, 2));

                        var size = new System.Numerics.Vector2(ImGui.GetItemRectSize().Y);

                        ImGuiHelper.IncrementCursorPosY(2);
                        if (ImguiCustomWidgets.EyeToggle($"vis{i}", ref value.IsVisible, size))
                        {
                            GLContext.ActiveContext.UpdateViewport = true;
                        }
                        ImGui.PopStyleVar();

                        ImGui.NextColumn();
                        if (ImGui.InputText($"##name{i}", ref name, 0x200))
                            GfxModel.MeshNodeVisibilities[i].Name = name;
                        ImGui.NextColumn();
                    }

                    ImGui.EndColumns();
                }
                ImGui.EndChild();

                ImGui.PopStyleColor();
            }

        }

        void DrawStats()
        {
            if (ImGui.CollapsingHeader("Stats", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeHandler.Theme.FrameBg);
                if (ImGui.BeginChild("statList"))
                {
                    ImGui.BeginColumns("meshStatListHeader", 2);

                    for (int i = 0; i < GfxModel.Meshes.Count; i++)
                    {
                        var mesh = GfxModel.Meshes[i];
                        string meshName = string.IsNullOrEmpty(mesh.Name) ? mesh.MeshNodeName : mesh.Name;
                        if (string.IsNullOrEmpty(meshName))
                            meshName = $"Mesh{i}";

                        var b = mesh.H3DMesh.RawBuffer;

                        bool expand = ImGui.TreeNodeEx(meshName, ImGuiTreeNodeFlags.DefaultOpen);

                        int numVerts = mesh.H3DMesh.RawBuffer.Length / mesh.H3DMesh.VertexStride;

                        ImGui.NextColumn();
                        ImGuiHelper.BoldText(Toolbox.Core.STMath.GetFileSize(b.Length) + $"({numVerts}) verts");
                        ImGui.NextColumn();

                        if (expand)
                        {
                            foreach (var att in mesh.H3DMesh.Attributes)
                            {
                                ImGui.TreeNodeEx(att.Name.ToString(), ImGuiTreeNodeFlags.Leaf);
                                ImGui.NextColumn();


                                ImGui.Text(Toolbox.Core.STMath.GetFileSize(numVerts * GetAttributeSize(att)));
                                ImGui.NextColumn();
                                ImGui.TreePop();
                            }
                        }
                        ImGui.TreePop();
                    }
                    ImGui.EndColumns();
                }
                ImGui.EndChild();

                ImGui.PopStyleColor();
            }
        }

        static int GetAttributeSize(SPICA.PICA.Commands.PICAAttribute attribute)
        {
            switch (attribute.Format)
            {
                case SPICA.PICA.Commands.PICAAttributeFormat.Short: 
                    return 2 * attribute.Elements;
                case SPICA.PICA.Commands.PICAAttributeFormat.Byte:
                case SPICA.PICA.Commands.PICAAttributeFormat.Ubyte:
                    return 1 * attribute.Elements;
                case SPICA.PICA.Commands.PICAAttributeFormat.Float: 
                    return 4 * attribute.Elements;
                default:
                    return 4 * attribute.Elements;
            }
        }

        private void DrawMeshItem(GfxMesh mesh, int index)
        {
            string meshName = string.IsNullOrEmpty(mesh.Name) ? mesh.MeshNodeName : mesh.Name;
            if (string.IsNullOrEmpty(meshName))
                meshName = $"Mesh{index}";

            bool selected = index == selectedMeshID;
            var mat = GfxModel.Materials[mesh.MaterialIndex].H3DMaterial;
            var visible = mesh.IsVisible;

            bool isDragging = hoveredMeshID == index && ImGui.IsMouseDragging(0) && ImGui.IsWindowFocused();
            if (isDragging)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ThemeHandler.Theme.Warning);
                ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 2);
            }

            ImGui.AlignTextToFramePadding();
            bool select = ImGui.Selectable($"##MeshItem{index}", selected, ImGuiSelectableFlags.SpanAllColumns);
            if (select)
                selectedMeshID = index;

            if (ImGui.IsItemClicked())
                selectedMeshID = index;

            //Drag drop order check
            if (isDragging)
            {
                if (hoveredMeshID != selectedMeshID)
                {
                    ReorderMesh(mesh, selectedMeshID, hoveredMeshID);
                    selectedMeshID = index;
                    hoveredMeshID = index;
                }
            }

            if (ImGui.IsItemHovered())
            {
                hoveredMeshID = index;
            }

            if (ImGui.IsMouseReleased(0))
            {
                hoveredMeshID = -1;
            }

            bool handleReorder = ImGui.IsItemActive() && !ImGui.IsItemHovered();

            ImGui.SameLine();

            ImGui.SetItemAllowOverlap();

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(2, 2));

            var size = new System.Numerics.Vector2(ImGui.GetItemRectSize().Y);

            ImGuiHelper.IncrementCursorPosY(2);
            if (ImguiCustomWidgets.EyeToggle($"{index}check", ref visible, size))
            {
                mesh.IsVisible = visible;
                if (mesh.H3DMesh != null)
                    mesh.H3DMesh.IsVisible = visible;

                GLContext.ActiveContext.UpdateViewport = true;
            }
            ImGui.PopStyleVar();

            ImGui.SameLine();

            ImGuiHelper.IncrementCursorPosY(-2);
            ImGui.Text(index.ToString());

            ImGui.NextColumn();

            ImGui.AlignTextToFramePadding();
            ImGui.Text(meshName);
            ImGui.NextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(mat.Name);
            ImGui.NextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(RenderLayer[mat.MaterialParams.RenderLayer]);
            ImGui.NextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(mesh.RenderPriority.ToString());
            ImGui.NextColumn();

            if (isDragging)
            {
                ImGui.PopStyleColor();
                ImGui.PopStyleVar();
            }
        }

        private void ReorderMesh(GfxMesh mesh, int index, int n_next)
        {
            //Swap them out
            var srcMesh = GfxModel.Meshes[index];
            var dstMesh = GfxModel.Meshes[n_next];

            GfxModel.Meshes[index] = dstMesh;
            GfxModel.Meshes[n_next] = srcMesh;
        }

        string[] RenderLayer = new string[]
         {
                "0 (Opaque)",
                "1 (Translucent)",
                "2 (Subtractive)",
                "3 (Additive)",
         };
    }
}
