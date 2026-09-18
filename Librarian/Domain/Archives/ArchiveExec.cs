using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveExec : Archive
    {
        public override string ShortTypeName { get { return "Executioners Archive"; } }
        public override string ShortTypeDescription { get { return "Executioners Archive"; } }
        public override string[] FileExtensions { get { return new string[] { "VOL" }; } }
        public override bool CanSave { get { return true; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            uint end = (uint)loadStream.Length;
            if (end < 2)
                throw new FileTypeLoadException("Archive not long enough for header.");
            Encoding enc = new ASCIIEncoding();
            loadStream.Position = 0;
            using (BinaryReader br = new BinaryReader(new NonDisposingStream(loadStream)))
            {
                ushort headerSize = br.ReadUInt16();
                if (headerSize == 0 || headerSize % 16 != 0)
                    throw new FileTypeLoadException("Data does not match expected structure.");
                if (end < headerSize)
                    throw new FileTypeLoadException("Archive not long enough for header.");
                bool isFirst = true;
                int files = headerSize / 16;
                List<ArchiveEntry> filesList = new List<ArchiveEntry>();
                for (int i = 0; i < files; ++i)
                {
                    byte[] nameBuffer = new byte[8];
                    loadStream.Read(nameBuffer, 0, 8);
                    string curName = enc.GetString(nameBuffer.TakeWhile(x => x != 0).ToArray()).TrimEnd();
                    if (curName.Length == 0)
                        throw new FileTypeLoadException("Empty entry in files list.");
                    curName += ".VOL";
                    int filePos = (int)br.ReadUInt32();
                    if (isFirst)
                    {
                        if (filePos != headerSize + 2)
                            throw new FileTypeLoadException("Data does not match expected structure.");
                        isFirst = false;
                    }
                    if (filePos > end)
                        throw new FileTypeLoadException("Entry is outside file bounds.");
                    int fileLen = (int)br.ReadUInt32();
                    if (filePos + fileLen > end)
                        throw new FileTypeLoadException("Entry is outside file bounds.");
                    ArchiveEntry file = new ArchiveEntry(curName, archivePath, filePos, fileLen);
                    filesList.Add(file);
                }
                return filesList;
            }
        }

        public override string GetInternalFilename(string filePath)
        {
            string filename = base.GetInternalFilename(filePath);
            return Path.GetFileNameWithoutExtension(filename) + ".VOL";
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            List<ArchiveEntry> filesList = archive.FilesList;
            int entries = filesList.Count;
            byte[] nameBuff = new byte[8];
            Encoding enc = new ASCIIEncoding();
            int headerLength = entries * 16;
            int firstFileOffset = 2 + headerLength;
            int currentOffs = firstFileOffset;
            using (BinaryWriter bw = new BinaryWriter(new NonDisposingStream(saveStream)))
            {
                bw.Write((ushort)headerLength);
                for (int i = 0; i < entries; ++i)
                {
                    ArchiveEntry entry = filesList[i];
                    string filename = Path.GetFileNameWithoutExtension(entry.FileName);
                    for (int j = 0; j < 8; ++j)
                        nameBuff[j] = 0x20;
                    enc.GetBytes(filename, 0, filename.Length, nameBuff, 0);
                    bw.Write(nameBuff, 0, 8);
                    int fileLength = entry.Length;
                    if (entry.PhysicalPath != null)
                    {
                        FileInfo fi = new FileInfo(entry.PhysicalPath);
                        if (!fi.Exists)
                            throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                        fileLength = (int)fi.Length;
                    }
                    bw.Write((uint)currentOffs);
                    bw.Write((uint)fileLength);
                    currentOffs += fileLength;
                }
                if (saveStream.Position != firstFileOffset)
                    throw new IndexOutOfRangeException("Programmer error: write start offset does not match end of index.");
                foreach (ArchiveEntry entry in filesList)
                    CopyEntryContentsToStream(entry, saveStream);
                return true;
            }
        }
    }
}
