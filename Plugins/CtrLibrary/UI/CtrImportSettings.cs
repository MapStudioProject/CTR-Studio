using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIFramework;
using ImGuiNET;
using MapStudio.UI;
using System.Numerics;
using Newtonsoft.Json;
using SPICA.PICA.Commands;

namespace CtrLibrary
{
    /// <summary>
    /// GUI for displaying and configuring model import settings.
    /// </summary>
    internal class CtrModelImportUI : Window
    {
        /// <summary>
        /// The import settings for configuring the model import.
        /// </summary>
        public CtrImportSettings Settings = new CtrImportSettings();

        private ImportPreset ImportSettingsPreset = ImportPreset.Default;

        public enum ImportPreset
        {
            Default,
            MarioKart7Character,
            Smash3DS,
            AnimalCrossing,
            Pokemon,
        }

        //Preset selector to toggle the material to assign as.
        MaterialPresetSelectionDialog Preset = new MaterialPresetSelectionDialog();

        private bool openPresetSelector = false;
        private string _savePresetName = "";

        public CtrModelImportUI()
        {
            Settings = CtrImportSettings.Load();
            Preset.Output = Settings.MaterialPresetFile;
            if (File.Exists(Preset.Output))
                _savePresetName = Path.GetFileNameWithoutExtension(Preset.Output);
        }

        public override void Render()
        {
            if (ImGui.Button("Save Settings"))
                Settings.Save();

            ImguiCustomWidgets.ComboScrollable("Settings Preset", ImportSettingsPreset.ToString(), ref ImportSettingsPreset, () =>
            {
                this.SetPreset();
            });

            ImGui.BeginTabBar("importTabs");
            if (ImguiCustomWidgets.BeginTab("importTabs", "Info"))
            {
                DrawMatPreset();

                ImGui.Checkbox("Is Pokemon ORAS/XY", ref Settings.IsPokemon);

                ImGui.Checkbox("Generate .div (MK7 Custom Track)", ref Settings.DivideMK7);
                ImGui.Checkbox("Display Both Face Sides", ref Settings.DisplayBothFaces);

                ImGui.Checkbox("Use Original Materials", ref Settings.UseOriginalMaterials);
                ImGui.Checkbox("No Skeleton", ref Settings.DisableSkeleton);

                ImGui.Checkbox("Import Textures", ref Settings.ImportTextures);

                if (!Settings.DisableSkeleton)
                    ImGui.Checkbox("Import Bones (Experimental)", ref Settings.ImportBones);

                ImGuiHelper.Tooltip("Imports bones from .dae/.fbx. Keep in mind blender is difficult to work with bones and may not output very well.");
                ImGui.Checkbox("Import Tangents", ref Settings.ImportTangents);
                ImGui.Checkbox("Import Vertex Colors", ref Settings.ImportVertexColors);

                ImGui.Checkbox("Flip UVs", ref Settings.FlipUVs);
                ImGui.Checkbox("Optimize Vertices", ref Settings.Optimize);
                ImGui.Checkbox("Limit Skin Count", ref Settings.LimitSkinCount);
                if (Settings.LimitSkinCount)
                {
                    ImGui.SliderInt("Skin Count Limit", ref Settings.SkinCountLimit, 0, 4);
                }

                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("importTabs", "Format"))
            {
                if (ImGui.Button("Reset"))
                {
                    Settings.Position = new AttributeSetting(PICAAttributeFormat.Float, 1.0f);
                    Settings.Normal = new AttributeSetting(PICAAttributeFormat.Float, 1.0f);
                    Settings.TexCoord = new AttributeSetting(PICAAttributeFormat.Float, 1.0f);
                    Settings.BoneIndices = new AttributeSetting(PICAAttributeFormat.Short, 1.0f);
                    Settings.Colors = new AttributeSetting(PICAAttributeFormat.Byte, 1.0f / 255f);
                    Settings.BoneWeights = new AttributeSetting(PICAAttributeFormat.Float, 1.0f);
                    Settings.Tangents = new AttributeSetting(PICAAttributeFormat.Float, 1.0f);
                }
                    
                ImGui.BeginColumns("Formatclmns", 3);
                ImGuiHelper.BoldText("Attribute");
                ImGui.NextColumn();
                ImGuiHelper.BoldText("Format");
                ImGui.NextColumn();
                ImGuiHelper.BoldText("Scale");
                ImGui.NextColumn();

                DrawFormat("Position Format", ref Settings.Position);
                DrawFormat("Normal Format", ref Settings.Normal);
                DrawFormat("Colors Format", ref Settings.Colors);
                DrawFormat("Tex Coord Format", ref Settings.TexCoord);
                DrawFormat("Bone Indices Format", ref Settings.BoneIndices);
                DrawFormat("Bone Weights Format", ref Settings.BoneWeights);

                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();

            DialogHandler.DrawCancelOk();
        }

        private void SetPreset()
        {
            Settings = CtrImportSettings.Load();
            switch (ImportSettingsPreset)
            {
                case ImportPreset.MarioKart7Character:
                    Settings.LimitSkinCount = true; //Shaders only support rigid skin count
                    Settings.SkinCountLimit = 1;
                    break;
                case ImportPreset.Pokemon:
                    Settings.IsPokemon = true; //Custom vertex shader settings and custom user data
                    break;
                case ImportPreset.AnimalCrossing: 
                    Settings.ImportVertexColors = false; //Hemi light usage. No vertex colors
                    break;
                case ImportPreset.Smash3DS: 
                    Settings.ImportVertexColors = false; //Hemi light usage. No vertex colors
                    Settings.UseSingleAttributeBuffer = true; //Single buffer
                    Settings.IsSmash3DS = true; //Custom vertex shader settings
                    Settings.BoneWeights.Scale = 0.01f; //Required scale for vertex shader
                    //Required formats for vertex shader
                    Settings.BoneWeights.Format = PICAAttributeFormat.Byte;
                    Settings.BoneIndices.Format = PICAAttributeFormat.Byte;
                    Settings.BoneIndices.Scale = 1;
                    Settings.Normal.Format = PICAAttributeFormat.Byte;
                    Settings.Normal.Scale = 1.0f / sbyte.MaxValue;
                    Settings.TexCoord.Format = PICAAttributeFormat.Short;
                    Settings.TexCoord.Scale = 1.0f / short.MaxValue;
                    //Todo the game uses shorts but these are tricky to encode nicely
                    //Float will work but will increase file size
                    Settings.Position.Format = PICAAttributeFormat.Float;
                    Settings.Position.Scale = 1.0f;
                    break;
            }
        }

        private void DrawMatPreset()
        {
            ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(
                  ThemeHandler.Theme.FrameBg.X,
                  ThemeHandler.Theme.FrameBg.Y,
                  ThemeHandler.Theme.FrameBg.Z, 255));

            if (ImGui.Button("Material Preset Selector"))
            {
                Preset.Output = "";
                Preset.LoadMaterialPresets();
                openPresetSelector = true;
            }
            ImGui.SameLine();

            ImGui.PushItemWidth(250);
            if (ImGui.InputText("Preset Name", ref _savePresetName, 0x200))
            {
            }
            ImGui.PopItemWidth();

            if (openPresetSelector)
            {
                if (Preset.Render("", ref openPresetSelector))
                {
                    _savePresetName = Path.GetFileNameWithoutExtension(Preset.Output);
                    Settings.MaterialPresetFile = Preset.Output;
                }
            }

            ImGui.PopStyleColor();
        }

