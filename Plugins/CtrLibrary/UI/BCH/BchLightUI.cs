using CtrLibrary.Bcres;
using ImGuiNET;
using MapStudio.UI;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrGfx.Camera;
using SPICA.Formats.CtrGfx.Fog;
using SPICA.Formats.CtrGfx.Light;
using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrH3D.Light;
using SPICA.Math3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CtrLibrary.UI
{
    public class BchLightUI
    {
        private H3DLight Light;
        private GfxDict<GfxMetaData> MetaData;

        //Cache settings if user switches different types
        private H3DFragmentLight CachedFragmentLightSettings;
        private H3DVertexLight CachedVertexLightSettings;
        private H3DHemisphereLight CachedHemisphereLightSettings;
        private H3DAmbientLight CachedAmbientLightSettings;

        public void Init(H3DLight light, GfxDict<GfxMetaData> metaData = null)
        {
            Light = light;
            MetaData = metaData;
        }

        public void Render()
        {
            ImGui.BeginTabBar("lightTabbar");

            if (ImguiCustomWidgets.BeginTab("lightTabbar", "Light Info"))
            {
                DrawLightInfo();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("lightTabbar", "Animation Binds"))
            {
                DrawAnimInfo();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("lightTabbar", "User Data"))
            {
                if (MetaData != null) //BCRES
                    UserDataInfoEditor.Render(MetaData);
                else //BCH
                    Bch.UserDataInfoEditor.Render(Light.MetaData);

                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        private void DrawLightInfo()
        {
            DrawLightSwitcher();

            if (Light.Content is H3DFragmentLight)
                DrawFragLightInfo((H3DFragmentLight)Light.Content);
            if (Light.Content is H3DVertexLight)
                DrawVertexLightInfo((H3DVertexLight)Light.Content);
            if (Light.Content is H3DHemisphereLight)
                DrawHemisphereLightInfo((H3DHemisphereLight)Light.Content);
            if (Light.Content is H3DAmbientLight)
                DrawAmbientLightInfo((H3DAmbientLight)Light.Content);

            if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImguiPropertyColumn.Begin("transformproperties");
                ImguiPropertyColumn.DragFloat3("Translation", ref Light.TransformTranslation);
                ImguiPropertyColumn.DragFloat3("Rotation", ref Light.TransformRotation);
                ImguiPropertyColumn.DragFloat3("Scale", ref Light.TransformScale);
                ImguiPropertyColumn.End();
            }
        }

        private void DrawLightSwitcher()
        {
            string type = "None";
            if (Light.Content is H3DFragmentLight) type = "Fragment";
            if (Light.Content is H3DVertexLight) type = "Vertex";
            if (Light.Content is H3DHemisphereLight) type = "Hemisphere";
            if (Light.Content is H3DAmbientLight) type = "Ambient";

            if (ImGui.BeginCombo("Type", type))
            {
                if (ImGui.Selectable("Fragment"))
                {
                    BeforeTypeChanged();
                    Light.Content = new H3DFragmentLight();
                    Light.Type = H3DLightType.Fragment;
                    AfterTypeChanged();
                }
                if (ImGui.Selectable("Vertex"))
                {
                    BeforeTypeChanged();
                    Light.Content = new H3DVertexLight();
                    Light.Type = H3DLightType.Vertex;
                    AfterTypeChanged();
                }
                if (ImGui.Selectable("Hemisphere"))
                {
                    BeforeTypeChanged();
                    Light.Content = new H3DHemisphereLight();
                    Light.Type = H3DLightType.Hemisphere;
                    AfterTypeChanged();
                }
                if (ImGui.Selectable("Ambient"))
                {
                    BeforeTypeChanged();
                    Light.Content = new H3DAmbientLight();
                    Light.Type = H3DLightType.Ambient;
                    AfterTypeChanged();
                }

                ImGui.EndCombo();
            }
        }

        private void BeforeTypeChanged()
        {
            //store the previous settings to keep if switched back
            if (Light.Content is H3DFragmentLight)
                CachedFragmentLightSettings = (H3DFragmentLight)Light.Content;
            if (Light.Content is H3DVertexLight)
                CachedVertexLightSettings = (H3DVertexLight)Light.Content;
            if (Light.Content is H3DAmbientLight)
                CachedAmbientLightSettings = (H3DAmbientLight)Light.Content;
            if (Light.Content is H3DHemisphereLight)
                CachedHemisphereLightSettings = (H3DHemisphereLight)Light.Content;
        }

        private void AfterTypeChanged()
        {
            //store the previous settings to keep if switched back
            if (Light.Content is H3DFragmentLight && CachedFragmentLightSettings != null)
                Light.Content = CachedFragmentLightSettings;
            if (Light.Content is H3DVertexLight && CachedVertexLightSettings != null)
                Light.Content = CachedVertexLightSettings;
            if (Light.Content is H3DAmbientLight && CachedAmbientLightSettings != null)
                Light.Content = CachedAmbientLightSettings;
            if (Light.Content is H3DHemisphereLight && CachedHemisphereLightSettings != null)
                Light.Content = CachedHemisphereLightSettings;
        }

        private void DrawFragLightInfo(H3DFragmentLight light)
        {
            if (ImGui.CollapsingHeader("Light Type", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImguiPropertyColumn.Begin("tproperties");

                //default type
                bool isDir = Light.Type == H3DLightType.FragmentDir || Light.Type == H3DLightType.Fragment;
                bool isPoint = Light.Type == H3DLightType.FragmentPoint;
                bool isSpot = Light.Type == H3DLightType.FragmentSpot;

                if (ImguiPropertyColumn.RadioButton("Directional", ref isDir))
                    Light.Type = H3DLightType.FragmentDir;
                if (ImguiPropertyColumn.RadioButton("Point", ref isPoint))
                    Light.Type = H3DLightType.FragmentPoint;
                if (ImguiPropertyColumn.RadioButton("Spot", ref isSpot))
                    Light.Type = H3DLightType.FragmentSpot;

                ImguiPropertyColumn.End();
            }

            if (ImGui.CollapsingHeader("Fragment Light", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImguiPropertyColumn.Begin("properties");

                ImguiPropertyColumn.DragFloat3("Direction", ref light.Direction);

                H3DUIHelper.Color("Diffuse Color", ref light.DiffuseColor);
                H3DUIHelper.Color("Ambient Color", ref light.AmbientColor);
                H3DUIHelper.Color("Specular 0 Color", ref light.Specular0Color);
                H3DUIHelper.Color("Specular 1 Color", ref light.Specular1Color);

                bool isTwoSided = Light.Flags.HasFlag(H3DLightFlags.IsTwoSidedDiffuse);
                bool IsDistanceAttenuation = Light.Flags.HasFlag(H3DLightFlags.HasDistanceAttenuation);
                bool updateFlags = false;

                updateFlags |= ImguiPropertyColumn.Bool("Is Two Sided", ref isTwoSided);

                if (Light.Type == H3DLightType.FragmentSpot || Light.Type == H3DLightType.FragmentPoint)
                {
                    updateFlags |= ImguiPropertyColumn.Bool("Use Distance Attenuation", ref IsDistanceAttenuation);

                    if (IsDistanceAttenuation)
                    {
                        ImguiPropertyColumn.DragFloat("Attenuation Start", ref light.AttenuationStart);
                        ImguiPropertyColumn.DragFloat("Attenuation End", ref light.AttenuationEnd);
                    }
                }

                if (updateFlags)
                {
                    Light.Flags &= ~H3DLightFlags.IsTwoSidedDiffuse;
                    Light.Flags &= ~H3DLightFlags.HasDistanceAttenuation;

                    if (isTwoSided) Light.Flags |= H3DLightFlags.IsTwoSidedDiffuse;
                    if (IsDistanceAttenuation) Light.Flags |= H3DLightFlags.HasDistanceAttenuation;
                }

                ImguiPropertyColumn.End();
            }
        }

        private void DrawVertexLightInfo(H3DVertexLight light)
        {
            if (ImGui.CollapsingHeader("Light Type", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImguiPropertyColumn.Begin("tproperties");

                //default type
                bool isDir = Light.Type == H3DLightType.VertexDir || Light.Type == H3DLightType.Vertex;
                bool isPoint = Light.Type == H3DLightType.VertexPoint;
                bool isSpot = Light.Type == H3DLightType.VertexSpot;

                if (ImguiPropertyColumn.RadioButton("Directional", ref isDir))
                    Light.Type = H3DLightType.VertexDir;
                if (ImguiPropertyColumn.RadioButton("Point", ref isPoint))
                    Light.Type = H3DLightType.VertexPoint;
                if (ImguiPropertyColumn.RadioButton("Spot", ref isSpot))
                    Light.Type = H3DLightType.VertexSpot;

                ImguiPropertyColumn.End();
            }

            if (ImGui.CollapsingHeader("Vertex Light", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImguiPropertyColumn.Begin("properties");

                ImguiPropertyColumn.ColorEdit4("Diffuse Color", ref light.DiffuseColor, ImGuiColorEditFlags.NoInputs);
                ImguiPropertyColumn.ColorEdit4("Ambient Color", ref light.AmbientColor, ImGuiColorEditFlags.NoInputs);
                ImguiPropertyColumn.DragFloat3("Direction", ref light.Direction);

                ImguiPropertyColumn.DragFloat("Attenuation Constant", ref light.AttenuationConstant);
                ImguiPropertyColumn.DragFloat("Attenuation Linear", ref light.AttenuationLinear);
                ImguiPropertyColumn.DragFloat("Attenuation Quadratic", ref light.AttenuationQuadratic);

                ImguiPropertyColumn.DragFloat("Spot Exponent", ref light.SpotExponent);
                ImguiPropertyColumn.DragFloat("Spot CutOff Angle", ref light.SpotCutOffAngle);

                ImguiPropertyColumn.End();
            }
        }

        private void DrawHemisphereLightInfo(H3DHemisphereLight light)
        {
            if (ImGui.CollapsingHeader("Hemisphere Light", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImguiPropertyColumn.Begin("properties");

                ImguiPropertyColumn.ColorEdit4("Sky Color", ref light.SkyColor, ImGuiColorEditFlags.NoInputs);
                ImguiPropertyColumn.ColorEdit4("Ground Color", ref light.GroundColor, ImGuiColorEditFlags.NoInputs);
                ImguiPropertyColumn.DragFloat3("Direction", ref light.Direction);
                ImguiPropertyColumn.SliderFloat("Lerp Factor", ref light.LerpFactor, 0, 1f);

                ImguiPropertyColumn.End();
            }
        }

        private void DrawAmbientLightInfo(H3DAmbientLight light)
        {
            if (ImGui.CollapsingHeader("Ambient Light", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImguiPropertyColumn.Begin("properties");

                H3DUIHelper.Color("Color", ref light.Color);

                ImguiPropertyColumn.End();
            }
        }

        private void DrawAnimInfo()
        {

        }
    }
}
