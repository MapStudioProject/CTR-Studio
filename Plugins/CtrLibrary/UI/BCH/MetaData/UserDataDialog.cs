using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using ImGuiNET;
using SPICA.Formats.CtrH3D;
using MapStudio.UI;

namespace CtrLibrary.Bch
{
    public class UserDataDialog
    {
        public List<string> ValuePresets = new List<string>();

        public bool canParse = true;

        private string ValuesEdit = "";

        public void Load(H3DMetaDataValue userData)
        {
            ValuesEdit = GetDataString(userData);
            if (string.IsNullOrEmpty(ValuesEdit))
                ValuesEdit = "";
        }

        public void Render(H3DMetaDataValue userData)
        {
            if (!canParse)
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Failed to parse type {userData.Type}!");
            if (string.IsNullOrEmpty(userData.Name))
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Make sure to input a name!");

            ImGuiHelper.InputFromText(TranslationSource.GetText("NAME"), userData, "Name", 200);

            bool formatChanged = BcresUIHelper.DrawEnum(TranslationSource.GetText("TYPE"), ref userData.Type);
            if (formatChanged)
                UpdateValues(userData);

            var windowSize = ImGui.GetWindowSize();
            var textSize = new Vector2(ImGui.GetWindowWidth(), ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - 32);

            if (userData.Type == H3DMetaDataType.BoundingBox)
            {
                var val = userData.Values as List<H3DBoundingBox>;
                var bounding = val.Count > 0 ? val[0] : new H3DBoundingBox();
                var orientRow1 = new Vector3(bounding.Orientation.M11, bounding.Orientation.M12, bounding.Orientation.M13);
                var orientRow2 = new Vector3(bounding.Orientation.M21, bounding.Orientation.M22, bounding.Orientation.M23);
                var orientRow3 = new Vector3(bounding.Orientation.M31, bounding.Orientation.M32, bounding.Orientation.M33);
                bool update = false;

                update |= ImGui.DragFloat3("Center", ref bounding.Center);
                update |= ImGui.DragFloat3("Extend", ref bounding.Size);
                update |= ImGui.DragFloat3("Rotation Row 0", ref orientRow1);
                update |= ImGui.DragFloat3("Rotation Row 1", ref orientRow2);
                update |= ImGui.DragFloat3("Rotation Row 2", ref orientRow3);
                if (update)
                {
                    bounding.Orientation = new SPICA.Math3D.Matrix3x3(
                        orientRow1.X, orientRow1.Y, orientRow1.Z,
                        orientRow2.X, orientRow2.Y, orientRow2.Z,
                        orientRow3.X, orientRow3.Y, orientRow3.Z);

                    if (val.Count > 0)
                        val[0] = bounding;
                    else
                        userData.Values.Add(bounding);
                }
            }
            else //Draw text editor with multi line for parsing multiple values
            {
                ImGui.PushItemWidth(ImGui.GetWindowWidth());
                if (ImGui.InputTextMultiline(TranslationSource.GetText("VALUE"), ref ValuesEdit, 0x1000, textSize))
                {
                    UpdateValues(userData);
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

        void UpdateValues(H3DMetaDataValue userData)
        {
            canParse = true;
            string[] values = ValuesEdit.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();

            if (userData.Type == H3DMetaDataType.Integer)
            {
                int[] data = new int[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    canParse = int.TryParse(values[i], out int value);
                    if (!canParse)
                        return;

                    data[i] = value;
                }
                userData.Values = data.ToList();
            }
            else if (userData.Type == H3DMetaDataType.Single)
            {
                float[] data = new float[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    canParse = float.TryParse(values[i], out float value);
                    if (!canParse)
                        return;

                    data[i] = value;
                }
                userData.Values = data.ToList();
            }
            else if (userData.Type == H3DMetaDataType.ASCIIString)
            {
                string[] data = new string[values.Length];
                for (int i = 0; i < values.Length; i++)
                    data[i] = values[i];

                userData.Values = data.ToList();
            }
            else if (userData.Type == H3DMetaDataType.UnicodeString)
            {
                H3DStringUtf16[] data = new H3DStringUtf16[values.Length];
                for (int i = 0; i < values.Length; i++)
                    data[i] = new H3DStringUtf16(values[i]);

                userData.Values = data.ToList();
            }
            else if (userData.Type == H3DMetaDataType.BoundingBox)
            {
                userData.Values = new List<H3DBoundingBox>();
            }
        }

        static string GetDataString(H3DMetaDataValue userData, string seperator = "\n")
        {
            if (userData.Values == null)
                return "";

            switch (userData.Type)
            {
                case H3DMetaDataType.ASCIIString:
                    return string.Join(seperator, (List<string>)userData.Values);
                case H3DMetaDataType.UnicodeString:
                    return string.Join(seperator, (List<H3DStringUtf16>)userData.Values);
                case H3DMetaDataType.Single:
                    return string.Join(seperator, (List<float>)userData.Values);
                case H3DMetaDataType.Integer:
                    return string.Join(seperator, (List<int>)userData.Values);
            }
            return "";
        }
    }
}
