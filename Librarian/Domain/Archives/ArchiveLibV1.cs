using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveLibV1 : Archive
    {
        // "LIB" + 0x1A
        private static readonly byte[] IdBytesLib = { 0x4C, 0x49, 0x42, 0x1A };

        public override string ShortTypeName { get { return "Mythos LIB Archive v1"; } }
        public override string ShortTypeDescription { get { return "Mythos LIB v1"; } }
        public override string[] FileExtensions { get { return new string[] { "LIB" }; } }
        public override bool CanSave { get { return true; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            int files = GetFilesCount(loadStream, IdBytesLib);
            return LoadLibArchive(loadStream, files, archivePath);
        }

        protected int GetFilesCount(Stream loadStream, byte[] idBytes)
        {
            loadStream.Position = 0;
            if (loadStream.Length < idBytes.Length + 2)
                throw new FileTypeLoadException("Too short to be a " + ShortTypeDescription + " archive.");
            byte[] testArray = new byte[idBytes.Length];
            loadStream.Read(testArray, 0, testArray.Length);
            if (!testArray.SequenceEqual(idBytes))
                throw new FileTypeLoadException("Not a " + ShortTypeDescription + " archive.");
            int files = loadStream.ReadByte() | (loadStream.ReadByte() << 8);
            if (files == 0)
                throw new FileTypeLoadException("No files in archive.");
            return files;
        }

        protected List<ArchiveEntry> LoadLibArchive(Stream loadStream, int files, string archivePath)
        {
            long end = loadStream.Length;
            int fileEntries = files + 1;
            Encoding enc = Encoding.GetEncoding(437);
            const int fileEntryLength = 0x11;
            byte[] buffer = new byte[fileEntryLength];
            string previousEntryName = null;
            int previousEntryStart = 0;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            //Console.Write("Reading archive entries...");
            for (int i = 0; i < fileEntries; ++i)
            {
                if (loadStream.Position + fileEntryLength > end)
                    throw new FileTypeLoadException("File too short for full header.");
                loadStream.Read(buffer, 0, fileEntryLength);
                string curName = enc.GetString(buffer.Take(13).TakeWhile(x => x != 0).ToArray()).Trim();
                int curEntryStart = (buffer[0x0D]) | (buffer[0x0E] << 8) | (buffer[0x0F] << 0x10) | (buffer[0x10] << 0x18);
                bool isEnd = curEntryStart == end;
                if (curEntryStart > end || curEntryStart < 0)
                    throw new FileTypeLoadException("Archive entry outside file bounds!");
                if (!string.IsNullOrEmpty(previousEntryName) && previousEntryStart != 0)
                {
                    int entryLength = curEntryStart - previousEntryStart;
                    ArchiveEntry archiveEntry = new ArchiveEntry(previousEntryName, archivePath, previousEntryStart, entryLength);
                    filesList.Add(archiveEntry);
                }
                else if (i != 0)
                    throw new FileTypeLoadException("Empty archive entry! Aborting");
                if (isEnd)
                    break;
                previousEntryName = curName;
                previousEntryStart = curEntryStart;
            }
            filesList = filesList.OrderBy(x => x.FileName).ToList();
            return filesList;
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            SaveHeader(archive, saveStream, IdBytesLib);
            return SaveLibArchive(archive, saveStream);
        }

        protected void SaveHeader(Archive archive, Stream saveStream, byte[] idBytes)
        {
            saveStream.Write(idBytes, 0, idBytes.Length);
            byte[] numBuf = new byte[2];
            ArrayUtils.WriteIntToByteArray(numBuf, 0, 2, true, (uint)archive.FilesList.Count);
            saveStream.Write(numBuf, 0, 2);
        }

        protected bool SaveLibArchive(Archive archive, Stream saveStream)
        {
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            const int fileEntryLength = 0x11;
            byte[] buffer = new byte[fileEntryLength];
            Encoding enc = Encoding.GetEncoding(437);
            int curEntryStart = (int)saveStream.Position + (entries.Length + 1) * buffer.Length;
            foreach (ArchiveEntry entry in entries)
            {
                int fileLength = entry.Length;
                if (entry.PhysicalPath != null)
                {
                    FileInfo fi = new FileInfo(entry.PhysicalPath);
                    if (!fi.Exists)
                        throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                    fileLength = (int) fi.Length;
                }
                string curName = GetInternalFilename(entry.FileName);
                int copySize = Math.Min(curName.Length, 12);
                Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                for (int b = copySize; b <= 13; ++b)
                    buffer[b] = 0;
                ArrayUtils.WriteIntToByteArray(buffer, 13, 4, true, (uint) curEntryStart);
                curEntryStart += fileLength;
                saveStream.Write(buffer, 0, fileEntryLength);
            }
            for (int b = 0; b < 13; ++b)
                buffer[b] = 0;
            ArrayUtils.WriteIntToByteArray(buffer, 13, 4, true, (uint)curEntryStart);
            saveStream.Write(buffer, 0, fileEntryLength);
            foreach (ArchiveEntry entry in entries)
                CopyEntryContentsToStream(entry, saveStream);
            return true;
        }

    }
}