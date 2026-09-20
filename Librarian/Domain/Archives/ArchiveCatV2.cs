using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    class ArchiveCatV2 : Archive
    {
        protected const int FileEntryLength = 0x18;

        public override string ShortTypeName { get { return "MPS Labs Catalog v2"; } }
        public override string ShortTypeDescription { get { return "MPS Labs Catalog v2"; } }
        public override string[] FileExtensions { get { return new string[] { "cat" }; } }
        public override bool CanSave { get { return true; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            long end = loadStream.Length;
            byte[] buffer = new byte[FileEntryLength];
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            if (end - loadStream.Position < 0x02)
                throw new FileTypeLoadException("Not a CAT v2 Archive.");
            loadStream.Read(buffer, 0, 2);
            int nrOfFiles = (int)ArrayUtils.ReadIntFromByteArray(buffer, 0, 2, true);
            if (nrOfFiles == 0 || end - loadStream.Position < nrOfFiles * FileEntryLength)
                throw new FileTypeLoadException("Not a CAT v2 Archive.");
            for (int i = 0; i < nrOfFiles; ++i)
            {
                loadStream.Read(buffer, 0, FileEntryLength);
                byte[] curNameB = buffer.Take(0x0C).TakeWhile(x => x != 0).ToArray();
                if (curNameB.Length == 0)
                    break;
                if (curNameB.Any(c => c < 0x20 || c >= 0x7F))
                    throw new FileTypeLoadException("Filename contains nonstandard characters.");
                string curName = enc.GetString(curNameB).Trim();
                ushort dosTime = (ushort)ArrayUtils.ReadIntFromByteArray(buffer, 0x0C, 2, true);
                ushort dosDate = (ushort)ArrayUtils.ReadIntFromByteArray(buffer, 0x0E, 2, true);
                DateTime dt;
                try
                {
                    dt = GeneralUtils.GetDosDateTime(dosTime, dosDate);
                }
                catch (ArgumentException argex)
                {
                    throw new FileTypeLoadException(argex.Message, argex);
                }
                string extraInfo = GeneralUtils.GetDateString(dt);
                int curEntryLength = (int)ArrayUtils.ReadIntFromByteArray(buffer, 0x10, 4, true);
                int curEntryPos = (int)ArrayUtils.ReadIntFromByteArray(buffer, 0x14, 4, true);
                if (curEntryPos + curEntryLength > end)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");
                if (curName.Length == 0 && curEntryLength == 0)
                    continue;
                ArchiveEntry entry = new ArchiveEntry(curName, archivePath, curEntryPos, curEntryLength, extraInfo);
                entry.Date = dt;
                filesList.Add(entry);
            }
            return filesList;
        }

        /// <summary>Inserts a file into the archive. This can be overridden to add filtering on the input.</summary>
        /// <param name="filePath">Path of the file to load.</param>
        public override ArchiveEntry InsertFile(string filePath, int insertIndex)
        {
            ArchiveEntry file = base.InsertFile(filePath, insertIndex);
            DateTime lastMod = file.Date ?? File.GetLastWriteTime(filePath);
            file.ExtraInfo = GeneralUtils.GetDateString(lastMod);
            return file;
        }


        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            DateTime writeDate = DateTime.Now;
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            int nrOfFiles = entries.Length;
            byte[] buffer = new byte[FileEntryLength];
            Encoding enc = Encoding.GetEncoding(437);
            int curEntryStart = nrOfFiles * buffer.Length + 2;
            // Write amount of files in table
            ArrayUtils.WriteIntToByteArray(buffer, 0, 2, true, (uint)nrOfFiles);
            saveStream.Write(buffer, 0, 2);
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
                    fileLength = (int)fi.Length;
                }
                string curName = GetInternalFilename(entry.FileName);
                int copySize = Math.Min(curName.Length, 12);
                Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                for (int b = copySize; b < 12; ++b)
                    buffer[b] = 0;
                DateTime dt = entry.Date ?? writeDate;
                ushort time = GeneralUtils.GetDosTimeInt(dt);
                ushort date = GeneralUtils.GetDosDateInt(dt);
                ArrayUtils.WriteIntToByteArray(buffer, 0x0C, 2, true, time);
                ArrayUtils.WriteIntToByteArray(buffer, 0x0E, 2, true, date);
                ArrayUtils.WriteIntToByteArray(buffer, 0x10, 4, true, (uint)fileLength);
                ArrayUtils.WriteIntToByteArray(buffer, 0x14, 4, true, (uint)curEntryStart);
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