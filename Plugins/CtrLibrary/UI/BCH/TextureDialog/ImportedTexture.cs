using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;
using MapStudio.UI;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;
using SixLabors.ImageSharp.Processing;
using SPICA.Formats.Common;
using CtrLibrary.UI;

namespace CtrLibrary
{
    /// <summary>
    /// Represents a texture that is used for importing and encoding H3D texture data.
    /// </summary>
    public class H3DImportedTexture
    {
        /// <summary>
        /// The texture format of the imported texture.
        /// </summary>
        public PICATextureFormat Format
        {
            get { return _format; }
            set
            {
                if (_format != value) {
                    _format = value;
                    UpdateMipsIfRequired();
                    ReloadEncodingSize();
                }
            }
        }

        private PICATextureFormat _format;

        /// <summary>
        /// The file path of the imported texture.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The name of the texture.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The time it took to encode the image.
        /// </summary>
        public string EncodingTime = "";

        /// <summary>
        /// The width of the image.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// The height of the image.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// A readable string of the total size of the encoded image.
        /// </summary>
        public string EncodingSize { get; private set; }

        private uint _mipCount;

        /// <summary>
        /// The mip count of the image.
        /// </summary>
        public uint MipCount
        {
            get { return _mipCount; }
            set
            {
                value = Math.Max(value, 1);
                value = Math.Min(value, MaxMipCount);
                _mipCount = value;
                //re encode all surfaces to adjust the mip count
                foreach (var surface in Surfaces)
                    surface.Encoded = false;
                ReloadEncodingSize();
            }
        }

        private uint MaxMipCount = 6;

        /// <summary>
        /// 
        /// </summary>
        public int ActiveArrayIndex = 0;

        /// <summary>
        /// Determines if the texture has been encoded in the "Format" or not.
        /// </summary>
        public bool Encoded
        {
            get { return Surfaces[ActiveArrayIndex].Encoded; }
            set
            {
                Surfaces[ActiveArrayIndex].Encoded = value;
            }
        }

        //Cache the encoded data so it can be applied when dialog is finished.
        //This prevents re encoding again saving time.
        public List<Surface> Surfaces = new List<Surface>();

        public H3DImportedTexture(string fileName) {
            FilePath = fileName;
            Format = PICATextureFormat.ETC1A4;

            //Remove extension. Fix for keeping . in filenames
            string ext = fileName.Split(".").LastOrDefault();
            Name = Path.GetFileName(fileName.Replace($".{ext}", ""));

            Surfaces.Add(new Surface(fileName));
            Reload(0);
        }

        public H3DImportedTexture(string name, byte[] rgbaData, uint width, uint height, uint mipCount, PICATextureFormat format)
        {
            Name = name;
            Format = format;
            Width = (int)width;
            Height = (int)height;
            MipCount = mipCount;
            Surfaces.Add(new Surface(rgbaData, Width, Height));
        }

        /// <summary>
        /// Adds a surface to the imported texture.
        /// </summary>
        public void AddSurface(string filePath)
        {
            var surface = new Surface(filePath);
            surface.Reload(Width, Height);
            Surfaces.Add(surface);
        }

        /// <summary>
        /// Removes a surface from the imported texture.
        /// </summary>
        public void RemoveSurface(int index)
        {
            var surface = Surfaces[index];
            surface.Dispose();
            Surfaces.RemoveAt(index);
        }

        /// <summary>
        /// Reloads the surface from its loaded file path.
        /// </summary>
        public void Reload(int index) {
            var surface = Surfaces[index];
            surface.Reload();
            Width = surface.ImageFile.Width;
            Height = surface.ImageFile.Height;

            MaxMipCount = 1 + CalculateMipCount();
            MipCount = MaxMipCount;
        }

        /// <summary>
        /// Updates the max mip count amount and clamps the mip count to the max if required.
        /// </summary>
        public void UpdateMipsIfRequired()
        {
            MaxMipCount = 1 + CalculateMipCount();
            if (MipCount > MaxMipCount)
                MipCount = MaxMipCount;
        }

