using SPICA.Formats.CtrH3D.Model.Material;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using MapStudio.UI;
using SPICA.PICA.Commands;
using CtrLibrary.Rendering;
using CtrLibrary.Bch;

namespace CtrLibrary
{
    internal class MaterialRenderStateUI
    {
        public H3DMaterial Material;
        public MaterialWrapper MaterialWrapper;

        string[] RenderLayer = new string[]
        {
             "0 (Opaque)",
             "1 (Translucent)",
             "2 (Subtractive)",
             "3 (Additive)",
        };

        Dictionary<PICABlendEquation, string> BlendEqList = new Dictionary<PICABlendEquation, string>()
        {
            { PICABlendEquation.FuncAdd, "Src + Dst" },
            { PICABlendEquation.FuncSubtract, "Src - Dst" },
            { PICABlendEquation.FuncReverseSubtract, "-(Src) + Dst" },
            { PICABlendEquation.Min, "Min(Src, Dst)" },
            { PICABlendEquation.Max, "Max(Src, Dst)" },
        };

        Dictionary<PICABlendFunc, string> BlendFuncList = new Dictionary<PICABlendFunc, string>()
        {
            { PICABlendFunc.Zero, "0" },
            { PICABlendFunc.One, "1" },
            { PICABlendFunc.OneMinusDestinationAlpha, "1 - Destination.A" },
            { PICABlendFunc.OneMinusDestinationColor, "1 - Destination.RGB" },
            { PICABlendFunc.OneMinusConstantAlpha, "1 - Constant.A" },
            { PICABlendFunc.OneMinusConstantColor, "1 - Constant.RGB" },
            { PICABlendFunc.OneMinusSourceColor, "1 - Source.RGB" },
            { PICABlendFunc.OneMinusSourceAlpha, "1 - Source.A" },
            { PICABlendFunc.ConstantColor, "Blend Color.RGB" },
            { PICABlendFunc.ConstantAlpha, "Blend Color.A" },
            { PICABlendFunc.SourceColor, "Source.RGB" },
            { PICABlendFunc.SourceAlpha, "Source.A" },
            { PICABlendFunc.SourceAlphaSaturate, "saturate(Src.A)" },
            { PICABlendFunc.DestinationColor, "Destination.RGB" },
            { PICABlendFunc.DestinationAlpha, "Destination.A" },
        };

