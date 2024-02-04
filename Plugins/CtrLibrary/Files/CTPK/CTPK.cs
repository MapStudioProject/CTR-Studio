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
using SPICA.PICA.Converters;
using Toolbox.Core.Hashes.Cryptography;

namespace CtrLibrary
{
    public class CTPK : MapStudio.UI.FileEditor, IFileFormat
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "CTR Texture PacKage" };
        public string[] Extension { get; set; } = new string[] { "*.ctpk" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            using (FileReader reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "CTPK");
            }
        }

        //Todo support SHIFT JIS
        static Encoding StringEncoding => Encoding.UTF8;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FileHeader
        {
            public Magic magic = "CTPK";
            public ushort version = 1;
            public ushort numTextures;

            public uint textureDataOffset;
            public uint textureDataSize;
            public uint hashListOffset;
            public uint conversionInfoOffset;

            public uint padding1;
            public uint padding2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class ImageHeader
        {
            public const int SIZE = 32;

            public uint NameOffset;
            public uint ImageSize;
            public uint DataOffset;
            public uint TextureFormat;
            public ushort Width;
            public ushort Height;
            public byte MipCount;
            public byte Type;
            public ushort FaceCount;
            public uint SizeOffset;
            public uint UnixTimeStamp;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class HashEntry
        {
            public const int SIZE = 8;

            public uint Crc32;
            public int Index;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class MipmapEntry
        {
            public const int SIZE = 4;

            public byte TextureFormat { get; set; }
            public byte MipCount { get; set; }
            public bool Compressed { get; set; }
            public byte Etc1Quality { get; set; }
        }

        private FileHeader FileHeaderInfo;

        private List<TextureEntry> Textures = new List<TextureEntry>();

        public void Load(Stream stream)
        {
            using (var reader = new FileReader(stream))
            {
                reader.SetByteOrder(false);

                FileHeaderInfo = reader.ReadStruct<FileHeader>();

                reader.Position = 0x20;
                var imageHeaders = reader.ReadMultipleStructs<ImageHeader>(FileHeaderInfo.numTextures);

                reader.SeekBegin(FileHeaderInfo.conversionInfoOffset);
                var mipmapInfos = reader.ReadMultipleStructs<MipmapEntry>(FileHeaderInfo.conversionInfoOffset);

                //Create each image instance
                for (int i = 0; i < FileHeaderInfo.numTextures; i++)
                {
                    TextureEntry entry = new TextureEntry();
                    entry.MipmapInfo = mipmapInfos[i];
                    entry.Header = imageHeaders[i];
                    Textures.Add(entry);

                    //Read name
                    reader.SeekBegin(entry.Header.NameOffset);
                    entry.Name = reader.ReadZeroTerminatedString(StringEncoding);

                    //Read image data
                    reader.SeekBegin(FileHeaderInfo.textureDataOffset + entry.Header.DataOffset);
                    entry.ImageData = reader.ReadBytes((int)entry.Header.ImageSize);

                    entry.ReloadImage();

                    Root.AddChild(entry.ImageBase);
                }
            }
        }

        public void Save(Stream stream)
        {
            foreach (var tex in this.Textures)
                tex.OnSave();

            using (var writer = new FileWriter(stream))
            {
                // Calculate offsets
                //From https://github.com/FanTranslatorsInternational/Kuriimu2/blob/dev/plugins/Nintendo/plugin_nintendo/Images/Ctpk.cs#L15
                var texEntryOffset = 0x20;
                var dataSizeOffset = texEntryOffset + this.Textures.Count * ImageHeader.SIZE;
                var namesOffset = dataSizeOffset + this.Textures.Sum(x => x.Header.MipCount + 1) * 4;
                var hashEntryOffset = namesOffset + ((this.Textures.Sum(x => Encoding.GetEncoding("SJIS").GetByteCount(x.Name) + 1) + 3) & ~3);
                var mipEntriesOffset = hashEntryOffset + this.Textures.Count * HashEntry.SIZE;
                var dataOffset = (mipEntriesOffset + this.Textures.Count * MipmapEntry.SIZE + 0x7F) & ~0x7F;

                var headerPos = writer.Position;
                writer.WriteStruct(FileHeaderInfo);

                foreach (var tex in this.Textures)
                {

                }

                //image headers
                foreach (var tex in this.Textures)
                    writer.WriteStruct(tex.Header);

                //image data sizes
                foreach (var tex in this.Textures)
                    writer.Write(tex.CalculateMipSizes());

                //names
                foreach (var tex in this.Textures)
                    writer.WriteString(tex.Name, StringEncoding);

                //hashes
                HashEntry[] hashes = new HashEntry[this.Textures.Count];
                for (int i = 0; i < this.Textures.Count; i++)
                    hashes[i] = new HashEntry()
                    {
                        Crc32 = this.Textures[i].CalculateHash(),
                        Index = i,
                    };

                for (int i = 0; i < hashes.Length; i++)
                    writer.WriteStruct(hashes[i]);

                //mip infos
                foreach (var tex in this.Textures)
                    writer.WriteStruct(tex.MipmapInfo);

                //Header
            }
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

        public class TextureEntry
        {
            public string Name;

            //Image header
            public ImageHeader Header;
            //Conversion info
            public MipmapEntry MipmapInfo;
            //Image data
            public byte[] ImageData;

            //Image base for handling the image ui/dislay
            public CtrImageBase ImageBase;

            public void ReloadImage()
            {
                var texture = new H3DTexture()
                {
                    Width = this.Header.Width,
                    Height = this.Header.Height,
                    Format = (PICATextureFormat)this.Header.TextureFormat,
                    MipmapSize = 1,
                    Name = Name,
                    RawBuffer = this.ImageData,
                };
                ImageBase = new CtrImageBase(texture);     
            }

            public void OnSave()
            {
                var tex = ImageBase.Texture;
                this.Header.Width = (ushort)tex.Width;
                this.Header.Height = (ushort)tex.Height;
                this.Header.TextureFormat = (uint)tex.Format;
                this.Header.MipCount = tex.MipmapSize;
            }

            public uint CalculateHash() => Crc32.Compute(this.Name);

            public uint[] CalculateMipSizes()
            {
                uint[] sizes = new uint[this.Header.MipCount];
                for (int level = 0; level < this.Header.MipCount; level++)
                {
                    int mwidth = Math.Max(1, this.Header.Width >> level);
                    int mheight = Math.Max(1, this.Header.Height >> level);
                    sizes[level] = (uint)TextureConverter.CalculateLength(mwidth, mheight, (PICATextureFormat)this.Header.TextureFormat);
                }
                return sizes;
            }
        }
    }
}
