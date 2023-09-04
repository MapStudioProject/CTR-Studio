using ImGuiNET;
using MapStudio.UI;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.PICA.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using CtrLibrary.Rendering;
using CtrLibrary.Bch;

namespace CtrLibrary
{
    internal class MaterialCombinerUI
    {
        private const int MAX_STAGE_COUNT = 6;

        private List<PICATexEnvStage> StageList = new List<PICATexEnvStage>();

        H3DMaterial Material;
        MaterialWrapper MaterialWrapper;

        StageViewer[] Stages = new StageViewer[MAX_STAGE_COUNT];

        class StageViewer
        {
            public TexStagePreviewer StagePreviewerColorSourceA = new TexStagePreviewer();
            public TexStagePreviewer StagePreviewerColorSourceB = new TexStagePreviewer();
            public TexStagePreviewer StagePreviewerColorSourceC = new TexStagePreviewer();

            public TexStagePreviewer StagePreviewerAlphaSourceA = new TexStagePreviewer();
            public TexStagePreviewer StagePreviewerAlphaSourceB = new TexStagePreviewer();
            public TexStagePreviewer StagePreviewerAlphaSourceC = new TexStagePreviewer();
        }

        //Icon keys
        const string ICON_PREVIOUS = "PREVIOUS";
        const string ICON_PREVIOUS_BUFFER = "PREVIOUS_BUFFER";
        const string ICON_PRIMARY = "PRIMARY";
        const string ICON_VERTEX_PRIMARY = "VERTEX_PRIMARY";
        const string ICON_SECONDARY = "ICON_SECONDARY";

        Dictionary<string, string> OperandText = new Dictionary<string, string>()
            {
                { PICATextureCombinerColorOp.Red.ToString(),   "Red" },
                { PICATextureCombinerColorOp.Green.ToString(), "Green" },
                { PICATextureCombinerColorOp.Blue.ToString(),  "Blue" },
                { PICATextureCombinerColorOp.Alpha.ToString(), "Alpha" },
                { PICATextureCombinerColorOp.Color.ToString(), "RGB" },

                { PICATextureCombinerColorOp.OneMinusRed.ToString(),   "1 - Red" },
                { PICATextureCombinerColorOp.OneMinusGreen.ToString(), "1 - Green" },
                { PICATextureCombinerColorOp.OneMinusBlue.ToString(),  "1 - Blue" },
                { PICATextureCombinerColorOp.OneMinusAlpha.ToString(), "1 - Alpha" },
                { PICATextureCombinerColorOp.OneMinusColor.ToString(), "1 - RGB" },
            };

        Dictionary<PICATextureCombinerScale, string> CombinerScaleText = new Dictionary<PICATextureCombinerScale, string>()
            {
                { PICATextureCombinerScale.One,   "1" },
                { PICATextureCombinerScale.Two,   "2" },
                { PICATextureCombinerScale.Four,  "4" },
            };

        Dictionary<PICATextureCombinerMode, string> CombinerModeText = new Dictionary<PICATextureCombinerMode, string>()
            {
                { PICATextureCombinerMode.Replace,   "A" },
                { PICATextureCombinerMode.Add,   "A + B" },
                { PICATextureCombinerMode.Subtract,   "A - B" },
                { PICATextureCombinerMode.Modulate,   "A * B" },
                { PICATextureCombinerMode.AddSigned,   "A + B - 0.5)" },
                { PICATextureCombinerMode.Interpolate,   "mix(B, A, C)" },
                { PICATextureCombinerMode.DotProduct3Rgb,   "dot(A, B).rgb" },
                { PICATextureCombinerMode.DotProduct3Rgba,   "dot(A, B).rgba" },
                { PICATextureCombinerMode.AddMult,   "(A + B) * C" },
                { PICATextureCombinerMode.MultAdd,   "A * B + C" },
            };

        Dictionary<PICATextureCombinerSource, string> SourceText2 = new Dictionary<PICATextureCombinerSource, string>()
            {
                { PICATextureCombinerSource.Texture0,   "Tex0" },
                { PICATextureCombinerSource.Texture1,   "Tex1" },
                { PICATextureCombinerSource.Texture2,   "Tex2" },
                { PICATextureCombinerSource.Texture3,   "Tex3" },
                { PICATextureCombinerSource.FragmentPrimaryColor,   "PixelL" },
                { PICATextureCombinerSource.FragmentSecondaryColor,   "SpecL" },
                { PICATextureCombinerSource.PrimaryColor,   "VertexL" },
                { PICATextureCombinerSource.PreviousBuffer,   "PrevBuffer" },
                { PICATextureCombinerSource.Previous,   "PrevStage" },
                { PICATextureCombinerSource.Constant,   "Constant" },
            };

