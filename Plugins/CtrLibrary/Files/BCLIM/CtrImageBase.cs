using CtrLibrary.Bch;
using CtrLibrary.Rendering;
using CtrLibrary.UI;
using GLFrameworkEngine;
using MapStudio.UI;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Formats.CtrH3D;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Toolbox.Core.ViewModels;
using Toolbox.Core;

namespace CtrLibrary
{
    /// <summary>
    /// A wrapper for CTR images to edit in GUI form using H3DTexture as a 3DS texture base.
    /// </summary>
    public class CtrImageBase : NodeBase
    {
        /// <summary>
        /// The model instance of the h3d texture.
        /// </summary>
        public H3DTexture Texture { get; set; }

        //pica transform mode for texture, ie flipping
        public CTR_3DS.Orientation Orientation = CTR_3DS.Orientation.Default;

        public virtual string DefaultExtension => ".png";

        public virtual string[] ExportFilters => new string[] { ".png", ".bctex", ".jpeg", ".jpg", ".bmp", ".gif", ".tga", ".tif", ".tiff", };
        public virtual string[] ReplaceFilters => new string[] { ".png", ".bctex", ".jpeg", ".jpg", ".bmp", ".gif", ".tga", ".tif", ".tiff", };

        public CtrImageBase(H3DTexture texture, CTR_3DS.Orientation orientation = CTR_3DS.Orientation.Default)
        {
            Icon = MapStudio.UI.IconManager.IMAGE_ICON.ToString();
            CanRename = true;
            Orientation = orientation;

            Texture = texture;
            Header = Texture.Name;
            Tag = new EditableTexture(this, Texture, Orientation, ReloadTexture);
            Icon = $"{Texture.Name}";
            if (IconManager.HasIcon(Icon))
                IconManager.RemoveTextureIcon(Icon);

            IconManager.AddTexture(Icon, (EditableTexture)Tag, 128, 128);

            this.ContextMenus.Add(new MenuItemModel("Export", ExportDialog));
            this.ContextMenus.Add(new MenuItemModel("Replace", ReplaceDialog));
        }

        void ReplaceDialog()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = false;
            dlg.FileName = $"{Header}{DefaultExtension}";

            foreach (var ext in ReplaceFilters)
                dlg.AddFilter(ext, ext);

            if (dlg.ShowDialog())
            {
                Replace(dlg.FilePath);
            }
        }

        void ExportDialog()
        {
            var selected = this.Parent.Children.Where(x => x.IsSelected).ToList();
            //Batch export
            if (selected.Count > 1)
            {
                ImguiFolderDialog dlg = new ImguiFolderDialog();
                if (dlg.ShowDialog())
                {
                    foreach (CtrImageBase node in selected)
                    {
                        node.Export(Path.Combine(dlg.SelectedPath, $"{node.Header}{DefaultExtension}"));
                    }
                }
            }
            else //Normal export
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = true;
                dlg.FileName = $"{Header}{DefaultExtension}";
                foreach (var ext in ExportFilters)
                    dlg.AddFilter(ext, ext);

                if (dlg.ShowDialog())
                {
                    Export(dlg.FilePath);
                }
            }
        }

        public virtual void Replace(string filePath)
        {
            ReplaceTexture(filePath);
        }

        public virtual void Export(string filePath)
        {        
            this.ExportTexture(filePath);
        }

        public void ReplaceTexture(string filePath)
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
            if (this.Texture.MipmapSize == 1)
                tex.MipCount = 1;

            DialogHandler.Show(dlg.Name, 850, 350, dlg.Render, (o) =>
            {
                if (o != true)
                    return;

                dlg.Textures[0].Name = this.Header;
                ImportTexture(dlg.Textures[0]);
            });
        }

        public void ImportTexture(H3DImportedTexture tex)
        {
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
            Tag = new EditableTexture(this, Texture, Orientation, ReloadTexture);
            //Update texture render used for icons
            ((EditableTexture)Tag).LoadRenderableTexture();

            //Reload icon
            if (IconManager.HasIcon(Icon))
                IconManager.RemoveTextureIcon(Icon);

            //Update tree name
            this.Header = Texture.Name;
            Icon = $"{Texture.Name}";

            IconManager.AddTexture(Icon, (EditableTexture)Tag, 128, 128);

            //Update viewer
            GLContext.ActiveContext.UpdateViewport = true;
        }

        public void ReloadTexture()
        {

        }

        public virtual void ReloadName()
        {
            string name = this.Header;

            //Set bcres and h3d render instances
            this.Texture.Name = name;

            //Reload UI
            Icon = $"{Texture.Name}";
        }

        public void ExportTexture(string filePath)
        {
            if (filePath.ToLower().EndsWith(".bctex"))
                this.Texture.Export(filePath);
            else
            {
                var tex = this.Tag as STGenericTexture;
                tex.Export(filePath, new TextureExportSettings());
            }
        }
    }

    //A texture instance for editing in UI and rendering
    class EditableTexture : STGenericTexture
    {
        private H3DTexture H3DTexture;
        private Action UpdateRender;

        public EditableTexture()
        {

        }

        public EditableTexture(NodeBase node, H3DTexture texRender, 
            CTR_3DS.Orientation swizzleMode, Action updateRender)
        {
            Name = node.Header;
            UpdateRender = updateRender;
            H3DTexture = texRender;
            var format = (CTR_3DS.PICASurfaceFormat)texRender.Format;
            Width = (uint)texRender.Width;
            Height = (uint)texRender.Height;
            MipCount = texRender.MipmapSize;
            this.Parameters.DontSwapRG = true;
            this.Platform = new Toolbox.Core.Imaging.CTRSwizzle(format)
            {
                SwizzleMode = swizzleMode,
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
    }
}
