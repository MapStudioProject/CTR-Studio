using SPICA.Formats.CtrH3D.Model.Material;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using MapStudio.UI;
using SPICA.PICA.Commands;
using CtrLibrary.Rendering;
using CtrLibrary.Bch;
using System.Numerics;
using GLFrameworkEngine;
using SPICA.Formats.CtrH3D.LUT;
using CtrLibrary.UI;

namespace CtrLibrary
{
    internal class MaterialLutUI
    {
        H3DMaterial Material;
        MaterialWrapper MaterialWrapper;

        private string TableName = "";

        GLTexture2D texture;

        string[][] LayerConfig = new string[][]
        {
            new string[] { "Spot", "Dist0", "ReflectR" },
            new string[] { "Spot", "ReflectR", "Fresnel" },
            new string[] { "Dist0", "Dist1", "ReflectR" },
            new string[] { "Dist0", "Dist1", "Fresnel" },
            new string[] { "Spot", "Dist0", "ReflectR", "ReflectG", "ReflectB" },
            new string[] { "Spot", "Dist0", "ReflectR", "ReflectG", "ReflectB", "Fresnel" },
            new string[] { "Spot", "Dist0", "Dist1", "ReflectR", "Fresnel" },
            new string[] { "Spot", "Dist0", "Dist1", "ReflectR", "ReflectG", "ReflectB", "Fresnel" },
        };

        string[] LookupTypes = new string[] {
            "Dist 0", "Dist 1", "Fresnel", "Specular Red", "Specular Green", "Specular Blue"
        };

        Dictionary<PICALUTScale, string> ScaleList = new Dictionary<PICALUTScale, string>()
        {
            { PICALUTScale.Quarter, "0.25" },
            { PICALUTScale.Half, "0.5" },
            { PICALUTScale.One, "1" },
            { PICALUTScale.Two, "2" },
            { PICALUTScale.Four, "4" },
            { PICALUTScale.Eight, "8" },
        };

        Dictionary<PICALUTInput, string> InputListProgramming = new Dictionary<PICALUTInput, string>()
        {
            { PICALUTInput.CosLightNormal, "dot(Light, Normal)" },
            { PICALUTInput.CosNormalView,  "dot(Normal, View)" },
            { PICALUTInput.CosNormalHalf,  "dot(Normal, Half-Angle)" },
            { PICALUTInput.CosLightSpot,   "dot(Light, Light-Direction)" },
            { PICALUTInput.CosViewHalf,    "dot(View, Half-Angle)" },
            { PICALUTInput.CosPhi,         "dot(HalfProj, Tangent)" },
        };

        Dictionary<PICALUTInput, string> InputList = new Dictionary<PICALUTInput, string>()
        {
            { PICALUTInput.CosLightNormal, "Fixed Light" },
            { PICALUTInput.CosNormalView,  "Phong" },
            { PICALUTInput.CosNormalHalf,  "Blinn-Phong" },
            { PICALUTInput.CosLightSpot,   "Fixed Light Direction" },
            { PICALUTInput.CosViewHalf,    "View Half Angle" },
            { PICALUTInput.CosPhi,         "dot(HalfProj, Tangent)" },
        };

        public void Init(H3DMaterial mat, MaterialWrapper wrapper)
        {
            Material = mat;
            MaterialWrapper = wrapper;
            TableName = FindLUTSet();
        }

