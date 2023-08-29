
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SPICA;

namespace ETC1Compressor
{
    internal class Program
    {
        static void Main(string[] args)
        {
          //  args = new string[] { "FLOOR NRM.png", "-m", "3" };

            string filePath = "";
            string outputPath = "temp.bin";
            int mipCount = 1;
            bool alpha = false;
            RG_ETC1.Quality = RG_ETC1.ETC1_Quality.high;

            for (int i = 0; i < args.Length; i++)
            {
                if (File.Exists(args[i]))
                    filePath = args[i];

                switch (args[i])
                {
                    case "-q":
                    case "-quality":
                        RG_ETC1.Quality = (RG_ETC1.ETC1_Quality)int.Parse(args[i + 1]);
                        break;
                    case "-o":
                    case "-output":
                        outputPath = args[i +1];
                        break;
                    case "-m":
                    case "-mip":
                        mipCount = int.Parse(args[i + 1]); 
                        break;
                    case "-a":
                    case "-alpha":
                        alpha = true;
                        break;
                }
            }

            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("No input file present!");
                Console.WriteLine($"args {string.Join(',', args)}");

                return;
            }

            var image = Image.Load<Rgba32>(filePath);
            var data = EncodeMipmaps(image, mipCount, alpha);
            image.Dispose();

            File.WriteAllBytes(outputPath, data);
        }

        static byte[] EncodeMipmaps(Image<Rgba32> img, int mipCount, bool isETC1A4)
        {
            var mips = ImageSharpTextureHelper.GenerateMipmaps(img, (uint)mipCount);
            var bpp = isETC1A4 ? 8 : 4;

            List<byte[]> mipmaps = new List<byte[]>();
            mipmaps.Add(Encode(img, isETC1A4));
            for (int i = 1; i < mipCount; i++)
            {
                mipmaps.Add(Encode(mips[i], isETC1A4));
            }

            var mem = new System.IO.MemoryStream();
            using (var writer = new System.IO.BinaryWriter(mem))
            {
                // In PICA all mipmap levels are stored next to each other
                long addr = 0;
                for (int i = 0; i < mipCount; i++)
                {
                    int width = Math.Max(1, img.Width >> i);
                    int height = Math.Max(1, img.Height >> i);

                    if (addr != writer.BaseStream.Position)
                        throw new Exception();

                    writer.Seek((int)addr, System.IO.SeekOrigin.Begin);
                    writer.Write(mipmaps[i]);

                    addr += width * height * bpp / 8;
                }
            }
            return mem.ToArray();
        }

        static byte[] Encode(Image<Rgba32> image, bool useAlpha)
        {
            if (useAlpha)
                return RG_ETC1.encodeETCa4(image);
            else
                return RG_ETC1.encodeETC(image);
        }
    }
}
