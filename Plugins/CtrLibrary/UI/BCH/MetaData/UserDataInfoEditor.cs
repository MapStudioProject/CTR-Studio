using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using SPICA.Formats.CtrH3D;
using MapStudio.UI;

namespace CtrLibrary.Bch
{
    public class UserDataInfoEditor
    {
        static List<H3DMetaDataValue> Selected = new List<H3DMetaDataValue>();

        static UserDataDialog ActiveDialog = new UserDataDialog();

        public static void Render(H3DMetaData userDataDict)
        {
            if (userDataDict == null) userDataDict = new H3DMetaData();

            if (ImGui.Button($"   {IconManager.ADD_ICON}   "))
            {
                var userData = new H3DMetaDataValue();
                userData.Name = "";
                ShowDialog(userDataDict, userData);
            }

            var diabledTextColor = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
            bool isDisabledEdit = Selected.Count == 0;
            if (isDisabledEdit)
                ImGui.PushStyleColor(ImGuiCol.Text, diabledTextColor);

            ImGui.SameLine();

            bool removed = ImGui.Button($"   {IconManager.DELETE_ICON}   ") && Selected.Count > 0;

            ImGui.SameLine();
            if (ImGui.Button($"   {IconManager.EDIT_ICON}   ") && Selected.Count > 0)
            {
                EditUserData(userDataDict, Selected[0]);
            }

        /*    ImGui.SameLine();
            if (ImGui.Button($"   {IconManager.COPY_ICON}   ") && Selected.Count > 0)
            {
                Dictionary<string, object> usd = new Dictionary<string, object>();
                foreach (var param in Selected)
                    usd.Add($"{param.Type}|{param.Name}", param.GetData());
                ImGui.SetClipboardText(Newtonsoft.Json.JsonConvert.SerializeObject(usd));
            }
            ImGui.SameLine();
            if (ImGui.Button($"   {IconManager.PASTE_ICON}   ") && Selected.Count > 0)
            {
                var json = ImGui.GetClipboardText();
                var usd = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (usd == null)
                    return;

                var userData = BfresLibrary.TextConvert.UserDataConvert.Convert(usd);
                foreach (var userEntry in userData.Values)
                {
                    if (!userDataDict.ContainsKey(userEntry.Name))
                        userDataDict.Add(userEntry.Name, userEntry);
                    else
                        userDataDict[userEntry.Name] = userEntry;
                }
            }*/

            if (isDisabledEdit)
                ImGui.PopStyleColor();

            RenderHeader();

            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);

            if (ImGui.BeginChild("USER_DATA_LIST"))
            {
                int index = 0;
                foreach (var userData in userDataDict)
                {
                    bool isSelected = Selected.Contains(userData);

                    ImGui.Columns(2);
                    if (ImGui.Selectable(userData.Name, isSelected, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                            Selected.Clear();

                        Selected.Add(userData);
                    }
                    if (isSelected && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                        EditUserData(userDataDict, userData);

                    ImGui.NextColumn();
                    ImGui.Text(GetDataString(userData, ","));
                    ImGui.NextColumn();

                    if (isSelected && ImGui.IsMouseDoubleClicked(0))
                    {
                        ImGui.OpenPopup("##user_data_dialog");
                    }
                    index++;

                    ImGui.Columns(1);
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();

            if (removed)
            {
                foreach (var usd in Selected)
                    userDataDict.Remove(usd);
                Selected.Clear();
            }
        }

        static void EditUserData(H3DMetaData userDataDict, H3DMetaDataValue selected)
        {
            //Apply data to new instance (so edits can be applied after)
            var userData = new H3DMetaDataValue();
            userData.Name = selected.Name;
            userData.Values = selected.Values;
            userData.Type = selected.Type;

            ShowDialog(userDataDict, userData);
        }

        static void ShowDialog(H3DMetaData userDataDict, H3DMetaDataValue userData)
        {
            string previousName = userData.Name;

            ActiveDialog.Load(userData);

            DialogHandler.Show("User Data", 300, 400, () =>
            {
                ActiveDialog.Render(userData);
            }, (ok) =>
            {
                if (!ok)
                    return;

                //Previous old entry
                if (previousName != userData.Name && userDataDict.Contains(previousName))
                    userDataDict.Remove(userDataDict[previousName]);

                //Add new entry or overrite the existing one
                if (!userDataDict.Contains(userData.Name))
                    userDataDict.Add(userData);
                else
                    userDataDict[userData.Name] = userData;

                Selected.Clear();
                Selected.Add(userData);
            });
        }

        static void RenderHeader()
        {
            ImGui.Columns(2);
            ImGuiHelper.BoldText(TranslationSource.GetText("NAME"));
            ImGui.NextColumn();
            ImGuiHelper.BoldText(TranslationSource.GetText("VALUE"));
            ImGui.Separator();
            ImGui.Columns(1);
        }

        static string GetDataString(H3DMetaDataValue userData, string seperator = "\n")
        {
            if (userData.Values == null) return "<NULL>";

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
                case H3DMetaDataType.BoundingBox:
                    {
                        var boundings = (List<H3DBoundingBox>)userData.Values;
                        if (boundings.Count > 0) {
                            return $"Center {boundings[0].Center} Extend {boundings[0].Size}";
                        }
                        else
                            return "None";
                    }
                case H3DMetaDataType.VertexData:
                    return "";
            }
            return "";
        }
    }
}