        public void Render()
        {
            var abs = Material.MaterialParams.LUTInputAbsolute;
            var sca = Material.MaterialParams.LUTInputScale;
            var sel = Material.MaterialParams.LUTInputSelection;
            var prm = Material.MaterialParams;
            string[] layer = LayerConfig[(int)prm.TranslucencyKind];

            bool clampSpecular = Material.MaterialParams.FragmentFlags.HasFlag(H3DFragmentFlags.IsClampHighLightEnabled);
            bool geoFactor0 = Material.MaterialParams.FragmentFlags.HasFlag(H3DFragmentFlags.IsLUTGeoFactor0Enabled);
            bool geoFactor1 = Material.MaterialParams.FragmentFlags.HasFlag(H3DFragmentFlags.IsLUTGeoFactor1Enabled);

            bool hasDist0 = layer.Contains("Dist0");
            bool hasDist1 = layer.Contains("Dist1");
            bool hasReflectR = layer.Contains("ReflectR");
            bool hasReflectG = layer.Contains("ReflectG");
            bool hasReflectB = layer.Contains("ReflectB");
            bool hasFresnel = layer.Contains("Fresnel");

            if (ImGui.CollapsingHeader("LUT Settings", ImGuiTreeNodeFlags.DefaultOpen))
            {
                //Clamps "CosLightNormal" 0 to 1
                if (ImGui.Checkbox("Clamp Highlight", ref clampSpecular))
                {
                    Material.MaterialParams.SetFlag(H3DFragmentFlags.IsClampHighLightEnabled, clampSpecular);
                    UpdateShaders();
                }

                if (ImGui.Checkbox("Use Geometry Factor (Specular 0)", ref geoFactor0))
                {
                    Material.MaterialParams.SetFlag(H3DFragmentFlags.IsLUTGeoFactor0Enabled, geoFactor0);
                    UpdateShaders();
                }

                if (ImGui.Checkbox("Use Geometry Factor (Specular 1)", ref geoFactor1))
                {
                    Material.MaterialParams.SetFlag(H3DFragmentFlags.IsLUTGeoFactor1Enabled, geoFactor1);
                    UpdateShaders();
                }

                bool updatedTerms = ImGuiHelper.ComboFromEnum<H3DTranslucencyKind>("Active Terms", Material.MaterialParams, "TranslucencyKind");
                if (updatedTerms)
                {
                    Material.MaterialParams.FragmentFlags &= ~H3DFragmentFlags.IsLUTDist0Enabled;
                    Material.MaterialParams.FragmentFlags &= ~H3DFragmentFlags.IsLUTDist1Enabled;
                    Material.MaterialParams.FragmentFlags &= ~H3DFragmentFlags.IsLUTReflectionEnabled;

                    if (hasDist0)
                        Material.MaterialParams.FragmentFlags |= H3DFragmentFlags.IsLUTDist0Enabled;
                    if (hasDist1)
                        Material.MaterialParams.FragmentFlags |= H3DFragmentFlags.IsLUTDist1Enabled;
                    if (hasReflectR || hasReflectG || hasReflectB)
                        Material.MaterialParams.FragmentFlags |= H3DFragmentFlags.IsLUTReflectionEnabled;
                    UpdateShaders();
                }
                ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeHandler.Theme.FrameBg);
                if (ImGui.BeginChild("layerTerms", new System.Numerics.Vector2(ImGui.GetWindowWidth() - 3, 50)))
                {
                    for (int i = 0; i < layer.Length; i++)
                    {
                        ImGui.Text(layer[i]);

                        if (i != 4)
                            ImGui.SameLine();
                    }
                }
                ImGui.EndChild();
                ImGui.PopStyleColor();
            }

            ImGui.BeginColumns("lookupTblClms", 2);
            ImGui.SetColumnWidth(0, 125);

            ImGui.AlignTextToFramePadding();
            ImGuiHelper.BoldText($"Lookup Table:");
            ImGui.NextColumn();

            ImGui.PushItemWidth(200);
            if (ImGui.InputText("##tableL", ref TableName, 0x200))
                SetLUTSet(TableName);

            ImGui.PopItemWidth();

            ImGui.SameLine();

