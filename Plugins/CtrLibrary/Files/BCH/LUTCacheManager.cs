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

        public static void Setup(bool force = false)
        {
            if (loaded && !force)
                return;

            loaded = true;
            Cache.Clear();

            string lutDir = Path.Combine(Toolbox.Core.Runtime.ExecutableDir, "LUTS");
            if (Directory.Exists(lutDir))
            {
                foreach (var lut in Directory.GetFiles(lutDir))
                {
                    using (var reader = new FileReader(lut))
                    {
                        if (reader.CheckSignature(3, "BCH"))
                            CacheLUTs(H3D.Open(File.ReadAllBytes(lut)));
                        if (reader.CheckSignature(4, "CGFX"))
                            CacheLUTs(Gfx.Open(lut).ToH3D());
                    }
                }
            }
        }

        public static void Load(Renderer renderer)
        {
            if (Cache.Any(x => renderer.LUTs.ContainsKey(x.Key)))
                return;

            H3DDict<H3DLUT> dict = new H3DDict<H3DLUT>();
            foreach (var lut in Cache.Values)
                dict.Add(lut);

            renderer.Merge(dict);
        }

        static void CacheLUTs(H3D h3D)
        {
            foreach (var l in h3D.LUTs)
            {
                if (!Cache.ContainsKey(l.Name))
                    Cache.Add(l.Name, l);
            }
        }
    }
}
