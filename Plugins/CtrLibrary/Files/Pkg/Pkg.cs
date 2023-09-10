using CtrLibrary;
using Toolbox.Core;
using Toolbox.Core.IO;
using UIFramework;

namespace FirstPlugin
{
    public class Pkg : MapStudio.UI.FileEditor, IArchiveFile, IFileFormat
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "Pkg" };
        public string[] Extension { get; set; } = new string[] { "*.pkg" };

        public File_Info FileInfo { get; set; }

        public bool CanAddFiles { get; set; } = true;
        public bool CanRenameFiles { get; set; } = true;
        public bool CanReplaceFiles { get; set; } = true;
        public bool CanDeleteFiles { get; set; } = true;

        public IEnumerable<ArchiveFileInfo> Files => files;

        private List<FileEntry> files = new List<FileEntry>();

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            files.Add(new FileEntry()
            {
                FileData = archiveFileInfo.FileData,
                FileName = archiveFileInfo.FileName,
            });
            return true;
        }

        public void ClearFiles() => files.Clear();

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            return files.Remove((FileEntry)archiveFileInfo);
        }

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            return fileInfo.Extension == ".pkg";
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

        public void Load(Stream stream)
        {
            using (FileReader reader = new FileReader(stream))
            {
                uint headerSize = reader.ReadUInt32();
                uint dataSize = reader.ReadUInt32();
                uint fileCount = reader.ReadUInt32();
                for (int i = 0; i < fileCount; i++)
                {
                    uint hash = reader.ReadUInt32();
                    uint startOffset = reader.ReadUInt32();
                    uint endOffset = reader.ReadUInt32();
                    uint size = endOffset - startOffset;

                    using (reader.TemporarySeek(startOffset, SeekOrigin.Begin))
                    {
                        var data = reader.ReadBytes((int)size); 
                        files.Add(new FileEntry()
                        {
                            FileData = new MemoryStream(data),
                            FileName = hash.ToString("X"),
                        });
                        
                    }
                }
            }
        }

        public void Save(Stream stream)
        {
            using (var writer = new FileWriter(stream))
            {
                writer.Write(uint.MaxValue); //header size
                writer.Write(uint.MaxValue); //file size
                writer.Write(files.Count);
                for (int i = 0; i < files.Count; i++)
                {
                    writer.Write(files[i].Hash);
                    writer.Write(uint.MaxValue); //start offset
                    writer.Write(uint.MaxValue); //end offset
                }
                writer.Align(128);

                writer.WriteUint32Offset(0, 4); //Size of header - 4

                long dataPos = writer.Position;
                for (int i = 0; i < files.Count; i++)
                {
                    files[i].SaveFileFormat();

                    writer.WriteUint32Offset(16 + (i * 12)); //start offset
                    files[i].FileData.CopyTo(writer.BaseStream);
                    writer.WriteUint32Offset(20 + (i * 12)); //end offset

                    writer.Align(128);
                }

                //write data size
                using (writer.TemporarySeek(4, SeekOrigin.Begin))
                {
                    writer.Write((int)(writer.BaseStream.Length - dataPos));
                }
            }
        }


        public class FileEntry : ArchiveFileInfo
        {
            public uint Hash
            {
                get
                {
                    return uint.Parse(FileName, System.Globalization.NumberStyles.HexNumber);
                }
            }
        }
    }
}
