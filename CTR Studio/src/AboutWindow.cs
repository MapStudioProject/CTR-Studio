﻿using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Numerics;
using UIFramework;
using ImGuiNET;
using MapStudio.UI;
using Toolbox.Core;

namespace CTRStudio
{
    public class AboutWindow : Window
    {
        public override string Name => TranslationSource.GetText("ABOUT");

        public override ImGuiWindowFlags Flags => ImGuiWindowFlags.NoDocking;

        string AppVersion;
        string[] ChangeLog;
        string[] ChangeType;

        public AboutWindow()
        {
            Size = new Vector2(500, 600);
            var asssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            AppVersion = asssemblyVersion.ToString();
            Opened = false;

        }

        public override void Render()
        {
            if (!IconManager.HasIcon("TOOL_ICON"))
                IconManager.AddIcon("TOOL_ICON", Properties.Resources.Icon, false);

            base.Render();

            ImGui.Image((IntPtr)IconManager.GetTextureIcon("TOOL_ICON"), new Vector2(50, 50));
            var bottom = ImGui.GetCursorPos();

            ImGui.SameLine();

            ImGui.SetWindowFontScale(1.5f);
            ImGui.AlignTextToFramePadding();

            var textPos = ImGui.GetCursorPos();
            ImGui.Text($"CTR Studio v{AppVersion}");
            ImGui.SetWindowFontScale(1);

            ImGui.SetCursorPos(new Vector2(textPos.X, textPos.Y + 30));
            MapStudio.UI.ImGuiHelper.HyperLinkText("Copyright @ KillzXGaming 2022");

            ImGui.SetCursorPos(bottom);

            if (ImGui.CollapsingHeader("Credits"))
            {
                ImGui.BulletText("KillzXGaming - main developer");
                ImGui.BulletText("Gdk Chan - SPICA library used to load/read/render formats.");
                ImGui.BulletText("JuPaHe64 - created animation timeline and helped me fix gizmo tools");
                ImGui.BulletText("OpenTK Team - for opengl c# bindings.");
            }

            ImGui.EndChild();
        }
    }
}
