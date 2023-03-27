using SPICA.PICA.Shader;
using SPICA.Rendering.Shaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using MapStudio.UI;
using SPICA.Rendering;

namespace CtrLibrary.UI
{
    internal class ShaderUI
    {
        private ShaderNameBlock VtxNames;
        private ShaderNameBlock GeoNames;

        private string Code;
        private string GeomCode;

        private ShaderBinary Shader;

        private int VertexProgramIndex;
        private int GeomProgramIndex;

        private ShaderDisplay Display = ShaderDisplay.Vertex;

        private enum ShaderDisplay
        {
            Vertex,
            Geometry,
        }

        public ShaderUI(ShaderBinary shader, int vertexProgramIdx, int geomProgramIdx) {
            Shader = shader;
            VertexProgramIndex = vertexProgramIdx;
            GeomProgramIndex = geomProgramIdx;
            VertexShaderGenerator VtxShaderGen = new VertexShaderGenerator(shader);
            Code = VtxShaderGen.GetVtxShader(vertexProgramIdx, geomProgramIdx != -1, out VtxNames);
            if (geomProgramIdx != -1)
            {
                GeometryShaderGenerator GeoShaderGen = new GeometryShaderGenerator(shader);
                GeomCode = GeoShaderGen.GetGeoShader(geomProgramIdx, VtxNames.Outputs, out GeoNames);
            }
        }

        public void Render()
        {
            ImGui.Text($"Vertex Program {VertexProgramIndex} Geom Program {GeomProgramIndex}");

            if (GeomProgramIndex != -1)
            {
                bool displayGeom = this.Display == ShaderDisplay.Geometry;
                if (ImGui.Checkbox("Display Geometry Shader", ref displayGeom))
                {
                    if (displayGeom)
                        this.Display = ShaderDisplay.Geometry;
                    else
                        this.Display = ShaderDisplay.Vertex;
                }
            }

            ImGui.BeginTabBar("menu_shader1");
            if (ImguiCustomWidgets.BeginTab("menu_shader1", $"Shader Code"))
            {
                LoadShaderStageCode();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("menu_shader1", "Shader Info"))
            {
                LoadShaderInfo();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        private void LoadShaderInfo()
        {
            ImGui.BeginChild("ShaderInfoC");

            var program = Shader.Programs[Display == ShaderDisplay.Vertex ? VertexProgramIndex : GeomProgramIndex];

            if (ImGui.CollapsingHeader("Bool Uniforms"))
            {
                ImGui.Columns(2);

                int id = 0;
                foreach (var uniform in program.BoolUniforms)
                {
                    ImGui.Text($"{uniform.Name} (bit {id})");
                    ImGui.NextColumn();
                    if (uniform.IsConstant)
                        ImGui.Text($"{uniform.Constant}");
                    else
                        ImGui.Text($"");

                    ImGui.NextColumn();
                    id++;
                }
                ImGui.Columns(1);
            }

            if (ImGui.CollapsingHeader("Vec4 Uniforms"))
            {
                ImGui.Columns(2);
                foreach (var uniform in program.Vec4Uniforms)
                {
                    ImGui.Text($"{uniform.Name}");
                    ImGui.NextColumn();
                    if (uniform.IsConstant)
                        ImGui.Text($"{uniform.Constant.X} {uniform.Constant.Y} {uniform.Constant.Z} {uniform.Constant.W}");
                    else
                        ImGui.Text($"");
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
            }

            if (ImGui.CollapsingHeader("iVec4 Uniforms"))
            {
                ImGui.Columns(2);
                foreach (var uniform in program.IVec4Uniforms)
                {
                    ImGui.Text($"{uniform.Name}");
                    ImGui.NextColumn();
                    if (uniform.IsConstant)
                        ImGui.Text($"{uniform.Constant.X} {uniform.Constant.Y} {uniform.Constant.Z} {uniform.Constant.W}");
                    else
                        ImGui.Text($"");
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
            }

            if (ImGui.CollapsingHeader("Labels"))
            {
                ImGui.Columns(4);
                ImGui.Text($"Name");
                ImGui.NextColumn();
                ImGui.Text($"ID");
                ImGui.NextColumn();
                ImGui.Text($"Offset");
                ImGui.NextColumn();
                ImGui.Text($"Length");
                ImGui.NextColumn();

                foreach (var label in program.Labels)
                {
                    ImGui.Text($"{label.Name}");
                    ImGui.NextColumn();
                    ImGui.Text($"{label.Id}");
                    ImGui.NextColumn();
                    ImGui.Text($"{label.Offset}");
                    ImGui.NextColumn();
                    ImGui.Text($"{label.Length}");
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
            }

            if (ImGui.CollapsingHeader("Input Regs"))
            {
                foreach (var inputs in program.InputRegs)
                    ImGui.Text($"{inputs}");
            }

            if (ImGui.CollapsingHeader("Output Regs"))
            {
                ImGui.Columns(2);
                ImGui.Text($"Name");
                ImGui.NextColumn();
                ImGui.Text($"Mask");
                ImGui.NextColumn();

                foreach (var output in program.OutputRegs)
                {
                    ImGui.Text($"{output.Name}");
                    ImGui.NextColumn();
                    ImGui.Text($"{output.Mask}");
                    ImGui.NextColumn();
                }
                ImGui.Columns(2);
            }

            ImGui.EndChild();
        }

        private void LoadShaderStageCode()
        {
            ImGui.BeginChild("stage_window");

            var size = ImGui.GetWindowSize();

            if (Display == ShaderDisplay.Vertex)
                ImGui.InputTextMultiline("Vertex", ref Code, 4000, size);
            else if (Display == ShaderDisplay.Geometry)
                ImGui.InputTextMultiline("Geometry", ref GeomCode, 4000, size);

            ImGui.EndChild();
        }
    }
}
