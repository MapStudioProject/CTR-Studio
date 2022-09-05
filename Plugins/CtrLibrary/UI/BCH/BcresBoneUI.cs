using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrGfx.Model.Mesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using GLFrameworkEngine;
using MapStudio.UI;
using SPICA.Formats.CtrH3D.Model;

namespace CtrLibrary.Bch
{
    /// <summary>
    /// GUI for H3D bones.
    /// </summary>
    internal class BchBoneUI
    {
        private H3DBone GfxBone;
        private BcresBone BoneWrapper;

        public void Init(BcresBone bn, H3DBone bone)
        {
            BoneWrapper = bn;
            GfxBone = bone;
        }

        public void Render()
        {
            ImGui.BeginTabBar("meshTabbar");

            if (ImguiCustomWidgets.BeginTab("meshTabbar", "Bone Info"))
            {
                DrawBoneInfo();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("modelTabbar", "User Data"))
            {
                UserDataInfoEditor.Render(GfxBone.MetaData);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        public void DrawBoneInfo()
        {
            bool isWorldMatrix = GfxBone.Flags.HasFlag(H3DBoneFlags.IsWorldMatrixUpdated);
            bool isSegmentScaleCompensate = GfxBone.Flags.HasFlag(H3DBoneFlags.IsSegmentScaleCompensate);

            bool edited = false;
            if (ImGui.CollapsingHeader("Info", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGuiHelper.InputFromText("Name", GfxBone, "Name", 0x200);
            }
            if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                edited |= ImGuiHelper.InputTKVector3("Scale", BoneWrapper, "Scale");
                edited |= ImGuiHelper.InputTKVector3("Rotation", BoneWrapper, "EulerRotationDegrees");
                edited |= ImGuiHelper.InputTKVector3("Translate", BoneWrapper, "Position");

                if (edited)
                {
                    BoneWrapper.UpdateBcresTransform();
                    BoneWrapper.Skeleton.Reset();
                    BoneWrapper.Skeleton.Update();

                    GLContext.ActiveContext.UpdateViewport = true;
                }
            }
            if (ImGui.CollapsingHeader("Billboard", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var billboard = GfxBone.BillboardMode;
                BcresUIHelper.DrawEnum("Billboard Mode", ref billboard, () =>
                {
                    GfxBone.BillboardMode = billboard;
                });
            }
            if (ImGui.CollapsingHeader("Flags", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Checkbox("Calculate World Space", ref isWorldMatrix))
                {
                    if (isWorldMatrix)
                        GfxBone.Flags |= H3DBoneFlags.IsWorldMatrixUpdated;
                    else
                        GfxBone.Flags &= ~H3DBoneFlags.IsWorldMatrixUpdated;
                }
                if (ImGui.Checkbox("Use Segment Scale Compensate", ref isSegmentScaleCompensate))
                {
                    if (isSegmentScaleCompensate)
                        GfxBone.Flags |= H3DBoneFlags.IsSegmentScaleCompensate;
                    else
                        GfxBone.Flags &= ~H3DBoneFlags.IsSegmentScaleCompensate;
                }
            }
        }
    }
}