        Dictionary<PICATextureCombinerSource, string> SourceText = new Dictionary<PICATextureCombinerSource, string>()
            {
                { PICATextureCombinerSource.Texture0,   "Texture_0" },
                { PICATextureCombinerSource.Texture1,   "Texture_1" },
                { PICATextureCombinerSource.Texture2,   "Texture_2" },
                { PICATextureCombinerSource.Texture3,   "Texture_3" },
                { PICATextureCombinerSource.FragmentPrimaryColor,   "Pixel_Lighting" },
                { PICATextureCombinerSource.FragmentSecondaryColor,   "Specular_Lighting" },
                { PICATextureCombinerSource.PrimaryColor,   "Vertex_Lighting" },
                { PICATextureCombinerSource.PreviousBuffer,   "Previous_Buffer" },
                { PICATextureCombinerSource.Previous,   "Previous_Stage" },
                { PICATextureCombinerSource.Constant,   "Constant" },
            };

        enum CombinerPreset
        {
            FlatTexture,
            FragmentShading,
            FragmentSpecularShading,
        }

        public void Init(MaterialWrapper materialWrapper, H3DMaterial material)
        {
            MaterialWrapper = materialWrapper;
            Material = material;

            StageList.Clear();
            //Prepare a list for making edits with based on non empty passes
            for (int i = 0; i < MAX_STAGE_COUNT; i++)
            {
                if (material.MaterialParams.TexEnvStages[i].IsColorPassThrough &&
                    material.MaterialParams.TexEnvStages[i].IsAlphaPassThrough)
                    continue;

                StageList.Add(material.MaterialParams.TexEnvStages[i]);
            }
            //Combiner source icons
            IconManager.TryAddIcon(ICON_PREVIOUS, Resources.PreviousCombinerSource);
            IconManager.TryAddIcon(ICON_PRIMARY, Resources.FragmentPrimaryCombinerSource);
            IconManager.TryAddIcon(ICON_VERTEX_PRIMARY, Resources.VertexCombinerSource);
            IconManager.TryAddIcon(ICON_SECONDARY, Resources.FragmentSecondaryCombinerSource);
            IconManager.TryAddIcon(ICON_PREVIOUS_BUFFER, Resources.PreviousBufferCombinerSource);
            //Preview renders for the stages
            for (int i = 0; i < Stages.Length; i++)
            {
                Stages[i] = new StageViewer();
                UpdateTevPreview(Stages[i].StagePreviewerColorSourceA, i, 0);
                UpdateTevPreview(Stages[i].StagePreviewerColorSourceB, i, 1);
                UpdateTevPreview(Stages[i].StagePreviewerColorSourceC, i, 2);
                UpdateTevPreview(Stages[i].StagePreviewerAlphaSourceA, i, 0, true);
                UpdateTevPreview(Stages[i].StagePreviewerAlphaSourceB, i, 1, true);
                UpdateTevPreview(Stages[i].StagePreviewerAlphaSourceC, i, 2, true);
            }
        }

        //Reloads the visuals of the source of color/alpha previews for all stages
        private void ReloadPreviewer()
        {
            for (int i = 0; i < Stages.Length; i++)
            {
                UpdateTevPreview(Stages[i].StagePreviewerColorSourceA, i, 0);
                UpdateTevPreview(Stages[i].StagePreviewerColorSourceB, i, 1);
                UpdateTevPreview(Stages[i].StagePreviewerColorSourceC, i, 2);
                UpdateTevPreview(Stages[i].StagePreviewerAlphaSourceA, i, 0, true);
                UpdateTevPreview(Stages[i].StagePreviewerAlphaSourceB, i, 1, true);
                UpdateTevPreview(Stages[i].StagePreviewerAlphaSourceC, i, 2, true);
            }
        }

        //Stages to remove
        List<int> removedStages = new List<int>();

        //Selected/hovered stage to drag/reorder
        private int selected_tev_stage = -1;
        private int hovered_tev_stage = -1;

