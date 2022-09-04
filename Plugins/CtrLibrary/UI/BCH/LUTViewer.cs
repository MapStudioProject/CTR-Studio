using SPICA.Formats.CtrH3D.LUT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using ImGuiNET;
using GLFrameworkEngine;
using MapStudio.UI;

namespace CtrLibrary
{
    /// <summary>
    /// A visualizer for LUT curves which determine the angle of light to use from 0 - 1 at 0 - 180 degrees.
    /// </summary>
    internal class LUTViewer
    {
        //Window width
        private float Width;
        //Window height
        private float Height;

        //Value to range timeline. Offset slightly
        float valueRangeMin = 0 - 0.03f;
        //Value to range timeline. Offset slightly
        float valueRangeMax = 1 + 0.03f;

        //Amount of frames to shift to stay center within the view
        const float frameShift = 20;

        //The max frame amount to display
        const float frameRangeMax = 512 + frameShift;
        //The mib frame amount to display
        const float frameRangeMin = 0 - frameShift;
        //Texture to display brightness amount as a gradient
        GLTexture2D ColorGradient;
        //Window pos to convert to screen space
        Vector2 WindowPos;

        public void Init()
        {
            ColorGradient = GLTexture2D.CreateUncompressedTexture(256, 1, OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba,
                 OpenTK.Graphics.OpenGL.PixelFormat.Rgba, OpenTK.Graphics.OpenGL.PixelType.Float);
        }

        public void Render(H3DLUTSampler sampler)
        {
            if (ColorGradient == null)
                Init();

            bool abs = (sampler.Flags & H3DLUTFlags.IsAbsolute) != 0;
            ImGui.Text(sampler.Name);

            //Toggle absoulte mode
            if (ImGui.Checkbox("Clamp At 90 Degrees", ref abs))
            {
                if (!abs)
                    sampler.Flags &= ~H3DLUTFlags.IsAbsolute;
                else
                    sampler.Flags |= H3DLUTFlags.IsAbsolute;
            }

            //Prepare LUT graph
            ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeHandler.Theme.FrameBg);
            ImGui.BeginChild("lutEditor");
            ImGui.PopStyleColor();

            //Set window width/height
            Width = ImGui.GetWindowWidth();
            Height = ImGui.GetWindowHeight();

            //Current position in screen space
            var realPos = ImGui.GetCursorScreenPos();
            float offset = FramesToPixelsX(frameRangeMin);
            float offsetMax = FramesToPixelsX(frameShift);

            WindowPos = realPos;

            //Prepare the table based on what mode to use 
            float[] Table = new float[512];
            if (abs)
            {
                //Sample only half the angle amount
                for (int i = 0; i < 256; i++)
                {
                    Table[i + 256] = sampler.Table[i];
                    Table[i + 0] = sampler.Table[0];
                }
            }
            else
            {
                //Sample for the full 180 degree angle
                for (int i = 0; i < 256; i += 2)
                {
                    int PosIdx = i >> 1;
                    int NegIdx = PosIdx + 128;

                    Table[i + 256] = sampler.Table[PosIdx];
                    Table[i + 257] = sampler.Table[PosIdx];
                    Table[i + 0] = sampler.Table[NegIdx];
                    Table[i + 1] = sampler.Table[NegIdx];
                }
            }

            var draw_list = ImGui.GetWindowDrawList();
            bool clamped = abs;

            //BG
            draw_list.AddRectFilled(new Vector2(
                realPos.X + FrameToX(clamped ? Table.Length / 2 : 0),
                realPos.Y), new Vector2(
                realPos.X + FrameToX(Table.Length),
                realPos.Y + Height), ImGui.ColorConvertFloat4ToU32(new Vector4(ThemeHandler.Theme.ChildBg.X,
                ThemeHandler.Theme.ChildBg.Y, ThemeHandler.Theme.ChildBg.Z, 1.0f)));

            DrawLightingGradient(Table);
            DrawText();

            //Draw key frames. Keep in mind these are all baked.
            //Would be better to clean it up and auto bake on save.
            for (int i = 0; i < Table.Length; i++)
            {
                if (i == Table.Length - 1)
                    continue;

                //Convert to ui space
                var pos = new Vector2(
                   WindowPos.X + FrameToX(i),
                   WindowPos.Y + ValueToY(Table[i]));
                var nextPos = new Vector2(
                   WindowPos.X + FrameToX(i + 1),
                   WindowPos.Y + ValueToY(Table[i + 1]));

                draw_list.AddLine(pos,  nextPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1,1,1,1)));