        /// <summary>
        /// Reloads the encoding size into a readable string.
        /// </summary>
        private void ReloadEncodingSize() {
            EncodingSize = STMath.GetFileSize(CalculateEncodingSize());
        }

        /// <summary>
        /// Disposes all surfaces in the imported texture.
        /// </summary>
        public void Dispose()
        {
            foreach (var surface in Surfaces)
                surface?.Dispose();
        }

        /// <summary>
        /// Decodes the current surface.
        /// </summary>
        public byte[] DecodeTexture(int index)
        {
            //Get the encoded data and turn back into raw rgba data for previewing purposes
            var surface = Surfaces[index];
            var encoded = surface.EncodedData; //Only need first mip level

            byte[] Buffer = TextureConverter.Decode(encoded, Width, Height, Format);
            if (this.Format == PICATextureFormat.ETC1 || this.Format == PICATextureFormat.ETC1A4)
                return Buffer;

            return Buffer;
        }

        /// <summary>
        /// Calculates the encoding size of the current image width, height, format and mip count
        /// </summary>
        /// <returns></returns>
        public int CalculateEncodingSize() {
            return TextureConverter.CalculateTotalSize(Width, Height, (int)MipCount, Format);
        }

        /// <summary>
        /// Encodes the current surface.
        /// </summary>
        public void EncodeTexture(int index)
        {
            var surface = Surfaces[index];

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            bool isETC1 = Format == PICATextureFormat.ETC1 || Format == PICATextureFormat.ETC1A4;
            if (isETC1 && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && ETC1Compressor.UseEncoder)
                surface.EncodedData = ETC1Compressor.Encode(surface.ImageFile, (int)MipCount, Format == PICATextureFormat.ETC1A4);
            else
                surface.EncodedData = TextureConverter.Encode(surface.ImageFile, Format, (int)MipCount);
            Encoded = true;

            stopWatch.Stop();

            TimeSpan ts = stopWatch.Elapsed;
            EncodingTime = string.Format("{0:00}ms", ts.Milliseconds);
        }

        /// <summary>
        /// Calculates the total possible mip count of the current width, height and format.
        /// ETC1 type is limited to 16 pixels on width/height due to tiling and will use less mips.
        /// </summary>
        /// <returns></returns>
        private uint CalculateMipCount() 
        {
            int MipmapNum = 0;
            int num = Math.Max(Height, Width);

            uint width = (uint)Width;
            uint height = (uint)Height;

            uint Pow2RoundDown(uint Value) {
                return IsPow2(Value) ? Value : Pow2RoundUp(Value) >> 1;
            }

            bool IsPow2(uint Value) {
                return Value != 0 && (Value & (Value - 1)) == 0;
            }

            uint Pow2RoundUp(uint Value)
            {
                Value--;

                Value |= (Value >> 1);
                Value |= (Value >> 2);
                Value |= (Value >> 4);
                Value |= (Value >> 8);
                Value |= (Value >> 16);

                return ++Value;
            }

            while (true)
            {
                num >>= 1;

                width = width / 2;
                height = height / 2;

                width = Pow2RoundDown(width);
                height = Pow2RoundDown(height);

                if (Format ==  PICATextureFormat.ETC1)
                {
                    if (width < 16 || height < 16)
                        break;
                }
                else if (width < 8 || height < 8)
                    break;

                if (num > 0)
                    ++MipmapNum;
                else
                    break;
            }
            return (uint)MipmapNum;
        }
        
