using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using MapStudio.UI;
using GLFrameworkEngine;
using CtrLibrary.Bcres;
using CtrLibrary.Bch;

namespace CtrLibrary
{
    /// <summary>
    /// Represents a plugin used to activate this library and load the editor contents.
    /// </summary>
    public class Plugin : IPlugin
    {
        /// <summary>
        /// The name of the plugin.
        /// </summary>
        public string Name => "3DS Editor";

        public Plugin()
        {
            //Add File -> New Bcres option
            UIManager.Subscribe(UIManager.UI_TYPE.NEW_FILE, "Bcres File", typeof(BCRES));
            UIManager.Subscribe(UIManager.UI_TYPE.NEW_FILE, "BCH File", typeof(BCH));
        }
    }
}