                draw_list.AddCircleFilled(pos, 1, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)));
                draw_list.AddCircleFilled(pos, 1.5f, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)));
            }
            ImGui.EndChild();
        }

        public void DrawText()
        {
            //Timeline resized too small, don't display font
            if (Height - 50 <= 0)
                return;

            var draw_list = ImGui.GetWindowDrawList();

            //Grid lines
            void DrawHoizontalLine(float id)
            {
                //Convert to ui space
                var pos = new Vector2(
                   WindowPos.X,
                   WindowPos.Y + ValueToY(id));
                var nextPos = new Vector2(
                   WindowPos.X + Width,
                   WindowPos.Y + ValueToY(id));

                draw_list.AddLine(pos, nextPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.2f)));
            }
            void DrawVerticalLine(int id)
            {
                //Convert to ui space
                var pos = new Vector2(
                   WindowPos.X + FrameToX(id),
                   WindowPos.Y);
                var nextPos = new Vector2(
                   WindowPos.X + FrameToX(id),
                   WindowPos.Y + Height);

                draw_list.AddLine(pos, nextPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.2f)));
            }


            float frameWidth = FramesToPixelsX(1f);
            var curPos = ImGui.GetCursorPos();

            #region text draw

            float offset = FramesToPixelsX(frameRangeMin);
            float shift  = FramesToPixelsX(frameShift);

            void DrawAngle(int id, string angle)
            {
                DrawVerticalLine(id);

                ImGui.SetCursorPos(new Vector2(id * frameWidth - offset, 30));
                DrawText(angle + "°");
            };

            void DrawValue(float value)
            {
                DrawHoizontalLine(value);

                ImGui.SetCursorPos(new Vector2(shift, ValueToY(value)));
                DrawText(value.ToString(), false, true);
            };

            DrawAngle(0, "0");
            DrawAngle(64 * 2, "45");
            DrawAngle(128 * 2, "90");
            DrawAngle(192 * 2, "135");
            DrawAngle(256 * 2, "180");

            DrawValue(0);
            DrawValue(0.25f);
            DrawValue(0.5f);
            DrawValue(0.75f);
            DrawValue(1.0f);

            #endregion

            ImGui.SetCursorPos(curPos);
        }

        //Draws a gradient with the provided LUT data.
        private void DrawLightingGradient(float[] values)
        {
            int height = 1;
            int width = (int)(this.Width);

            float[] data = new float[width * height * 4];
            //Create a 1D texture sheet from the span of the timeline covering all the colors
            int index = 0;
            for (int i = 0; i < Width; i++)
            {
                float time = XToFrame(i);
                time = Math.Min(time, values.Length - 1);
                time = Math.Max(time, 0);

                var color = values[(int)time];
                data[index + 0] = color;
                data[index + 1] = color;
                data[index + 2] = color;
                data[index + 3] = 1.0f;
                index += 4;
            }
            ColorGradient.Reload(width, height, data);
            //Draw the color sheet
            ImGui.Image((IntPtr)ColorGradient.ID, new Vector2(this.Width, ImGui.GetFrameHeight() - 2));
        }

        //Draws bold text in place with centering if toggled
        private void DrawText(string text, bool centerX = true, bool centerY = false)
        {
            var size = ImGui.CalcTextSize(text);
            var posX = ImGui.GetCursorPosX();
            var posY = ImGui.GetCursorPosY();

            if (centerX)
                ImGui.SetCursorPosX(posX - (size.X / 2));
            if (centerY)
                ImGui.SetCursorPosY(posY - (size.Y / 2));

            ImGuiHelper.BoldText(text);
        }

        #region Conversion

        private float ValueUnitsToPixelsY(float value)
        {
            return value * (Height - 70) / (valueRangeMax - valueRangeMin);
        }
        private static float Map(float value, float minIn, float maxIn, float minOut, float maxOut)
        {
            return (value - minIn) * (maxOut - minOut) / (maxIn - minIn) + minOut;
        }
        private float FrameToX(float frame)
        {
            return Map(frame, frameRangeMin, frameRangeMax, 0, Width);
        }

        private float XToFrame(float x)
        {
            return Map(x, 0, Width, frameRangeMin, frameRangeMax);
        }

        private float ValueToY(float value)
        {
            return Map(value, valueRangeMin, valueRangeMax, Height - 5, 35);
        }

        private float YToValue(float y)
        {
            return Map(y, Height - 5, 35, valueRangeMin, valueRangeMax);
        }

        private float FramesToPixelsX(float frames)
        {
            return frames * (Width) / (frameRangeMax - frameRangeMin);
        }

        #endregion
    }
}
