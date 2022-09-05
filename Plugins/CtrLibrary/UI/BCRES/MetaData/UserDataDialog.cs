using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using ImGuiNET;
using SPICA.Formats.CtrGfx;
using MapStudio.UI;

namespace CtrLibrary.Bcres
{
    public class UserDataDialog
    {
        public List<string> ValuePresets = new List<string>();

        public bool canParse = true;

        private string ValuesEdit = "";

        public void Load(GfxMetaData userData)
        {
            ValuesEdit = GetDataString(userData);
            if (string.IsNullOrEmpty(ValuesEdit))
                ValuesEdit = "";
        }

        public void Render(ref GfxMetaData userData)
        {
            if (!canParse)
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Failed to parse type {userData.Type}!");
            if (string.IsNullOrEmpty(userData.Name))
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Make sure to input a name!");

            ImGuiHelper.InputFromText(TranslationSource.GetText("NAME"), userData, "Name", 200);

            bool formatChanged = BcresUIHelper.DrawEnum(TranslationSource.GetText("TYPE"), ref userData.Type);
            if (formatChanged)
                UpdateValues(ref userData);

            var windowSize = ImGui.GetWindowSize();
            var textSize = new Vector2(ImGui.GetWindowWidth(), ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - 32);

            if (userData.Type == GfxMetaDataType.Color)
            {
            }
            else if (userData.Type == GfxMetaDataType.Vector3)
            {
            }
            else //Draw text editor with multi line for parsing multiple values
            {
                ImGui.PushItemWidth(ImGui.GetWindowWidth());
                if (ImGui.InputTextMultiline(TranslationSource.GetText("VALUE"), ref ValuesEdit, 0x1000, textSize))
                {
                    UpdateValues(ref userData);
                }
                ImGui.PopItemWidth();
            }


            ImGui.SetCursorPosY(windowSize.Y - 28);

            var buttonSize = new Vector2(ImGui.GetWindowWidth() / 2 - 2, ImGui.GetFrameHeight());
            if (ImGui.Button("Cancel", buttonSize))
            {
                DialogHandler.ClosePopup(false);
            }
            ImGui.SameLine();
            if (ImGui.Button("Ok", buttonSize))
            {
                if (canParse && !string.IsNullOrEmpty(userData.Name))
                    DialogHandler.ClosePopup(true);
            }
        }

        void UpdateValues(ref GfxMetaData userData)
        {
            canParse = true;
            string[] values = ValuesEdit.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();
            string name = userData.Name;

            if (userData.Type == GfxMetaDataType.Integer)
            {
                int[] data = new int[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    canParse = int.TryParse(values[i], out int value);
                    if (!canParse)
                        return;

                    data[i] = value;
                }
                userData = GfxMetaData.Create(userData.Type, data.ToList());
            }
            else if (userData.Type == GfxMetaDataType.Single)
            {
                float[] data = new float[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    canParse = float.TryParse(values[i], out float value);
                    if (!canParse)
                        return;

                    data[i] = value;
                }
                userData = GfxMetaData.Create(userData.Type, data.ToList());
            }
            else if (userData.Type == GfxMetaDataType.String)
            {
                string[] data = new string[values.Length];
                for (int i = 0; i < values.Length; i++)
                    data[i] = values[i];

                userData = GfxMetaData.Create(userData.Type, data.ToList());
            }
            else if (userData.Type == GfxMetaDataType.Vector3)
            {
                userData = GfxMetaData.Create(userData.Type, new List<Vector3>());
            }
            else if (userData.Type == GfxMetaDataType.Color)
            {
                userData = GfxMetaData.Create(userData.Type, new List<Vector4>());
            }
            userData.Name = name;
        }

        static string GetDataString(GfxMetaData userData, string seperator = "\n")
        {
            var values = userData.GetValue();
            if (values == null) return "";

            switch (userData.Type)
            {
                case GfxMetaDataType.String:
                    return string.Join(seperator, (List<string>)values);
                case GfxMetaDataType.Color:
                    return string.Join(seperator, (List<Vector4>)values);
                case GfxMetaDataType.Vector3:
                    return string.Join(seperator, (List<Vector3>)values);
                case GfxMetaDataType.Single:
                    return string.Join(seperator, (List<float>)values);
                case GfxMetaDataType.Integer:
                    return string.Join(seperator, (List<int>)values);
            }
            return "";
        }
    }
}
