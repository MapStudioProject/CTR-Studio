using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ImGuiNET;
using Toolbox.Core;
using System.IO;
using System.ComponentModel;
using Toolbox.Core.Imaging;
using GLFrameworkEngine;
using UIFramework;
using MapStudio.UI;
using SPICA.PICA.Commands;
using CtrLibrary.UI;

namespace CtrLibrary
{
    public class H3DTextureDialog : Window
    {
        //Name of window
        public override string Name => TranslationSource.GetText("TEXTURE_DIALOG");

        /// <summary>
        /// The supported formats for loading into the dialog.
        /// </summary>
        public static string[] SupportedExtensions = new string[] {
            ".png",".jpeg",".jpg",".bmp",".gif",".tga", ".tif", ".tiff", ".exr",
        };

        /// <summary>
        /// The list of imported textures.
        /// </summary>
        public List<H3DImportedTexture> Textures = new List<H3DImportedTexture>();

        //Selected editor indices
        List<int> SelectedIndices = new List<int>();

        //Image display
        private GLTexture2D DecodedTexture;

        //The thread to encode/decode the texture.
        private Thread Thread;

        //task display
        private string TaskProgress = "";

        //Selected texture to configure
        private int ActiveTextureIndex = -1;

        //A check for when the encoder has finished processing
        bool finishedEncoding = false;

        //Init a default blank texture to prepare loading the images onto.
        private void OnLoad() {
            DecodedTexture = GLTexture2D.CreateUncompressedTexture(1, 1);
        }

        public H3DTextureDialog()
        {

        }


        /// <summary>
        /// Adds an importable texture with the given supported file format.
        /// The settings are returned and can be configured per file.
        /// Returns null if the format is not supported.
        /// </summary>
        public H3DImportedTexture AddTexture(string fileName) {
            if (!File.Exists(fileName))
                throw new Exception($"Invalid input file path! {fileName}");
            //File not supported, return
            if (!SupportedExtensions.Contains(Path.GetExtension(fileName).ToLower()))
                return null;

            var tex = new H3DImportedTexture(fileName);
            Textures.Add(tex);

            //Default to ETC1 (best compression size) but check for alpha and other bits 
            tex.Format = tex.TryDetectFormat(PICATextureFormat.ETC1);

            return tex;
        }

        /// <summary>
        /// Adds an importable texture with the given raw rgba data, width, height, format and mip count.
        /// The settings are returned and can be configured per file.
        /// </summary>
        public H3DImportedTexture AddTexture(string name, byte[] rgbaData, uint width, uint height, uint mipCount, PICATextureFormat format)
        {
            var tex = new H3DImportedTexture(name, rgbaData, width, height, mipCount, format);
            Textures.Add(tex);
            return tex;
        }

        /// <summary>
        /// Renders the dialog window.
        /// </summary>
        public override void Render()
        {
            //Display the first image
            if (ActiveTextureIndex == -1)
                ReloadImageDisplay();

            if (ImGui.IsKeyPressed((int)ImGuiKey.Enter))
            {
                //finish encoding all textures that haven't encoded yet
                //Execute before draw for progress bar to update
                UIManager.ActionExecBeforeUIDraw = () =>
                {
                    EncodeAll();
                    Dispose();
                    DialogHandler.ClosePopup(true);
                };
            }

            ImGui.Columns(3);
            DrawList();
            ImGui.NextColumn();

            ImGui.Text(TaskProgress);
            DrawCanvas();
            ImGui.NextColumn();
            DrawProperties();
            ImGui.NextColumn();
            ImGui.Columns(1);
        }

        /// <summary>
        /// Draws a list of textures for encoding.
        /// </summary>
        private void DrawList()
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);

            if (ImGui.BeginChild("##texture_dlg_list")){
                ImGui.Columns(2);

                //Force a selection
                if (SelectedIndices.Count == 0)
                    SelectedIndices.Add(0);

                for (int i = 0; i < Textures.Count; i++)
                {
                    bool isSelected = SelectedIndices.Contains(i);

                    if (ImGui.Selectable(Textures[i].Name, isSelected, ImGuiSelectableFlags.SpanAllColumns)) {

                        if (!ImGui.GetIO().KeyShift)
                            SelectedIndices.Clear();

                        SelectedIndices.Add(i);

                        //Selection range
                        if (ImGui.GetIO().KeyShift)
                        {
                            bool selectRange = false;
                            for (int j = 0; j < Textures.Count; j++)
                            {
                                if (SelectedIndices.Contains(j) || j == i)
                                {
                                    if (!selectRange)
                                        selectRange = true;
                                    else
                                        selectRange = false;
                                }
                                if (selectRange && !SelectedIndices.Contains(j))
                                    SelectedIndices.Add(j);
                            }
                        }
                        ReloadImageDisplay();
                    }
                    if (ImGui.IsItemFocused() && !isSelected)
                    {
                        if (!ImGui.GetIO().KeyShift)
                            SelectedIndices.Clear();

                        SelectedIndices.Add(i);
                        ReloadImageDisplay();
                    }

                    ImGui.NextColumn();
                    ImGui.Text(Textures[i].Format.ToString());
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
            }
            ImGui.EndChild();

            ImGui.PopStyleColor();
        }

