using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Toolbox.Core;
using System.Drawing;
using Toolbox.Core.IO;
using MapStudio.UI;

namespace CtrLibrary.UI
{
    /// <summary>
    /// Represents a 2D UV viewport used for displaying UV data from meshes and tiled textures.
    /// </summary>
    public class UVViewport : Viewport2D
    {
        /// <summary>
        /// The texture map to render as.
        /// </summary>
        public TextureSamplerMap ActiveTextureMap = null;

        /// <summary>
        /// The transform matrix to transform the UV data.
        /// </summary>
        public Matrix4 ActiveMatrix = Matrix4.Identity;

        /// <summary>
        /// A list of meshes to display UV data on.
        /// </summary>
        public List<UVMeshObject> ActiveObjects = new List<UVMeshObject>();

        /// <summary>
        /// The drawable UV instance.
        /// </summary>
        public UVMap DrawableUVMap = new UVMap();

        /// <summary>
        /// The brightness of the background display.
        /// </summary>
        public float Brightness = 1.0f;

        /// <summary>
        /// Determines to display UVs or not.
        /// </summary>
        public bool DisplayUVs = true;

        /// <summary>
        /// Determines to update the current UV display with the currently active mesh objects.
        /// </summary>
        public bool UpdateVertexBuffer { get; set; } 

        public override void RenderScene()
        {
            var shader = GlobalShaders.GetShader("UV_WINDOW");
            shader.Enable();

            if (ActiveTextureMap == null)
                return;

            //Texture aspect ratio
            Vector2 aspectScale = UpdateAspectScale(Width, Height, ActiveTextureMap);
            //Draw the tiled background with the texture map data and texture aspect ratio
            UVBackground.Draw(ActiveTextureMap, Brightness, aspectScale, Camera);
            //Update UV drawable with the current object list
            if (UpdateVertexBuffer)
            {
                UpdateVertexBuffer = false;
                //Update buffer data
                DrawableUVMap.UpdateVertexBuffer(ActiveObjects);
            }
            //Display UVs if used
            if (DisplayUVs)
                DrawableUVMap.Draw(Camera, ActiveMatrix, aspectScale);
        }

        /// <summary>
        /// Resets the UV map to default.
        /// </summary>
        public void Reset() {
            DrawableUVMap.Reset();
        }

        //Gets the scale value based on the width/height of sampler map texture
        static Vector2 UpdateAspectScale(int width, int height, TextureSamplerMap texMap)
        {
            Vector2 scale = new Vector2(1);

            var tex = texMap;

            if (tex == null) return scale;

            //Adjust scale via aspect ratio
            if (width > height)
            {
                float aspect = (float)tex.Width / (float)tex.Height;
                scale.X *= aspect;
            }
            else
            {
                float aspect = (float)tex.Height / (float)tex.Width;
                scale.Y *= aspect;
            }
            return scale;
        }
    }
}
