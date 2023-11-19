using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrGfx.Texture;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Texture;
using Toolbox.Core;
using SPICA.PICA.Commands;
using MapStudio.UI;
using System.IO;
using GLFrameworkEngine;
using CtrLibrary.Rendering;
using IONET.Collada.FX.Rendering;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SPICA.PICA.Converters;
using SPICA.Math3D;
using SixLabors.ImageSharp.Processing;
using static CtrLibrary.Bch.BCH;
using SPICA.Rendering;
using CtrLibrary.UI;
using System.Runtime.InteropServices;

namespace CtrLibrary.Bch
{
    public class TextureFolder<T> : BCH.H3DGroupNode<T> where T : SPICA.Formats.Common.INamed
    {
        //Render for updating texture cache
        internal H3DRender H3DRender;

        //Tree node type to add when creating new UI nodes
        public override Type ChildNodeType => typeof(CTEX<T>);

        /// <summary>
        /// Gets all the current textures from the folder
        /// </summary>
        public H3DDict<H3DTexture> GetTextures()
        {
            H3DDict<H3DTexture> textures = new H3DDict<H3DTexture>();
            foreach (CTEX<H3DTexture> tex in this.Children)
                textures.Add(tex.Texture);

            return textures;
        }

        public TextureFolder(H3DRender render, H3DDict<T> subSections) : base(BCH.H3DGroupType.Textures, subSections)
        {
            H3DRender = render;
            for (int i = 0; i < subSections.Count; i++)
                AddChild(new CTEX<T>(subSections, subSections[i]));
        }

        public override void Import()
        {
            //Dialog for importing textures. 
            ImguiFileDialog fileDialog = new ImguiFileDialog();
            fileDialog.MultiSelect = true;
            fileDialog.AddFilter(".bctex", ".bctex");

            foreach (var ext in TextureDialog.SupportedExtensions)
                fileDialog.AddFilter(ext, ext);

            if (fileDialog.ShowDialog())
                ImportTexture(fileDialog.FilePaths);
        }

        public override void ReplaceAll()
        {
            ImguiFolderDialog dlg = new ImguiFolderDialog();
            if (dlg.ShowDialog())
            {
                List<string> files = new List<string>();
                foreach (string f in Directory.GetFiles(dlg.SelectedPath))
                {
                    foreach (CTEX<T> tex in this.Children)
                    {
                        if (Path.GetFileNameWithoutExtension(f) == tex.Header)
                            files.Add(f);
                    }
                }
                if (files.Count > 0)
                    ImportTexture(files.ToArray());
            }
        }

        public override void ExportAll()
        {
            ImguiFolderDialog dlg = new ImguiFolderDialog();
            if (dlg.ShowDialog())
            {
                foreach (CTEX<T> tex in this.Children)
                    tex.ExportTexture(Path.Combine(dlg.SelectedPath, $"{tex.Header}.png"));
            }
        }

        public override void Clear()
        {
            int result = TinyFileDialog.MessageBoxInfoYesNo("Are you sure you want to clear all textures? This cannot be undone!");
            if (result != 1)
                return;

            var children = this.Children.ToList();
            foreach (CTEX<H3DTexture> tex in children)
            {
                SectionList.Remove((T)tex.Section);
                this.Children.Remove(tex);

                OnTextureDeleted(tex);
            }
        }

        public void OnTextureDeleted(CTEX<H3DTexture> tex)
        {
            //Update cache
            if (H3DRender.TextureCache.ContainsKey(tex.Texture.Name))
                H3DRender.TextureCache.Remove(tex.Texture.Name);
            //Remove from render cache
            if (SPICA.Rendering.Renderer.TextureCache.ContainsKey(tex.Texture.Name))
                SPICA.Rendering.Renderer.TextureCache.Remove(tex.Texture.Name);
            //Remove from render
            if (H3DRender.Renderer.Textures.ContainsKey(tex.Texture.Name))
                H3DRender.Renderer.Textures.Remove(tex.Texture.Name);
            //Remove from UI
            if (IconManager.HasIcon(tex.Icon))
                IconManager.RemoveTextureIcon(tex.Icon);
            //Update viewer
            GLContext.ActiveContext.UpdateViewport = true;
        }

        public void ImportTexture(string filePath) => ImportTexture(new string[] { filePath });

