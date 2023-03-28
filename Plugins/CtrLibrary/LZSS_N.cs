using System.IO;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace CtrLibrary
{
    /// <summary>
    /// Plugin for decompressing LZSS files.
    /// </summary>
    public class LZSS_N : ICompressionFormat
    {
        public string[] Description { get; set; } = new string[] { "LZSS Compression" };
        public string[] Extension { get; set; } = new string[] { "*.lzs", "*.lzss" };

        public bool Identify(Stream stream, string fileName)
        {
            //Check by extension. Used by games like kirby planet robobot
            if (stream.Length > 16 && fileName.EndsWith(".cmp"))
            {
                using (var reader = new FileReader(stream, true))
                {
                    uint cmp = reader.ReadUInt32();
                    if ((cmp & 0xff) == 0x13) cmp = reader.ReadUInt32();

                    reader.Position = 0;

                    switch (cmp & 0xff)
                    {
                        case 0x11:
                            return true;
                    }
                }
                }
            return false;
        }

        public bool CanCompress { get; } = true;

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
            var res = new List<byte>();
            res.AddRange(new byte[] { (byte)0x11, (byte)(stream.Length & 0xff), (byte)(stream.Length >> 8 & 0xff), (byte)(stream.Length >> 16 & 0xff) });

            res.AddRange(CompressLZ11(stream).ToArray());
            return new MemoryStream(res.ToArray());
        }

        //Compression code from https://github.com/IcySon55/Kuriimu/blob/3f05ffc993e0908929e92373c455acb633b8f28d/src/Kontract/Compression/LZ11.cs#L61

        private unsafe Stream CompressLZ11(Stream stream)
        {
            // There should be room for four bytes, however I'm not 100% sure if that can be used
            // in every game, as it may not be a built-in function.
            long inLength = stream.Length;
            Stream outstream = new MemoryStream();

            // save the input data in an array to prevent having to go back and forth in a file
            byte[] indata = new byte[inLength];
            int numReadBytes = stream.Read(indata, 0, (int)inLength);
            if (numReadBytes != inLength)
                throw new Exception("Stream too short!");

            int compressedLength = 0;

            fixed (byte* instart = &indata[0])
            {
                // we do need to buffer the output, as the first byte indicates which blocks are compressed.
                // this version does not use a look-ahead, so we do not need to buffer more than 8 blocks at a time.
                // (a block is at most 4 bytes long)
                byte[] outbuffer = new byte[8 * 4 + 1];
                outbuffer[0] = 0;
                int bufferlength = 1, bufferedBlocks = 0;
                int readBytes = 0;
                while (readBytes < inLength)
                {
                    #region If 8 blocks are bufferd, write them and reset the buffer
                    // we can only buffer 8 blocks at a time.
                    if (bufferedBlocks == 8)
                    {
                        outstream.Write(outbuffer, 0, bufferlength);
                        compressedLength += bufferlength;
                        // reset the buffer
                        outbuffer[0] = 0;
                        bufferlength = 1;
                        bufferedBlocks = 0;
                    }
                    #endregion

                    // determine if we're dealing with a compressed or raw block.
                    // it is a compressed block when the next 3 or more bytes can be copied from
                    // somewhere in the set of already compressed bytes.
                    int disp;
                    int oldLength = Math.Min(readBytes, 0x1000);
                    int length = GetOccurrenceLength(instart + readBytes, (int)Math.Min(inLength - readBytes, 0x10110),
                                                          instart + readBytes - oldLength, oldLength, out disp);

                    // length not 3 or more? next byte is raw data
                    if (length < 3)
                    {
                        outbuffer[bufferlength++] = *(instart + (readBytes++));
                    }
                    else
                    {
                        // 3 or more bytes can be copied? next (length) bytes will be compressed into 2 bytes
                        readBytes += length;

                        // mark the next block as compressed
                        outbuffer[0] |= (byte)(1 << (7 - bufferedBlocks));

                        if (length > 0x110)
                        {
                            // case 1: 1(B CD E)(F GH) + (0x111)(0x1) = (LEN)(DISP)
                            outbuffer[bufferlength] = 0x10;
                            outbuffer[bufferlength] |= (byte)(((length - 0x111) >> 12) & 0x0F);
                            bufferlength++;
                            outbuffer[bufferlength] = (byte)(((length - 0x111) >> 4) & 0xFF);
                            bufferlength++;
                            outbuffer[bufferlength] = (byte)(((length - 0x111) << 4) & 0xF0);
                        }
                        else if (length > 0x10)
                        {
                            // case 0; 0(B C)(D EF) + (0x11)(0x1) = (LEN)(DISP)
                            outbuffer[bufferlength] = 0x00;
                            outbuffer[bufferlength] |= (byte)(((length - 0x11) >> 4) & 0x0F);
                            bufferlength++;
                            outbuffer[bufferlength] = (byte)(((length - 0x11) << 4) & 0xF0);
                        }
                        else
                        {
                            // case > 1: (A)(B CD) + (0x1)(0x1) = (LEN)(DISP)
                            outbuffer[bufferlength] = (byte)(((length - 1) << 4) & 0xF0);
                        }
                        // the last 1.5 bytes are always the disp
                        outbuffer[bufferlength] |= (byte)(((disp - 1) >> 8) & 0x0F);
                        bufferlength++;
                        outbuffer[bufferlength] = (byte)((disp - 1) & 0xFF);
                        bufferlength++;
                    }
                    bufferedBlocks++;
                }

                // copy the remaining blocks to the output
                if (bufferedBlocks > 0)
                {
                    outstream.Write(outbuffer, 0, bufferlength);
                    compressedLength += bufferlength;
                    /*/ make the compressed file 4-byte aligned.
                    while ((compressedLength % 4) != 0)
                    {
                        outstream.WriteByte(0);
                        compressedLength++;
                    }/**/
                }
            }

            outstream.Position = 0;
            return outstream;
        }

        public static unsafe int GetOccurrenceLength(byte* newPtr, int newLength, byte* oldPtr, int oldLength, out int disp, int minDisp = 1)
        {
            disp = 0;
            if (newLength == 0)
                return 0;
            int maxLength = 0;
            // try every possible 'disp' value (disp = oldLength - i)
            for (int i = 0; i < oldLength - minDisp; i++)
            {
                // work from the start of the old data to the end, to mimic the original implementation's behaviour
                // (and going from start to end or from end to start does not influence the compression ratio anyway)
                byte* currentOldStart = oldPtr + i;
                int currentLength = 0;
                // determine the length we can copy if we go back (oldLength - i) bytes
                // always check the next 'newLength' bytes, and not just the available 'old' bytes,
                // as the copied data can also originate from what we're currently trying to compress.
                for (int j = 0; j < newLength; j++)
                {
                    // stop when the bytes are no longer the same
                    if (*(currentOldStart + j) != *(newPtr + j))
                        break;
                    currentLength++;
                }

                // update the optimal value
                if (currentLength > maxLength)
                {
                    maxLength = currentLength;
                    disp = oldLength - i;

                    // if we cannot do better anyway, stop trying.
                    if (maxLength == newLength)
                        break;
                }
            }
            return maxLength;
        }
    }
}