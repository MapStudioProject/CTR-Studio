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

namespace CtrLibrary.Bch
{
    public class TextureFolder : NodeBase
    {
        public override string Header => "Textures";

        H3DRender H3DRender;

        public H3DDict<H3DTexture> GetTextures()
        {
            H3DDict<H3DTexture> textures = new H3DDict<H3DTexture>();
            foreach (CTEX tex in this.Children)
                textures.Add(tex.Texture);

            return textures;
        }

        public TextureFolder(H3DRender render, List<H3DTexture> textures)
        {
            H3DRender = render;

            ContextMenus.Add(new MenuItemModel("Import", ImportTextures));
            ContextMenus.Add(new MenuItemModel("Export All", ExportTextures));
            ContextMenus.Add(new MenuItemModel(""));
            ContextMenus.Add(new MenuItemModel("Clear", Clear));

            for (int i = 0; i < textures.Count; i++)
                AddChild(new CTEX(render, textures[i]));
        }

        private void ExportTextures()
        {
            ImguiFolderDialog dlg = new ImguiFolderDialog();
            if (dlg.ShowDialog())
            {
                foreach (CTEX tex in this.Children)
                    tex.ExportTexture(Path.Combine(dlg.SelectedPath, $"{tex.Header}.png"));
            }
        }

        public void RemoveTexture(CTEX tex)
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
            this.Children.Remove(tex);

            //Update viewer
            GLContext.ActiveContext.UpdateViewport = true;
        }

        private void Clear()
        {
            int result = TinyFileDialog.MessageBoxInfoYesNo("Are you sure you want to clear all textures? This cannot be undone!");
            if (result != 1)
                return;

            var children = this.Children.ToList();
            foreach (CTEX tex in children)
                RemoveTexture(tex);
        }

        private void ImportTextures()
        {
            //Dialog for importing textures. 
            ImguiFileDialog fileDialog = new ImguiFileDialog();
            fileDialog.MultiSelect = true;
            foreach (var ext in TextureDialog.SupportedExtensions)
                fileDialog.AddFilter(ext, ext);

            if (fileDialog.ShowDialog())
                ImportTexture(fileDialog.FilePaths);
        }

        public void ImportTexture(string filePath) => ImportTexture(new string[] { filePath });

        public void ImportTexture(string[] filePaths)
        {
            var dlg = new H3DTextureDialog();
            foreach (var filePath in filePaths)
                dlg.AddTexture(filePath);

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
                ((CTEX)duped).ImportTexture(tex);
            else
            {
                CTEX ctex = new CTEX(H3DRender);
                ctex.ImportTexture(tex);
                AddChild(ctex);
            }
        }
    }

    public class CTEX : NodeBase
    {
        private H3DRender H3DRender;

        /// <summary>
        /// The model instance of the h3d texture.
        /// </summary>
        public H3DTexture Texture { get; set; }

        public CTEX(H3DRender render)
        {
            H3DRender = render;
            Icon = MapStudio.UI.IconManager.IMAGE_ICON.ToString();
            CanRename = true;

            ContextMenus.Add(new MenuItemModel("Export", ExportTextureDialog));
            ContextMenus.Add(new MenuItemModel("Replace", ReplaceTextureDialog));
            ContextMenus.Add(new MenuItemModel(""));
            ContextMenus.Add(new MenuItemModel("Rename", () => { ActivateRename = true; }));
            ContextMenus.Add(new MenuItemModel(""));
            ContextMenus.Add(new MenuItemModel("Delete", RemoveBatch));
        }

        public CTEX(H3DRender render, H3DTexture texture) : this(render)
        {
            Texture = texture;
            Header = texture.Name;
            Tag = new EditableTexture(this, Texture);
            Icon = $"{texture.Name}";
            if (IconManager.HasIcon(Icon))
                IconManager.RemoveTextureIcon(Icon);

            IconManager.AddTexture(Icon, (EditableTexture)Tag, 128, 128);
            OnHeaderRenamed += delegate
            {
                Rename(this.Header);
            };
        }

        public void OnSave()
        {

        }

        private void ReplaceTextureDialog()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = false;
            dlg.AddFilter(".bctex", "Raw Texture (H3D)");

            foreach (var ext in TextureDialog.SupportedExtensions)
                dlg.AddFilter(ext, ext);

            if (dlg.ShowDialog())
                ReplaceTexture(dlg.FilePath);
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

            Tag = new EditableTexture(this, Texture);
            //Update texture render used for icons
            ((EditableTexture)Tag).LoadRenderableTexture();

            //Reload renderer
            if (H3DRender.TextureCache.ContainsKey(Texture.Name))
                H3DRender.TextureCache.Remove(Texture.Name);

            if (H3DRender.Renderer.Textures.ContainsKey(Texture.Name))
                H3DRender.Renderer.Textures.Remove(Texture.Name);
            H3DRender.Renderer.Textures.Add(Texture.Name, new SPICA.Rendering.Texture(Texture));
            H3DRender.TextureCache.Add(Texture.Name, Texture);

            if (IconManager.HasIcon(Icon))
                IconManager.RemoveTextureIcon(Icon);

            IconManager.AddTexture(Icon, (EditableTexture)Tag, 128, 128);

            this.Header = Texture.Name;
            Icon = $"{Texture.Name}";

            //Update viewer
            GLContext.ActiveContext.UpdateViewport = true;
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

            var folder = ((TextureFolder)this.Parent);

            foreach (CTEX tex in selected)
                folder.RemoveTexture(tex);
        }

        public void Rename(string name)
        {
            //Remove current name uses
            if (H3DRender.Renderer.Textures.ContainsKey(this.Texture.Name))
                H3DRender.Renderer.Textures.Remove(this.Texture.Name);
            if (IconManager.HasIcon(Icon))
                IconManager.RemoveTextureIcon(Icon);

            //Set bcres and h3d render instances
            this.Texture.Name = name;

            //Reload renderer
            H3DRender.Renderer.Textures.Add(Texture.Name, new SPICA.Rendering.Texture(Texture));

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

        private void ExportTextureDialog()
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
                        ExportTexture(Path.Combine(dlg.SelectedPath, $"{sel.Header}.png"));
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
                    ExportTexture(dlg.FilePath);
            }
        }
    }

    class EditableTexture : STGenericTexture
    {
        private H3DTexture H3DTexture;

        public EditableTexture()
        {

        }

        public EditableTexture(CTEX node, H3DTexture texRender)
        {
            H3DTexture = texRender;
            var format = (CTR_3DS.PICASurfaceFormat)texRender.Format;
            Width = (uint)texRender.Width;
            Height = (uint)texRender.Height;
            MipCount = 1;
            this.Parameters.DontSwapRG = true;
            this.Platform = new Toolbox.Core.Imaging.CTRSwizzle(format)
            {
            };
            this.DisplayProperties = new Properties(texRender.Name, texRender.Format,
                texRender.Width, texRender.Height, texRender.MipmapSize);
            this.DisplayPropertiesChanged += delegate
            {
                node.Header = H3DTexture.Name;
            };
        }

        class Properties
        {
            public string Name { get; set; }

            public PICATextureFormat Format { get; private set; }
            public int MipCount { get; private set; }
            public int Width { get; private set; }
            public int Height { get; private set; }

            public Properties(string name, PICATextureFormat format, int width, int height, byte mipCount)
            {
                Name = name;
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
            throw new NotImplementedException();
        }
    }
}
