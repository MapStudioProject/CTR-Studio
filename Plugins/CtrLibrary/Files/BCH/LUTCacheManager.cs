using CtrLibrary.Bch;
using CtrLibrary.Bcres;
using CtrLibrary.Rendering;
using IONET.Collada.FX.Rendering;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.LUT;
using SPICA.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;

namespace CtrLibrary.UI
{
    internal class LUTCacheManager
    {
        /// <summary>
        /// The texture cache of globally loaded LUTs. This cache is only used for UI purposes to get the H3D instances for viewing.
        /// </summary>v
        public static Dictionary<string, H3DLUT> Cache = new Dictionary<string, H3DLUT>();

        static bool loaded = false;

        public static void Setup(Renderer renderer)
        {
            if (loaded)
                return;

            loaded = true;

            string lutDir = Path.Combine(Toolbox.Core.Runtime.ExecutableDir, "LUTS");
            if (Directory.Exists(lutDir))
            {
                foreach (var lut in Directory.GetFiles(lutDir))
                {
                    var file = STFileLoader.OpenFileFormat(lut);
                    if (file == null)
                        continue;

                    Console.WriteLine($"Loading file {lut}");

                    if (file is BCRES)
                        CacheLUTs(renderer, ((BCRES)file).BcresData.ToH3D());
                    if (file is BCH)
                        CacheLUTs(renderer, ((BCH)file).H3DData);
                }
            }
        }

        static void CacheLUTs(Renderer renderer, H3D h3D)
        {
            foreach (var l in h3D.LUTs)
            {
                if (!Cache.ContainsKey(l.Name))
                    Cache.Add(l.Name, l);
            }
            if (h3D.LUTs.Any(x => renderer.LUTs.ContainsKey(x.Name)))
                return;

            renderer.Merge(h3D);
        }
    }
}
