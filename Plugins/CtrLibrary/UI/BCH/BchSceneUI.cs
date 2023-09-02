using CtrLibrary.Bcres;
using CtrLibrary.Rendering;
using GLFrameworkEngine;
using ImGuiNET;
using MapStudio.UI;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrGfx.Camera;
using SPICA.Formats.CtrGfx.Fog;
using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrGfx.Scene;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CtrLibrary.UI
{
    public class BchSceneUI
    {
        private H3DScene Scene;
        private GfxDict<GfxMetaData> MetaData;

        private List<int> selectedLightsetIndices = new List<int>();
        private List<int> selectedLightIndices = new List<int>();
        private List<int> selectedCameraIndices = new List<int>();
        private List<int> selectedFogIndices = new List<int>();

        public void Init(H3DScene scene, GfxDict<GfxMetaData> metaData = null)
        {
            Scene = scene;
            MetaData = metaData;
        }

        public void Render()
        {
            ImGui.BeginTabBar("sceneTabbar");

            if (ImguiCustomWidgets.BeginTab("sceneTabbar", "Scene Info"))
            {
                DrawSceneInfo();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("sceneTabbar", "User Data"))
            {
                if (MetaData != null)
                    UserDataInfoEditor.Render(MetaData);
                else
                    Bch.UserDataInfoEditor.Render(Scene.MetaData);

                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        private void DrawSceneInfo()
        {
            if (ImGui.CollapsingHeader("Cameras", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawCameraList(selectedCameraIndices);
            }
            if (ImGui.CollapsingHeader("Light Set", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawLightSetList(selectedLightsetIndices);
            }
            if (ImGui.CollapsingHeader("Lights", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawLightsList(selectedLightIndices);
            }
            if (ImGui.CollapsingHeader("Fogs", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawFogList(selectedFogIndices);
            }
        }

        private void DrawCameraList(List<int> selectedIndices)
        {
            if (ImGui.Button($"   {IconManager.ADD_ICON}   ##CamAdd"))
            {
                //Add camera
                int index = Scene.Cameras.Count;
                EditEntry(EditMode.Camera, index, "", (int id, string name) =>
                {
                    Scene.Cameras.Add(new IndexedName() { Index = id, Name = name });
                });
            }

            var diabledTextColor = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
            bool isDisabledEdit = selectedIndices.Count == 0;
            if (isDisabledEdit)
                ImGui.PushStyleColor(ImGuiCol.Text, diabledTextColor);

            ImGui.SameLine();

            bool removed = ImGui.Button($"   {IconManager.DELETE_ICON}   ##CamDel") && selectedIndices.Count > 0;

            if (removed)
            {
                foreach (var index in selectedIndices)
                    this.Scene.Cameras.RemoveAt(index);
                selectedIndices.Clear();
            }

            ImGui.SameLine();
            if (ImGui.Button($"   {IconManager.EDIT_ICON}   ##CamEdit") && selectedIndices.Count > 0)
            {
                var camera = Scene.Cameras[selectedIndices[0]];
                EditEntry(EditMode.Camera, camera.Index, camera.Name, (int id, string name) =>
                {
                    camera.Index = id;
                    camera.Name = name;
                });
            }

            if (isDisabledEdit)
                ImGui.PopStyleColor();

            ImGui.Columns(2);

            ImGui.SetColumnWidth(0, 60);
            ImGuiHelper.BoldText("Index");
            ImGui.NextColumn();
            ImGuiHelper.BoldText("Name");
            ImGui.NextColumn();
            ImGui.Columns(1);

            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);

            if (ImGui.BeginChild("CameraList", new Vector2(ImGui.GetWindowWidth(), 100)))
            {
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 60);

                int index = 0;
                foreach (var camera in Scene.Cameras)
                {
                    bool isSelected = selectedIndices.Contains(index);

                    if (ImGui.Selectable($"{camera.Index}##CameraIndex", ref isSelected, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                            selectedIndices.Clear();

                        selectedIndices.Add(index);
                    }
                    bool isEditMode = isSelected && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0);

                    ImGui.NextColumn();

                    ImGui.Text($"{camera.Name}");

                    ImGui.NextColumn();

                    if (isEditMode)
                    {
                        EditEntry(EditMode.Camera, camera.Index, camera.Name, (int id, string name) =>
                        {
                            camera.Index = id;
                            camera.Name = name;
                        });
                    }

                    index++;
                }
                ImGui.Columns(1);
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }

        private void DrawFogList(List<int> selectedIndices)
        {
            if (ImGui.Button($"   {IconManager.ADD_ICON}   ##FogAdd"))
            {
                //Add fog
                int index = Scene.Fogs.Count;

                EditEntry(EditMode.Fog, index, "", (int id, string name) =>
                {
                    this.Scene.Fogs.Add(new IndexedName() { Index = id, Name = name });
                });
            }

            var diabledTextColor = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
            bool isDisabledEdit = selectedIndices.Count == 0;
            if (isDisabledEdit)
                ImGui.PushStyleColor(ImGuiCol.Text, diabledTextColor);

            ImGui.SameLine();

            bool removed = ImGui.Button($"   {IconManager.DELETE_ICON}   ##FogDel") && selectedIndices.Count > 0;

            if (removed)
            {
                foreach (var index in selectedIndices)
                    this.Scene.Fogs.RemoveAt(index);
                selectedIndices.Clear();
            }

            ImGui.SameLine();
            if (ImGui.Button($"   {IconManager.EDIT_ICON}   ##FogEdit") && selectedIndices.Count > 0)
            {
                var fog = Scene.Fogs[selectedIndices[0]];
                EditEntry(EditMode.Fog, fog.Index, fog.Name, (int id, string name) =>
                {
                    fog.Index = id;
                    fog.Name = name;
                });
            }

            if (isDisabledEdit)
                ImGui.PopStyleColor();

            ImGui.Columns(2);

            ImGui.SetColumnWidth(0, 60);
            ImGuiHelper.BoldText("Index");
            ImGui.NextColumn();
            ImGuiHelper.BoldText("Name");
            ImGui.NextColumn();
            ImGui.Columns(1);

            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);

            if (ImGui.BeginChild("FogList", new Vector2(ImGui.GetWindowWidth(), 100)))
            {
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 60);

                int index = 0;
                foreach (var fog in Scene.Fogs)
                {
                    bool isSelected = selectedIndices.Contains(index);

                    if (ImGui.Selectable($"{fog.Index}##FogIndex", ref isSelected, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                            selectedIndices.Clear();

                        selectedIndices.Add(index);
                    }
                    bool isEditMode = isSelected && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0);

                    ImGui.NextColumn();

                    ImGui.Text($"{fog.Name}");

                    ImGui.NextColumn();

                    if (isEditMode)
                    {
                        EditEntry(EditMode.Fog, fog.Index, fog.Name, (int id, string name) =>
                        {
                            fog.Index = id;
                            fog.Name = name;
                        });
                    }

                    index++;
                }
                ImGui.Columns(1);
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }

        private void DrawLightSetList(List<int> selectedIndices)
        {
            if (ImGui.Button($"   {IconManager.ADD_ICON}   "))
            {
                //Add light set
                this.Scene.LightSets.Add(new IndexedNameArray() { Index = Scene.LightSets.Count });
            }

            var diabledTextColor = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
            bool isDisabledEdit = selectedIndices.Count == 0;
            if (isDisabledEdit)
                ImGui.PushStyleColor(ImGuiCol.Text, diabledTextColor);

            ImGui.SameLine();

            bool removed = ImGui.Button($"   {IconManager.DELETE_ICON}   ") && selectedIndices.Count > 0;

            if (removed)
            {
                foreach (var index in selectedIndices)
                    this.Scene.LightSets.RemoveAt(index);
                selectedIndices.Clear();
            }

            ImGui.SameLine();
            if (ImGui.Button($"   {IconManager.EDIT_ICON}   ") && selectedIndices.Count > 0)
            {
                var lightSet = Scene.LightSets[selectedIndices[0]];
                EditEntry(lightSet.Index, (int id) =>
                {
                    lightSet.Index = id;
                });
            }

            if (isDisabledEdit)
                ImGui.PopStyleColor();

            ImGui.Columns(2);

            ImGui.SetColumnWidth(0, 60);
            ImGuiHelper.BoldText("Index");
            ImGui.NextColumn();
            ImGuiHelper.BoldText("Name");
            ImGui.NextColumn();
            ImGui.Columns(1);

            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);

            if (ImGui.BeginChild("LightSetList", new Vector2(ImGui.GetWindowWidth(), 100)))
            {
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 60);

                int index = 0;
                foreach (var lightSet in Scene.LightSets)
                {
                    bool isSelected = selectedIndices.Contains(index);

                    if (ImGui.Selectable($"{lightSet.Index}##LightSetIndex", ref isSelected, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                            selectedIndices.Clear();

                        selectedIndices.Add(index);
                    }
                    bool isEditMode = isSelected && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0);

                    ImGui.NextColumn();

                    ImGui.Text($"Light Set {lightSet.Index}");

                    ImGui.NextColumn();

                    if (isEditMode)
                    {
                        EditEntry(lightSet.Index, (int id) =>
                        {
                            lightSet.Index = id;
                        });
                    }

                    index++;
                }
                ImGui.Columns(1);
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }

        private void DrawLightsList(List<int> selectedIndices)
        {
            if (ImGui.Button($"   {IconManager.ADD_ICON}   ##LightAdd") && selectedLightsetIndices.Count > 0)
            {
                //Add light
                var light_set = Scene.LightSets[selectedLightsetIndices.FirstOrDefault()];

                int index = Scene.Cameras.Count;
                EditEntry(EditMode.Light, index, "", (int id, string name) =>
                {
                    light_set.Names.Add(name);
                });
            }

            var diabledTextColor = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
            bool isDisabledEdit = selectedIndices.Count == 0;
            if (isDisabledEdit)
                ImGui.PushStyleColor(ImGuiCol.Text, diabledTextColor);

            ImGui.SameLine();

            bool removed = ImGui.Button($"   {IconManager.DELETE_ICON}   ##LightDel") && selectedIndices.Count > 0;

            if (removed && selectedLightsetIndices.Count > 0)
            {
                var light_set = Scene.LightSets[selectedLightsetIndices.FirstOrDefault()];

                foreach (var index in selectedIndices)
                    light_set.Names.RemoveAt(index);
                selectedIndices.Clear();
            }

            ImGui.SameLine();
            if (ImGui.Button($"   {IconManager.EDIT_ICON}   ##LightEdit")
                && selectedIndices.Count > 0 && selectedLightsetIndices.Count > 0)
            {
                var light_set = Scene.LightSets[selectedLightsetIndices.FirstOrDefault()];

                var light = light_set.Names[selectedIndices[0]];
                EditEntry(EditMode.Light, 0, light, (int id, string name) =>
                {
                    light_set.Names[selectedIndices[0]] = light;
                });
            }

            if (isDisabledEdit)
                ImGui.PopStyleColor();

            ImGui.Columns(2);

            ImGui.SetColumnWidth(0, 60);
            ImGuiHelper.BoldText("Index");
            ImGui.NextColumn();
            ImGuiHelper.BoldText("Name");
            ImGui.NextColumn();
            ImGui.Columns(1);

            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);

            if (ImGui.BeginChild("LightsList", new Vector2(ImGui.GetWindowWidth(), 100)))
            {
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 60);

                if (selectedLightsetIndices.Count > 0)
                {
                    var light_set = Scene.LightSets[selectedLightsetIndices.FirstOrDefault()];
                    for (int i = 0; i  < light_set.Names.Count; i++)
                    {
                        bool isSelected = selectedIndices.Contains(i);

                        if (ImGui.Selectable($"{i}##LightIndex", ref isSelected, ImGuiSelectableFlags.SpanAllColumns))
                        {
                            if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                                selectedIndices.Clear();

                            selectedIndices.Add(i);
                        }
                        bool isEditMode = isSelected && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0);

                        ImGui.NextColumn();

                        ImGui.Text($"{light_set.Names[i]}");

                        ImGui.NextColumn();

                        if (isEditMode)
                        {
                            int index = i;

                            EditEntry(EditMode.Light, i, light_set.Names[i], (int id, string name) =>
                            {
                                light_set.Names[index] = name;
                            });
                        }
                    }
                    ImGui.Columns(1);
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }

        private void EditEntry(int index, Action<int> onApply)
        {
            DialogHandler.Show("Entry", () =>
            {
                ImGui.InputInt("Index", ref index, 1);

                DialogHandler.DrawCancelOk();
            }, (o) =>
            {
                if (o)
                    onApply(index);
            }, 250, 80);
        }

        private void EditEntry(EditMode mode, int index, string name,  Action<int, string> onApply)
        {
            bool open_dialog = false;
            StringListSelectionDialog selector = new StringListSelectionDialog();
            foreach (var render in H3DRender.H3DRenderCache)
            {
                switch (mode)
                {
                    case EditMode.Camera:
                        foreach (var cam in render.Scene.Cameras)
                            selector.Strings.Add(cam.Name);
                        break;
                    case EditMode.Fog:
                        foreach (var fog in render.Scene.Fogs)
                            selector.Strings.Add(fog.Name);
                        break;
                    case EditMode.Light:
                        foreach (var light in render.Scene.Lights)
                            selector.Strings.Add(light.Name);
                        break;
                }
            }

            DialogHandler.Show("Entry", () =>
            {
                if (ImGui.Button($"   {IconManager.EDIT_ICON}   "))
                {
                    open_dialog = true;
                }
                ImGui.SameLine();

                if (open_dialog)
                {
                    bool changed = selector.Render(name, ref open_dialog);
                    if (changed)
                    {
                        name = selector.Output;
                    }
                }

                string texName = name == null ? "" : name;
                ImGui.InputText("##Name", ref name, 0x200);

                if (mode != EditMode.Light)
                {
                    ImguiPropertyColumn.Begin("entryPopup");

                    ImGui.AlignTextToFramePadding();
                    ImguiPropertyColumn.InputInt("Index", ref index);
                    ImguiPropertyColumn.End();
                }

                DialogHandler.DrawCancelOk();
            }, (o) =>
            {
                if (o && !string.IsNullOrEmpty(name))
                    onApply(index, name);
            }, 250, 120);
        }

        enum EditMode
        {
            Camera,
            Light,
            Fog,
        }
    }
}