        public void Render()
        {
            int displayID = H3DMaterialParams.DisplayStageID;

            string stageDisplay = displayID == -1 ? "Default" : $"Stage {displayID}";
            if (ImGui.BeginCombo("Display Stage", stageDisplay))
            {
                bool select = displayID == -1;
                if (ImGui.Selectable("Default"))
                {
                    H3DMaterialParams.DisplayStageID = -1;
                    UpdateShaders();
                }
                if (select)
                    ImGui.SetItemDefaultFocus();

                for (int i = 0; i < MAX_STAGE_COUNT; i++)
                {
                    select = displayID == i;
                    if (ImGui.Selectable($"Stage {i}"))
                    {
                        H3DMaterialParams.DisplayStageID = i;
                        UpdateShaders();
                    }
                    if (select)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            if (H3DMaterialParams.DisplayStageID != -1)
            {
                if (ImGui.Checkbox("Blank Previous Stage", ref H3DMaterialParams.DisplayStageDirect))
                    UpdateShaders();
            }

            ImGui.BeginTabBar("tevPages");

            if (ImguiCustomWidgets.BeginTab("tevPages", $"Color"))
            {
                //Drag drop order check
                if (ImGui.IsMouseDragging(0) && selected_tev_stage != -1)
                {
                    if (hovered_tev_stage != selected_tev_stage)
                    {
                        TransferStage(selected_tev_stage, hovered_tev_stage);
                        selected_tev_stage = hovered_tev_stage;
                    }
                }

                if (ImGui.IsMouseReleased(0))
                {
                    selected_tev_stage = -1;
                    hovered_tev_stage = -1;
                }

                for (int i = 0; i < StageList.Count; i++)
                {
                    bool selected = selected_tev_stage == i && ImGui.IsMouseDragging(0);
                    if (selected)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Border, ThemeHandler.Theme.Warning);  
                        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 2);
                    }


                    DrawStageList(StageList[i], i, false, i == MAX_STAGE_COUNT - 1);

                    if (selected)
                    {
                        ImGui.PopStyleColor();
                        ImGui.PopStyleVar();
                    }
                    if (ImGui.IsItemHovered() && ImGui.IsWindowFocused())
                    {
                        hovered_tev_stage = i;
                    }
                    if (ImGui.IsMouseClicked(0) && hovered_tev_stage == i)
                    {
                        selected_tev_stage = i;
                        hovered_tev_stage = i;
                    }
                }

                if (StageList.Count < MAX_STAGE_COUNT)
                {
                    if (ImGui.Button("Add Stage", new System.Numerics.Vector2(ImGui.GetWindowWidth() - 10, 30)))
                    {
                        StageList.Add(PICATexEnvStage.PassThrough);
                        ReloadStageList();
                    }
                }
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("tevPages", $"Alpha"))
            {
                for (int i = 0; i < StageList.Count; i++)
                {
                    DrawStageList(StageList[i], i, true, i == MAX_STAGE_COUNT - 1);
                }
                if (StageList.Count < MAX_STAGE_COUNT)
                {
                    if (ImGui.Button("Add Stage", new System.Numerics.Vector2(ImGui.GetWindowWidth() - 10, 30)))
                    {
                        StageList.Add(PICATexEnvStage.PassThrough);
                        ReloadStageList();
                    }
                }
                ImGui.EndTabItem();
            }

            if (removedStages.Count > 0)
            {
                int result = TinyFileDialog.MessageBoxInfoYesNo("Are you sure you want to remove the selected tev stage? This cannot be undone!");
                if (result == 1)
                {
                    foreach (var stageId in removedStages)
                        StageList.RemoveAt(stageId);

                    ReloadStageList();
                }

                removedStages.Clear();
            }

            ImGui.EndTabBar();
        }

        private void ReloadStageList()
        {
            //Reset in material
            for (int i = 0; i < 6; i++)
                Material.MaterialParams.TexEnvStages[i] = PICATexEnvStage.PassThrough;

            //Assign current stage list
            for (int i = 0; i < StageList.Count; i++)
                Material.MaterialParams.TexEnvStages[i] = StageList[i];

            ReloadPreviewer();
            UpdateShaders();
        }

