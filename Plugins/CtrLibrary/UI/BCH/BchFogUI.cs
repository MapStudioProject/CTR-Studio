using CtrLibrary.Bcres;
using ImGuiNET;
using MapStudio.UI;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrGfx.Fog;
using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrH3D.Fog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CtrLibrary.UI
{
    public class BchFogUI
    {
        private H3DFog Fog;
        private GfxDict<GfxMetaData> MetaData;

        public void Init(H3DFog fog, GfxDict<GfxMetaData> metaData = null)
        {
            MetaData = metaData;
            Fog = fog;
        }

        public void Render()
        {
            ImGui.BeginTabBar("fogTabbar");

            if (ImguiCustomWidgets.BeginTab("fogTabbar", "Fog Info"))
            {
                DrawFogInfo();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("fogTabbar", "Animation Binds"))
            {
                DrawAnimInfo();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("fogTabbar", "User Data"))
            {
                if (MetaData != null)
                    UserDataInfoEditor.Render(MetaData);
                else
                    Bch.UserDataInfoEditor.Render(Fog.MetaData);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        private void DrawFogInfo()
        {
            ImguiPropertyColumn.Begin("properties");

            ImguiPropertyColumn.Combo("Type", ref Fog.Type);

            H3DUIHelper.Color("Color", ref Fog.Color);

            ImguiPropertyColumn.DragFloat("Density", ref Fog.Density);
            ImguiPropertyColumn.DragFloat("Near", ref Fog.MinDepth);
            ImguiPropertyColumn.DragFloat("Far", ref Fog.MaxDepth);

            ImguiPropertyColumn.End();
        }

        private void DrawAnimInfo()
        {

        }
    }
}