        /// <summary>
        /// Attempts to detect what format might be best suited based on the image contents.
        /// </summary>
        public PICATextureFormat TryDetectFormat(PICATextureFormat defaultFormat)
        {
            var imageData = Surfaces[0].ImageFile.GetSourceInBytes();
            int index = 0;
            int stride = 4;
            bool isAlphaTranslucent = false;
            bool hasAlpha = false;

            bool isGrayscale = true;

            for (int w = 0; w < Width; w++) {
                for (int h = 0; h < Height; h++) {
                    byte red   = imageData[index];
                    byte green = imageData[index+1];
                    byte blue  = imageData[index+2];
                    byte alpha = imageData[index+3];

                    //not grayscale
                    if (red != green && red != blue)
                        isGrayscale = false;

                    //alpha supported
                    if (alpha != 255)
                        hasAlpha = true;

                    //alpha values > 0 and < 255 supported
                    if (alpha != 255 && alpha != 0)
                        isAlphaTranslucent = true;

                    index += stride;
                }
            }

            //Red only
           // if (isGrayscale && Name != null)
           //     return PICATextureFormat.LA8;
            //Has transparency
            if (isAlphaTranslucent)
                return PICATextureFormat.ETC1A4;
            //Has alpha
            if (hasAlpha)
                return PICATextureFormat.ETC1A4;

            return defaultFormat;
        }

        public class Surface
        {
            /// <summary>
            /// Determines if the texture has been encoded in the "Format" or not.
            /// </summary>
            public bool Encoded { get; set; }

            /// <summary>
            /// The encoded mip map data of the surface.
            /// </summary>
            public byte[] EncodedData;

            /// <summary>
            /// The original file path of the image imported.
            /// </summary>
            public string SourceFilePath { get; set; }

            /// <summary>
            /// The raw image file data.
            /// </summary>
            public Image<Rgba32> ImageFile;

            public Surface(string fileName) {
                SourceFilePath = fileName;
            }

            public Surface(byte[] rgba, int width, int height) {
                ImageFile = Image.LoadPixelData<Rgba32>(rgba, width, height);
            }

            public void Reload() {
                if (ImageFile != null)
                    ImageFile.Dispose();

                if (SourceFilePath.EndsWith(".exr"))
                    ImageFile = ImageSharpTextureHelper.LoadAlphaEncodedExr(SourceFilePath);
                else if (SourceFilePath.EndsWith(".tiff") || SourceFilePath.EndsWith(".tif"))
                {
                    var bitmap = new System.Drawing.Bitmap(SourceFilePath);
                    var rgba = BitmapExtension.ImageToByte(bitmap);
                    ImageFile = Image.LoadPixelData<Rgba32>(rgba, bitmap.Width, bitmap.Height);
                    bitmap.Dispose();
                }
                else
                    ImageFile = Image.Load<Rgba32>(SourceFilePath);

                //Update width/height based on 3DS limitations
                int width = Math.Min(ImageFile.Width, 1024);
                int height = Math.Min(ImageFile.Height, 1024);
                //GPU requires power of 2
                width = (int)BitUtils.Pow2RoundDown((uint)width);
                height = (int)BitUtils.Pow2RoundDown((uint)height);

                if (ImageFile.Width != width || ImageFile.Height != height)
                    ImageSharpTextureHelper.Resize(ImageFile, width, height);
            }

            public void Reload(int width, int height) {
                ImageFile = Image.Load<Rgba32>(SourceFilePath);
                if (ImageFile.Width != width || ImageFile.Height != height) 
                    ImageSharpTextureHelper.Resize(ImageFile, width, height);
            }

            public List<byte[]> GenerateMipMaps(uint mipCount)
            {
                var mipmaps = ImageSharpTextureHelper.GenerateMipmaps(ImageFile, mipCount);

                List<byte[]> output = new List<byte[]>();
                for (int i = 0; i < mipCount; i++)
                {
                    output.Add(mipmaps[i].GetSourceInBytes());
                    //Dispose base images after if not the base image
                    if (i != 0)
                        mipmaps[i].Dispose();
                }
                return output;
            }

            public void Dispose() {
                ImageFile?.Dispose();
            }
        }
    }
}