        public void Render()
        {
            var blend = Material.MaterialParams.BlendFunction;
            var alphaTest = Material.MaterialParams.AlphaTest;
            var depthTest = Material.MaterialParams.DepthColorMask;

            var blendModeGfx = Material.MaterialParams.BlendMode;
            var blendMode = Material.MaterialParams.ColorOperation.BlendMode;
            var fragOpMode = Material.MaterialParams.ColorOperation.FragOpMode;

            var colorSrc = blend.ColorSrcFunc;
            var colorDst = blend.ColorDstFunc;
            var alphaSrc = blend.AlphaSrcFunc;
            var alphaDst = blend.AlphaDstFunc;
            var colorEq = blend.ColorEquation;
            var alphaEq = blend.AlphaEquation;

            var alphaFunction = alphaTest.Function;
            bool enableAlphaTest = alphaTest.Enabled;
            float alphaRef = alphaTest.Reference / 255.0f;
            bool update = false;
            bool updateShaders = false;

            var depthFunction = depthTest.DepthFunc;
            var depthEnabled = depthTest.Enabled;
            var depthWrite = depthTest.DepthWrite;

            bool hasPolygonOffset = Material.MaterialParams.Flags.HasFlag(H3DMaterialFlags.IsPolygonOffsetEnabled);

            void UpdateState()
            {
                Material.MaterialParams.BlendFunction = new PICABlendFunction()
                {
                    ColorSrcFunc = colorSrc, ColorDstFunc = colorDst,
                    AlphaSrcFunc = alphaSrc, AlphaDstFunc =  alphaDst,
                    ColorEquation = colorEq, AlphaEquation = alphaEq,
                };
                Material.MaterialParams.AlphaTest = new SPICA.PICA.Commands.PICAAlphaTest()
                {
                    Reference = (byte)(alphaRef * 255),
                    Enabled = enableAlphaTest,
                    Function = alphaFunction,
                };
                Material.MaterialParams.DepthColorMask = new SPICA.PICA.Commands.PICADepthColorMask()
                {
                    DepthFunc = depthFunction,
                    DepthWrite = depthWrite,
                    Enabled = depthEnabled,
                    RedWrite = depthTest.RedWrite, GreenWrite = depthTest.GreenWrite,
                    BlueWrite = depthTest.BlueWrite, AlphaWrite = depthTest.AlphaWrite,
                };
                Material.MaterialParams.ColorOperation = new SPICA.PICA.Commands.PICAColorOperation()
                {
                    BlendMode = blendMode,
                    FragOpMode = fragOpMode,
                };
                Material.MaterialParams.BlendMode = blendModeGfx;

                if (updateShaders)
                    MaterialWrapper.UpdateShaders();

                GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
            }

            if (ImGui.CollapsingHeader("Info", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiHelper.ComboFromEnum<H3DMaterial.RenderPreset>("Preset##renderPreset", Material, "RenderingPreset"))
                {
                    MaterialWrapper.UpdateShaders();
                    GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
                }

                var transparentLayer = Material.MaterialParams.RenderLayer.ToString();
                if (ImguiCustomWidgets.ComboScrollable("Render Layer", $"Layer {RenderLayer[Material.MaterialParams.RenderLayer]}", ref transparentLayer, RenderLayer))
                {
                    Material.MaterialParams.RenderLayer = int.Parse(transparentLayer[0].ToString());
                }
            }


            if (ImGui.CollapsingHeader("Depth Test", ImGuiTreeNodeFlags.DefaultOpen))
            {
                update |= ImGui.Checkbox("Enable##depthEn", ref depthEnabled);
                if (depthEnabled)
                {
                    ImGui.SameLine();
                    update |= ImGui.Checkbox("Write##depthWr", ref depthWrite);
                    BcresUIHelper.DrawEnum("Function##depthFunc", ref depthFunction, UpdateState);
                }
            }

            if (ImGui.CollapsingHeader("Alpha Test", ImGuiTreeNodeFlags.DefaultOpen))
            {
                updateShaders |= ImGui.Checkbox("Enable##alphaEn", ref enableAlphaTest);
                if (enableAlphaTest)
                {
                    update |= ImGui.SliderFloat("Reference##alphaRef", ref alphaRef, 0, 1);
                    BcresUIHelper.DrawEnum("Function##alphaF", ref alphaFunction, () => updateShaders = true);
                }
            }

            if (ImGui.CollapsingHeader("Blend State", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (MaterialWrapper is Bcres.MTOB) //Used by bcres only
                    BcresUIHelper.DrawEnum("Blend Mode", ref blendModeGfx, UpdateState);
                else
                {
                    BcresUIHelper.DrawEnum("Blend Mode", ref blendMode, UpdateState);
                    BcresUIHelper.DrawEnum("Frag Operation Source", ref fragOpMode, UpdateState);
                }
            }

            if (ImGui.CollapsingHeader("Color Blend", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawEquation("Color", ref colorEq, ref colorSrc, ref colorDst, UpdateState);
            }
            if (ImGui.CollapsingHeader("Alpha Blend", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawEquation("Alpha", ref alphaEq, ref alphaSrc, ref alphaDst, UpdateState);
            }

            if (update || updateShaders)
                UpdateState();
        }

        private void DrawBlendEq(string type, ref PICABlendEquation eq, Action UpdateState)
        {
            if (ImGui.BeginCombo($"##Equation{type}", $"Equation = {BlendEqList[eq]}", ImGuiComboFlags.NoArrowButton))
            {
                foreach (PICABlendEquation val in Enum.GetValues(typeof(PICABlendEquation)))
                {
                    bool isSelected = eq.Equals(val);

                    if (ImGui.Selectable(BlendEqList[val], isSelected))
                    {
                        eq = val;
                        UpdateState.Invoke();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
        }

        private void DrawBlendFunc(string type, ref PICABlendFunc func,  Action UpdateState)
        {
            if (ImGui.BeginCombo($"##Equation{type}", $"{BlendFuncList[func]}", ImGuiComboFlags.NoArrowButton))
            {
                foreach (PICABlendFunc val in Enum.GetValues(typeof(PICABlendFunc)))
                {
                    bool isSelected = func.Equals(val);

                    if (ImGui.Selectable(BlendFuncList[val], isSelected))
                    {
                        func = val;
                        UpdateState.Invoke();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
        }

        private void DrawEquation(string type, ref PICABlendEquation eq, ref PICABlendFunc src, ref PICABlendFunc dst, Action UpdateState)
        {
            DrawBlendEq(type, ref eq, UpdateState);

            ImGui.PushItemWidth(180);

            void DrawSrc(ref PICABlendFunc value) {
                ImGui.AlignTextToFramePadding();
                ImGuiHelper.BoldText("Src:"); ImGui.SameLine();
                DrawBlendFunc($"##{type}Src", ref value, UpdateState);
            }
            void DrawDst(ref PICABlendFunc value) {
                ImGui.AlignTextToFramePadding();
                ImGuiHelper.BoldText("Dst:"); ImGui.SameLine();
                DrawBlendFunc($"##{type}Dst", ref value, UpdateState);
            }

            switch (eq)
            {
                case PICABlendEquation.FuncAdd:
                    DrawSrc(ref src);
                    DrawAdd();
                    DrawDst(ref dst);
                    break;
                case PICABlendEquation.FuncSubtract:
                    DrawSrc(ref src);
                    DrawSub();
                    DrawDst(ref dst);
                    break;
                case PICABlendEquation.FuncReverseSubtract:
                    OperatorText("-("); ImGui.SameLine();
                    DrawSrc(ref src);
                    ImGui.SameLine();
                    OperatorText(")");
                    DrawSub();
                    DrawDst(ref dst);
                    break;
                case PICABlendEquation.Max:
                    OperatorText("max("); ImGui.SameLine();
                    DrawSrc(ref src);
                    DrawSub();
                    DrawDst(ref dst);
                    ImGui.SameLine();
                    OperatorText(")");
                    break;
                case PICABlendEquation.Min:
                    OperatorText("min("); ImGui.SameLine();
                    DrawSrc(ref src);
                    DrawSub();
                    DrawDst(ref dst);
                    ImGui.SameLine();
                    OperatorText(")");
                    break;
                default:
                    BcresUIHelper.DrawEnum("Source##clrSrc", ref src, UpdateState);
                    BcresUIHelper.DrawEnum("Dest##clrDst", ref dst, UpdateState);
                    break;
            }
            ImGui.PopItemWidth();
        }

        private void OperatorText(string text)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.PushFont(ImGuiController.DefaultFontBold);
            ImGui.Text(text);
            ImGui.PopFont();
        }

        private void DrawAdd()
        {
            ImGui.SameLine();
            OperatorText("+");
            ImGui.SameLine();
        }

        private void DrawSub()
        {
            ImGui.SameLine();
            OperatorText("-");
            ImGui.SameLine();
        }

        private void DrawMultiply()
        {
            ImGui.SameLine();
            OperatorText("x");
            ImGui.SameLine();
        }
    }
}
