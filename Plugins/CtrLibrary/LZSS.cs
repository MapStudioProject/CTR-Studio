using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using Toolbox.Core;

namespace CtrLibrary
{
    /// <summary>
    /// Plugin for decompressing LZSS files.
    /// </summary>
    public class LZSS : ICompressionFormat
    {
        public string[] Description { get; set; } = new string[] { "LZSS Compression" };
        public string[] Extension { get; set; } = new string[] { "*.lzs", "*.lzss" };

        public bool Identify(Stream stream, string fileName)
        {
            //Check by extension. Used by games like kirby planet robobot
            return fileName.EndsWith(".cmp");
        }

        public bool CanCompress { get; } = false;

        public Stream Decompress(Stream stream)
        {
            byte[] input = stream.ToArray();

            uint compressedSize = 0;
            uint decodedLength = BitConverter.ToUInt32(input, 0) >> 8;

            byte[] output = new byte[decodedLength];
            long outputOffset = 0;
            long inputOffset = 4;

            byte mask = 0;
            byte header = 0;

            while (outputOffset < decodedLength)
            {
                if ((mask >>= 1) == 0)
                {
                    header = input[inputOffset++];
                    mask = 0x80;
                }

                if ((header & mask) == 0)
                {
                    output[outputOffset++] = input[inputOffset++];
                }
                else
                {
                    int byte1, byte2, byte3, byte4;
                    byte1 = input[inputOffset++];
                    int position, length;
                    switch (byte1 >> 4)
                    {
                        case 0:
                            byte2 = input[inputOffset++];
                            byte3 = input[inputOffset++];

                            position = ((byte2 & 0xf) << 8) | byte3;
                            length = (((byte1 & 0xf) << 4) | (byte2 >> 4)) + 0x11;
                            break;
                        case 1:
                            byte2 = input[inputOffset++];
                            byte3 = input[inputOffset++];
                            byte4 = input[inputOffset++];

                            position = ((byte3 & 0xf) << 8) | byte4;
                            length = (((byte1 & 0xf) << 12) | (byte2 << 4) | (byte3 >> 4)) + 0x111;
                            break;
                        default:
                            byte2 = input[inputOffset++];

                            position = ((byte1 & 0xf) << 8) | byte2;
                            length = (byte1 >> 4) + 1;
                            break;
                    }
                    position++;

                    while (length > 0)
                    {
                        output[outputOffset] = output[outputOffset - position];
                        outputOffset++;
                        length--;
                    }
                }
            }

            return new MemoryStream(output);
        }

        public Stream Compress(Stream stream)
        {
            var mem = new MemoryStream();
            return stream;
        }
    }
}