using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLFrameworkEngine;
using MapStudio.UI;

namespace CtrLibrary.UI
{
    /// <summary>
    /// The UV background to display a texture canvas.
    /// </summary>
    public class UVBackground
    {
        //Quad drawer
        static PlaneRenderer QuadRender;

        public static void Init()
        {
            if (QuadRender == null)
                QuadRender = new PlaneRenderer();
        }

        public static void Draw(TextureSamplerMap texture, float brightness, Vector2 aspectScale, Viewport2D.Camera2D camera)
        {
            Vector2 bgscale = new Vector2(1000, 1000);

            Init();

            GL.Disable(EnableCap.CullFace);

            var shader = GlobalShaders.GetShader("UV_WINDOW");
            shader.Enable();

            var cameraMtx = camera.ViewMatrix * camera.ProjectionMatrix;
            shader.SetMatrix4x4("mtxCam", ref cameraMtx);
            //reset
            var mtx = Matrix4.Identity;
            shader.SetMatrix4x4("mtxMdl", ref mtx);

            GL.ActiveTexture(TextureUnit.Texture1);
            BindTexture(texture);
            shader.SetInt("uvTexture", 1);
            shader.SetInt("hasTexture", 1);
            shader.SetVector2("scale", bgscale * aspectScale);
            shader.SetVector2("texCoordScale", bgscale);
            shader.SetVector4("uColor", new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
            shader.SetFloat("brightness", brightness);
            shader.SetBoolToInt("isBC5S", false);

            //Draw background
            QuadRender.UpdatePrimitiveType(PrimitiveType.TriangleStrip);
            QuadRender.Draw(shader);

            //Draw main texture quad inside boundings (0, 1)
            shader.SetVector2("scale", aspectScale);
            shader.SetVector2("texCoordScale", new Vector2(1));
            shader.SetVector4("uColor", new Vector4(1));

            QuadRender.UpdatePrimitiveType(PrimitiveType.TriangleStrip);
            QuadRender.Draw(shader);

            //Draw outline of boundings (0, 1)
            shader.SetInt("hasTexture", 0);
            shader.SetVector2("scale", aspectScale);
            shader.SetVector2("texCoordScale", new Vector2(1));
            shader.SetVector4("uColor", new Vector4(0,0,0,1));

            QuadRender.UpdatePrimitiveType(PrimitiveType.LineLoop);
            QuadRender.Draw(shader);

            GL.Enable(EnableCap.CullFace);
        }

        static void BindTexture(TextureSamplerMap tex)
        {
            if (tex == null || tex == null)
                return;

            var target = TextureTarget.Texture2D;
            var texID = tex.ID;

            GL.BindTexture(target, texID);
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (float)tex.WrapU);
            GL.TexParameter(target, TextureParameterName.TextureWrapT, (float)tex.WrapV);
            GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)tex.MinFilter);
            GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)tex.MagFilter);
            GL.TexParameter(target, TextureParameterName.TextureMinLod, tex.MinLOD);
            GL.TexParameter(target, TextureParameterName.TextureLodBias, tex.LODBias);
        }
    }
}
