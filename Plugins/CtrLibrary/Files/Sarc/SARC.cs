using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.IO;
using System.IO;
using MapStudio.UI;
using UIFramework;

namespace CtrLibrary
{
    public class SARC : FileEditor, IFileFormat, IArchiveFile, IDisposable
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "SARC" };
        public string[] Extension { get; set; } = new string[] { "*.sarc", "*.szs" };

        public File_Info FileInfo { get; set; }

        public bool CanAddFiles { get; set; } = true;
        public bool CanRenameFiles { get; set; } = true;
        public bool CanReplaceFiles { get; set; } = true;
        public bool CanDeleteFiles { get; set; } = true;

        public List<ArchiveFileInfo> files = new List<ArchiveFileInfo>();
        public IEnumerable<ArchiveFileInfo> Files => files;

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "SARC");
            }
        }

        public bool BigEndian => SarcData.endianness == Syroot.BinaryData.ByteOrder.BigEndian;

        public SarcData SarcData;

        public SARC()
        {
            FileInfo = new File_Info();
            SarcData = new SarcData()
            {
                endianness = Syroot.BinaryData.ByteOrder.LittleEndian,
                Files = new Dictionary<string, byte[]>(),
            };
        }

        public static byte[] GetFile(string sarcPath, string file)
        {
            Stream stream = File.OpenRead(sarcPath);
            if (YAZ0.IsCompressed(sarcPath))
                stream = new MemoryStream(YAZ0.Decompress(sarcPath));

            var sarc = SARC_Parser.UnpackRamN(stream);
            return sarc.Files[file];
        }

        public void Load(System.IO.Stream stream) {
            files.Clear();
            SarcData = SARC_Parser.UnpackRamN(stream);
            foreach (var file in SarcData.Files)
            {
                var fileEntry = new FileEntry();
                fileEntry.FileName = file.Key;
                if (SarcData.HashOnly)
                {
                    fileEntry.FileName = SARC_Parser.TryGetNameFromHashTable(file.Key);
                    fileEntry.HashName = file.Key;
                }
                fileEntry.SetData(file.Value);
                files.Add(fileEntry);
            }
            files = files.OrderBy(x => x.FileName).ToList();
        }

        public void SetFileData(string key, Stream stream) {
            SarcData.Files[key] = stream.ToArray();
        }

        public void ClearFiles() { files.Clear(); }

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            files.Add(new FileEntry()
            {
                FileData = archiveFileInfo.FileData,
                FileName = archiveFileInfo.FileName,
            });
            return true;
        }

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            files.Remove((FileEntry)archiveFileInfo);
            return true;
        }

        public void Save(System.IO.Stream stream)
        {
            SarcData.Files.Clear();
            foreach (FileEntry file in this.files)
            {
                file.SaveFileFormat();

                if (SarcData.HashOnly)
                    SarcData.Files.Add(file.HashName, file.AsBytes());
                else
                    SarcData.Files.Add(file.FileName, file.AsBytes());
            }

            //Save data to stream
            var saved = SARC_Parser.PackN(SarcData);
            using (var writer = new FileWriter(stream)) {
                writer.Write(saved.Item2);
            }

            //Save alignment to compression type yaz0
            if (FileInfo.Compression != null && FileInfo.Compression is Yaz0) {
                ((Yaz0)FileInfo.Compression).Alignment = saved.Item1;
            }
        }

        public void Dispose()
        {
            foreach (var file in files)
            {
                if (file.FileFormat != null && file.FileFormat is IDisposable)
                    ((IDisposable)file.FileFormat).Dispose();
                file.FileData?.Dispose();
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

        public class FileEntry : ArchiveFileInfo
        {
            /// <summary>
            /// The hash calculated in hex format turned into a string used for hash only files.
            /// </summary>
            public string HashName
            {
                get
                {
                    if (hashName == null)
                        hashName = SARC_Parser.NameHash(FileName).ToString("X8");

                    return hashName;
                }
                set { hashName = value; }
            }
            private string hashName;
        }
    }
}
