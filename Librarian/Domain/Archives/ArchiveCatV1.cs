using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    class ArchiveCatV1 : Archive
    {
        protected const int FileEntryLength = 0x12;

        public override string ShortTypeName { get { return "MPS Labs Catalog v1"; } }
        public override string ShortTypeDescription { get { return "MPS Labs Catalog v1"; } }
        public override string[] FileExtensions { get { return new string[] { "cat" }; } }
        public override bool CanSave { get { return true; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            long end = loadStream.Length;
            byte[] buffer = new byte[FileEntryLength];
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            if (end - loadStream.Position < 0x02)
                throw new FileTypeLoadException("Not a CAT v1 Archive.");
            loadStream.Read(buffer, 0, 2);
            int fatlength = (int)ArrayUtils.ReadIntFromByteArray(buffer, 0, 2, true);
            if (fatlength == 0 || end - loadStream.Position < fatlength)
                throw new FileTypeLoadException("Not a CAT v1 Archive.");
            if (fatlength % FileEntryLength != 0)
                throw new FileTypeLoadException("Not a CAT v1 Archive.");
            int nrOfFiles = fatlength / FileEntryLength;
            for (int i = 0; i < nrOfFiles; ++i)
            {
                loadStream.Read(buffer, 0, FileEntryLength);
                byte[] curNameB = buffer.Take(0x0C).TakeWhile(x => x != 0).ToArray();
                if (curNameB.Length == 0)
                    break;
                if (curNameB.Any(c => c < 0x20 || c >= 0x7F))
                    throw new FileTypeLoadException("Filename contains nonstandard characters.");
                string curName = enc.GetString(curNameB).Trim();
                int curEntryPos = (int)ArrayUtils.ReadIntFromByteArray(buffer, 0x0C, 4, true);
                int curEntryLength = (short)ArrayUtils.ReadIntFromByteArray(buffer, 0x10, 2, true);
                if (curEntryPos + curEntryLength > end)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");
                if (curName.Length == 0 && curEntryLength == 0)
                    continue;
                filesList.Add(new ArchiveEntry(curName, archivePath, curEntryPos, curEntryLength));
            }
            return filesList;
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            byte[] buffer = new byte[FileEntryLength];
            Encoding enc = Encoding.GetEncoding(437);
            int curEntryStart = entries.Length * buffer.Length;
            // Write files table size
            ArrayUtils.WriteIntToByteArray(buffer, 0, 2, true, (uint)curEntryStart);
            saveStream.Write(buffer, 0, 2);
            curEntryStart += 2;
            // Write files table
            for (int i = 0; i < entries.Length; ++i)
            {
                ArchiveEntry entry = entries[i];
                int fileLength = entry.Length;
                if (entry.PhysicalPath != null)
                {
                    FileInfo fi = new FileInfo(entry.PhysicalPath);
                    if (!fi.Exists)
                        throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                    fileLength = (int) fi.Length;
                    if (fileLength > ushort.MaxValue)
                        throw new ArgumentException("The file \"" + entry.PhysicalPath + "\" is too large to write to this type of archive!");
                }
                string curName = GetInternalFilename(entry.FileName);
                int copySize = Math.Min(curName.Length, 12);
                Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                for (int b = copySize; b < 12; ++i)
                    buffer[b] = 0;
                ArrayUtils.WriteIntToByteArray(buffer, 0x0C, 4, true, (uint) curEntryStart);
                ArrayUtils.WriteIntToByteArray(buffer, 0x10, 2, true, (uint) fileLength);
                curEntryStart += fileLength;
                saveStream.Write(buffer, 0, FileEntryLength);
            }
            // Write files
            foreach (ArchiveEntry entry in entries)
                CopyEntryContentsToStream(entry, saveStream);
            return true;
        }
    }
}