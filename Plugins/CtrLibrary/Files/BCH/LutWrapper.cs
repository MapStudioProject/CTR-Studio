using CtrLibrary.Rendering;
using MapStudio.UI;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.LUT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using Toolbox.Core;
using static CtrLibrary.Bch.BCH;
using SPICA.Rendering;
using CtrLibrary.UI;
using SPICA.Formats.Common;

namespace CtrLibrary.Bch
{
    internal class LutWrapper
    {

    }

    internal class LUTFolder<T> : BCH.H3DGroupNode<T> where T : SPICA.Formats.Common.INamed
    {
        public override string Header => "Look Ups";

        internal H3DRender H3DRender;
        H3D H3DFile;

        public override Type ChildNodeType => typeof(LUTWrapper<T>);

        public LUTFolder(H3DRender render, H3DDict<T> subSections) : base(BCH.H3DGroupType.Lookups, subSections)
        {
            H3DRender = render;

            foreach (var lut in subSections)
                AddChild(new LUTWrapper<T>(subSections, lut));
        }

        public H3DDict<T> GetLuts()
        {
            H3DDict<T> luts = new H3DDict<T>();
            foreach (LUTWrapper<T> lut in this.Children)
                luts.Add((T)lut.Section);

            return luts;
        }
    }

    internal class LUTWrapper<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
    {
        internal H3DLUT LUT => (H3DLUT)this.Section;
        H3DRender H3DRender => ((LUTFolder<T>)Parent).H3DRender;

        public LUTWrapper(H3DDict<T> subSections, object section) : base(subSections, section)
        {
            Icon = '\uf0ce'.ToString();

            foreach (var sampler in LUT.Samplers)
                AddChild(new LUTSamplerWrapper(sampler));
        }

        public override void Replace(string filePath)
        {
            base.Replace(filePath);

            this.Children.Clear();
            foreach (var sampler in LUT.Samplers)
                AddChild(new LUTSamplerWrapper(sampler));
        }

        public override MenuItemModel[] ExtraMenuItems => new MenuItemModel[2]
        {
            new MenuItemModel("Create Sampler", CreateSampler),
            new MenuItemModel("Import Sampler", ImportSampler),
        };

        public override bool Delete()
        {
            if (base.Delete())
            {
                var selected = Parent.Children.Where(x => x.IsSelected);
                foreach (LUTWrapper<T> lut in selected)
                    lut.RemoveLUT();
                return true;
            }
            return false;
        }

        internal void RemoveSampler(LUTSamplerWrapper wrapper)
        {
            this.Children.Remove(wrapper);
            LUT.Samplers.Remove(wrapper.Sampler);
            ReloadRender();
        }

        private void CreateSampler()
        {
            var samp = new H3DLUTSampler()
            {
                Name = "NewSampler",
            };
            samp.Name = Utils.RenameDuplicateString(samp.Name, this.Children.Select(x => x.Header).ToList());

            LUT.Samplers.Add(samp);
            AddChild(new LUTSamplerWrapper(samp));
            ReloadRender();
        }

        private void ImportSampler()
        {
            var wrapper = LUTSamplerWrapper.Import();
            if (wrapper == null) //cancelled
                return;

            wrapper.Sampler.Name = Utils.RenameDuplicateString(wrapper.Sampler.Name, this.Children.Select(x => x.Header).ToList());
            wrapper.Header = wrapper.Sampler.Name;

            AddChild(wrapper);
            LUT.Samplers.Add(wrapper.Sampler);

            ReloadRender();
        }

        internal void ReloadRender()
        {
            //Remove from cache
            if (SPICA.Rendering.Renderer.LUTCache.ContainsKey(this.Header))
                SPICA.Rendering.Renderer.LUTCache.Remove(this.Header);
            //Remove from current render
            if (H3DRender.Renderer.LUTs.ContainsKey(this.Header))
                H3DRender.Renderer.LUTs.Remove(this.Header);
            H3DRender.Renderer.LUTs.Add(this.Header, new SPICA.Rendering.LUT(LUT));
        }

        internal void RemoveLUT()
        {
            //Remove from cache
            if (SPICA.Rendering.Renderer.LUTCache.ContainsKey(Header))
                SPICA.Rendering.Renderer.LUTCache.Remove(Header);
            if (LUTCacheManager.Cache.ContainsKey(Header))
                LUTCacheManager.Cache.Remove(Header);
            //Remove from current render
            if (H3DRender.Renderer.LUTs.ContainsKey(Header))
                H3DRender.Renderer.LUTs.Remove(Header);
        }

        public override void ReloadName()
        {
            string originalName = ((INamed)Section).Name;

            if (LUTCacheManager.Cache.ContainsKey(originalName))
            {
                LUTCacheManager.Cache.Remove(originalName);
                LUTCacheManager.Cache.Add(Header, LUT);
            }

            base.ReloadName();

        }

        internal class LUTSamplerWrapper : NodeBase, IPropertyUI
        {
            internal H3DLUTSampler Sampler;

