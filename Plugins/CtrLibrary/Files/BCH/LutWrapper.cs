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

namespace CtrLibrary.Bch
{
    internal class LutWrapper
    {

    }

    internal class LUTFolder : NodeBase
    {
        public override string Header => "Look Ups";

        H3DRender H3DRender;
        H3D H3DFile;

        public LUTFolder(H3DRender render, H3D file)
        {
            H3DFile = file;
            H3DRender = render;

            foreach (var lut in file.LUTs)
                AddChild(new LUTWrapper(render, file, lut));

            this.ContextMenus.Add(new MenuItemModel("Create", Create));
            this.ContextMenus.Add(new MenuItemModel("Import", Import));
        }

        public H3DDict<H3DLUT> GetLuts()
        {
            H3DDict<H3DLUT> luts = new H3DDict<H3DLUT>();
            foreach (LUTWrapper lut in this.Children)
                luts.Add(lut.Section);

            return luts;
        }

        private void Create()
        {
            var lut = new H3DLUT()
            {
                Name = "LUT_Table",
            };
            AddChild(new LUTWrapper(H3DRender, H3DFile, lut));

            H3DFile.LUTs.Add(lut);
            H3DRender.LUTCache.Add(lut.Name, lut);
        }

        private void Import()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = false;
        }
    }

    internal class LUTWrapper : NodeBase
    {
        internal H3DLUT Section;
        H3D H3D;
        H3DRender H3DRender;

        public LUTWrapper(H3DRender render, H3D h3d, H3DLUT lut)
        {
            H3DRender = render;
            H3D = h3d;
            Section = lut;
            Header = lut.Name;
            Icon = '\uf0ce'.ToString();
            CanRename = true;
            OnHeaderRenamed += delegate
            {
                Section.Name = this.Header;
            };

            this.ContextMenus.Add(new MenuItemModel("Create Sampler", CreateSampler));
            this.ContextMenus.Add(new MenuItemModel("Import Sampler", ImportSampler));
            this.ContextMenus.Add(new MenuItemModel(""));
            this.ContextMenus.Add(new MenuItemModel("Export", Export));
            this.ContextMenus.Add(new MenuItemModel("Replace", Replace));
            this.ContextMenus.Add(new MenuItemModel(""));
            this.ContextMenus.Add(new MenuItemModel("Rename", () => { ActivateRename = true; }));

            foreach (var sampler in lut.Samplers)
                AddChild(new LUTSamplerWrapper(sampler));
        }

        private void CreateSampler()
        {
            var samp = new H3DLUTSampler()
            {
                Name = "NewSampler",
            };
            Section.Samplers.Add(samp);
            AddChild(new LUTSamplerWrapper(samp));
            ReloadRender();
        }

        private void ImportSampler()
        {
            var wrapper = LUTSamplerWrapper.Import();
            AddChild(wrapper);
            Section.Samplers.Add(wrapper.Sampler);

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
            H3DRender.Renderer.LUTs.Add(this.Header, new SPICA.Rendering.LUT(Section));
        }

        void Replace()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = false;
            dlg.FileName = $"{Header}.json";
            dlg.AddFilter("json", "json");
            if (dlg.ShowDialog())
            {
                Section = JsonConvert.DeserializeObject<H3DLUT>(File.ReadAllText(dlg.FilePath));
                H3D.LUTs[this.Header] = Section;
            }
        }

        void Export()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = true;
            dlg.FileName = $"{Header}.json";
            dlg.AddFilter("json", "json");
            if (dlg.ShowDialog())
            {
                File.WriteAllText(dlg.FilePath, JsonConvert.SerializeObject(Section, Formatting.Indented));
            }
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
                this.ContextMenus.Add(new MenuItemModel("Replace", Replace));
                this.ContextMenus.Add(new MenuItemModel(""));
                this.ContextMenus.Add(new MenuItemModel("Rename", () => { ActivateRename = true; }));
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
                wrapper.Replace();
                return wrapper;
            }

            void Replace()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = false;
                dlg.FileName = $"{Header}.png";
                dlg.AddFilter("png", "png");
                if (dlg.ShowDialog())
                {
                    var image = Image.Load<Rgba32>(dlg.FilePath);
                    if (image.Width != 512)
                        throw new Exception($"Invalid image width for LUT! Expected 512 but got {image.Width}!");

                    image.Mutate(x => x.GaussianBlur());

                    //Get rgba data
                    var rgba = image.GetSourceInBytes();
                    //Turn the rgba data into LUT
                    Sampler.Table = RemapTable(FromRGBA(rgba, image.Height));
                    ReloadRender();
                }
            }

            void Export()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = true;
                dlg.FileName = $"{Header}.png";
                dlg.AddFilter("png", "png");
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
                    ((LUTWrapper)this.Parent).ReloadRender();
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
                //Create a 1D texture sheet from the span of the timeline covering all the colors
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