        public void ImportTexture(string[] filePaths)
        {
            var dlg = new H3DTextureDialog();
            foreach (var filePath in filePaths)
            {
                if (filePath.ToLower().EndsWith(".bctex"))
                {
                    H3DTexture texture = new H3DTexture();
                    texture.Replace(filePath);
                    texture.Name = Path.GetFileNameWithoutExtension(filePath);
                    AddChild(new CTEX<T>(SectionList, texture));
                }
                else
                    dlg.AddTexture(filePath);
            }

            if (dlg.Textures.Count == 0)
                return;

            DialogHandler.Show(dlg.Name, 850, 350, dlg.Render, (o) =>
            {
                if (o != true)
                    return;

                ProcessLoading.Instance.IsLoading = true;
                foreach (var tex in dlg.Textures)
                {
                    var surfaces = tex.Surfaces;
                    AddTexture(tex.FilePath, tex);
                }
                ProcessLoading.Instance.IsLoading = false;
            });
        }

        public void ImportTextureDirect(string filePath)
        {
            var dlg = new H3DTextureDialog();
            dlg.AddTexture(filePath);

            dlg.Textures[0].EncodeTexture(0);

            var surfaces = dlg.Textures[0].Surfaces;
            AddTexture(dlg.Textures[0].FilePath, dlg.Textures[0]);
        }
        private void AddTexture(string filePath, H3DImportedTexture tex)
        {
            //Check for duped 
            var duped = this.Children.FirstOrDefault(x => x.Header == tex.Name);
            if (duped != null)
                ((CTEX<T>)duped).ImportTexture(tex);
            else
            {
                CTEX<T> ctex = new CTEX<T>(SectionList, new H3DTexture());
                AddChild(ctex);
                ctex.ImportTexture(tex);
            }
        }
    }

    public class CTEX<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
    {
        /// <summary>
        /// The model instance of the h3d texture.
        /// </summary>
        public H3DTexture Texture { get; set; }

        public enum EditMode
        {
            Default,
            ColorOnly,
            AlphaOnly,
        }

        public override string DefaultExtension => ".png";

        public override string[] ExportFilters => new string[] { ".png", ".bctex", ".jpeg",".jpg",".bmp",".gif",".tga", ".tif", ".tiff", };
        public override string[] ReplaceFilters => new string[] { ".png", ".bctex", ".jpeg", ".jpg", ".bmp", ".gif", ".tga", ".tif", ".tiff", };

        public override MenuItemModel[] ExtraMenuItems
        {
            get
            {
                var exportMenu = new MenuItemModel("Export Special");
                exportMenu.MenuItems.Add(new MenuItemModel("Color Only", () => ExportTextureDialog(EditMode.ColorOnly)));
                exportMenu.MenuItems.Add(new MenuItemModel("Alpha Only", () => ExportTextureDialog(EditMode.AlphaOnly)));

                var replaceMenu = new MenuItemModel("Replace Special");
                replaceMenu.MenuItems.Add(new MenuItemModel("Color Only", () => ReplaceTextureDialog(EditMode.ColorOnly)));
                replaceMenu.MenuItems.Add(new MenuItemModel("Alpha Only", () => ReplaceTextureDialog(EditMode.AlphaOnly)));

                return new MenuItemModel[2] { exportMenu, replaceMenu };
            }
        }

        public CTEX(H3DDict<T> subSections, object section) : base(subSections, section)
        {
            Icon = MapStudio.UI.IconManager.IMAGE_ICON.ToString();
            CanRename = true;

            Texture = (H3DTexture)section;
            Header = Texture.Name;
            Tag = new EditableTexture(this, Texture, ReloadTexture);
            Icon = $"{Texture.Name}";
            if (IconManager.HasIcon(Icon))
                IconManager.RemoveTextureIcon(Icon);

            IconManager.AddTexture(Icon, (EditableTexture)Tag, 128, 128);
        }

        public override void Replace(string filePath)
        {
            ReplaceTexture(filePath);
        }

        public override void Export(string filePath)
        {
            this.ExportTexture(filePath);
        }

        private void ReplaceTextureDialog(EditMode editMode)
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = false;
            dlg.AddFilter(".bctex", "Raw Texture (H3D)");

            foreach (var ext in TextureDialog.SupportedExtensions)
                dlg.AddFilter(ext, ext);

            if (dlg.ShowDialog())
                ReplaceTexture(dlg.FilePath, editMode);
        }

        public void ReplaceTexture(string filePath, EditMode editMode = EditMode.Default)
        {
            if (filePath.ToLower().EndsWith(".bctex"))
            {
                this.Texture.Replace(filePath);
                this.Texture.Name = this.Header;
                ReloadImported();
                return;
            }

            var dlg = new H3DTextureDialog();
            var tex = dlg.AddTexture(filePath);
            tex.Format = Texture.Format;

            if (editMode != EditMode.Default)
            {
                if (tex.Width != Texture.Width && tex.Height != Texture.Height)
                {
                    TinyFileDialog.MessageBoxErrorOk($"Image must have the same width/height during RGB/Alpha replace.");
                    return;
                }
            }

            DialogHandler.Show(dlg.Name, 850, 350, dlg.Render, (o) =>
            {
                if (o != true)
                    return;

                dlg.Textures[0].Name = this.Header;
                ImportTexture(dlg.Textures[0], editMode);
            });
        }

