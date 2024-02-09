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
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Mesh;

namespace CtrLibrary.Bch
{
    /// <summary>
    /// UI for H3D meshes.
    /// </summary>
    internal class BchMeshUI
    {
        private H3DModel GfxModel;
        private H3DMesh GfxMesh;
        private SOBJ MeshNode;

        public void Init(SOBJ meshNode, H3DModel model, H3DMesh mesh)
        {
            MeshNode = meshNode;
            GfxModel = model;
            GfxMesh = mesh;
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
            int renderPriority = GfxMesh.Priority;
            int materialIdx = GfxMesh.MaterialIndex;

            if (ImGui.InputText("Vis Name", ref MeshNode.MeshVisName, 0x200))
                MeshNode.UpdateNodeName();

            void DrawMatIcon(int id)
            {
                string matIcon = GfxModel.Name + GfxModel.Materials[id].Name + "_mat";
                if (IconManager.HasIcon(matIcon))
                    IconManager.DrawIcon(matIcon, 19);
                else
                    IconManager.DrawIcon("TEXTURE", 19);

                ImGui.SameLine();
            }

            ImGui.Text($"VertexStride: {GfxMesh.VertexStride}");

            if (ImGui.SliderInt("Render Priority", ref renderPriority, 0, 1000))
            {
                GfxMesh.Priority = (int)renderPriority;
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
                        GfxMesh.MaterialIndex = (ushort)i;
                        GLContext.ActiveContext.UpdateViewport = true;
                    }
                    if (select)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }


            if (ImGui.Button("View Vertex Data"))
            {
                var vertices = GfxMesh.GetVertices();
                var boneNames = GfxModel.Skeleton.Select(x => x.Name).ToList();
        
                PicaVertexViewer viewer = new PicaVertexViewer();
                viewer.Show(vertices, boneNames, GfxMesh);
            }

            if (ImGui.CollapsingHeader("Attributes", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.BeginColumns("attributesTbl", 4);
                ImGuiHelper.BoldText("Name"); ImGui.NextColumn();
                ImGuiHelper.BoldText("Format"); ImGui.NextColumn();
                ImGuiHelper.BoldText("Elements"); ImGui.NextColumn();
                ImGuiHelper.BoldText("Scale"); ImGui.NextColumn();

                foreach (var att in GfxMesh.Attributes)
                {
                    ImGui.Selectable(att.Name.ToString(), false, ImGuiSelectableFlags.SpanAllColumns);
                    ImGui.NextColumn();
                    ImGui.Text(att.Format.ToString());
                    ImGui.NextColumn();
                    ImGui.Text(att.Elements.ToString());
                    ImGui.NextColumn();
                    ImGui.Text(att.Scale.ToString());
                    ImGui.NextColumn();
                }
                ImGui.EndColumns();

            }
            if (ImGui.CollapsingHeader("Attributes (Fixed)", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.BeginColumns("attributesFixedTbl", 2);
                ImGuiHelper.BoldText("Name"); ImGui.NextColumn();
                ImGuiHelper.BoldText("Value"); ImGui.NextColumn();

                foreach (var att in GfxMesh.FixedAttributes)
                {
                    ImGui.Selectable(att.Name.ToString(), false, ImGuiSelectableFlags.SpanAllColumns);
                    ImGui.NextColumn();
                    ImGui.Text(att.Value.ToString());
                    ImGui.NextColumn();
                }
                ImGui.EndColumns();
            }

            if (ImGui.CollapsingHeader("Sub Meshes", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.BeginColumns("subMeshTbl", 4);
                ImGuiHelper.BoldText("Name"); ImGui.NextColumn();
                ImGuiHelper.BoldText("Face Descriptors"); ImGui.NextColumn();
                ImGuiHelper.BoldText("Skinning"); ImGui.NextColumn();
                ImGuiHelper.BoldText("BoneIndices"); ImGui.NextColumn();

                int id = 0;
                foreach (var sm in GfxMesh.SubMeshes)
                {
                    ImGui.Text($"SubMesh_{id++}");
                    ImGui.NextColumn();
                    ImGui.Text(sm.Indices.Length.ToString());
                    ImGui.NextColumn();
                    ImGui.Text(sm.Skinning.ToString());
                    ImGui.NextColumn();
                    ImGui.Text(sm.BoneIndicesCount.ToString());
                    ImGui.NextColumn();
                }
                ImGui.EndColumns();
            }
        }
    }
}
