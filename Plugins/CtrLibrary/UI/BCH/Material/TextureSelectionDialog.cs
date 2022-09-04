using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using ImGuiNET;
using Toolbox.Core;
using MapStudio.UI;

namespace CtrLibrary
{
    public class TextureSelectionDialog
    {
        public List<string> Textures = new List<string>();

        public string OutputName = "";
        public string Previous = "";

        string _searchText = "";

        bool popupOpened = false;
        bool scrolled = false;

        public void Init() { OutputName = ""; }

        public bool Render(string input, ref bool dialogOpened)
        {
            if (string.IsNullOrEmpty(OutputName))
            {
                OutputName = input;
                Previous = input;
            }

            var pos = ImGui.GetCursorScreenPos();

            if (!popupOpened)
            {
                ImGui.OpenPopup("textureSelector1");
                popupOpened = true;
                scrolled = false;
            }

            ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);

            var color = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];
            ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(color.X, color.Y, color.Z, 1.0f));

            bool hasInput = false;
            if (ImGui.BeginPopup("textureSelector1", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
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

                ImGui.TextColored(ThemeHandler.Theme.Error, $"   {IconManager.DELETE_ICON}   ");
                ImGui.SameLine();

                if (ImGui.Selectable("None", string.IsNullOrEmpty(OutputName)))
                {
                    OutputName = "";
                    hasInput = true;
                }

                if (Textures != null)
                {
                    var width = ImGui.GetWindowWidth();

                    float size = ImGui.GetFrameHeight();
                    ImGui.BeginChild("textureList", new System.Numerics.Vector2(320, 300));
                    bool isSearch = !string.IsNullOrEmpty(_searchText);

                    foreach (var tex in Textures.OrderBy(x => x))
                    {
                        bool HasText = tex.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                        if (isSearch && !HasText)
                            continue;

                        bool isSelected = OutputName == tex;

                        int ID = IconManager.GetTextureIcon("TEXTURE");
                        if (IconManager.HasIcon(tex))
                            ID = IconManager.GetTextureIcon(tex);

                        IconManager.DrawTexture(tex, ID);
                        ImGui.SameLine();

                        if (!scrolled && isSelected)
                        {
                            ImGui.SetScrollHereY();
                            scrolled = true;
                        }

                        if (ImGui.Selectable(tex, isSelected))
                        {
                            OutputName = tex;
                            hasInput = true;
                        }
                        if (ImGui.IsItemFocused() && !isSelected)
                        {
                            OutputName = tex;
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
                }
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