        private void DrawStageList(PICATexEnvStage stage, int index, bool isAlpha, bool isLastStage)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(0, 0));

            var pos = ImGui.GetCursorPosX();

            string func = isAlpha ? CombinerModeText[stage.Combiner.Alpha] : CombinerModeText[stage.Combiner.Color];

            bool open = ImGui.CollapsingHeader($"Stage {(isAlpha ? "Alpha" : "Color")} {index}     ( {func} )", ImGuiTreeNodeFlags.DefaultOpen);
            ImGui.SetItemAllowOverlap();

            ImGui.SameLine();

            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 100);

            if (ImGui.ArrowButton($"stageUp{index}", ImGuiDir.Up))
            {
                if (index != 0)
                    TransferStage(index, index - 1);
            }
            ImGui.SameLine();
            if (ImGui.ArrowButton($"stageDown{index}", ImGuiDir.Down))
            {
                if (index != StageList.Count - 1)
                    TransferStage(index, index + 1);
            }
            ImGui.SameLine();

            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 45);

            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.5f, 0, 0, 1));
            if (ImGui.Button($"X##stageClose{index}{isAlpha}", new System.Numerics.Vector2(30, 21)))
                removedStages.Add(index);

            ImGui.PopStyleColor();

            ImGui.PopStyleVar();
            ImGui.PopStyleVar();

            if (open)
            {
                ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeHandler.Theme.ChildBg);

                if (ImGui.BeginChild($"texStage{index}ch{isAlpha}", new System.Numerics.Vector2(ImGui.GetWindowWidth() - 2, 140), true, ImGuiWindowFlags.HorizontalScrollbar))
                {
                     RenderStage(index, isAlpha);
                }

                ImGui.EndChild();

                ImGui.PopStyleColor();
            }
        }

        public void RenderStage(int index, bool isAlpha = false)
        {
            var stage = StageList[index];
            var nextStage = ((index + 1) < StageList.Count) ? StageList[index + 1] : null;

            var stageView = Stages[index];

            var color = stage.Color;

            var sourceColorA = stage.Source.Color[0];
            var sourceColorB = stage.Source.Color[1];
            var sourceColorC = stage.Source.Color[2];

            var opColorA = stage.Operand.Color[0];
            var opColorB = stage.Operand.Color[1];
            var opColorC = stage.Operand.Color[2];

            var colorCombiner = stage.Combiner.Color;
            var colorScale = stage.Scale.Color;

            var sourceAlphaA = stage.Source.Alpha[0];
            var sourceAlphaB = stage.Source.Alpha[1];
            var sourceAlphaC = stage.Source.Alpha[2];

            var opAlphaA = stage.Operand.Alpha[0];
            var opAlphaB = stage.Operand.Alpha[1];
            var opAlphaC = stage.Operand.Alpha[2];

            var alphaCombiner = stage.Combiner.Alpha;
            var alphaScale = stage.Scale.Alpha;

            var colorUpdate = nextStage != null ? nextStage.UpdateColorBuffer : false;
            var alphaUpdate = nextStage != null ? nextStage.UpdateAlphaBuffer : false;

            bool update = false;

            void UpdateStage()
            {
                stage.Source.Color[0] = sourceColorA;
                stage.Source.Color[1] = sourceColorB;
                stage.Source.Color[2] = sourceColorC;
                stage.Source.Alpha[0] = sourceAlphaA;
                stage.Source.Alpha[1] = sourceAlphaB;
                stage.Source.Alpha[2] = sourceAlphaC;
                stage.Combiner.Color = colorCombiner;
                stage.Combiner.Alpha = alphaCombiner;
                stage.Scale.Color = colorScale;
                stage.Scale.Alpha = alphaScale;
                stage.Operand.Color[0] = opColorA;
                stage.Operand.Color[1] = opColorB;
                stage.Operand.Color[2] = opColorC;
                stage.Operand.Alpha[0] = opAlphaA;
                stage.Operand.Alpha[1] = opAlphaB;
                stage.Operand.Alpha[2] = opAlphaC;
                if (nextStage != null)
                {
                    nextStage.UpdateColorBuffer = colorUpdate;
                    nextStage.UpdateAlphaBuffer = alphaUpdate;
                }

                UpdateShaders();

                for (int i = 0; i < Stages.Length; i++)
                {
                    UpdateTevPreview(Stages[i].StagePreviewerColorSourceA, i, 0);
                    UpdateTevPreview(Stages[i].StagePreviewerColorSourceB, i, 1);
                    UpdateTevPreview(Stages[i].StagePreviewerColorSourceC, i, 2);
                }

                GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
            }

            if (isAlpha)
                DrawCombinerFunc($"Function##func{index}", ref alphaCombiner, UpdateStage);
            else
                DrawCombinerFunc($"Function##func{index}", ref colorCombiner, UpdateStage);

            void DrawA()
            {
                if (isAlpha)
                    DrawAlphaSource("A", index, ref sourceAlphaA, ref opAlphaA, stageView.StagePreviewerAlphaSourceA, UpdateStage);
                else
                    DrawColorSource("A", index, ref sourceColorA, ref opColorA, stageView.StagePreviewerColorSourceA, UpdateStage);
            }
            void DrawB()
            {
                if (isAlpha)
                    DrawAlphaSource("B", index, ref sourceAlphaB, ref opAlphaB, stageView.StagePreviewerAlphaSourceB, UpdateStage);
                else
                    DrawColorSource("B", index, ref sourceColorB, ref opColorB, stageView.StagePreviewerColorSourceB, UpdateStage);
            }
            void DrawC()
            {
                if (isAlpha)
                    DrawAlphaSource("C", index, ref sourceAlphaC, ref opAlphaC, stageView.StagePreviewerAlphaSourceC, UpdateStage);
                else
                    DrawColorSource("C", index, ref sourceColorC, ref opColorC, stageView.StagePreviewerColorSourceC, UpdateStage); 
            }

            var mode = isAlpha ? stage.Combiner.Alpha : stage.Combiner.Color;
            switch (mode)
            {
                case PICATextureCombinerMode.Replace:
                    DrawA();
                    break;
                case PICATextureCombinerMode.Add:
                case PICATextureCombinerMode.AddSigned:
                    DrawA();
                    DrawAdd();
                    DrawB();
                    break;
                case PICATextureCombinerMode.Modulate:
                    DrawA();
                    DrawMultiply();
                    DrawB();
                    break;
                case PICATextureCombinerMode.Interpolate:
                    OperatorText("mix("); ImGui.SameLine();
                    DrawA(); ImGui.SameLine();
                    OperatorText(","); ImGui.SameLine();
                    DrawB(); ImGui.SameLine();
                    OperatorText(","); ImGui.SameLine();
                    DrawC(); ImGui.SameLine();
                    OperatorText(")");
                    break;
                case PICATextureCombinerMode.Subtract:
                    DrawA();
                    DrawSub();
                    DrawB();
                    break;
                case PICATextureCombinerMode.MultAdd:
                    DrawA();
                    DrawMultiply();
                    DrawB();
                    DrawAdd();
                    DrawC();
                    break;
                case PICATextureCombinerMode.AddMult:
                    DrawA();
                    DrawAdd();
                    DrawB();
                    DrawMultiply();
                    DrawC();
                    break;
                case PICATextureCombinerMode.DotProduct3Rgb:
                case PICATextureCombinerMode.DotProduct3Rgba:
                    OperatorText("dot("); ImGui.SameLine();
                    DrawA(); ImGui.SameLine();
                    OperatorText(","); ImGui.SameLine();
                    DrawB(); ImGui.SameLine();
                    OperatorText(","); ImGui.SameLine();
                    DrawC(); ImGui.SameLine();
                    OperatorText(")");
                    break;
            }

            if (nextStage != null && ImGui.Checkbox("Save To Buffer", ref ((isAlpha) ? ref alphaUpdate : ref colorUpdate)))
            {
                UpdateStage();
                ImGui.SameLine();
            }

            if (isAlpha)
                DrawScale("Alpha Scale", index, ref alphaScale, UpdateStage);
            else
                DrawScale("Color Scale", index, ref colorScale, UpdateStage);
            ImGui.SameLine();

            if (!isAlpha)
            {
                if ((sourceColorA == PICATextureCombinerSource.Constant) || 
                    (sourceColorB == PICATextureCombinerSource.Constant && HasSourceB(mode)) ||
                    (sourceColorC == PICATextureCombinerSource.Constant && HasSourceC(mode)))
                {
                    ImGui.PushItemWidth(100);
                    BcresUIHelper.DrawEnum($"Constant##const{index}", ref stage.Constant, () =>
                    {
                        UpdateStage();
                    });
                    ImGui.PopItemWidth();
                }
            }
            else
            {

                if (sourceAlphaA == PICATextureCombinerSource.Constant ||
                    (sourceAlphaB == PICATextureCombinerSource.Constant && HasSourceB(mode)) ||
                    (sourceAlphaC == PICATextureCombinerSource.Constant && HasSourceC(mode)))
                {
                    ImGui.PushItemWidth(100);
                    BcresUIHelper.DrawEnum($"Constant##const{index}", ref stage.Constant, () =>
                    {
                        UpdateStage();
                    });
                    ImGui.PopItemWidth();
                }
            }
        }

        public bool HasSourceB(PICATextureCombinerMode func)
        {
            if (func == PICATextureCombinerMode.Replace) return false;
            return true;
        }

        public bool HasSourceC(PICATextureCombinerMode func)
        {
            switch (func)
            {
                case PICATextureCombinerMode.Replace:
                case PICATextureCombinerMode.Add:
                case PICATextureCombinerMode.AddSigned:
                case PICATextureCombinerMode.Subtract:
                case PICATextureCombinerMode.Modulate:
                    return false;
            }
            return true;
        }

        public void TransferStage(int source, int target)
        {
            //Swap out the target and dst color values
            var src = StageList[source];
            var dst = StageList[target];

            StageList[source] = dst;
            StageList[target] = src;

            ReloadStageList();
        }

        public void TransferColorStage(int source, int target)
        {
            //Swap out the target and dst color values
            var src = Material.MaterialParams.TexEnvStages[source];
            var dst = Material.MaterialParams.TexEnvStages[target];

            //Values
            var srcCombiner = dst.Combiner.Color;
            var srcOperand = dst.Operand.Color;
            var scaleCombiner = dst.Scale.Color;
            var srcUpdate = dst.UpdateColorBuffer;
            var srcConstant = dst.Constant;

            //Values
            var dstCombiner = dst.Combiner.Color;
            var dstOperand = dst.Operand.Color;
            var dstScaleCombiner = dst.Scale.Color;
            var dstUpdate = dst.UpdateColorBuffer;
            var dstConstant = dst.Constant;

            //Apply dest
            dst.Combiner.Color = srcCombiner;
            dst.Operand.Color = srcOperand;
            dst.Scale.Color = scaleCombiner;
            dst.UpdateColorBuffer = srcUpdate;
            dst.Constant = srcConstant;

            //Apply src
            dst.Combiner.Color = dstCombiner;
            dst.Operand.Color = dstOperand;
            dst.Scale.Color = dstScaleCombiner;
            dst.UpdateColorBuffer = dstUpdate;
            dst.Constant = dstConstant;
        }

        void UpdateShaders()
        {
            MaterialWrapper.UpdateShaders();
            GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
        }

        private void DrawScale(string label, int stage, ref PICATextureCombinerScale scale, Action update)
        {
            ImGui.PushItemWidth(100);

            if (ImGui.BeginCombo($"##Scale{label}{stage}", $"Scale = {CombinerScaleText[scale]}", ImGuiComboFlags.NoArrowButton))
            {
                foreach (PICATextureCombinerScale val in Enum.GetValues(typeof(PICATextureCombinerScale)))
                {
                    bool isSelected = scale.Equals(val);

                    if (ImGui.Selectable(CombinerScaleText[val], isSelected))
                    {
                        scale = val;
                        update.Invoke();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
        }

        private void DrawOperand(string label, int stage, ref PICATextureCombinerColorOp op, Action update)
        {
            if (ImGui.BeginCombo($"##Operand{label}{stage}", $"{OperandText[op.ToString()]}", ImGuiComboFlags.NoArrowButton))
            {
                foreach (PICATextureCombinerColorOp val in Enum.GetValues(typeof(PICATextureCombinerColorOp)))
                {
                    bool isSelected = op.Equals(val);

                    if (ImGui.Selectable(OperandText[val.ToString()], isSelected))
                    {
                        op = val;
                        update.Invoke();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
        }

        private void DrawOperand(string label, int stage, ref PICATextureCombinerAlphaOp op, Action update)
        {
            if (ImGui.BeginCombo($"##Operand{label}{stage}", $"{OperandText[op.ToString()]}", ImGuiComboFlags.NoArrowButton))
            {
                foreach (PICATextureCombinerAlphaOp val in Enum.GetValues(typeof(PICATextureCombinerAlphaOp)))
                {
                    bool isSelected = op.Equals(val);

                    if (ImGui.Selectable(OperandText[val.ToString()], isSelected))
                    {
                        op = val;
                        update.Invoke();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
        }

        private void DrawSource(string label, int stage, ref PICATextureCombinerSource mode, Action update)
        {
            if (ImGui.BeginCombo($"##Source{label}{stage}", $"{label} = {SourceText2[mode]}", ImGuiComboFlags.NoArrowButton))
            {
                foreach (PICATextureCombinerSource val in Enum.GetValues(typeof(PICATextureCombinerSource)))
                {
                    bool isSelected = mode.Equals(val);

                    if (ImGui.Selectable(SourceText[val], isSelected))
                    {
                        mode = val;
                        update.Invoke();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
        }

        private void DrawCombinerFunc(string label, ref PICATextureCombinerMode mode, Action update)
        {
            ImGui.PushItemWidth(ImGui.GetWindowWidth() - 25);
            if (ImGui.BeginCombo($"##Function{label}", $"Function:             {CombinerModeText[mode]}"))
            {
                foreach (PICATextureCombinerMode val in Enum.GetValues(typeof(PICATextureCombinerMode)))
                {
                    bool isSelected = mode.Equals(val);
                    string cblabel = CombinerModeText[val];

                    if (ImGui.Selectable(cblabel, isSelected))
                    {
                        mode = val;
                        update.Invoke();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
        }

        private void OperatorText(string text)
        {
            ImGui.PushFont(ImGuiController.FontOperator);
            ImGui.Text(text);
            ImGui.PopFont();
        }

        private void DrawAdd()
        {
            ImGui.SameLine();
            ImGui.PushFont(ImGuiController.FontOperator);
            ImGui.Text("+");
            ImGui.PopFont();
            ImGui.SameLine();
        }

        private void DrawSub()
        {
            ImGui.SameLine();
            ImGui.PushFont(ImGuiController.FontOperator);
            ImGui.Text("-");
            ImGui.PopFont();
            ImGui.SameLine();
        }

        private void DrawMultiply()
        {
            ImGui.SameLine();
            ImGui.PushFont(ImGuiController.FontOperator);
            ImGui.Text("x");
            ImGui.PopFont();
            ImGui.SameLine();
        }

        private void DrawAlphaSource(string label, int stage,
          ref PICATextureCombinerSource source,
          ref PICATextureCombinerAlphaOp operand,
          TexStagePreviewer previewer, Action update)
        {

            ImGui.BeginGroup();

            var prev = ImGui.GetCursorScreenPos();

            if (source == PICATextureCombinerSource.Constant)
            {
                var stg = Material.MaterialParams.TexEnvStages[stage];
                var color = GetConstantColor(stg);
                var col = new System.Numerics.Vector4(color.R, color.G, color.B, color.A) / 255.0f;

                if (ImGui.ColorButton($"##{label}{stage}srcConst", col, ImGuiColorEditFlags.AlphaPreview, new System.Numerics.Vector2(50, 50)))
                    ImGui.OpenPopup($"##{label}{stage}constant-picker");
                if (ImGui.BeginPopup($"##{label}{stage}constant-picker"))
                {
                    if (ImGui.ColorPicker4("##constant_picker", ref col, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar))
                    {
                        SetConstantColor(stg, new SPICA.Math3D.RGBA(
                             (byte)(col.X * 255),
                             (byte)(col.Y * 255),
                             (byte)(col.Z * 255),
                             (byte)(col.W * 255)));
                        GLContext.ActiveContext.UpdateViewport = true;
                    }
                    ImGui.EndPopup();
                }
            }
            else
                previewer.Draw();

            ImGui.SameLine();

            var pos = ImGui.GetCursorPosX();
            var screenPos = ImGui.GetCursorScreenPos();
            bool selected = selected_tev_stage != -1 && ImGui.IsMouseDragging(0);

            ImGuiHelper.BeginBoldText();

            ImGui.PushItemWidth(100);
            DrawSource($"{label}", stage, ref source, update);

            if (ImGui.IsItemHovered() && !selected)
                ImGuiHelper.Tooltip("Vertex Lighting: Enables vertex colors and vertex lighting info.\n" +
                    "Pixel Lighting: Enables per pixel lighting.\n" +
                    "Specular Lighting: Enables specular lighting for shiny materials. \n" +
                    "Texture 0: Use First Texture.\n" +
                    "Texture 1: Use Second Texture.\n" +
                    "Texture 2: Use Third Texture.\n" +
                    "Previous Buffer: Use the previous stage with update buffer enabled or uses buffer color.\n" +
                    "Previous: Use the last stage before the current one.\n" +
                    "Constant: Use a specified color.");

            ImGui.SetCursorPosX(pos);
            ImGui.SetCursorScreenPos(new System.Numerics.Vector2(screenPos.X, screenPos.Y + 30));

            DrawOperand($"{label}", stage, ref operand, update);
            ImGui.PopItemWidth();

            if (ImGui.IsItemHovered() && !selected)
                ImGuiHelper.Tooltip("Configures what channel layout to use.");

            ImGuiHelper.EndBoldText();

            ImGui.EndGroup();
        }

        private void DrawColorSource(string label, int stage,
            ref PICATextureCombinerSource source,
            ref PICATextureCombinerColorOp operand,
            TexStagePreviewer previewer, Action update)
        {

            ImGui.BeginGroup();

            var prev = ImGui.GetCursorScreenPos();

            if (source == PICATextureCombinerSource.Constant)
            {
                var stg = Material.MaterialParams.TexEnvStages[stage];
                var color = GetConstantColor(stg);
                var col = new System.Numerics.Vector4(color.R, color.G, color.B, color.A) / 255.0f;

                bool hasAlpha = operand == PICATextureCombinerColorOp.Alpha || operand == PICATextureCombinerColorOp.OneMinusAlpha;

                if (ImGui.ColorButton($"##{label}{stage}srcConst", col, hasAlpha ? ImGuiColorEditFlags.AlphaPreview : ImGuiColorEditFlags.None, new System.Numerics.Vector2(50, 50)))
                    ImGui.OpenPopup($"##{label}{stage}constant-picker");
                if (ImGui.BeginPopup($"##{label}{stage}constant-picker"))
                {
                    if (ImGui.ColorPicker4("##constant_picker", ref col, ImGuiColorEditFlags.AlphaBar))
                    {
                        SetConstantColor(stg, new SPICA.Math3D.RGBA(
                             (byte)(col.X * 255),
                             (byte)(col.Y * 255),
                             (byte)(col.Z * 255),
                             (byte)(col.W * 255)));
                        GLContext.ActiveContext.UpdateViewport = true;
                    }
                    ImGui.EndPopup();
                }
            }
            else
                previewer.Draw();

            ImGui.SameLine();

            var pos = ImGui.GetCursorPosX();
            var screenPos = ImGui.GetCursorScreenPos();

            ImGui.PushItemWidth(100);

            ImGuiHelper.BeginBoldText();
            DrawSource($"{label}", stage, ref source, update);

            if (ImGui.IsItemHovered())
                ImGuiHelper.Tooltip("Vertex Lighting: Enables vertex colors and vertex lighting info.\n" +
                    "Pixel Lighting: Enables per pixel lighting.\n" +
                    "Specular Lighting: Enables specular lighting for shiny materials. \n" +
                    "Texture 0: Use First Texture.\n" +
                    "Texture 1: Use Second Texture.\n" +
                    "Texture 2: Use Third Texture.\n" +
                    "Previous Buffer: Use the stage with update buffer enabled.\n" +
                    "Previous: Use the last stage before the current one.\n" +
                    "Constant: Use a specified color.");

            ImGui.SetCursorPosX(pos);
            ImGui.SetCursorScreenPos(new System.Numerics.Vector2(screenPos.X, screenPos.Y + 30));

            DrawOperand($"{label}", stage, ref operand, update);
            ImGui.PopItemWidth();

            ImGuiHelper.Tooltip("Configures what channel layout to use.");

            ImGuiHelper.EndBoldText();

            ImGui.EndGroup();
        }

        private void UpdateTevPreview(TexStagePreviewer previewer, int stageID, int id, bool isAlpha = false)
        {
            var stage = Material.MaterialParams.TexEnvStages[stageID];
            previewer.TextureID = -1;
            previewer.ColorOperand = stage.Operand.Color[id];
            previewer.AlphaOperand = stage.Operand.Alpha[id];
            previewer.Color = new OpenTK.Vector4(1,1,1,1);
            previewer.IsAlpha = isAlpha;
            previewer.ShowAlpha = false;

            var combiner = isAlpha ? stage.Source.Alpha[id] : stage.Source.Color[id];

            void SetTex(string textureName)
            {
                if (textureName != null && IconManager.HasIcon(textureName))
                    previewer.TextureID = IconManager.GetTextureIcon(textureName);
                else
                {
                    //Toggle alpha display for the texture icon
                    previewer.ShowAlpha = true;
                    previewer.TextureID = IconManager.GetTextureIcon("TEXTURE");
                }
            }

            switch (combiner)
            {
                case PICATextureCombinerSource.Texture0:
                    SetTex(Material.Texture0Name);
                    break;
                case PICATextureCombinerSource.Texture1:
                    SetTex(Material.Texture1Name);
                    break;
                case PICATextureCombinerSource.Texture2:
                    SetTex(Material.Texture2Name);
                    break;
                case PICATextureCombinerSource.Previous:
                    previewer.TextureID = IconManager.GetTextureIcon(ICON_PREVIOUS);
                    break;
                case PICATextureCombinerSource.PreviousBuffer:
                    previewer.TextureID = IconManager.GetTextureIcon(ICON_PREVIOUS_BUFFER);
                    break;
                case PICATextureCombinerSource.PrimaryColor:
                    previewer.TextureID = IconManager.GetTextureIcon(ICON_VERTEX_PRIMARY);
                    break;
                case PICATextureCombinerSource.FragmentPrimaryColor:
                    previewer.TextureID = IconManager.GetTextureIcon(ICON_PRIMARY);
                    break;
                case PICATextureCombinerSource.FragmentSecondaryColor:
                    previewer.TextureID = IconManager.GetTextureIcon(ICON_SECONDARY);
                    break;
                case PICATextureCombinerSource.Constant:
                    previewer.TextureID = -1; //color
                    var col = GetConstantColor(stage).ToVector4();
                    previewer.Color = new OpenTK.Vector4(col.X, col.Y, col.Z, col.W);
                    break;
            }
            previewer.Update = true;
        }

        private SPICA.Math3D.RGBA GetConstantColor(PICATexEnvStage stage)
        {
            var mparam = Material.MaterialParams;
            switch (stage.Constant)
            {
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Constant0: return mparam.Constant0Color;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Constant1: return mparam.Constant1Color;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Constant2: return mparam.Constant2Color;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Constant3: return mparam.Constant3Color;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Constant4: return mparam.Constant4Color;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Constant5: return mparam.Constant5Color;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Diffuse: return mparam.DiffuseColor;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Ambient: return mparam.AmbientColor;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Specular0: return mparam.Specular0Color;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Specular1: return mparam.Specular1Color;
                default:
                    return stage.Color;
            }
        }

        private void SetConstantColor(PICATexEnvStage stage, SPICA.Math3D.RGBA color)
        {
            var mparam = Material.MaterialParams;
            switch (stage.Constant)
            {
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Constant0: mparam.Constant0Color = color; break;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Constant1: mparam.Constant1Color = color; break;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Constant2: mparam.Constant2Color = color; break;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Constant3: mparam.Constant3Color = color; break;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Constant4: mparam.Constant4Color = color; break;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Constant5: mparam.Constant5Color = color; break;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Diffuse: mparam.DiffuseColor = color; break;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Ambient: mparam.AmbientColor = color; break;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Specular0: mparam.Specular0Color = color; break;
                case SPICA.Formats.CtrGfx.Model.Material.GfxTexEnvConstant.Specular1: mparam.Specular1Color = color; break;
                default:
                    stage.Color = color; break;
            }
            stage.Color = color;
        }
    }
}
