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

namespace CtrLibrary.Bcres
{
    internal class BcresBoneUI
    {
        private GfxBone GfxBone;
        private BcresBone BoneWrapper;

        public void Init(BcresBone bn, GfxBone bone)
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
            bool isLocalMatrix = GfxBone.Flags.HasFlag(GfxBoneFlags.IsLocalMtxCalculate);
            bool isWorldMatrix = GfxBone.Flags.HasFlag(GfxBoneFlags.IsWorldMtxCalculate);
            bool renderBinded = GfxBone.Flags.HasFlag(GfxBoneFlags.IsNeededRendering);
            bool hasSkinningMatrix = GfxBone.Flags.HasFlag(GfxBoneFlags.HasSkinningMtx);
            bool hasSegmentScaleCompensate = GfxBone.Flags.HasFlag(GfxBoneFlags.IsSegmentScaleCompensate);

            bool edited = false;
            if (ImGui.CollapsingHeader("Info", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGuiHelper.InputFromText("Name", GfxBone, "Name", 0x200);
                if (GfxBone.Parent != null) ImGui.Text($"Parent : {GfxBone.Parent.Name}");
                if (GfxBone.NextSibling != null) ImGui.Text($"NextSibling : {GfxBone.NextSibling.Name}");
                if (GfxBone.PrevSibling != null) ImGui.Text($"PrevSibling : {GfxBone.PrevSibling.Name}");
                if (GfxBone.Child != null) ImGui.Text($"Child : {GfxBone.Child.Name}");
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
                BcresUIHelper.DrawEnum("Billboard Mode", ref GfxBone.BillboardMode);
            }
            if (ImGui.CollapsingHeader("Flags", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Checkbox("Use Rendering", ref renderBinded))
                {
                    if (renderBinded)
                        GfxBone.Flags |= GfxBoneFlags.IsNeededRendering;
                    else
                        GfxBone.Flags &= ~GfxBoneFlags.IsNeededRendering;
                }
                if (ImGui.Checkbox("Calculate World Space", ref isWorldMatrix))
                {
                    if (isWorldMatrix)
                        GfxBone.Flags |= GfxBoneFlags.IsWorldMtxCalculate;
                    else
                        GfxBone.Flags &= ~GfxBoneFlags.IsWorldMtxCalculate;
                }
                if (ImGui.Checkbox("Calculate Local Space", ref isLocalMatrix))
                {
                    if (isLocalMatrix)
                        GfxBone.Flags |= GfxBoneFlags.IsLocalMtxCalculate;
                    else
                        GfxBone.Flags &= ~GfxBoneFlags.IsLocalMtxCalculate;
                }
                if (ImGui.Checkbox("Has Skinning Matrix", ref hasSkinningMatrix))
                {
                    if (hasSkinningMatrix)
                        GfxBone.Flags |= GfxBoneFlags.HasSkinningMtx;
                    else
                        GfxBone.Flags &= ~GfxBoneFlags.HasSkinningMtx;
                }
                if (ImGui.Checkbox("Use Segment Scale Compensate", ref hasSegmentScaleCompensate))
                {
                    if (hasSegmentScaleCompensate)
                        GfxBone.Flags |= GfxBoneFlags.IsSegmentScaleCompensate;
                    else
                        GfxBone.Flags &= ~GfxBoneFlags.IsSegmentScaleCompensate;
                }
            }
        }
    }
}
