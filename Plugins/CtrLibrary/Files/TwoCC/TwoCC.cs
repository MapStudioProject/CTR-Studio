using CtrLibrary;
using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.IO;
using UIFramework;

namespace FirstPlugin
{
    public class TwoCC : MapStudio.UI.FileEditor, IArchiveFile, IFileFormat
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { ".pc" };
        public string[] Extension { get; set; } = new string[] { "*.pc" };

        public File_Info FileInfo { get; set; }

        public bool CanAddFiles { get; set; } = true;
        public bool CanRenameFiles { get; set; } = true;
        public bool CanReplaceFiles { get; set; } = true;
        public bool CanDeleteFiles { get; set; } = true;

        public IEnumerable<ArchiveFileInfo> Files => files;

        private List<TFileInfo> files = new List<TFileInfo>();

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            files.Add(new TFileInfo(this)
            {
                FileData = archiveFileInfo.FileData,
                FileName = archiveFileInfo.FileName,
            });
            return true;
        }

        public void ClearFiles() => files.Clear();

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            return files.Remove((TFileInfo)archiveFileInfo);
        }

        private string[] MAGIC = new string[] { "AD", "BB", "BM", "BS", "CM", "CP", "GR", "NA", "MM", "PB", "PC", "PK", "PT", "PF" };

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            using (FileReader reader = new FileReader(stream, true))
                for (int i = 0; i < MAGIC.Length; i++)
                    if (reader.CheckSignature(2, MAGIC[i]))
                        return true;

            return false;
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

        private string Identifier;

        public void Load(Stream stream)
        {
            using (FileReader reader = new FileReader(stream))
            {
                Identifier = reader.ReadMagic(0, 2);
                ushort numSections = reader.ReadUInt16();
                for (int i = 0; i < numSections; i++)
                {
                    reader.Seek(4 + (i * 4), SeekOrigin.Begin);
                    uint startOffset = reader.ReadUInt32();
                    uint endOffset = reader.ReadUInt32();

                    reader.Seek(startOffset, SeekOrigin.Begin);
                    byte[] data = reader.ReadBytes((int)(endOffset - startOffset));

                    string ext = SARC_Parser.GuessFileExtension(data);
                    files.Add(new TFileInfo(this)
                    {
                        FileName = $"File{i}{ext}",
                        IsLZ11 = data.Length > 0 ? data[0] == 0x11 : false,
                        FileData = new MemoryStream(data),
                    });
                }
            }
        }

        public void Save(Stream stream)
        {
            using (FileWriter writer = new FileWriter(stream))
            {
                foreach (var file in files)
                    file.SaveFileFormat();

                writer.WriteSignature(Identifier);
                writer.Write((ushort)files.Count);
                writer.Write(8 * files.Count); //reserved for file start/end offsets

                writer.Align(128);
                for (int i = 0; i < files.Count; i++)
                {
                    long startOffset = writer.Position;

                    using (writer.TemporarySeek(4 + (i * 4), SeekOrigin.Begin))
                    {
                        //Write start and end offsets
                        writer.Write((uint)startOffset);
                        //Last end offset
                        if (i == files.Count - 1)
                            writer.Write((uint)(startOffset + files[i].CompressedStream.Length));
                    }
                    files[i].CompressedStream.CopyTo(writer.BaseStream);
                }
            }
        }

        public class TFileInfo : ArchiveFileInfo
        {
            public TwoCC ArchiveFile;

            public bool IsLZ11;

            public Stream CompressedStream => base.FileData;

            public TFileInfo(TwoCC arc)
            {
                ArchiveFile = arc;
            }

               public override Stream FileData
               {
                   get { return DecompressBlock(); }
                   set
                   {
                       base.FileData = value;
                   }
               }

            private Stream DecompressBlock()
            {
                byte[] data = base.FileData.ToArray();
                return new MemoryStream(data);
            }
        }
    }
}
