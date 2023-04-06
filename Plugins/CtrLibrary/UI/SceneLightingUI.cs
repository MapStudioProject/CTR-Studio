using CtrLibrary.Rendering;
using GLFrameworkEngine;
using ImGuiNET;
using MapStudio.UI;
using Newtonsoft.Json;
using SPICA.Formats.CtrH3D.Light;
using SPICA.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;

namespace CtrLibrary.UI
{
    /// <summary>
    /// UI for configuring the scene lighting along with drawing a light instance.
    /// </summary>
    internal class SceneLightingUI
    {
        /// <summary>
        /// A sprite for displaying light sources in 3D view.
        /// </summary>
        public class LightPreview : IDrawable
        {
            public Light Light { get; set; }

            public bool IsVisible
            {
                get { return Light.Enabled; }
                set { }
            }

            SpriteDrawer Model;

            public LightPreview(Light light)
            {
                Light = light;
                Model = new SpriteDrawer(2);
                Model.XRay = false;

                if (!IconManager.HasIcon("POINT_LIGHT"))
                    IconManager.AddIcon("POINT_LIGHT", GLTexture2D.FromBitmap(Resources.Pointlight).ID);

                Model.TextureID = IconManager.GetTextureIcon("POINT_LIGHT");
            }

            public void DrawModel(GLContext control, Pass pass)
            {
                if (pass != Pass.TRANSPARENT)
                    return;

                Model.Transform.Position = Light.Position;
                Model.Transform.UpdateMatrix(true);
                Model.DrawModel(control);
            }

            public void Dispose()
            {
            }
        }

        public static List<NodeBase> Setup(H3DRender render, List<Light> lights)
        {
            List<NodeBase> nodes = new List<NodeBase>();
            foreach (Light light in lights)
            {
                var lightNode = new NodeBase("Scene Light")
                {
                    Tag = light,
                    Icon = IconManager.LIGHT_ICON.ToString(),
                };
                lightNode.TagUI.UIDrawer += delegate
                {
                    bool update = false;

                    if (ImGui.CollapsingHeader("Hemi Lighting", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        update |= ImGui.ColorEdit3("Sky Color", ref Renderer.GlobalHsLSCol);
                        update |= ImGui.ColorEdit3("Ground Color", ref Renderer.GlobalHsLGCol);
                    }
                    if (ImGui.CollapsingHeader("Lighting", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        update |= ImGui.Checkbox("Enabled", ref light.Enabled);
                        update |= ImGui.Checkbox("TwoSidedDiffuse", ref light.TwoSidedDiffuse);

                        BcresUIHelper.DrawEnum("Type", ref light.Type, () => { update = true; });
                        update |= EditVec3("Position", ref light.Position);
                        update |= EditVec3("Direction", ref light.Direction);

                        update |= EditColor("Diffuse", ref light.Diffuse); ImGui.SameLine();
                        update |= EditColor("Specular0", ref light.Specular0); ImGui.SameLine();
                        update |= EditColor("Specular1", ref light.Specular1);
                        update |= EditColor("Ambient", ref light.Ambient);
                    }
                    if (update)
                    {   
                        GLContext.ActiveContext.UpdateViewport = true;
                        render.UpdateAllUniforms();
                    }
                };
                nodes.Add(lightNode);
            }
            return nodes;
        }

        static bool EditColor(string label, ref OpenTK.Graphics.Color4 color)
        {
            var diffuse = new Vector4(color.R, color.G, color.B, color.A);
            if (ImGui.ColorEdit4(label, ref diffuse, ImGuiColorEditFlags.NoInputs))
            {
                color = new OpenTK.Graphics.Color4(diffuse.X, diffuse.Y, diffuse.Z, diffuse.W);
                return true;
            }
            return false;
        }

        static bool EditVec3(string label, ref OpenTK.Vector3 v)
        {
            var vec = new Vector3(v.X, v.Y, v.Z);
            if (ImGui.DragFloat3(label, ref vec, 0.1f))
            {
                v = new OpenTK.Vector3(vec.X, vec.Y, vec.Z);
                return true;
            }
            return false;
        }
    }
}
