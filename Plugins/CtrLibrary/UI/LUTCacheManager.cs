using CtrLibrary.Rendering;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrH3D.LUT;
using SPICA.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CtrLibrary.UI
{
    internal class LUTCacheManager
    {
        /// <summary>
        /// The texture cache of globally loaded LUTs. This cache is only used for UI purposes to get the H3D instances for viewing.
        /// </summary>v
        public static Dictionary<string, H3DLUT> Cache = new Dictionary<string, H3DLUT>();

        public static void Setup(Renderer renderer)
        {
            string shaderDir = Path.Combine(Toolbox.Core.Runtime.ExecutableDir, "Shaders");

            string lutDir = Path.Combine(Toolbox.Core.Runtime.ExecutableDir, "LUTS");
            if (Directory.Exists(lutDir))
            {
                foreach (var lut in Directory.GetFiles(lutDir))
                {
                    var luts = Gfx.Open(lut).ToH3D();
                    foreach (var l in luts.LUTs)
                    {
                        if (!Cache.ContainsKey(l.Name))
                            Cache.Add(l.Name, l);
                    }
                    if (luts.LUTs.Any(x => renderer.LUTs.ContainsKey(x.Name)))
                        continue;

                    renderer.Merge(luts);
                }
            }
        }
    }
}