            public LUTSamplerWrapper(H3DLUTSampler sampler)
            {
                Icon = '\uf55b'.ToString();
                Sampler = sampler;
                Header = sampler.Name;
                Tag = sampler;
                CanRename = true;
                OnHeaderRenamed += delegate
                {
                    Sampler.Name = this.Header;
                };

                this.ContextMenus.Add(new MenuItemModel("Export", Export));
                this.ContextMenus.Add(new MenuItemModel("Replace", () => { Replace(); }));
                this.ContextMenus.Add(new MenuItemModel(""));
                this.ContextMenus.Add(new MenuItemModel("Rename", () => { ActivateRename = true; }));
                this.ContextMenus.Add(new MenuItemModel(""));
                this.ContextMenus.Add(new MenuItemModel("Delete", RemoveBatch));
            }

            public void RemoveBatch()
            {
                var selected = this.Parent.Children.Where(x => x.IsSelected).ToList();

                string msg = $"Are you sure you want to delete the ({selected.Count}) selected nodes? This cannot be undone!";
                if (selected.Count == 1)
                    msg = $"Are you sure you want to delete {Header}? This cannot be undone!";

                int result = TinyFileDialog.MessageBoxInfoYesNo(msg);
                if (result != 1)
                    return;

                var folder = (LUTWrapper<T>)this.Parent;

                foreach (LUTSamplerWrapper lut in selected)
                    folder.RemoveSampler(lut);
            }

            public Type GetTypeUI() => typeof(LUTViewer);

            public void OnLoadUI(object uiInstance)
            {
            }

            public void OnRenderUI(object uiInstance)
            {
                ((LUTViewer)uiInstance).Render(Sampler);
            }

            public static LUTSamplerWrapper Import()
            {
                LUTSamplerWrapper wrapper = new LUTSamplerWrapper(new H3DLUTSampler()
                {
                    Name = "NewSampler",
                });
                if (wrapper.Replace())
                    return wrapper;

                return null;
            }

            bool Replace()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = false;
                dlg.FileName = $"{Header}.png";
                dlg.AddFilter(".png", ".png");
                if (dlg.ShowDialog())
                {
                    var image = Image.Load<Rgba32>(dlg.FilePath);
                    if (image.Width != 512)
                        throw new Exception($"Invalid image width for LUT! Expected 512 but got {image.Width}!");

                    //Get rgba data
                    var rgba = image.GetSourceInBytes();
                    //Turn the rgba data into LUT
                    Sampler.Table = RemapTable(FromRGBA(rgba, image.Height));
                    ReloadRender();

                    return true;
                }
                return false;
            }

            void Export()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = true;
                dlg.FileName = $"{Header}.png";
                dlg.AddFilter(".png", ".png");
                if (dlg.ShowDialog())
                {
                    //From png. Convert rgba then create image
                    var data = GetTable();
                    var rgba = ToRGBA(data, 128);
                    var image = Image.LoadPixelData<Rgba32>(rgba, 512, 128);
                    image.Save(dlg.FilePath);
                }
            }

            private void ReloadRender()
            {
                if (Parent != null)
                    ((LUTWrapper<H3DLUT>)this.Parent).ReloadRender();
            }

            private float[] FromRGBA(byte[] rgba, int height)
            {
                float[] Table = new float[512];

                int index = 0;
                for (int i = 0; i < 512; i++)
                {
                    Table[i] = rgba[index] / 255f;
                    index += 4;
                }
                return Table;
            }

            private byte[] ToRGBA(float[] table, int height)
            {
                bool abs = (Sampler.Flags & H3DLUTFlags.IsAbsolute) != 0;
                int width = 512;

                byte[] data = new byte[width * height * 4];
                //Create a 1D texture sheet
                int index = 0;
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        data[index + 0] = (byte)(table[w] * 255);
                        data[index + 1] = (byte)(table[w] * 255);
                        data[index + 2] = (byte)(table[w] * 255);
                        data[index + 3] = 255;
                        index += 4;
                    }
                }

                return data;
            }

            private float[] RemapTable(float[] data)
            {
                bool abs = (Sampler.Flags & H3DLUTFlags.IsAbsolute) != 0;

                float[] Table = new float[256];
                if (abs)
                {
                    //Sample only half the angle amount
                    for (int i = 0; i < 256; i++)
                    {
                        Table[i] = data[i + 256];
                        Table[0] = data[i + 0];
                    }
                }
                else
                {
                    //Sample for the full 180 degree angle
                    for (int i = 0; i < 256; i += 2)
                    {
                        int PosIdx = i >> 1;
                        int NegIdx = PosIdx + 128;

                        Table[PosIdx] = data[i + 256];
                        Table[PosIdx] = data[i + 257];
                        Table[NegIdx] = data[i + 0];
                        Table[NegIdx] = data[i + 1];
                    }
                }
                return Table;
            }

            private float[] GetTable()
            {
                bool abs = (Sampler.Flags & H3DLUTFlags.IsAbsolute) != 0;

                float[] Table = new float[512];
                if (abs)
                {
                    //Sample only half the angle amount
                    for (int i = 0; i < 256; i++)
                    {
                        Table[i + 256] = Sampler.Table[i];
                        Table[i + 0] = Sampler.Table[0];
                    }
                }
                else
                {
                    //Sample for the full 180 degree angle
                    for (int i = 0; i < 256; i += 2)
                    {
                        int PosIdx = i >> 1;
                        int NegIdx = PosIdx + 128;

                        Table[i + 256] = Sampler.Table[PosIdx];
                        Table[i + 257] = Sampler.Table[PosIdx];
                        Table[i + 0] = Sampler.Table[NegIdx];
                        Table[i + 1] = Sampler.Table[NegIdx];
                    }
                }
                return Table;
            }
        }
    }
}