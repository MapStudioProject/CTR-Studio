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
    public class DARC : MapStudio.UI.FileEditor, IArchiveFile, IFileFormat
    {
        public bool CanSave { get; set; } = false;

        public string[] Description { get; set; } = new string[] { "DARC" };
        public string[] Extension { get; set; } = new string[] { "*.darc" };

        public File_Info FileInfo { get; set; }

        public bool CanAddFiles { get; set; } = true;
        public bool CanRenameFiles { get; set; } = true;
        public bool CanReplaceFiles { get; set; } = true;
        public bool CanDeleteFiles { get; set; } = true;

        public IEnumerable<ArchiveFileInfo> Files => files;

        private List<FileEntry> files = new List<FileEntry>();

        private ushort Bom;
        private uint Version;

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
            using (FileReader reader = new FileReader(stream, true))
                return reader.CheckSignature(4, "darc");
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
            files.Clear();

            FileInfo.Stream = stream;
            FileInfo.KeepOpen = true;   
            using (FileReader reader = new FileReader(stream, true))
            {
                reader.ByteOrder = Syroot.BinaryData.ByteOrder.BigEndian;
                reader.ReadSignature(4, "darc");
                Bom = reader.ReadUInt16();
                reader.CheckByteOrderMark(Bom);
                ushort headerLength = reader.ReadUInt16();

                Version = reader.ReadUInt32();
                uint FileSize = reader.ReadUInt32();
                uint FileTableOffset = reader.ReadUInt32();
                uint FileTableLength = reader.ReadUInt32();
                uint FileDataOffset = reader.ReadUInt32();

                uint endOfTable = FileDataOffset + FileTableLength;

                List<NodeEntry> entries = new List<NodeEntry>();
                reader.SeekBegin(FileTableOffset);
                entries.Add(new NodeEntry(reader));
                for (int i = 0; i < entries[0].Size - 1; i++)
                    entries.Add(new NodeEntry(reader));

                for (int i = 0; i < entries.Count; i++)
                    entries[i].Name = ReadCStringW(reader);

                for (int i = 0; i < entries.Count; i++)
                {
                    string Name = entries[i].Name;
                    if (entries[i].IsFolder)
                    {
                        for (int s = 0; s < entries[i].Size; s++)
                            entries[s].FullName += $"{Name}/";
                    }
                    else
                        entries[i].FullName += Name;
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    if (!entries[i].IsFolder)
                    {
                        var file = new FileEntry();
                        file.FileName = entries[i].FullName;
                        file.FileData = new SubStream(reader.BaseStream, entries[i].Offset, entries[i].Size);
                        files.Add(file);
                    }
                }
            }
        }

        public string ReadCStringW(FileReader reader) => string.Concat(
            Enumerable.Range(0, 999).Select(_ => (char)reader.ReadInt16()).TakeWhile(c => c != 0));

        public void Save(Stream stream)
        {
            using (FileWriter writer = new FileWriter(stream))
            {
                
            }
        }

        public class NodeEntry
        {
            public uint NameOffset;
            public uint Size;
            public uint Offset;

            public bool IsFolder => (NameOffset >> 24) == 1;

            public string Name;

            public string FullName;

            public NodeEntry(FileReader reader)
            {
                NameOffset = reader.ReadUInt32();
                Offset = reader.ReadUInt32();
                Size = reader.ReadUInt32();
            }
        }

        public class FileEntry : ArchiveFileInfo
        {
        }
    }
}
