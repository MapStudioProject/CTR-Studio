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
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Mesh;
using Toolbox.Core.ViewModels;

namespace CtrLibrary.Bch
{
    /// <summary>
    /// GUI for H3D models.
    /// </summary>
    internal class BchModelUI
    {
        private H3DModel H3DModel;

        private int selectedMeshID = 0;
        private int hoveredMeshID = -1;

        public void Init(NodeBase modelNode, H3DModel model)
        {
            H3DModel = model;
        }

        public void Render()
        {
            ImGui.BeginTabBar("modelTabbar");

            if (ImguiCustomWidgets.BeginTab("modelTabbar", "Model Info"))
            {
                DrawModelInfo();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("modelTabbar", "User Data"))
            {
                UserDataInfoEditor.Render(H3DModel.MetaData);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        private void DrawModelInfo()
        {
            if (ImGui.CollapsingHeader("Model Settings", ImGuiTreeNodeFlags.DefaultOpen))
            {

            }
            if (ImGui.CollapsingHeader("Mesh Rendering", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (H3DModel.Meshes.Count > 0)
                {
                    var mesh = H3DModel.Meshes[selectedMeshID];
                    var mat = H3DModel.Materials[mesh.MaterialIndex];

                    var transparentLayer = mesh.Layer.ToString();
                    if (ImguiCustomWidgets.ComboScrollable("Render Layer", RenderLayer[mesh.Layer], ref transparentLayer, RenderLayer))
                    {
                        mesh.Layer = int.Parse(transparentLayer[0].ToString());

                        var meshList = H3DModel.Meshes.ToList();
                        H3DModel.ClearMeshes();
                        H3DModel.AddMeshes(meshList);
                    }

                    var renderPriority = H3DModel.Meshes[selectedMeshID].Priority;
                    if (ImGui.SliderInt("Priority", ref renderPriority, 0, 255))
                    {
                        H3DModel.Meshes[selectedMeshID].Priority = renderPriority;
                        GLContext.ActiveContext.UpdateViewport = true;
                    }

                    ImGui.BeginColumns("meshListHeader", 4);

                    ImGuiHelper.BoldText("Mesh");
                    ImGui.NextColumn();
                    ImGuiHelper.BoldText("Material");
                    ImGui.NextColumn();
                    ImGuiHelper.BoldText("Layer");
                    ImGui.NextColumn();
                    ImGuiHelper.BoldText("Priority");
                    ImGui.NextColumn();

                    ImGui.EndColumns();

                    ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeHandler.Theme.FrameBg);
                    if (ImGui.BeginChild("modelList", new System.Numerics.Vector2(ImGui.GetWindowWidth() - 2, 400)))
                    {
                        ImGui.BeginColumns("meshListHeader", 4);

                        //Set the same header colors as hovered and active. This makes nav scrolling more seamless looking
                        var active = ImGui.GetStyle().Colors[(int)ImGuiCol.Header];
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, active);
                        ImGui.PushStyleColor(ImGuiCol.NavHighlight, new System.Numerics.Vector4(0));

                        for (int i = 0; i < H3DModel.Meshes.Count; i++)
                            DrawMeshItem(H3DModel.Meshes[i], i);

                        ImGui.PopStyleColor(2);

                        ImGui.EndColumns();
                        ImGui.EndChild();
                    }
                    ImGui.PopStyleColor();
                }
            }

            if (ImGui.CollapsingHeader("Vis Nodes", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.BeginColumns("meshVisHeader", 2);

                ImGuiHelper.BoldText("Show");
                ImGui.NextColumn();
                ImGuiHelper.BoldText("Mesh");
                ImGui.NextColumn();

                ImGui.EndColumns();

                ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeHandler.Theme.FrameBg);
                if (ImGui.BeginChild("modelVisList", new System.Numerics.Vector2(ImGui.GetWindowWidth() - 2, 250)))
                {
                    ImGui.BeginColumns("meshVisHeader", 2);

                    for (int i = 0; i < H3DModel.MeshNodesTree?.Count; i++)
                    {
                        var name = H3DModel.MeshNodesTree.Find(i);
                        var value = H3DModel.MeshNodesVisibility.Count > i ? H3DModel.MeshNodesVisibility[i] : false;

                        if (ImGui.Checkbox($"##vis{i}", ref value)) {
                            H3DModel.MeshNodesVisibility[i] = value;
                        }

                        ImGui.NextColumn();
                        ImGui.InputText($"##name{i}", ref name, 0x200);
                        ImGui.NextColumn();
                    }

                    ImGui.EndColumns();
                }
                ImGui.EndChild();

                ImGui.PopStyleColor();
            }
        }

        private void DrawMeshItem(H3DMesh mesh, int index)
        {
            string meshName = $"Mesh{index}";
            if (H3DModel.MeshNodesTree.Count > mesh.NodeIndex) {
                meshName = H3DModel.MeshNodesTree.Find(mesh.NodeIndex);
            }

            bool isDragging = hoveredMeshID == index && ImGui.IsMouseDragging(0) && ImGui.IsWindowFocused();
            if (isDragging)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ThemeHandler.Theme.Warning);
                ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 2);
            }

            bool selected = index == selectedMeshID;
            var mat = H3DModel.Materials[mesh.MaterialIndex];

            ImGui.AlignTextToFramePadding();
            bool select = ImGui.Selectable($"##MeshItem{index}", selected, ImGuiSelectableFlags.SpanAllColumns);
            if (select)
                selectedMeshID = index;

            if (ImGui.IsItemHovered())
            {
                hoveredMeshID = index;
            }

            if (ImGui.IsMouseReleased(0))
            {
                hoveredMeshID = -1;
            }


            if (ImGui.IsItemClicked())
                selectedMeshID = index;
            ImGui.SetItemAllowOverlap();

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



            ImGui.SameLine();

            ImGui.AlignTextToFramePadding();
            ImGui.Text(meshName);
            ImGui.NextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(mat.Name);
            ImGui.NextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(RenderLayer[mesh.Layer]);
            ImGui.NextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(mesh.Priority.ToString());
            ImGui.NextColumn();

            if (isDragging)
            {
                ImGui.PopStyleColor();
                ImGui.PopStyleVar();
            }
        }

        private bool isReorder = false;

        private void ReorderMesh(H3DMesh mesh, int index, int n_next)
        {
            if (isReorder) return;

            isReorder = true;

            //Swap them out
            var srcMesh = H3DModel.Meshes[index];
            var dstMesh = H3DModel.Meshes[n_next];

            H3DModel.Meshes[index] = dstMesh;
            H3DModel.Meshes[n_next] = srcMesh;

            H3DModel.MeshesLayer0[index] = dstMesh;

            var meshList = H3DModel.Meshes.ToList();
            H3DModel.ClearMeshes();
            H3DModel.AddMeshes(meshList);

            isReorder = false;
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
