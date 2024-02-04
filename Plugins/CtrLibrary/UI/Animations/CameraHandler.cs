using GLFrameworkEngine;
using OpenTK;
using SPICA.Formats.CtrH3D.Camera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CtrLibrary.UI
{
    public class CameraHandler
    {
        public H3DCamera H3DCamera;

        CameraRenderer CameraRenderer;
        Camera Camera;
        SPICA.Rendering.Camera CameraPica;

        public CameraHandler(H3DCamera camera)
        {
            SetCamera(camera);
        }

        private void SetCamera(H3DCamera h3dCamera)
        {
            //Set a camera instance for viewing in the viewport
            Camera = new Camera();
            Camera.Width = 16;
            Camera.Height = 9;

            //Use the SPICA camera instance for handling all the calculations
            CameraPica = new SPICA.Rendering.Camera(16, 9);
            CameraPica.Set(h3dCamera);

            //Set height/aspect ratio
            if (h3dCamera.Projection is H3DCameraProjectionOrthogonal)
            {
                CameraPica.AspectRatio = ((H3DCameraProjectionOrthogonal)h3dCamera.Projection).AspectRatio;
                CameraPica.Height = ((H3DCameraProjectionOrthogonal)h3dCamera.Projection).Height;
            }
            if (h3dCamera.Projection is H3DCameraProjectionPerspective)
            {
                CameraPica.AspectRatio = ((H3DCameraProjectionPerspective)h3dCamera.Projection).AspectRatio;

                Camera.ZNear = ((H3DCameraProjectionPerspective)h3dCamera.Projection).ZNear;
                Camera.ZFar = ((H3DCameraProjectionPerspective)h3dCamera.Projection).ZFar;
                Camera.Fov = ((H3DCameraProjectionPerspective)h3dCamera.Projection).FOVY;
            }
        }

        public void Activate()
        {
            GLContext.ActiveContext.Camera = Camera;
        }

        public void Render(GLContext context)
        {
            Calculate();

            Render(context, Matrix4.Identity);
        }

        public void Render(GLContext context, Matrix4 transform)
        {
            if (Camera == null)
                return;

            //Create a camera render if not initialized
            if (CameraRenderer == null)
            {
                CameraRenderer = new CameraRenderer(Camera);
                CameraRenderer.Update(Camera);
            }

            //Setup the camera render transform
            var scale = Matrix4.CreateScale(1);
            CameraRenderer.Transform = scale * transform;

            //Draw the camera with basic material
            StandardMaterial mat = new StandardMaterial();
            mat.Render(context);

            CameraRenderer.Draw(context);
        }

        private void Calculate()
        {
            Camera.ViewMatrix = CameraPica.ViewMatrix;
            Camera.ProjectionMatrix = CameraPica.ProjectionMatrix;
        }
    }
}
