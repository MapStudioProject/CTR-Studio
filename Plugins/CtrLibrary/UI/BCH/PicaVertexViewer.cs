using ImGuiNET;
using IONET.Collada.Kinematics.Articulated_Systems;
using MapStudio.UI;
using OpenTK;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CtrLibrary
{
    /// <summary>
    /// A visualizer for vertex data used by H3DMesh.
    /// </summary>
    internal class PicaVertexViewer
    {
        private int selectedSubMesh = 0;

        private PICAAttributeName AttributeView = PICAAttributeName.Position;

        private List<PICAAttributeName> AttributeList = new List<PICAAttributeName>();

        //Column lists
        string[] rgba = new string[4] { "R", "G", "B", "A" };
        string[] xyzw = new string[4] { "X", "Y", "Z", "W" };

        public void Show(PICAVertex[] vertices, List<string> bones, H3DMesh mesh)
        {
            selectedSubMesh = 0;

            AttributeList = new List<PICAAttributeName>();
            AttributeList.AddRange(mesh.Attributes.Select(x => x.Name));
            if (mesh.FixedAttributes != null)
                AttributeList.AddRange(mesh.FixedAttributes.Select(x => x.Name));

            DialogHandler.Show("Vertex Data", 400, 500, () =>
            {
                Render(vertices, bones, mesh);
            }, (o) => { });
        }

        public void Render(PICAVertex[] vertices, List<string> bones, H3DMesh mesh)
        {
            if (ImGui.BeginCombo("Attribute", AttributeView.ToString()))
            {
                foreach (var att in AttributeList)
                {
                    bool select = att == AttributeView;
                    if (ImGui.Selectable(att.ToString(), select))
                    {
                        AttributeView = att;
                    }
                    if (select)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.BeginChild("VertexList");

            switch (AttributeView)
            {
                case PICAAttributeName.Color:
                    DrawColor(vertices, mesh);
                    break;
                case PICAAttributeName.BoneWeight:
                case PICAAttributeName.BoneIndex:
                    var SM = mesh.SubMeshes[selectedSubMesh];
                    DrawWeightTable(vertices, bones, mesh);
                    break;
                case PICAAttributeName.Position: DrawGeneric(vertices, mesh, 3); break;
                case PICAAttributeName.Normal: DrawGeneric(vertices, mesh, 3); break;
                case PICAAttributeName.Tangent: DrawGeneric(vertices, mesh, 3); break;
                case PICAAttributeName.TexCoord0: DrawGeneric(vertices, mesh, 2); break;
                case PICAAttributeName.TexCoord1: DrawGeneric(vertices, mesh, 2); break;
                case PICAAttributeName.TexCoord2: DrawGeneric(vertices, mesh, 2); break;
            }

            ImGui.EndChild();
        }

        private void DrawGeneric(PICAVertex[] vertices, H3DMesh mesh, int numElements)
        {
            ImGui.BeginColumns("vertexTbl", 1 + numElements);

            ImGui.Text($"Vertex");
            ImGui.NextColumn();
            for (int i = 0; i < numElements; i++)
            {
                ImGui.Text(xyzw[i]);
                ImGui.NextColumn();
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                ImGui.Text($"Vertex {i}");
                ImGui.NextColumn();

                var vec4 = GetVector4(vertices[i]);

                for (int j = 0; j < numElements; j++)
                {
                    switch (j)
                    {
                        case 0: ImGui.Text(vec4.X.ToString()); break;
                        case 1: ImGui.Text(vec4.Y.ToString()); break;
                        case 2: ImGui.Text(vec4.Z.ToString()); break;
                        case 3: ImGui.Text(vec4.W.ToString()); break;
                    }
                    ImGui.NextColumn();
                }
            }

            ImGui.EndColumns();
        }

        private void DrawColor(PICAVertex[] vertices, H3DMesh mesh)
        {
            ImGui.BeginColumns("vertexTbl", 6);

            ImGui.Text($"Vertex");
            ImGui.NextColumn();

            for (int i = 0; i < 4; i++)
            {
                ImGui.Text(rgba[i]);
                ImGui.NextColumn();
            }

            for (int i = 0; i < 1; i++)
            {
                ImGui.Text("Color");
                ImGui.NextColumn();
            }

            for (int i = 0; i < vertices.Length; i++) 
            {
                ImGui.Text($"Vertex {i}");
                ImGui.NextColumn();

                var vec4 = GetVector4(vertices[i]);

                for (int j = 0; j < 4; j++)
                {
                    switch (j)
                    {
                        case 0: ImGui.Text(vec4.X.ToString()); break;
                        case 1: ImGui.Text(vec4.Y.ToString()); break;
                        case 2: ImGui.Text(vec4.Z.ToString()); break;
                        case 3: ImGui.Text(vec4.W.ToString()); break;
                    }
                    ImGui.NextColumn();
                }

                ImGui.ColorButton("", vertices[i].Color, ImGuiColorEditFlags.AlphaPreviewHalf);
                ImGui.NextColumn();
            }

            ImGui.EndColumns();
        }

        private void DrawWeightTable(PICAVertex[] vertices, List<string> bones, H3DMesh mesh)
        {
            if (ImGui.BeginCombo("Sub Mesh", $"{selectedSubMesh}"))
            {
                for (int i = 0; i < mesh.SubMeshes.Count; i++)
                {
                    bool select = i == selectedSubMesh;
                    if (ImGui.Selectable($"SubMesh" + i.ToString(), select))
                    {
                        selectedSubMesh = i;
                    }
                    if (select)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            var SM = mesh.SubMeshes[selectedSubMesh];

            var boneAtt = mesh.Attributes.FirstOrDefault(x => x.Name == SPICA.PICA.Commands.PICAAttributeName.BoneIndex);
            var weightAtt = mesh.Attributes.FirstOrDefault(x => x.Name == SPICA.PICA.Commands.PICAAttributeName.BoneWeight);

            ImGuiHelper.BoldText("Weights:");

            ImGui.BeginColumns("vertexTbl", 1 + (weightAtt.Elements + boneAtt.Elements));


            ImGui.Text($"Vertex");
            ImGui.NextColumn();
            for (int i = 0; i < boneAtt.Elements; i++)
            {
                ImGui.Text($"Bone{i}");
                ImGui.NextColumn();
            }
            for (int i = 0; i < weightAtt.Elements; i++)
            {
                ImGui.Text($"Weight{i}");
                ImGui.NextColumn();
            }

            foreach (var ind in SM.Indices)
            {
                var vertex = vertices[ind];

                ImGui.Text($"Vertex {ind}");
                ImGui.NextColumn();

                for (int i = 0; i < boneAtt.Elements; i++)
                {
                    int index = vertex.Indices[i];

                    string boneName = bones[SM.BoneIndices[index]];
                    ImGui.Text(boneName);
                    ImGui.NextColumn();
                }
                for (int i = 0; i < weightAtt.Elements; i++)
                {
                    float weight = vertex.Weights[i];

                    ImGui.Text(weight.ToString());
                    ImGui.NextColumn();
                }
            }

            ImGui.EndColumns();
        }

        private System.Numerics.Vector4 GetVector4(PICAVertex vertex)
        {
            switch (AttributeView)
            {
                case PICAAttributeName.Position: return vertex.Position;
                case PICAAttributeName.Normal: return vertex.Normal;
                case PICAAttributeName.Tangent: return vertex.Tangent;
                case PICAAttributeName.TexCoord0: return vertex.TexCoord0;
                case PICAAttributeName.TexCoord1: return vertex.TexCoord1;
                case PICAAttributeName.TexCoord2: return vertex.TexCoord2;
                case PICAAttributeName.Color: return vertex.Color;
            }
            return new System.Numerics.Vector4();
        }
    }
}
