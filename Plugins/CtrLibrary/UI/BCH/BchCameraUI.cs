using CtrLibrary.Bcres;
using ImGuiNET;
using MapStudio.UI;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrH3D.Camera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;

namespace CtrLibrary.UI
{
    public class BchCameraUI
    {
        private H3DCamera Camera;
        private GfxDict<GfxMetaData> MetaData;

        //Cached properties
        private H3DCameraViewAim CachedCameraViewAim;
        private H3DCameraViewLookAt CachedCameraViewLookat;
        private H3DCameraViewRotation CachedCameraViewRot;

        //keep during persp switch
        private float FOVY;
        private float Height;

        public void Init(H3DCamera camera)
        {
            Camera = camera;
        }

        public void Init(H3DCamera camera, GfxDict<GfxMetaData> metaData)
        {
            MetaData = metaData;
            Camera = camera;
        }

        public void Render()
        {
            ImGui.BeginTabBar("cameraTabbar");

            if (ImguiCustomWidgets.BeginTab("cameraTabbar", "Camera Info"))
            {
                DrawCameraInfo();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("cameraTabbar", "Animation Binds"))
            {
                DrawAnimInfo();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("cameraTabbar", "User Data"))
            {
                if (MetaData != null) //BCRES
                    UserDataInfoEditor.Render(MetaData);
                else //H3D
                    Bch.UserDataInfoEditor.Render(Camera.MetaData);

                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        private void DrawCameraInfo()
        {
            if (ImGui.CollapsingHeader("View", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawViewSwitch();

                if (Camera.View is H3DCameraViewAim)
                    DrawCameraViewAim((H3DCameraViewAim)Camera.View);
                if (Camera.View is H3DCameraViewLookAt)
                    DrawCameraViewLookAt((H3DCameraViewLookAt)Camera.View);
                if (Camera.View is H3DCameraViewRotation)
                    DrawCameraViewRotation((H3DCameraViewRotation)Camera.View);
            }
            if (ImGui.CollapsingHeader("Projection", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawProjSwitch();

                if (Camera.Projection is H3DCameraProjectionPerspective)
                    DrawCameraPerspProj((H3DCameraProjectionPerspective)Camera.Projection);
                if (Camera.Projection is H3DCameraProjectionOrthogonal)
                    DrawCameraOrthoProj((H3DCameraProjectionOrthogonal)Camera.Projection);
            }
        }

        private void DrawProjSwitch()
        {
            ImguiPropertyColumn.Begin("ProjProperties");
            bool edited = ImguiPropertyColumn.Combo("Projection Type", ref Camera.ProjectionType);
            ImguiPropertyColumn.End();

            if (edited)
            {
                float zfar = 0;
                float znear = 0;
                float aspect = 0;

                //transfer over current settings to new struct
                if (Camera.Projection is H3DCameraProjectionPerspective)
                {
                    zfar = ((H3DCameraProjectionPerspective)Camera.Projection).ZFar;
                    znear = ((H3DCameraProjectionPerspective)Camera.Projection).ZNear;
                    aspect = ((H3DCameraProjectionPerspective)Camera.Projection).AspectRatio;
                    this.FOVY = ((H3DCameraProjectionPerspective)Camera.Projection).FOVY;
                }
                if (Camera.Projection is H3DCameraProjectionOrthogonal)
                {
                    zfar = ((H3DCameraProjectionOrthogonal)Camera.Projection).ZFar;
                    znear = ((H3DCameraProjectionOrthogonal)Camera.Projection).ZNear;
                    aspect = ((H3DCameraProjectionOrthogonal)Camera.Projection).AspectRatio;
                    this.Height = ((H3DCameraProjectionOrthogonal)Camera.Projection).Height;
                }

                if (Camera.ProjectionType == H3DCameraProjectionType.Orthogonal)
                    Camera.Projection = new H3DCameraProjectionOrthogonal() { ZFar = zfar, ZNear = znear, AspectRatio = aspect, Height = Height, };
                else
                    Camera.Projection = new H3DCameraProjectionPerspective() { ZFar = zfar, ZNear = znear, AspectRatio = aspect, FOVY = FOVY, };
            }
        }

        private void DrawViewSwitch()
        {
            ImguiPropertyColumn.Begin("ViewProperties");
            bool edited = ImguiPropertyColumn.Combo("View Type", ref Camera.ViewType);
            ImguiPropertyColumn.End();

            if (edited)
            {
                BeforeViewChanged();
                if (Camera.ViewType == H3DCameraViewType.Rotate)
                    Camera.View = new H3DCameraViewRotation();
                if (Camera.ViewType == H3DCameraViewType.Aim)
                    Camera.View = new H3DCameraViewAim();
                if (Camera.ViewType == H3DCameraViewType.LookAt)
                    Camera.View = new H3DCameraViewLookAt();
                AfterViewChanged();
            }
        }

        private void BeforeViewChanged()
        {
            if (Camera.View is H3DCameraViewRotation)
                CachedCameraViewRot = (H3DCameraViewRotation)Camera.View;
            if (Camera.View is H3DCameraViewAim)
                CachedCameraViewAim = (H3DCameraViewAim)Camera.View;
            if (Camera.View is H3DCameraViewLookAt)
                CachedCameraViewLookat = (H3DCameraViewLookAt)Camera.View;
        }

        private void AfterViewChanged()
        {
            if (Camera.ViewType == H3DCameraViewType.Rotate && CachedCameraViewRot != null)
                Camera.View = CachedCameraViewRot;
            if (Camera.ViewType == H3DCameraViewType.Aim && CachedCameraViewAim != null)
                Camera.View = CachedCameraViewAim;
            if (Camera.ViewType == H3DCameraViewType.LookAt && CachedCameraViewLookat != null)
                Camera.View = CachedCameraViewLookat;
        }

        private void DrawCameraViewAim(H3DCameraViewAim view)
        {
            ImguiPropertyColumn.Begin("ViewAimProperties");

            ImguiPropertyColumn.DragFloat3("Position", ref Camera.TransformTranslation);
            ImguiPropertyColumn.DragFloat3("Target", ref view.Target);
            ImguiPropertyColumn.SliderFloat("Twist", ref view.Twist, 0, 360 * STMath.Deg2Rad);

            ImguiPropertyColumn.End();
        }

        private void DrawCameraViewLookAt(H3DCameraViewLookAt view)
        {
            ImguiPropertyColumn.Begin("ViewLookatProperties");
            ImguiPropertyColumn.DragFloat3("Position", ref Camera.TransformTranslation);
            ImguiPropertyColumn.DragFloat3("Target", ref view.Target);
            ImguiPropertyColumn.DragFloat3("Up Vector", ref view.UpVector);
            ImguiPropertyColumn.End();
        }

        private void DrawCameraViewRotation(H3DCameraViewRotation view)
        {
            ImguiPropertyColumn.Begin("ViewRotProperties");
            ImguiPropertyColumn.DragFloat3("Position", ref Camera.TransformTranslation);
            ImguiPropertyColumn.DragDegreesFloat3("Rotation", ref view.Rotation);
            ImguiPropertyColumn.End();
        }

        private void DrawCameraPerspProj(H3DCameraProjectionPerspective proj)
        {
            ImguiPropertyColumn.Begin("CameraProjectionPerspective");

            ImguiPropertyColumn.DragDegreesFloat("Fov", ref proj.FOVY);

            ImguiPropertyColumn.DragFloat("Near", ref proj.ZNear);
            ImguiPropertyColumn.DragFloat("Far", ref proj.ZFar);

            DrawAspectSelect(ref proj.AspectRatio);

            ImguiPropertyColumn.End();
        }

        private void DrawCameraOrthoProj(H3DCameraProjectionOrthogonal proj)
        {
            ImguiPropertyColumn.Begin("CameraProjectionPerspective");

            ImguiPropertyColumn.DragFloat("Height", ref proj.Height);

            ImguiPropertyColumn.DragFloat("Near", ref proj.ZNear);
            ImguiPropertyColumn.DragFloat("Far", ref proj.ZFar);

            DrawAspectSelect(ref proj.AspectRatio);

            ImguiPropertyColumn.End();
        }

        private void DrawAspectSelect(ref float aspect)
        {
            ImguiPropertyColumn.DragFloat("Aspect Ratio", ref aspect);

            if (ImGui.RadioButton("Aspect 15:9", aspect == 15f / 9f))
            {
                aspect = 15f / 9f;
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("Aspect 4:3", aspect == 4f / 3f))
            {
                aspect = 4f / 3f;
            }
            ImGui.NextColumn();
            ImGui.NextColumn();

        }

        private void DrawAnimInfo()
        {

        }
    }
}