        public void ImportTexture(H3DImportedTexture tex, EditMode editMode = EditMode.Default)
        {
            if (editMode != EditMode.Default)
            {
                //Keep original alpha channel intact by encoding the data back with original alpha channel
                for (int i = 0; i < tex.Surfaces.Count; i++)
                {
                    if (editMode == EditMode.ColorOnly)
                        tex.Surfaces[i].EncodedData = EncodeWithOriginalAlpha(tex.DecodeTexture(i), tex.Format, (int)tex.MipCount);
                    else if (editMode == EditMode.AlphaOnly)
                        tex.Surfaces[i].EncodedData = EncodeWithOriginalColor(tex.DecodeTexture(i), tex.Format, (int)tex.MipCount);
                }
            }

            Texture = new H3DTexture();
            Texture.Name = tex.Name;
            if (tex.Surfaces.Count == 6)
            {
                Texture.RawBufferXNeg = tex.Surfaces[0].EncodedData;
                Texture.RawBufferYNeg = tex.Surfaces[1].EncodedData;
                Texture.RawBufferZNeg = tex.Surfaces[2].EncodedData;
                Texture.RawBufferXPos = tex.Surfaces[3].EncodedData;
                Texture.RawBufferYPos = tex.Surfaces[4].EncodedData;
                Texture.RawBufferZPos = tex.Surfaces[5].EncodedData;
            }
            else
            {
                Texture.RawBuffer = tex.Surfaces[0].EncodedData;
            }
            Texture.Width = tex.Width;
            Texture.Height = tex.Height;
            Texture.MipmapSize = (byte)tex.MipCount;
            Texture.Format = tex.Format;
            ReloadImported();
        }

        private void ReloadImported()
        {
            Tag = new EditableTexture(this, Texture, ReloadTexture);
            //Update texture render used for icons
            ((EditableTexture)Tag).LoadRenderableTexture();

            var folder = ((TextureFolder<H3DTexture>)this.Parent);
            var renderer = folder.H3DRender.Renderer;

            //Reload renderer
            if (H3DRender.TextureCache.ContainsKey(Texture.Name))
                H3DRender.TextureCache.Remove(Texture.Name);

            if (renderer.Textures.ContainsKey(Texture.Name))
                renderer.Textures.Remove(Texture.Name);
            renderer.Textures.Add(Texture.Name, new SPICA.Rendering.Texture(Texture));
            H3DRender.TextureCache.Add(Texture.Name, Texture);

            if (IconManager.HasIcon(Icon))
                IconManager.RemoveTextureIcon(Icon);

            IconManager.AddTexture(Icon, (EditableTexture)Tag, 128, 128);

            this.Header = Texture.Name;
            Icon = $"{Texture.Name}";

            //Update viewer
            GLContext.ActiveContext.UpdateViewport = true;
        }

        public void ReloadTexture()
        {
            var folder = ((TextureFolder<H3DTexture>)this.Parent);
            var renderer = folder.H3DRender.Renderer;

            //Reload renderer
            if (H3DRender.TextureCache.ContainsKey(Texture.Name))
                H3DRender.TextureCache.Remove(Texture.Name);

            if (renderer.Textures.ContainsKey(Texture.Name))
                renderer.Textures.Remove(Texture.Name);
            renderer.Textures.Add(Texture.Name, new SPICA.Rendering.Texture(Texture));
            H3DRender.TextureCache.Add(Texture.Name, Texture);
        }

        //Replace only RGB color, not alpha
        private byte[] EncodeWithOriginalAlpha(byte[] rgba, PICATextureFormat format, int mipCount)
        {
            //Get original rgba 
            var originalRgba = TextureConverter.FlipData(Texture.ToRGBA(0), Texture.Width, Texture.Height);


            int index = 0;
            for (int w = 0; w < Texture.Width; w++)
            {
                for (int h = 0; h < Texture.Height; h++)
                {
                    //Set alpha to original
                    rgba[index + 3] = originalRgba[index + 3];
                    index += 4;
                }
            }

            //Prepare imagesharp img used for encoding and mip generating
            Image<Rgba32> Img = Image.LoadPixelData<Rgba32>(rgba, Texture.Width, Texture.Height);

            //Re encode with updated alpha
            byte[] output = new byte[0];

            bool isETC1 = format == PICATextureFormat.ETC1 || format == PICATextureFormat.ETC1A4;

            if (isETC1 && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && ETC1Compressor.UseEncoder)
                output = ETC1Compressor.Encode(Img, (int)mipCount, format == PICATextureFormat.ETC1A4);
            else
                output = TextureConverter.Encode(Img, format, mipCount);

            Img.Dispose();

            return output;
        }