        private void DrawFormat(string label, ref AttributeSetting setting)
        {
            ImGuiHelper.BoldText($"{label}:");
            ImGui.NextColumn();

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 2);

            var formatList = new PICAAttributeFormat[]
            {
                PICAAttributeFormat.Float, PICAAttributeFormat.Short, PICAAttributeFormat.Byte,
            };

            if (ImGui.BeginCombo(label, $"{setting.Format}##{label}Format"))
            {
                foreach (PICAAttributeFormat val in formatList)
                {
                    bool isSelected = setting.Format.Equals(val);
                    string cblabel = val.ToString();

                    if (ImGui.Selectable(cblabel, isSelected)) {
                        setting.Format = val;
                        UpdateFormat(label, ref setting);
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 2);
            ImGui.InputFloat($"##{label}Scale", ref setting.Scale, 0, 0, setting.Scale.ToString(), ImGuiInputTextFlags.CharsDecimal);
            ImGui.PopItemWidth();

            ImGui.NextColumn();
        }

        private void UpdateFormat(string label, ref AttributeSetting setting)
        {
            //Apply scale value based on attribute type when format has been switched

            //Position gets calculated and not required to setup.

            if (label == "Normal Format")
            {
                switch (setting.Format)
                {
                    case PICAAttributeFormat.Float: setting.Scale = 1.0f; break;
                    case PICAAttributeFormat.Short: setting.Scale = 1.0f / 32767f; break;
                    case PICAAttributeFormat.Byte: setting.Scale = 1.0f / sbyte.MaxValue; break;
                }
            }
            if (label == "Bone Weights Format")
            {
                switch (setting.Format)
                {
                    case PICAAttributeFormat.Float: setting.Scale = 1.0f; break;
                    case PICAAttributeFormat.Short: setting.Scale = 1.0f / 65535f; break;
                    case PICAAttributeFormat.Byte: setting.Scale = 1.0f / byte.MaxValue; break;
                }
            }
            if (label == "Tex Coord Format")
            {
                switch (setting.Format)
                {
                    case PICAAttributeFormat.Float: setting.Scale = 1.0f; break;
                    case PICAAttributeFormat.Short: setting.Scale = 1.0f / 32767f; break;
                    case PICAAttributeFormat.Byte: setting.Scale = 1.0f / sbyte.MaxValue; break;
                }
            }
            if (label == "Colors Format")
            {
                switch (setting.Format)
                {
                    case PICAAttributeFormat.Float: setting.Scale = 1.0f; break;
                    case PICAAttributeFormat.Short: setting.Scale = 1.0f / 65535f; break;
                    case PICAAttributeFormat.Byte: setting.Scale = 1.0f / byte.MaxValue; break;
                }
            }

            if (label == "Bone Indices Format")
                setting.Scale = 1.0f;
        }
    }

