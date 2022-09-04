using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLFrameworkEngine;
using SPICA.PICA.Commands;

namespace CtrLibrary
{
    internal class TexStagePreviewer
    {
        public Vector4 Color = new Vector4();

        public bool Update = false;

        public bool IsAlpha = false;

        public bool ShowAlpha = false;

        public int TextureID = -1;

        public PICATextureCombinerColorOp ColorOperand = PICATextureCombinerColorOp.Color;
        public PICATextureCombinerAlphaOp AlphaOperand = PICATextureCombinerAlphaOp.Alpha;

        private Framebuffer fbo;

        private const int IconSize = 50;

        private void Init()
        {
            fbo = new Framebuffer(FramebufferTarget.Framebuffer, IconSize, IconSize);
        }

        private void Render()
        {
            if (fbo == null)
                Init();

            fbo.Bind();

            GL.Viewport(0, 0, IconSize, IconSize);
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var shader = GlobalShaders.GetShader("TEX_STAGE", "TexStageEnv");
            shader.Enable();

            shader.SetVector4("color", Color);
            if (IsAlpha)
                shader.SetInt("operand", (int)AlphaOperand);
            else
                shader.SetInt("operand", (int)ColorOperand);

            shader.SetInt("type", TextureID != -1 ? 1 : 0);
            shader.SetBoolToInt("isAlpha", IsAlpha);
            shader.SetBoolToInt("showAlpha", ShowAlpha);

            if (TextureID != -1)
            {
                GL.ActiveTexture(TextureUnit.Texture10);
                GL.BindTexture(TextureTarget.Texture2D, TextureID);
                shader.SetInt("textureInput", 10);
            }

            ScreenQuadRender.Draw();

            fbo.Unbind();
            GL.UseProgram(0);

            var errorcheck = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (errorcheck != FramebufferErrorCode.FramebufferComplete)
                throw new Exception(errorcheck.ToString());

            Update = false;
        }

        public void Draw()
        {
          //  if (Update || fbo == null)
                Render();

            var texID = ((GLTexture2D)fbo.Attachments[0]).ID;
            ImGuiNET.ImGui.Image((IntPtr)texID, new System.Numerics.Vector2(IconSize, IconSize));
        }
    }
}