            ImGui.PushItemWidth(20);
            if (ImGui.BeginCombo("##lutSelect", "", ImGuiComboFlags.HeightLargest | ImGuiComboFlags.PopupAlignLeft))
            {
                ImGuiHelper.BeginBoldText();
                if (ImGui.MenuItem("Open LUT Folder"))
                {
                    string folder = Path.Combine(Toolbox.Core.Runtime.ExecutableDir, "LUTS");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    FileUtility.OpenFolder(folder);
                }
                if (ImGui.MenuItem("Reload LUT Folder"))
                {
                    string folder = Path.Combine(Toolbox.Core.Runtime.ExecutableDir, "LUTS");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    var render = H3DRender.RenderCache.FirstOrDefault();
                    LUTCacheManager.Setup(true);
                    LUTCacheManager.Load(render);

                }
                ImGuiHelper.EndBoldText();

                ImGui.Spacing();

                foreach (var lut in LUTCacheManager.Cache)
                {
                    if (ImGui.Selectable($"   {'\uf0ce'}    {lut.Key}"))
                    {
                        TableName = lut.Key;
                        SetLUTSet(TableName);
                    }
                }

                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();

            ImGui.NextColumn();
            ImGui.EndColumns();

            ImGui.Text("");

            if (SPICA.Rendering.Renderer.DebugLUTShadingMode != 0)
            {
                SPICA.Rendering.Renderer.DebugLUTShadingMode = 0;
                GLContext.ActiveContext.UpdateViewport = true;
            }

            DrawLUTSampler("Distribution Specular 0", ref prm.LUTDist0SamplerName, ref sel.Dist0, ref sca.Dist0, ref abs.Dist0, hasDist0, 0);
            DrawLUTSampler("Distribution Specular 1", ref prm.LUTDist1SamplerName, ref sel.Dist1, ref sca.Dist1, ref abs.Dist1, hasDist1, 1);
            DrawLUTSampler("Reflect R ", ref prm.LUTReflecRSamplerName, ref sel.ReflecR, ref sca.ReflecR, ref abs.ReflecR, hasReflectR, 3);
            DrawLUTSampler("Reflect G", ref prm.LUTReflecGSamplerName, ref sel.ReflecG, ref sca.ReflecG, ref abs.ReflecG, hasReflectG, 4);
            DrawLUTSampler("Reflect B", ref prm.LUTReflecBSamplerName, ref sel.ReflecB, ref sca.ReflecB, ref abs.ReflecB, hasReflectB, 5);
            DrawLUTSampler("Fresnel", ref prm.LUTFresnelSamplerName, ref sel.Fresnel, ref sca.Fresnel, ref abs.Fresnel, hasFresnel, 2);
        }

        private void DrawLUTSampler(string label, ref string name, ref PICALUTInput sel, ref PICALUTScale scale, ref bool abs, bool enable, int index)
        {
            if (!ImGui.CollapsingHeader(label, ImGuiTreeNodeFlags.DefaultOpen))
                return;

            ImGui.BeginColumns("lookupSamplerClms", 4);
            ImGui.SetColumnWidth(0, 35);
            ImGui.SetColumnWidth(1, 250);
            ImGui.SetColumnWidth(2, 250);
            ImGui.SetColumnWidth(3, 100);
            
            if (!enable)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
            }

            void SetToggle(H3DFragmentFlags flags)
            {
                bool use = Material.MaterialParams.FragmentFlags.HasFlag(flags);
                if (ImGui.Checkbox($"##Use{label}", ref use))
                {
                    Material.MaterialParams.SetFlag(flags, use);
                    UpdateShaders();
                }
            }

            if (label.Contains("Reflect")) SetToggle(H3DFragmentFlags.IsLUTReflectionEnabled);
            if (label.Contains("Distribution Specular 0")) SetToggle(H3DFragmentFlags.IsLUTDist0Enabled);
            if (label.Contains("Distribution Specular 1")) SetToggle(H3DFragmentFlags.IsLUTDist1Enabled);
            if (label.Contains("Fresnel"))
            {
                bool use = Material.MaterialParams.FresnelSelector != H3DFresnelSelector.No;
                if (ImGui.Checkbox($"##Use{label}", ref use))
                {
                    if (!use)
                        Material.MaterialParams.FresnelSelector = H3DFresnelSelector.No;
                    else
                        Material.MaterialParams.FresnelSelector = H3DFresnelSelector.PriSec;

                    UpdateShaders();
                }
            }

            ImGui.NextColumn();

            DrawSamplerSelector(label, ref name, TableName, index);

            ImGui.NextColumn();

            DrawInputSelector($"##sel{label}", ref sel);
            ImGui.NextColumn();

            DrawScaleSelector($"##scale{label}", ref scale);
            ImGui.NextColumn();
            ImGui.EndColumns();

            var width = 200 + 250 + 185;

            //ImGuiHelper.BoldText($"{label}:");
            //ImGui.NextColumn();

