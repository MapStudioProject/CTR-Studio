using ImGuiNET;
using SPICA.Math3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CtrLibrary.UI
{
    public class H3DUIHelper
    {
        public static bool Color(string label, ref RGBA rgba)
        {
            ImGui.Text(label);
            ImGui.NextColumn();

            ImGui.PushItemWidth(ImGui.GetColumnWidth(1) - 5);

            var color = rgba.ToVector4();
            bool edit = ImGui.ColorEdit4($"##{label}", ref color, ImGuiColorEditFlags.NoInputs);
            if (edit)
            {
                rgba = new RGBA(
                             (byte)(color.X * 255),
                             (byte)(color.Y * 255),
                             (byte)(color.Z * 255),
                             (byte)(color.W * 255));
            }

            ImGui.PopItemWidth();

            ImGui.NextColumn();

            return edit;
        }
    }
}
