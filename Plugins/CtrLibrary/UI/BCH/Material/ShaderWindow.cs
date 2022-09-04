using ImGuiNET;
using MapStudio.UI;
using SPICA.Formats.CtrH3D.Model.Material;
using UIFramework;

namespace CtrLibrary
{
    public class ShaderWindow : DockWindow
    {
        public override string Name => "Shader Viewer";

        public H3DMaterial Material;

        public ShaderWindow(DockSpaceWindow parent) : base(parent)
        {
            this.DockDirection = ImGuiDir.Down;
            this.SplitRatio = 0.3f;
        }

        public override void Render()
        {
            if (Material == null)
            {
                ImGui.Text($"No material selected");
                return;
            }

            var shader = Material.FragmentShader;
            if (ImGui.Button("Copy"))
            {
                ImGui.SetClipboardText(shader);
            }
            ImGui.SameLine();

            ImGuiHelper.BeginBoldText();
            ImGui.Text($"Material [{Material.Name}]");

            ImGui.TextWrapped("This code is auto generated! 3DS does not support custom fragment shader code and is only made for viewing on PC!");
            ImGuiHelper.EndBoldText();

            ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeHandler.Theme.FrameBg);
            if (ImGui.BeginChild("shaderWindow"))
            {
                var size = ImGui.GetWindowSize();

                if (shader != null)
                    ImGui.InputTextMultiline("PIXEL", ref shader, 0x4000, size);
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }
    }
}