            if (LUTCacheManager.Cache.ContainsKey(TableName))
            {
                string sampler = name;

                var lutTable = LUTCacheManager.Cache[TableName];
                var samp = lutTable.Samplers.FirstOrDefault(x => x.Name == sampler);
                if (samp != null)
                {
                    DrawLightingGradient(texture, width, GenerateLUT(samp));
                }
            }

            var pos = ImGui.GetCursorPos();

            if (label == "Fresnel")
            {
                bool fresnelSecPrimary = Material.MaterialParams.FresnelSelector.HasFlag(H3DFresnelSelector.PriSec);

                bool fresnelPrimary = Material.MaterialParams.FresnelSelector.HasFlag(H3DFresnelSelector.Pri) || fresnelSecPrimary;
                bool fresnelSecondary = Material.MaterialParams.FresnelSelector.HasFlag(H3DFresnelSelector.Sec) || fresnelSecPrimary;

                void UpdateSelector()
                {
                    if (fresnelPrimary && fresnelSecondary)
                        Material.MaterialParams.FresnelSelector = H3DFresnelSelector.PriSec;
                    else if (fresnelPrimary)
                        Material.MaterialParams.FresnelSelector = H3DFresnelSelector.Pri;
                    else if (fresnelSecondary)
                        Material.MaterialParams.FresnelSelector = H3DFresnelSelector.Sec;
                    else
                        Material.MaterialParams.FresnelSelector = H3DFresnelSelector.No;
                    UpdateShaders();
                }

                if (ImGui.Checkbox("Fresnel Primary", ref fresnelPrimary))
                {
                    UpdateSelector();
                }
                if (ImGui.Checkbox("Fresnel Secondary", ref fresnelSecondary))
                {
                    UpdateSelector();
                }
            }

            // ImGui.Checkbox("Abs", ref abs);
            //   ImGui.NextColumn();

            if (!enable)
                ImGui.PopStyleColor();

          //  ImGui.EndColumns();


        }

        private void DrawInputSelector(string label, ref PICALUTInput value)
        {
            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 2);

