using ImGuiNET;
using MapStudio.UI;
using SPICA.Formats.CtrH3D.Model.Mesh;
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

        public void Show(PICAVertex[] vertices, List<string> bones, H3DMesh mesh)
        {
            DialogHandler.Show("Vertex Data", 400, 500, () =>
            {
                Render(vertices, bones, mesh);
            }, (o) => { });
        }

        public void Render(PICAVertex[] vertices, List<string> bones, H3DMesh mesh)
        {
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

            foreach (var ind in SM.Indices) {
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
    }
}
