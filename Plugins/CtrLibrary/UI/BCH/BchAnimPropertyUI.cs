using CtrLibrary.Bcres;
using ImGuiNET;
using MapStudio.UI;
using SixLabors.ImageSharp.Formats.Gif;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrH3D.Camera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using static MapStudio.UI.AnimationTree;

namespace CtrLibrary.UI
{
    public class BchAnimPropertyUI
    {
        public void Render(AnimationWrapper wrapper, GfxDict<GfxMetaData> gfxMetaData = null)
        {
            ImGui.BeginTabBar("animTabbar");

            if (ImguiCustomWidgets.BeginTab("cameraTabbar", "Animation Info"))
            {
                DrawAnimInfo(wrapper);

                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("cameraTabbar", "User Data"))
            {
                if (gfxMetaData != null) //BCRES
                    Bcres.UserDataInfoEditor.Render(gfxMetaData);
                else //H3D
                    Bch.UserDataInfoEditor.Render(wrapper.H3DAnimation.MetaData);

                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        private void DrawAnimInfo(AnimationWrapper wrapper)
        {
            string name = wrapper.H3DAnimation.Name;
            float frameCount = wrapper.FrameCount;
            bool loop = wrapper.Loop;
            bool constant = wrapper.H3DAnimation.AnimationFlags.HasFlag(H3DAnimationFlags.IsConstant);

            ImguiPropertyColumn.Begin("animProperties");

            if (ImguiPropertyColumn.Text("Name", ref name))
            {
                wrapper.Root.Header = name;
                wrapper.Root.OnHeaderRenamed?.Invoke(wrapper.Root, EventArgs.Empty);
            }
            if (ImguiPropertyColumn.DragFloat("Frame Count", ref frameCount, 1))
            {
                wrapper.FrameCount = MathF.Max(frameCount, 1);
                ((AnimNode)wrapper.Root).UpdateFrameCounter();
            }
            if (ImguiPropertyColumn.Bool("Loop", ref loop))
            {
                wrapper.Loop = loop;
            }
            if (ImguiPropertyColumn.Bool("Is Constant", ref constant))
            {
                if (constant)
                    wrapper.H3DAnimation.AnimationFlags |= H3DAnimationFlags.IsConstant;
                else
                    wrapper.H3DAnimation.AnimationFlags &= ~H3DAnimationFlags.IsConstant;
            }

            ImguiPropertyColumn.Combo("Animation Type", ref wrapper.H3DAnimation.AnimationType);

            if (wrapper.H3DAnimation is H3DCameraAnim)
            {
                var camAnim = (H3DCameraAnim)wrapper.H3DAnimation;
                ImguiPropertyColumn.Combo("Camera View Type", ref camAnim.ViewType);
                ImguiPropertyColumn.Combo("Camera Projection Type", ref camAnim.ProjectionType);
            }

            if (wrapper.H3DAnimation is H3DLightAnim)
            {
                var lightAnim = (H3DLightAnim)wrapper.H3DAnimation;
                ImguiPropertyColumn.Combo("Light Type", ref lightAnim.LightType);
            }

            ImguiPropertyColumn.End();

            ImGuiHelper.BoldText($"Anim Count {wrapper.H3DAnimation.Elements.Count}");
        }
    }
}
