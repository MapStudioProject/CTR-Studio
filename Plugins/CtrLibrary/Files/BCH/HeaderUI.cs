using SPICA.Formats.CtrH3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace CtrLibrary
{
    internal class HeaderUI
    {
        private H3D H3DData;

        public void Init(H3D h3d)
        {
            H3DData = h3d;
        }

        public void Render()
        {
            if (H3DData == null)
                return;

            int version = H3DData.ForwardCompatibility;
            int backVersion = H3DData.BackwardCompatibility;

            DrawPreset();

            if (ImGui.InputInt("Version", ref version)) H3DData.ForwardCompatibility = (byte)version;
            if (ImGui.InputInt("Backward Version", ref backVersion)) H3DData.BackwardCompatibility = (byte)backVersion;
        }

        private void DrawPreset()
        {

        }

        Dictionary<int, string> VersionPresets = new Dictionary<int, string>()
        {
            { 35, "Latest" },
            { 34, "" },
            { 33, "" },
        };
    }
}
