using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CtrLibrary
{
    internal class BcresUIHelper
    {
        public static void DrawFloatArray(string label, ref float[] value)
        {
            if (value.Length == 1)
            {
                var v = value[0];
                if (ImGui.DragFloat(label, ref v))
                    value[0] = v;
            }
            else if (value.Length == 2)
            {
                var v = new Vector2(value[0], value[1]);
                if (ImGui.DragFloat2(label, ref v))
                {
                    value[0] = v.X;
                    value[1] = v.Y;
                }
            }
            else if (value.Length == 3)
            {
                var v = new Vector3(value[0], value[1], value[2]);
                if (ImGui.DragFloat3(label, ref v))
                {
                    value[0] = v.X;
                    value[1] = v.Y;
                    value[2] = v.Z;
                }
            }
            else if (value.Length == 4)
            {
                var v = new Vector4(value[0], value[1], value[2], value[3]);
                if (ImGui.DragFloat4(label, ref v))
                {
                    value[0] = v.X;
                    value[1] = v.Y;
                    value[2] = v.Z;
                    value[3] = v.W;
                }
            }
        }

        public static bool DrawEnum<T>(string label, ref T value, Action changed = null, ImGuiComboFlags flags = ImGuiComboFlags.None)
        {
            bool edited = false;

            if (ImGui.BeginCombo(label, value.ToString(), flags))
            {
                var values = Enum.GetValues(typeof(T));
                foreach (T val in values)
                {
                    bool isSelected = value.Equals(val);
                    string cblabel = val.ToString();

                    if (ImGui.Selectable(cblabel, isSelected))
                    {
                        value = val;
                        changed?.Invoke();
                        edited = true;
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            if (ImGui.IsItemHovered() && ImGui.IsItemActive()) //Check for combo box hover
            {
                var delta = ImGui.GetIO().MouseWheel;
                if (delta < 0) //Check for mouse scroll change going up
                {
                    List<T> list = Enum.GetValues(typeof(T)).Cast<T>().ToList();

                    int index = list.IndexOf(value);
                    if (index < list.Count - 1)
                    { //Shift upwards if possible
                        value = list[index + 1];
                        changed?.Invoke();
                    }
                }
                if (delta > 0) //Check for mouse scroll change going down
                {
                    List<T> list = Enum.GetValues(typeof(T)).Cast<T>().ToList();

                    int index = list.IndexOf(value);
                    if (index > 0)
                    { //Shift downwards if possible
                        value = list[index - 1];
                        changed?.Invoke();
                    }
                }
            }

            return edited;
        }
    }
}