        private byte[] decodedImage;

        /// <summary>
        /// Draws the canvas where images output to.
        /// </summary>
        private void DrawCanvas()
        {
            //Prepare the render image
            if (DecodedTexture == null)
                OnLoad();

            var selectedIndex = SelectedIndices.FirstOrDefault();
            var texture = Textures[selectedIndex];

            if (finishedEncoding) {
                //Display the decoded data as an RGBA texture
                DecodedTexture.Width = texture.Width;
                DecodedTexture.Height = texture.Height;
                DecodedTexture.Reload(texture.Width, texture.Height, decodedImage);
                finishedEncoding = false;
            }

            if (ImGui.BeginChild("##texture_dlg_canvas"))
            {
                var size = ImGui.GetWindowSize();

                //background
                var pos = ImGui.GetCursorPos();
                ImGui.Image((IntPtr)IconManager.GetTextureIcon("CHECKERBOARD"), size);
                //image

                //Aspect size

                #region Calculate Aspect Size
                float tw, th, tx, ty;

                int w = DecodedTexture.Width;
                int h = DecodedTexture.Height;

                double whRatio = (double)w / h;
                if (DecodedTexture.Width >= DecodedTexture.Height)
                {
                    tw = size.X;
                    th = (int)(tw / whRatio);
                }
                else
                {
                    th = size.Y;
                    tw = (int)(th * whRatio);
                }

                //Rectangle placement
                tx = (size.X - tw) / 2;
                ty = (size.Y - th) / 2;

                #endregion

                ImGui.SetCursorPos(new Vector2(pos.X, pos.Y + ty));
                ImGui.Image((IntPtr)DecodedTexture.ID, new Vector2(tw, th));
            }
            ImGui.EndChild();
        }

