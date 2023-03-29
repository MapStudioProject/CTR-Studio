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
            using (var reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "IECP");
            }
        }

        public bool CanCompress { get; } = true;

        public Stream Decompress(Stream data)
        {
            uint decodedLength = 0;
            using (var reader = new FileReader(data, true))
            {
                reader.ReadUInt32();
                decodedLength = reader.ReadUInt32();
            }

            byte[] input = new byte[data.Length - data.Position];
            data.Read(input, 0, input.Length);
            data.Close();
            long inputOffset = 0;

            byte[] output = new byte[decodedLength];
            byte[] dictionary = new byte[4096];

            long outputOffset = 0;
            long dictionaryOffset = 4078;

            ushort mask = 0x80;
            byte header = 0;

            while (outputOffset < decodedLength)
            {
                if ((mask <<= 1) == 0x100)
                {
                    header = input[inputOffset++];
                    mask = 1;
                }

                if ((header & mask) > 0)
                {
                    if (outputOffset == output.Length) break;
                    output[outputOffset++] = input[inputOffset];
                    dictionary[dictionaryOffset] = input[inputOffset++];
                    dictionaryOffset = (dictionaryOffset + 1) & 0xfff;
                }
                else
                {
                    ushort value = (ushort)(input[inputOffset++] | (input[inputOffset++] << 8));
                    int length = ((value >> 8) & 0xf) + 3;
                    int position = ((value & 0xf000) >> 4) | (value & 0xff);

                    while (length > 0)
                    {
                        dictionary[dictionaryOffset] = dictionary[position];
                        output[outputOffset++] = dictionary[dictionaryOffset];
                        dictionaryOffset = (dictionaryOffset + 1) & 0xfff;
                        position = (position + 1) & 0xfff;
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