using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using ImGuiNET;
using Toolbox.Core;
using MapStudio.UI;
using System.IO.Compression;
using SPICA.Formats.CtrH3D.Model.Material;
using Newtonsoft.Json;
using CtrLibrary.Rendering;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.PICA.Commands;

namespace CtrLibrary
{
    public class MaterialPresetSelectionDialog
    {
        public List<string> Presets = new List<string>();

        public string Output = "";
        public string Previous = "";

        string _searchText = "";

        bool popupOpened = false;
        bool scrolled = false;

        public void Init() { Output = ""; }

        public void LoadMaterialPresets()
        {
            string folder = Path.Combine(Runtime.ExecutableDir, "Lib", "Presets", "Materials");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string[] files = Directory.GetFiles(folder);

            Presets.Clear();
            foreach (var file in files)
                Presets.Add(file);
        }

        public bool Render(string input, ref bool dialogOpened)
        {
            if (string.IsNullOrEmpty(Output))
            {
                Output = input;
                Previous = input;
            }

            var pos = ImGui.GetCursorScreenPos();

            if (!popupOpened)
            {
                ImGui.OpenPopup("presetSelector1");
                popupOpened = true;
                scrolled = false;
            }

            ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);

            var color = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];
            ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(color.X, color.Y, color.Z, 1.0f));

            bool hasInput = false;
            if (ImGui.BeginPopup("presetSelector1", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if (ImGui.IsKeyDown((int)ImGuiKey.Enter))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.AlignTextToFramePadding();
                ImGui.Text($"   {IconManager.SEARCH_ICON}  ");

                ImGui.SameLine();

                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
                if (ImGui.InputText("Search", ref _searchText, 200))
                {
                }
                ImGui.PopStyleVar();

                var width = ImGui.GetWindowWidth();

                float size = ImGui.GetFrameHeight();
                ImGui.BeginChild("presetList", new System.Numerics.Vector2(320, 300));
                bool isSearch = !string.IsNullOrEmpty(_searchText);

                foreach (var file in Presets.OrderBy(x => x))
                {
                    string name = Path.GetFileNameWithoutExtension(file);

                    bool HasText = name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (isSearch && !HasText)
                        continue;

                    bool isSelected = Output == file;

                    ImGui.Text($"   {'\uf0e7'}   ");
                    ImGui.SameLine();

                    if (!scrolled && isSelected)
                    {
                        ImGui.SetScrollHereY();
                        scrolled = true;
                    }

                    if (ImGui.Selectable(name, isSelected))
                    {
                        Output = file;
                        hasInput = true;
                    }
                    if (ImGui.IsItemFocused() && !isSelected)
                    {
                        Output = file;
                        hasInput = true;
                    }
                    if (ImGui.IsMouseDoubleClicked(0) && ImGui.IsItemHovered())
                    {
                        ImGui.CloseCurrentPopup();
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndChild();

                ImGui.EndPopup();
            }
            else if (popupOpened)
            {
                dialogOpened = false;
                popupOpened = false;
            }
            ImGui.PopStyleColor();

            return hasInput;
        }
    }
}
