using IONET.Collada.FX.Texturing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;

namespace CtrLibrary.UI
{
    public class ETC1Compressor
    {
        public static bool IsHighQuality = false;
        public static bool UseEncoder = true;

        public static byte[] Encode(Image<Rgba32> image, int mipCount, bool isAlpha)
        {
            string dir = Runtime.ExecutableDir;
            if (!File.Exists(Path.Combine(dir, "ETC1Compressor.exe")))
            {
                //fall back to in tool compressor (worse quality but faster)
                return EncodeByTool(image, mipCount, isAlpha);
            }

            string intputFile = Path.Combine(dir, "temp.png");
            string outputFile = Path.Combine(dir, "temp.bin");

            string alphaArg = isAlpha ? "-a" : "";
            string quality = IsHighQuality ? "2" : "1";

            image.SaveAsPng(intputFile);

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = $"ETC1Compressor.exe";
            start.WorkingDirectory = dir;
            start.Arguments = $"{AddQuotesIfRequired(intputFile)} -m {Math.Max(mipCount, 1)} {alphaArg} -q {quality}";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;
            start.WindowStyle = ProcessWindowStyle.Hidden;
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    try
                    {
                        var t = reader.ReadToEnd();
                        Console.WriteLine(t);

                        byte[] data = File.ReadAllBytes(outputFile);

                        return data;
                    }
                    catch (Exception ex)
                    {
                        //fall back to in tool compressor (worse quality but faster)
                        return EncodeByTool(image, mipCount, isAlpha);
                    }
                }
            }
        }

        static byte[] EncodeByTool(Image<Rgba32> image, int mipCount, bool isAlpha)
        {
            //fall back to in tool compressor (worse quality but faster)
            if (isAlpha)
                return TextureConverter.Encode(image, PICATextureFormat.ETC1A4, mipCount);
            else
                return TextureConverter.Encode(image, PICATextureFormat.ETC1, mipCount);
        }

        static string AddQuotesIfRequired(string path)
        {
            return !string.IsNullOrWhiteSpace(path) ?
                path.Contains(" ") && (!path.StartsWith("\"") && !path.EndsWith("\"")) ?
                    "\"" + path + "\"" : path :
                    string.Empty;
        }
    }
}
