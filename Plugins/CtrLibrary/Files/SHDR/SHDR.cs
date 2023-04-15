using CtrLibrary;
using SPICA.PICA.Shader;
using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.ViewModels;
using UIFramework;
using CtrLibrary.UI;

namespace CtrLibrary
{
    public class SHDR : MapStudio.UI.FileEditor, IFileFormat
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { ".shbin" };
        public string[] Extension { get; set; } = new string[] { "*.shbin" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            using (FileReader reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "DVLB");
            }
        }

        /// <summary>
        /// Prepares the dock layouts to be used for the file format.
        /// </summary>
        public override List<DockWindow> PrepareDocks()
        {
            List<DockWindow> windows = new List<DockWindow>();
            windows.Add(Workspace.Outliner);
            windows.Add(Workspace.PropertyWindow);
            return windows;
        }

        ShaderBinary ShaderBinary;

        public void Load(Stream stream)
        {
            ShaderBinary = new ShaderBinary(stream.ReadAllBytes());

            int vertexIndex = 0;

            foreach (var prog in ShaderBinary.Programs)
            {
                NodeBase proNode = new NodeBase($"Program {Root.Children.Count}");

                //Index of current program
                var programIndex = ShaderBinary.Programs.IndexOf(prog);
                //Set current geom index if program is geometry
                var geomIndex = prog.IsGeometryShader ? programIndex : -1;
                //If geometry use last vertex ID and assume it is used in combination

                var ShaderUI = new ShaderUI(ShaderBinary, !prog.IsGeometryShader ? programIndex : vertexIndex - 1, geomIndex);
                proNode.TagUI.UIDrawer += delegate
                {
                    ShaderUI.Render();
                };

                Root.AddChild(proNode);

                if (!prog.IsGeometryShader)
                    vertexIndex++;
            }
        }

        public void Save(Stream stream)
        {

        }
    }
}
