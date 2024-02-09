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
using SPICA.Rendering;

namespace CtrLibrary.Bcres
{
    internal class BcresMeshUI
    {
        private GfxModel GfxModel;
        private GfxMesh GfxMesh;
        private SOBJ MeshNode;

        public void Init(SOBJ meshNode, GfxModel model, GfxMesh mesh)
        {
            GfxModel = model;
            GfxMesh = mesh;
            MeshNode = meshNode;
        }

        public void Render()
        {
            ImGui.BeginTabBar("meshTabbar");

            if (ImguiCustomWidgets.BeginTab("meshTabbar", "Mesh Info"))
            {
                DrawMeshInfo();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("modelTabbar", "User Data"))
            {
                UserDataInfoEditor.Render(GfxMesh.MetaData);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        public void DrawMeshInfo()
        {
            int renderPriority = GfxMesh.RenderPriority;
            int materialIdx = GfxMesh.MaterialIndex;
            bool isVisible = GfxMesh.IsVisible;

            void UpdateUIMeshName()
            {
                MeshNode.Header = string.IsNullOrEmpty(GfxMesh.Name) ? GfxMesh.MeshNodeName : GfxMesh.Name;
                if (string.IsNullOrEmpty(MeshNode.Header))
                    MeshNode.Header = $"Mesh{GfxModel.Meshes.IndexOf(GfxMesh)}";
            }

            if (ImGuiHelper.InputFromText("Name", GfxMesh, "Name", 0x200))
                UpdateUIMeshName();

            if (ImGuiHelper.InputFromText("Vis Node Name", GfxMesh, "MeshNodeName", 0x200))
                UpdateUIMeshName();

            if (ImGui.Checkbox("Is Visible", ref isVisible))
            {
                var selected = MeshNode.Parent.Children.Where(x => x.IsSelected);
                foreach (SOBJ select in selected)
                {
                    select.Mesh.IsVisible = isVisible;
                    if (select.Mesh.H3DMesh != null)
                        select.Mesh.H3DMesh.IsVisible = isVisible;
                }

                GLContext.ActiveContext.UpdateViewport = true;
            }
            void DrawMatIcon(int id)
            {
                string matIcon = GfxModel.Name + GfxModel.Materials[id].Name + "_mat";
                if (IconManager.HasIcon(matIcon))
                    IconManager.DrawIcon(matIcon, 19);
                else
                    IconManager.DrawIcon("TEXTURE", 19);

                ImGui.SameLine();
            }

            if (ImGui.SliderInt("Render Priority", ref renderPriority, 0, 255))
            {
                GfxMesh.RenderPriority = (byte)renderPriority;
                GfxMesh.H3DMesh.Priority = (byte)renderPriority;
                GLContext.ActiveContext.UpdateViewport = true;
            }

            //Draw icon
            DrawMatIcon(materialIdx);
            if (ImGui.BeginCombo("Material", GfxModel.Materials[materialIdx].Name, ImGuiComboFlags.HeightLargest))
            {
                for (int i = 0; i < GfxModel.Materials.Count; i++)
                {
                    //Draw icon for list preview
                    DrawMatIcon(i);

                    bool select = i == materialIdx;
                    if (ImGui.Selectable(GfxModel.Materials[i].Name, select))
                    {
                        GfxMesh.MaterialIndex = i;
                        //Update visuals in viewer
                        if (GfxMesh.H3DMesh != null)
                            GfxMesh.H3DMesh.MaterialIndex = (ushort)i;
                        GLContext.ActiveContext.UpdateViewport = true;
                    }
                    if (select)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            if (ImGui.Button("View Vertex Data"))
            {
                var vertices = GfxMesh.H3DMesh.GetVertices();

                var boneNames = new List<string>();
                if (GfxModel is GfxModelSkeletal)
                    boneNames = ((GfxModelSkeletal)GfxModel).Skeleton.Bones.Select(x => x.Name).ToList();

                PicaVertexViewer viewer = new PicaVertexViewer();
                viewer.Show(vertices, boneNames, GfxMesh.H3DMesh);
            }

            if (ImGui.CollapsingHeader("Attributes", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.BeginColumns("attributesTbl", 4);
                ImGuiHelper.BoldText("Name"); ImGui.NextColumn();
                ImGuiHelper.BoldText("Format"); ImGui.NextColumn();
                ImGuiHelper.BoldText("Elements"); ImGui.NextColumn();
                ImGuiHelper.BoldText("Scale"); ImGui.NextColumn();

                foreach (var buffer in GfxModel.Shapes[GfxMesh.ShapeIndex].VertexBuffers)
                {
                    if (buffer is GfxVertexBufferInterleaved)
                    {
                        var inter = buffer as GfxVertexBufferInterleaved;
                        foreach (var att in inter.Attributes)
                        {
                            ImGui.Selectable(att.AttrName.ToString(), false, ImGuiSelectableFlags.SpanAllColumns);
                            ImGui.NextColumn();
                            ImGui.Text(att.Format.ToString());
                            ImGui.NextColumn();
                            ImGui.Text(att.Elements.ToString());
                            ImGui.NextColumn();
                            ImGui.Text(att.Scale.ToString());
                            ImGui.NextColumn();
                        }
                    }
                }
                ImGui.EndColumns();
            }
            if (ImGui.CollapsingHeader("Sub Meshes", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.BeginColumns("subMeshTbl", 4);
                ImGuiHelper.BoldText("Name"); ImGui.NextColumn();
                ImGuiHelper.BoldText("Faces"); ImGui.NextColumn();
                ImGuiHelper.BoldText("Skinning"); ImGui.NextColumn();
                ImGuiHelper.BoldText("BoneIndices"); ImGui.NextColumn();

                int id = 0;
                foreach (var sm in GfxModel.Shapes[GfxMesh.ShapeIndex].SubMeshes)
                {
                    ImGui.Text($"SubMesh_{id++}");
                    ImGui.NextColumn();
                    ImGui.Text(sm.Faces.Count.ToString());
                    ImGui.NextColumn();
                    ImGui.Text(sm.Skinning.ToString());
                    ImGui.NextColumn();
                    ImGui.Text(sm.BoneIndices.Count.ToString());
                    ImGui.NextColumn();
                }
                ImGui.EndColumns();
            }
        }
    }
}