        //Replace only alpha, not RGB color
        private byte[] EncodeWithOriginalColor(byte[] rgba, PICATextureFormat format, int mipCount)
        {
            //Get original rgba 
            var originalRgba = TextureConverter.FlipData(Texture.ToRGBA(0), Texture.Width, Texture.Height);

            int index = 0;
            for (int w = 0; w < Texture.Width; w++)
            {
                for (int h = 0; h < Texture.Height; h++)
                {
                    //Set alpha directly from red
                    rgba[index + 3] = rgba[index + 0];
                    //Set color to original
                    rgba[index + 0] = originalRgba[index + 0];
                    rgba[index + 1] = originalRgba[index + 1];
                    rgba[index + 2] = originalRgba[index + 2];
                    index += 4;
                }
            }

            //Prepare imagesharp img used for encoding and mip generating
            Image<Rgba32> Img = Image.LoadPixelData<Rgba32>(rgba, Texture.Width, Texture.Height);

            //Re encode with updated alpha
            byte[] output = new byte[0];

            bool isETC1 = format == PICATextureFormat.ETC1 || format == PICATextureFormat.ETC1A4;

            if (isETC1 && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && ETC1Compressor.UseEncoder)
                output = ETC1Compressor.Encode(Img, (int)mipCount, format == PICATextureFormat.ETC1A4);
            else
                output = TextureConverter.Encode(Img, format, mipCount);

            Img.Dispose();

            return output;
        }

        public override bool Delete()
        {
            var selected = this.Parent.Children.Where(x => x.IsSelected).ToList();

            if (base.Delete())
            {
                var folder = ((TextureFolder<T>)this.Parent);
                foreach (CTEX<H3DTexture> tex in selected)
                    folder.OnTextureDeleted(tex);

                return true;
            }
            return false;
        }

        public void RemoveBatch()
        {
            var selected = this.Parent.Children.Where(x => x.IsSelected).ToList();

            string msg = $"Are you sure you want to delete the ({selected.Count}) selected textures? This cannot be undone!";
            if (selected.Count == 1)
                msg = $"Are you sure you want to delete {Header}? This cannot be undone!";

            int result = TinyFileDialog.MessageBoxInfoYesNo(msg);
            if (result != 1)
                return;

            var folder = ((TextureFolder<H3DTexture>)this.Parent);

            foreach (CTEX<H3DTexture> tex in selected)
                folder.OnTextureDeleted(tex);
        }

        public override void ReloadName()
        {
            string name = this.Header;

            //Remove current name uses
            var folder = ((TextureFolder<H3DTexture>)this.Parent);
            var renderer = folder.H3DRender.Renderer;

            if (renderer.Textures.ContainsKey(this.Texture.Name))
                renderer.Textures.Remove(this.Texture.Name);
            if (IconManager.HasIcon(Icon))
                IconManager.RemoveTextureIcon(Icon);

            //Set bcres and h3d render instances
            this.Texture.Name = name;

            ((EditableTexture)this.Tag).Name = name;

            //Reload renderer
            renderer.Textures.Add(Texture.Name, new SPICA.Rendering.Texture(Texture));

            //Reload UI
            Icon = $"{Texture.Name}";
        }

        public void ExportTexture(string filePath, EditMode channel = EditMode.Default)
        {
            if (filePath.ToLower().EndsWith(".bctex"))
                this.Texture.Export(filePath);
            else
            {
                if (channel != EditMode.Default)
                {
                    if (channel == EditMode.ColorOnly)
                        ExportColorOnly(filePath);
                    else
                        ExportAlphaOnly(filePath);
                }
                else
                {
                    var tex = this.Tag as STGenericTexture;
                    tex.Export(filePath, new TextureExportSettings());
                }
            }
        }

        private void ExportColorOnly(string filePath)
        {
            //Get original rgba 
            var rgba = TextureConverter.FlipData(Texture.ToRGBA(0), Texture.Width, Texture.Height);

            int index = 0;
            for (int w = 0; w < Texture.Width; w++)
            {
                for (int h = 0; h < Texture.Height; h++)
                {
                    //Set alpha to 255
                    rgba[index + 3] =  255;
                    index += 4;
                }
            }

            Image<Rgba32> Img = Image.LoadPixelData<Rgba32>(rgba, Texture.Width, Texture.Height);
            Img.Save(filePath);
            Img.Dispose();
        }