        private void DrawProperties()
        {
            if (Textures.Count == 0)
                return;

            //There is always a selected texture
            var selectedIndex = SelectedIndices.FirstOrDefault();
            var texture = Textures[selectedIndex];
            var size = ImGui.GetWindowSize();

            var wndsize = new Vector2(ImGui.GetColumnWidth(), ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - 36);
            if (ImGui.BeginChild("##texture_dlg_properties", wndsize))
            {
                string encoding_size = texture.EncodingSize;

                ImGuiHelper.BoldTextLabel("Encoding Size:", encoding_size);

                if (ImGui.BeginCombo("Format", texture.Format.ToString()))
                {
                    foreach (PICATextureFormat format in Enum.GetValues(typeof(PICATextureFormat)))
                    {
                        bool isSelected = format == texture.Format;
                        if (ImGui.Selectable(format.ToString()))
                        {
                            //Disable current display
                            DecodedTexture.Reload(1, 1, new byte[4]);
                            //Multi edit
                            foreach (var selection in SelectedIndices)
                            {
                                Textures[selection].Format = format;
                                //Re encode format
                                Textures[selection].Encoded = false;
                            }
                            ReloadImageDisplay();
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }

                    ImGui.EndCombo();
                }

                if (ImGuiHelper.InputFromUint(TranslationSource.GetText("MIP_COUNT"), texture, "MipCount", 1, false))
                {
                    foreach (var index in SelectedIndices)
                        this.Textures[index].MipCount = texture.MipCount;
                }

                ImGuiHelper.BoldTextLabel(TranslationSource.GetText("WIDTH"), texture.Width.ToString());
                ImGuiHelper.BoldTextLabel(TranslationSource.GetText("HEIGHT"), texture.Height.ToString());

                bool isETC1 = texture.Format == PICATextureFormat.ETC1 || texture.Format == PICATextureFormat.ETC1A4;
                if (isETC1)
                {
                    if (ImGui.Checkbox("Use Better Encoder", ref ETC1Compressor.UseEncoder))
                    {
                        //Disable current display
                        DecodedTexture.Reload(1, 1, new byte[4]);
                        //Multi edit
                        foreach (var selection in SelectedIndices)
                        {
                            //Re encode format
                            Textures[selection].Encoded = false;
                        }
                        ReloadImageDisplay();
                    }
                    if (ImGui.Checkbox("High Quality (Slow Compress)", ref ETC1Compressor.IsHighQuality))
                    {
                        //Disable current display
                        DecodedTexture.Reload(1, 1, new byte[4]);
                        //Multi edit
                        foreach (var selection in SelectedIndices)
                        {
                            //Re encode format
                            Textures[selection].Encoded = false;
                        }
                        ReloadImageDisplay();
                    }
                }
            }
            ImGui.EndChild();

            ImGui.SetCursorPos(new Vector2(size.X - 160, size.Y - 35));

            var buttonSize = new Vector2(70, 30);
            //Don't allow applying till an encoding operation is finished
            if (encoding)
            {
                var disabled = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
                ImGui.PushStyleColor(ImGuiCol.Text, disabled);
                ImGui.Button("Ok", buttonSize);
                ImGui.PopStyleColor();
            }
            else
            {
                if (ImGui.Button("Ok", buttonSize))
                {
                    //finish encoding all textures that haven't encoded yet
                    //Execute before draw for progress bar to update
                    UIManager.ActionExecBeforeUIDraw = () =>
                    {
                        EncodeAll();
                        Dispose();
                        DialogHandler.ClosePopup(true);
                    };
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel", buttonSize))
            {
                Dispose();
                DialogHandler.ClosePopup(false);
            }
        }

        /// <summary>
        /// Applies the encoding for all images and disposes the loaded files.
        /// </summary>
        public void Apply()
        {
            EncodeAll();
            Dispose();
        }

        //Dispose all UI resources and loaded texture files.
        private void Dispose()
        {
            //Dispose all raw texture file data (encoded is kept)
            foreach (var texture in Textures)
                texture.Dispose();
        }

        //Starts an encoding process for the selected texture to display after.
        private void ReloadImageDisplay()
        {
            if (Textures.Count == 0)
                return;

            var selectedIndex = SelectedIndices.FirstOrDefault();
            var texture = Textures[selectedIndex];

            ActiveTextureIndex = selectedIndex;

            Task task = Task.Factory.StartNew(DisplayEncodedTexture);
            task.Wait();
        }

        private bool encoding = false;

        private void DisplayEncodedTexture()
        {
            if (Textures.Count == 0 || encoding)
                return;

            TaskProgress = "Encoding texture..";

            var selectedIndex = SelectedIndices.FirstOrDefault();
            var texture = Textures[selectedIndex];

            Thread = new Thread((ThreadStart)(() =>
            {
                encoding = true;

                try
                {
                    //Encode the current format
                    if (!texture.Encoded)
                    {
                        texture.EncodeTexture(texture.ActiveArrayIndex);
                        texture.Encoded = true;
                    }
                    TaskProgress = "Decoding texture..";

                    //Decode the newly encoded image data
                    decodedImage = texture.DecodeTexture(texture.ActiveArrayIndex);
                    //Check if the texture has been changed or not while the thread is running
                    if (texture != Textures[selectedIndex])
                        return;

                    TaskProgress = $"Encoded {texture.Format} in {texture.EncodingTime}";
                    finishedEncoding = true;
                    encoding = false;
                }
                catch
                {
                    TaskProgress = $"Failed to encode {texture.Format}!";
                    encoding = false;
                }
            }));
            Thread.Start();
        }

        //Encodes all textures present in the dialog including all surface levels
        private void EncodeAll()
        {
            if (Textures.Count == 0)
                return;

            if (Textures.Count > 0)
                ProcessLoading.Instance.IsLoading = true;

            //Also do not run this on a thread for now. 
            //All operations need to be finished before the dialog can close and dispose any resources.
            for (int j = 0; j < Textures.Count; j++)
            {
                ProcessLoading.Instance.Update(j, Textures.Count, $"Encoding {Textures[j].Name}");

                //Encode the current format
                for (int i = 0; i < Textures[j].Surfaces.Count; i++)
                {
                    if (!Textures[j].Encoded)
                    {
                        try
                        {
                            Textures[j].EncodeTexture(i);
                            Textures[j].Encoded = true;
                        }
                        catch (Exception ex)
                        {
                            DialogHandler.ShowException(ex);
                        }
                    }
                }
            }
            ProcessLoading.Instance.Update(100, 100, $"Finished Encoding!");
            ProcessLoading.Instance.IsLoading = false;
        }
    }
}
