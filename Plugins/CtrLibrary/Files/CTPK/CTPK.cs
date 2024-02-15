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

        static Encoding StringEncoding => Encoding.GetEncoding("SJIS");

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
            public byte Compressed { get; set; }
            public byte Etc1Quality { get; set; }
        }

        private FileHeader FileHeaderInfo;

        private List<TextureEntry> Textures = new List<TextureEntry>();

        private List<int[]> MipmapSizes  = new List<int[]>();

        public void Load(Stream stream)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (var reader = new FileReader(stream))
            {
                reader.SetByteOrder(false);

                FileHeaderInfo = reader.ReadStruct<FileHeader>();

                var imageHeaders = reader.ReadMultipleStructs<ImageHeader>(FileHeaderInfo.numTextures);

                for (int i = 0; i < FileHeaderInfo.numTextures; i++)
                {
                    TextureEntry entry = new TextureEntry();
                    Textures.Add(entry);

                    entry.Header = imageHeaders[i];

                    // Read mip map sizes
                    reader.SeekBegin(0x40 + 4 * i);
                    MipmapSizes.Add(reader.ReadInt32s(entry.Header.MipCount));

                    // Read name
                    reader.SeekBegin(entry.Header.NameOffset);
                    entry.Name = reader.ReadZeroTerminatedString(StringEncoding);

                    // Read mip map entry
                    reader.SeekBegin(FileHeaderInfo.conversionInfoOffset + i * MipmapEntry.SIZE);
                    var mipmapInfo = reader.ReadStruct<MipmapEntry>();
                    entry.MipmapInfo = mipmapInfo;

                    // Read image data
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
                var imageHeaderOffset = 0x20;
                var mipmapSizeOffset = imageHeaderOffset + this.Textures.Count * ImageHeader.SIZE;
                var namesOffset = mipmapSizeOffset + this.Textures.Sum(x => x.Header.MipCount) * 4;
                var hashListOffset = namesOffset + ((this.Textures.Sum(x => Encoding.GetEncoding("SJIS").GetByteCount(x.Name) + 1) + 3) & ~3);
                var mipEntriesOffset = hashListOffset + this.Textures.Count * HashEntry.SIZE;
                var textureDataOffset = (mipEntriesOffset + this.Textures.Count * MipmapEntry.SIZE + 0x7F) & ~0x7F;

                // write file header
                FileHeaderInfo.numTextures = (ushort) this.Textures.Count;
                FileHeaderInfo.textureDataOffset = (uint) textureDataOffset;
                FileHeaderInfo.textureDataSize = (uint) this.Textures.Sum(x => x.ImageData.Length);
                FileHeaderInfo.hashListOffset = (uint) hashListOffset;
                FileHeaderInfo.conversionInfoOffset = (uint) mipEntriesOffset;
                writer.WriteStruct(FileHeaderInfo);

                // write image headers
                writer.SeekBegin(imageHeaderOffset);
                foreach (var tex in this.Textures)
                    // TODO: As the header contains offsets e..g NameOffset, it would need a calculation
                    // for each texture's offsets when supporting multiple textures per file
                    writer.WriteStruct(tex.Header);

                // mip map sizes
                writer.SeekBegin(mipmapSizeOffset);
                foreach (var tex in this.Textures)
                    writer.Write(tex.CalculateMipSizes());

                // names
                writer.SeekBegin(namesOffset);
                foreach (var tex in this.Textures)
                    writer.WriteString(tex.Name, StringEncoding);

                // hashes
                HashEntry[] hashes = new HashEntry[this.Textures.Count];
                for (int i = 0; i < this.Textures.Count; i++)
                    hashes[i] = new HashEntry()
                    {
                        Crc32 = this.Textures[i].CalculateHash(),
                        Index = i,
                    };

                writer.SeekBegin(hashListOffset);
                for (int i = 0; i < hashes.Length; i++)
                    writer.WriteStruct(hashes[i]);

                // mip infos
                writer.SeekBegin(mipEntriesOffset);
                foreach (var tex in this.Textures)
                    writer.WriteStruct(tex.MipmapInfo);

                // write data
                writer.Position = textureDataOffset;
                foreach (var tex in this.Textures)
                    writer.Write(tex.ImageBase.Texture.RawBuffer);
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
                    MipmapSize = this.Header.MipCount,
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
