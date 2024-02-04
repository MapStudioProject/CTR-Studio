using System;
using System.Collections.Generic;
using System.Linq;
using SharpEXR.Compression;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace Kontract.Compression
{
    public class Nintendo : ICompressionFormat
    {
        public string[] Description => new string[0];

        public string[] Extension => new string[0];

        public bool CanCompress => false;

        public enum Method : byte
        {
            LZ10 = 0x10,
            LZ11 = 0x11,
            Huff4 = 0x24,
            Huff8 = 0x28,
            RLE = 0x30,
            LZ40 = 0x40,
            LZ60 = 0x60
        }

        public static byte[] Decompress(Stream input)
        {
            using (var br = new FileReader(input, true))
            {
                var methodSize = br.ReadUInt32();
                var method = (Method)(methodSize & 0xff);
                int size = (int)((methodSize & 0xffffff00) >> 8);

                using (var brB = new FileReader(new MemoryStream(br.ReadBytes((int)input.Length - 4))))
                    switch (method)
                    {
                        case Method.LZ60:   //yes, LZ60 does indeed seem to be the exact same as LZ40
                            return LZ40.Decompress(brB.BaseStream, size);
                        default:
                            return br.BaseStream.ToArray();
                    }
            }
        }

        public bool Identify(Stream stream, string fileName)
        {
            return fileName.ToLower().EndsWith("bcol") ||
                fileName.ToLower().EndsWith("cbch");
        }

        Stream ICompressionFormat.Decompress(Stream stream)
        {
            return new MemoryStream(Decompress(stream));
        }

        public Stream Compress(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}