            if (ImGui.BeginCombo(label, $"Input:     {InputList[value]}"))
            {
                var values = Enum.GetValues(typeof(PICALUTInput));
                foreach (PICALUTInput val in values)
                {
                    bool isSelected = value.Equals(val);
                    string cblabel = InputList[val];

                    if (ImGui.Selectable(cblabel, isSelected))
                    {
                        value = val;
                        UpdateShaders();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
        }

        private void DrawScaleSelector(string label, ref PICALUTScale value)
        {
            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 2);

            if (ImGui.BeginCombo(label, $"Scale: {ScaleList[value]}"))
            {
                var values = Enum.GetValues(typeof(PICALUTScale));
                foreach (PICALUTScale val in values)
                {
                    bool isSelected = value.Equals(val);
                    string cblabel = ScaleList[val];

                    if (ImGui.Selectable(cblabel, isSelected))
                    {
                        value = val;
                        UpdateShaders();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
        }

        private string previousSampler = "NULL";

        private void DrawSamplerSelector(string label, ref string name, string table, int index)
        {
            string sampler = string.IsNullOrEmpty(name) ? "None" : $"   {'\uf55b'}     {name}";
            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 2);

            //Draw as a drop down for selecting samplers in a lut table if one exists
            if (LUTCacheManager.Cache.ContainsKey(table))
            {
                var lutTable = LUTCacheManager.Cache[table];
                if (ImGui.BeginCombo($"##sampler{label}", $"{sampler}", ImGuiComboFlags.HeightLargest))
                {
                    //Removable option
                    if (ImGui.Selectable("None", string.IsNullOrEmpty(sampler)))
                    {
                        name = "";
                        UpdateShaders();
                    }
                    if (string.IsNullOrEmpty(sampler))
                        ImGui.SetItemDefaultFocus();

                    bool hasHover = false;

                    //List of all samplers in table
                    foreach (var samp in lutTable.Samplers)
                    {
                        bool select = samp.Name == sampler;
                        if (ImGui.Selectable($"   {'\uf55b'}     {samp.Name}", select))
                        {
                            name = samp.Name;
                            SetLUTSet(TableName);
                            UpdateShaders();
                        }
                        if (ImGui.IsItemHovered())
                        {
                            hasHover = true;

                            if (previousSampler == "NULL")
                                previousSampler = name;

                            name = samp.Name;
                            SetLUTSet(TableName);
                            UpdateShaders();
                        }

                        if (select)
                            ImGui.SetItemDefaultFocus();
                    }
                    if (!hasHover && previousSampler != "NULL" && previousSampler != null)
                    {
                        name = previousSampler;
                        previousSampler = "NULL";
                        UpdateShaders();
                    }

                    ImGui.EndCombo();
                }
            }
            else
            {
                //Text box to input a sampler name manually incase table is not present/opened
                if (ImGui.InputText($"##sampler{label}", ref sampler, 0x200))
                {
                    if (sampler != "None")
                        name = sampler;
                    else
                        sampler = "";

                    SetLUTSet(TableName);
                    UpdateShaders();
                }
            }
            ImGui.PopItemWidth();
        }

        //Go through all terms and find an active table
        //All should use the same set
        private string FindLUTSet()
        {
            var matParams = Material.MaterialParams;
            if (matParams.LUTDist0TableName != null) return matParams.LUTDist0TableName;
            else if (matParams.LUTDist1TableName != null) return matParams.LUTDist1TableName;
            else if (matParams.LUTReflecRTableName != null) return matParams.LUTReflecRTableName;
            else if (matParams.LUTReflecGSamplerName != null) return matParams.LUTReflecGSamplerName;
            else if (matParams.LUTReflecBTableName != null) return matParams.LUTReflecBTableName;
            else if (matParams.LUTFresnelTableName != null) return matParams.LUTFresnelTableName;
            return "";
        }

        private void SetLUTSet(string tableName)
        {
            var matParams = Material.MaterialParams;
            if (matParams.LUTDist0SamplerName != null) matParams.LUTDist0TableName = tableName;
            if (matParams.LUTDist1SamplerName != null) matParams.LUTDist1TableName = tableName;
            if (matParams.LUTReflecRSamplerName != null) matParams.LUTReflecRTableName = tableName;
            if (matParams.LUTReflecGSamplerName != null) matParams.LUTReflecGTableName = tableName;
            if (matParams.LUTReflecBSamplerName != null) matParams.LUTReflecBTableName = tableName;
            if (matParams.LUTFresnelSamplerName != null) matParams.LUTFresnelTableName = tableName;
        }

        void UpdateShaders()
        {
            MaterialWrapper.UpdateShaders();
            GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
        }

        private void DrawLightingGradient(GLTexture2D texture, float width, float[] values)
        {
            if (texture == null)
                texture = GLTexture2D.CreateUncompressedTexture(1, 1, 
                    OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba,
                    OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
                     OpenTK.Graphics.OpenGL.PixelType.Float);

            int height = 1;

            float[] data = new float[(int)width * height * 4];
            //Create a 1D texture sheet from the span of the timeline covering all the colors
            int index = 0;
            for (int i = 0; i < width; i++)
            {
                float time = i;
                time = Math.Min(time, values.Length - 1);
                time = Math.Max(time, 0);

                var color = values[(int)time];
                data[index + 0] = color;
                data[index + 1] = color;
                data[index + 2] = color;
                data[index + 3] = 1.0f;
                index += 4;
            }
            texture.Reload((int)width, height, data);
            //Draw the color sheet
            ImGui.Image((IntPtr)texture.ID, new Vector2(width, ImGui.GetFrameHeight() * 2 - 2));
        }

        private float[] GenerateLUT(H3DLUTSampler samp)
        {
            float[] Table = new float[512];
            if (samp.Flags.HasFlag(H3DLUTFlags.IsAbsolute))
            {
                for (int i = 0; i < 256; i++)
                {
                    Table[i + 256] = samp.Table[i];
                    Table[i + 0] = samp.Table[0];
                }
            }
            else
            {
                for (int i = 0; i < 256; i += 2)
                {
                    int PosIdx = i >> 1;
                    int NegIdx = PosIdx + 128;

                    Table[i + 256] = samp.Table[PosIdx];
                    Table[i + 257] = samp.Table[PosIdx];
                    Table[i + 0] = samp.Table[NegIdx];
                    Table[i + 1] = samp.Table[NegIdx];
                }
            }
            return Table;
        }
    }
}
