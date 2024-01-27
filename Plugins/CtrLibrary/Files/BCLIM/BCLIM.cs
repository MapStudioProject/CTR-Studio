using CtrLibrary;
using SPICA.PICA.Shader;
using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.ViewModels;
using UIFramework;
using CtrLibrary.UI;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.PICA.Commands;

namespace CtrLibrary
{
    public class BCLIM : MapStudio.UI.FileEditor, IFileFormat
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { ".bclim" };
        public string[] Extension { get; set; } = new string[] { "*.bclim" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            if (stream.Length < 0x40 || (!fileInfo.FileName.EndsWith(".bclim")))
                return false;

            using (FileReader reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "CLIM", reader.BaseStream.Length - 0x28);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FileHeader
        {
            public Magic magic = "CLIM";
            public ushort bom = 0xFEFF;
            public ushort headerSize = 0x14;
            public uint version = 0x02020000;
            public uint fileSize;
            public ushort numBlocks = 1;
            public ushort padding;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class ImageHeader
        {
            public Magic magic = "imag";
            public uint blockSize = 0x10;
            public short Width;
            public short Height;
            public byte Format;
            public byte TransformMode;
            public short Alignment;
            public int DataSize;
        }

        private FileHeader FileHeaderInfo;
        private ImageHeader ImageInfo;

        private byte[] ImageData;

        private CtrImageBase ImageBase;

        public void Load(Stream stream)
        {
            using (var reader = new FileReader(stream))
            {
                uint FileSize = (uint)reader.BaseStream.Length;

                reader.SeekBegin(FileSize - 0x28);
                reader.SetByteOrder(false);

                FileHeaderInfo = reader.ReadStruct<FileHeader>();
                ImageInfo = reader.ReadStruct<ImageHeader>();

                reader.Position = 0;
                ImageData = reader.ReadBytes((int)ImageInfo.DataSize);

                ReloadImage(ImageInfo);
            }
        }

        public void Save(Stream stream)
        {
            FromH3D(ImageBase.Texture);

            using (var writer = new FileWriter(stream))
            {
                ImageInfo.DataSize = ImageData.Length;
                ImageInfo.Alignment = 128;

                writer.SetByteOrder(false);
                writer.Write(ImageData);

                var headerPos = writer.Position;
                writer.WriteStruct(FileHeaderInfo);
                writer.WriteStruct(ImageInfo);

                //write file size
                using (writer.TemporarySeek(headerPos + 12, System.IO.SeekOrigin.Begin)) {
                    writer.Write((uint)writer.BaseStream.Length);
                }
            }
        }

        private void ReloadImage(ImageHeader image)
        {
            var texture = ToH3D();
            var transformMode = (CTR_3DS.Orientation)ImageInfo.TransformMode;

            ImageBase = new CtrImageBase(texture, transformMode);
            this.Root.AddChild(ImageBase);
        }

        void FromH3D(H3DTexture texture)
        {
            this.ImageInfo.Width = (short)texture.Width;
            this.ImageInfo.Height = (short)texture.Height;
            this.ImageInfo.Format = FormatList.FirstOrDefault(x => x.Value == texture.Format).Key;
            this.ImageInfo.TransformMode = (byte)ImageBase.Orientation;

            this.ImageData = texture.RawBuffer;
        }

        private H3DTexture ToH3D()
        {
            return new H3DTexture()
            {
                Width = this.ImageInfo.Width,
                Height = this.ImageInfo.Height,
                Format = FormatList[this.ImageInfo.Format],
                MipmapSize = 1,
                Name = Path.GetFileNameWithoutExtension(FileInfo.FileName),
                RawBuffer = this.ImageData,
            };
        }

        /// <summary>
        /// Prepares the dock layouts to be used for the file format.
        /// </summary>
        public override List<DockWindow> PrepareDocks()
        {
            List<DockWindow> windows = new List<DockWindow>();
            windows.Add(Workspace.Outliner);
            windows.Add(Workspace.PropertyWindow);
            return windows;
        }

        public void DrawArchiveFileEditor()
        {
            MapStudio.UI.ImageEditor.LoadEditor(ImageBase.Tag as STGenericTexture);
        }


        Dictionary<byte, PICATextureFormat> FormatList = new Dictionary<byte, PICATextureFormat>()
        {
            { 0, PICATextureFormat.L8 },
            { 1, PICATextureFormat.A8 },
            { 2, PICATextureFormat.LA4 },
            { 3, PICATextureFormat.LA8 },
            { 4, PICATextureFormat.HiLo8 },
            { 5, PICATextureFormat.RGB565 },
            { 6, PICATextureFormat.RGB8 },
            { 7, PICATextureFormat.RGBA5551 },
            { 8, PICATextureFormat.RGBA4 },
            { 9, PICATextureFormat.RGBA8 },
            { 10, PICATextureFormat.ETC1 },
            { 11, PICATextureFormat.ETC1A4 },
            { 12, PICATextureFormat.L4 },
            { 13, PICATextureFormat.A4 },
        };
    }
}
