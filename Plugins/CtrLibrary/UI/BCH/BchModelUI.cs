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

namespace CtrLibrary.Bch
{
    /// <summary>
    /// GUI for H3D models.
    /// </summary>
    internal class BchModelUI
    {
        private H3DModel H3DModel;

        private int selectedMeshID = 0;

        public void Init(CMDL modelNode, H3DModel model)
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
            if (ImguiCustomWidgets.BeginTab("modelTabbar", "Animation Binds"))
            {
                DrawAnimInfo();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("modelTabbar", "User Data"))
            {
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        private void DrawAnimInfo()
        {
        }

        private void DrawModelInfo()
        {
            if (ImGui.CollapsingHeader("Model Settings", ImGuiTreeNodeFlags.DefaultOpen))
            {

            }
            if (ImGui.CollapsingHeader("Mesh Rendering", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var mesh = H3DModel.Meshes[selectedMeshID];
                var mat = H3DModel.Materials[mesh.MaterialIndex];

                var transparentLayer = mesh.Layer.ToString();
                if (ImguiCustomWidgets.ComboScrollable("Render Layer", RenderLayer[mesh.Layer], ref transparentLayer, RenderLayer))
                {
                    mesh.Layer = int.Parse(transparentLayer[0].ToString());
                }

                var renderPriority = H3DModel.Meshes[selectedMeshID].Priority;
                if (ImGui.SliderInt("Priority", ref renderPriority, 0, 255))
                    H3DModel.Meshes[selectedMeshID].Priority = renderPriority;

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

                    for (int i = 0; i < H3DModel.Meshes.Count; i++)
                        DrawMeshItem(H3DModel.Meshes[i], i);

                    ImGui.EndColumns();
                    ImGui.EndChild();
                }
                ImGui.PopStyleColor();
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

            bool selected = index == selectedMeshID;
            var mat = H3DModel.Materials[mesh.MaterialIndex];

            if (ImGui.Selectable($"{meshName}##MeshItem{index}", selected, ImGuiSelectableFlags.SpanAllColumns))
            {
                selectedMeshID = index;
            }
            ImGui.NextColumn();
            ImGui.Text(mat.Name);
            ImGui.NextColumn();
            ImGui.Text(RenderLayer[mesh.Layer]);
            ImGui.NextColumn();
            ImGui.Text(mesh.Priority.ToString());
            ImGui.NextColumn();
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