        private void ExportAlphaOnly(string filePath)
        {
            //Get original rgba 
            var rgba = TextureConverter.FlipData(Texture.ToRGBA(0), Texture.Width, Texture.Height);

            int index = 0;
            for (int w = 0; w < Texture.Width; w++)
            {
                for (int h = 0; h < Texture.Height; h++)
                {
                    //Set alpha to 255
                    rgba[index + 0] = rgba[index + 3];
                    rgba[index + 1] = rgba[index + 3];
                    rgba[index + 2] = rgba[index + 3];
                    rgba[index + 3] = 255;
                    index += 4;
                }
            }

            Image<Rgba32> Img = Image.LoadPixelData<Rgba32>(rgba, Texture.Width, Texture.Height);
            Img.Save(filePath);
            Img.Dispose();
        }

        private void ExportTextureDialog(EditMode channel = EditMode.Default)
        {
            //Multi select export
            var selected = this.Parent.Children.Where(x => x.IsSelected).ToList();
            if (selected.Count > 1)
            {
                ImguiFolderDialog dlg = new ImguiFolderDialog();
                //Todo configurable formats for folder dialog
                if (dlg.ShowDialog())
                {
                    foreach (var sel in selected)
                        ExportTexture(Path.Combine(dlg.SelectedPath, $"{sel.Header}.png"), channel);
                }
            }
            else
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = true;
                dlg.FileName = $"{this.Header}.png";
                dlg.AddFilter(".bctex", "Raw Texture (H3D)");

                foreach (var ext in TextureDialog.SupportedExtensions)
                    dlg.AddFilter(ext, ext);

                if (dlg.ShowDialog())
                    ExportTexture(dlg.FilePath, channel);
            }
        }
    }

    class EditableTexture : STGenericTexture
    {
        private H3DTexture H3DTexture;
        private Action UpdateRender;

        public EditableTexture()
        {

        }

        public EditableTexture(NodeBase node, H3DTexture texRender, Action updateRender)
        {
            UpdateRender = updateRender;
            H3DTexture = texRender;
            var format = (CTR_3DS.PICASurfaceFormat)texRender.Format;
            Width = (uint)texRender.Width;
            Height = (uint)texRender.Height;
            MipCount = 1;
            this.Parameters.DontSwapRG = true;
            this.Platform = new Toolbox.Core.Imaging.CTRSwizzle(format)
            {
            };
            this.DisplayProperties = new Properties(texRender, texRender.Format,
                texRender.Width, texRender.Height, texRender.MipmapSize);
            this.DisplayPropertiesChanged += delegate
            {
                node.Header = H3DTexture.Name;
            };
        }

        class Properties
        {
            public string Name
            {
                get { return Texture.Name; }
                set { Texture.Name = value; }
            }

            public PICATextureFormat Format { get; private set; }
            public int MipCount { get; private set; }
            public int Width { get; private set; }
            public int Height { get; private set; }

            private H3DTexture Texture;

            public Properties(H3DTexture tex, PICATextureFormat format, int width, int height, byte mipCount)
            {
                Texture = tex;
                Format = format;
                MipCount = mipCount;
                Width = width;
                Height = height;
            }
        }

        public override byte[] GetImageData(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0)
        {
            return H3DTexture.RawBuffer;
        }

        public override void SetImageData(List<byte[]> imageData, uint width, uint height, int arrayLevel = 0)
        {
            //Prepare imagesharp img used for encoding and mip generating
            Image<Rgba32> Img = Image.LoadPixelData<Rgba32>(imageData[0], (int)width, (int)height);

            //Re encode with updated alpha
            bool isETC1 = H3DTexture.Format == PICATextureFormat.ETC1 || H3DTexture.Format == PICATextureFormat.ETC1A4;

            if (isETC1 && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                H3DTexture.RawBuffer = ETC1Compressor.Encode(Img, (int)MipCount, H3DTexture.Format == PICATextureFormat.ETC1A4);
            else
                H3DTexture.RawBuffer = TextureConverter.Encode(Img, H3DTexture.Format, (int)this.MipCount);

            Img.Dispose();

            LoadRenderableTexture();

            UpdateRender?.Invoke();
        }
/*
        public override void OnRenderUpdated()
        {
            UpdateRender?.Invoke();
        }*/
    }
}
