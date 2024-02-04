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
using SPICA.Formats.CtrH3D.Texture;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;
using Toolbox.Core.Hashes.Cryptography;
using static Toolbox.Core.Decode_Gamecube;

namespace CtrLibrary
{
    public class MTXT : MapStudio.UI.FileEditor, IFileFormat
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "MTXT" };
        public string[] Extension { get; set; } = new string[] { "*.mtxt" };

        public File_Info FileInfo { get; set; }

        public Header FileHeader;

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            using (FileReader reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "MTXT");
            }
        }

        public void Load(Stream stream)
        {
            using (FileReader reader = new FileReader(stream))
            {
                FileHeader = reader.ReadStruct<Header>();
                reader.SeekBegin(FileHeader.DataOffset);
                var data = reader.ReadBytes(FileHeader.CTPKSize);

                CTPK ctpk = new CTPK();
                ctpk.Load(new MemoryStream(data));
                foreach (var child in ctpk.Root.Children)
                    Root.AddChild(child);
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

        public void Save(Stream stream)
        {
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public Magic Magic = "MTXT";
            public short VersionMajor;
            public short VersionMinor;
            public int TextureFormat;
            public int Width;
            public int Height;
            public int MipmapCount;
            public int NameOffset;
            public int DataOffset;//minus header
            public int CTPKSize;
        }
    }
}