    /// <summary>
    /// Import settings used to configure importing an H3D model.
    /// </summary>
    public class CtrImportSettings
    {
        //Saved settings path
        const string SavedFilePath = "CTRModelImportSettings.json";

        /// <summary>
        /// Determines to divide the mesh into multiple sub meshes (BCRES only)
        /// </summary>
        public bool DivideMK7;

        /// <summary>
        /// Determines to use Pokemon specific vertex shader adjustments on import.
        /// </summary>
        public bool IsPokemon;

        /// <summary>
        /// Determines to use Smash 3DS specific vertex shader adjustments on import.
        /// </summary>
        public bool UseSingleAttributeBuffer = false;

        /// <summary>
        /// Determines to use Smash 3DS specific vertex shader adjustments on import.
        /// </summary>
        public bool IsSmash3DS = false;

        /// <summary>
        /// Determines to keep original materials during import.
        /// </summary>
        public bool UseOriginalMaterials = true;

        /// <summary>
        /// Determines to load the raw vertex data into one buffer.
        /// Indices will index just this one buffer.
        /// Used by Smash 3DS
        /// </summary>
        public bool UseSingleBuffer = false;

        /// <summary>
        /// Determines to remove culling for both back/front faces.
        /// </summary>
        public bool DisplayBothFaces = false;

        /// <summary>
        /// Imports texture linked from the provided .dae/.fbx file.
        /// </summary>
        public bool ImportTextures = true;

        /// <summary>
        /// Don't import any bones.
        /// </summary>
        public bool DisableSkeleton = false;

        /// <summary>
        /// Imports bones from the provided .dae/.fbx instead of using original boneset.
        /// </summary>
        public bool ImportBones = false;

        /// <summary>
        /// Determines to import and calculate tangents for all meshes.
        /// </summary>
        public bool ImportTangents = false;

        /// <summary>
        /// Determines to import vertex colors for all meshes.
        /// </summary>
        public bool ImportVertexColors = true;

        /// <summary>
        /// Determines to optmize the vertices before import by removing duplicates.
        /// </summary>
        public bool Optimize = true;

        /// <summary>
        /// Flips UVs vertically for all meshes and UV sets.
        /// </summary>
        public bool FlipUVs = false;

        public bool LimitSkinCount = false;

        public int SkinCountLimit = 4;

        /// <summary>
        /// The preset file to assign to all materials (if not using original material setting)
        /// </summary>
        public string MaterialPresetFile = "";

        //Attribute settings
        public AttributeSetting Position = new AttributeSetting(PICAAttributeFormat.Float, 1.0f);
        public AttributeSetting Normal = new AttributeSetting(PICAAttributeFormat.Float, 1.0f);
        public AttributeSetting TexCoord = new AttributeSetting(PICAAttributeFormat.Float, 1.0f);
        public AttributeSetting BoneIndices = new AttributeSetting(PICAAttributeFormat.Byte, 1);
        public AttributeSetting Colors = new AttributeSetting(PICAAttributeFormat.Byte, 1.0f / 255f);
        public AttributeSetting BoneWeights = new AttributeSetting(PICAAttributeFormat.Byte, 0.01f);
        public AttributeSetting Tangents = new AttributeSetting(PICAAttributeFormat.Float, 1.0f);

        /// <summary>
        /// Saves the settings to disk.
        /// </summary>
        public void Save()
        {
            File.WriteAllText(SavedFilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        /// <summary>
        /// Loads the setting file from disk if exists.
        /// </summary>
        public static CtrImportSettings Load()
        {
            if (File.Exists(SavedFilePath))
                return JsonConvert.DeserializeObject<CtrImportSettings>(File.ReadAllText(SavedFilePath));
            else
                return new CtrImportSettings();
        }
    }

    /// <summary>
    /// Represents settings for a single vertex attribute.
    /// </summary>
    public class AttributeSetting
    {
        /// <summary>
        /// The formmat to write the vertex attributee as.
        /// </summary>
        public PICAAttributeFormat Format;

        /// <summary>
        /// The scale used to compress the attribute with the format used.
        /// </summary>
        public float Scale;

        public AttributeSetting(PICAAttributeFormat format, float scale)
        {
            Format = format;
            Scale = scale;
        }
    }
}
