using System;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.Util;

namespace LibrarianTool.Domain
{
    public class ArchiveLibV1 : Archive
    {
        // "LIB" + 0x1A
        private static readonly Byte[] IdBytesLib = { 0x4C, 0x49, 0x42, 0x1A };

        public override String ShortTypeName { get { return "Mythos LIB Archive v1"; } }
        public override String ShortTypeDescription { get { return "Mythos LIB v1"; } }
        public override String[] FileExtensions { get { return new String[] { "LIB" }; } }


        public override Boolean LoadArchive(Stream loadStream, String archivePath)
        {
            this.FileName = archivePath;
            Int32 files = GetFilesCount(loadStream, IdBytesLib);
            if (files == -1)
                return false;
            return this.LoadArchive(loadStream, files, archivePath);
        }

        protected Int32 GetFilesCount(Stream loadStream, Byte[] idBytes)
        {
            loadStream.Position = 0;
            if (loadStream.Length < idBytes.Length + 2)
                return -1;
            Byte[] testArray = new Byte[idBytes.Length];
            loadStream.Read(testArray, 0, testArray.Length);
            if (!testArray.SequenceEqual(idBytes))
                return -1;
            return loadStream.ReadByte() | (loadStream.ReadByte() << 8);
        }

        protected Boolean LoadArchive(Stream loadStream, Int32 files, String archivePath)
        {
            this.FileName = archivePath;
            Int64 end = loadStream.Length;
            Int32 fileEntries = files + 1;
            Encoding enc = Encoding.GetEncoding(437);
            const Int32 fileEntryLength = 0x11;
            Byte[] buffer = new Byte[fileEntryLength];
            String previousEntryName = null;
            Int32 previousEntryStart = 0;
            this._filesList.Clear();

            //Console.Write("Reading archive entries...");
            for (Int32 i = 0; i < fileEntries; i++)
            {
                loadStream.Read(buffer, 0, fileEntryLength);
                String curName = enc.GetString(buffer.Take(13).TakeWhile(x => x != 0).ToArray()).Trim();
                Int32 curEntryStart = (buffer[0x0D]) | (buffer[0x0E] << 8) | (buffer[0x0F] << 0x10) | (buffer[0x10] << 0x18);
                Boolean isEnd = curEntryStart == end;
                if (curEntryStart > end || curEntryStart < 0)
                {
                    //Console.WriteLine("Archive entry outside file bounds!");
                    return false;
                }
                if (!String.IsNullOrEmpty(previousEntryName) && previousEntryStart != 0)
                {
                    Int32 entryLength = curEntryStart - previousEntryStart;
                    ArchiveEntry archiveEntry = new ArchiveEntry(previousEntryName, archivePath, previousEntryStart, entryLength);
                    this._filesList.Add(archiveEntry);
                }
                else if (i != 0)
                {
                    // Console.WriteLine("Empty archive entry! Aborting");
                    return false;
                }
                if (isEnd)
                    break;
                previousEntryName = curName;
                previousEntryStart = curEntryStart;
            }
            this._filesList = this._filesList.OrderBy(x => x.FileName).ToList();
            return true;
        }

        public override Boolean SaveArchive(Archive archive, Stream saveStream)
        {
            SaveHeader(archive, saveStream, IdBytesLib);
            return SaveFiles(archive, saveStream);
        }

        protected void SaveHeader(Archive archive, Stream saveStream, Byte[] idBytes)
        {
            saveStream.Write(idBytes, 0, idBytes.Length);
            Byte[] numBuf = new Byte[2];
            ArrayUtils.WriteIntToByteArray(numBuf, 0, 2, true, (UInt32)archive.FilesList.Count);
            saveStream.Write(numBuf, 0, 2);
        }

        protected Boolean SaveFiles(Archive archive, Stream saveStream)
        {
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            const Int32 fileEntryLength = 0x11;
            Byte[] buffer = new Byte[fileEntryLength];
            Encoding enc = Encoding.GetEncoding(437);
            Int32 curEntryStart = (Int32)saveStream.Position + (entries.Length + 1) * buffer.Length;
            foreach (ArchiveEntry entry in entries)
            {
                Int32 fileLength = entry.Length;
                if (entry.PhysicalPath != null)
                {
                    FileInfo fi = new FileInfo(entry.PhysicalPath);
                    if (!fi.Exists)
                        return false;
                    fileLength = (Int32) fi.Length;
                }
                String curName = new String(entry.FileName.ToUpperInvariant().Where(x => x >= ' ' && ((Byte)x) < 128).ToArray());
                Int32 copySize = Math.Min(curName.Length, 12);
                Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                for (Int32 b = copySize; b < 13; b++)
                    buffer[b] = 0;
                ArrayUtils.WriteIntToByteArray(buffer, 13, 4, true, (UInt32) curEntryStart);
                curEntryStart += fileLength;
                saveStream.Write(buffer, 0, fileEntryLength);
            }
            for (Int32 b = 0; b < 13; b++)
                buffer[b] = 0;
            ArrayUtils.WriteIntToByteArray(buffer, 13, 4, true, (UInt32)curEntryStart);
            saveStream.Write(buffer, 0, fileEntryLength);
            foreach (ArchiveEntry entry in entries)
                this.CopyEntryContentsToStream(entry, saveStream);
            return true;
        }

        public override Boolean ExtractFile(String filename, String savePath)
        {
            ArchiveEntry entry = this._filesList.FirstOrDefault(e => filename.Equals(e.FileName, StringComparison.InvariantCultureIgnoreCase));
            if (entry == null)
                return false;
            using (FileStream fs = new FileStream(savePath, FileMode.Create))
                this.CopyEntryContentsToStream(entry, fs);
            return true;
        }

        protected void CopyEntryContentsToStream(ArchiveEntry entry, Stream saveStream)
        {
            String readFile;
            Int32 start;
            Int32 length;
            if (entry.PhysicalPath != null)
            {
                readFile = entry.PhysicalPath;
                start = 0;
                FileInfo fi = new FileInfo(entry.PhysicalPath);
                length = (Int32)fi.Length;
            }
            else
            {
                readFile = entry.ArchivePath;
                start = entry.StartOffset;
                length = entry.Length;
            }
            using (FileStream fs = new FileStream(readFile, FileMode.Open))
            {
                fs.Seek(start, SeekOrigin.Begin);
                CopyStream(fs, saveStream, length);
            }
        }
    }
}