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
        protected const Int32 FileEntryLength = 0x18;

        public override String ShortTypeName { get { return "MPS Labs Catalog v2"; } }
        public override String ShortTypeDescription { get { return "MPS Labs Catalog v2"; } }
        public override String[] FileExtensions { get { return new String[] { "cat" }; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, String archivePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            Int64 end = loadStream.Length;
            Byte[] buffer = new Byte[FileEntryLength];
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            if (end - loadStream.Position < 0x02)
                throw new FileTypeLoadException("Not a CAT v2 Archive.");
            loadStream.Read(buffer, 0, 2);
            Int32 nrOfFiles = (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0, 2, true);
            if (end - loadStream.Position < nrOfFiles * FileEntryLength)
                throw new FileTypeLoadException("Not a CAT v2 Archive.");
            for (Int32 i = 0; i < nrOfFiles; i++)
            {
                loadStream.Read(buffer, 0, FileEntryLength);
                Byte[] curNameB = buffer.Take(0x0C).TakeWhile(x => x != 0).ToArray();
                if (curNameB.Length == 0)
                    break;
                if (curNameB.Any(c => c < 0x20 || c >= 0x7F))
                    throw new FileTypeLoadException("Filename contains nonstandard characters.");
                String curName = enc.GetString(curNameB).Trim();
                Int32 dosTime = (Int16)ArrayUtils.ReadIntFromByteArray(buffer, 0x0C, 2, true);
                Int32 sec = (dosTime & 0x1F) * 2;
                Int32 min = ((dosTime >> 5) & 0x3F);
                Int32 hour = ((dosTime >> 11) & 0x1F);
                if (sec > 59 || min > 59 || hour > 23)
                    throw new FileTypeLoadException("Bad time stamp.");
                Int32 dosDate = (Int16)ArrayUtils.ReadIntFromByteArray(buffer, 0x0E, 2, true);
                Int32 day = (dosDate & 0x1F);
                Int32 month = ((dosDate >> 5) & 0x0F);
                Int32 year = 1980 + ((dosDate >> 9) & 0x7F);
                if (day == 0 || month == 0 || month > 12)
                    throw new FileTypeLoadException("Bad date stamp.");
                DateTime dt = new DateTime(year, month, day, hour, min, sec);
                String extraInfo = getDateStr(dt);
                Int32 curEntryLength = (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0x10, 4, true);
                Int32 curEntryPos= (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0x14, 4, true);
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

        private String getDateStr(DateTime datestamp)
        {
            return "Date: " + datestamp.Year.ToString("D4") + "-" + datestamp.Month.ToString("D2") + "-" + datestamp.Day.ToString("D2") + "\n"
                    + "Time: " + datestamp.Hour.ToString("D2") + ":" + datestamp.Minute.ToString("D2") + ":" + datestamp.Second.ToString("D2");
        }


        /// <summary>Inserts a file into the archive. This can be overridden to add filtering on the input.</summary>
        /// <param name="filePath">Path of the file to load.</param>
        public override ArchiveEntry InsertFile(String filePath)
        {
            ArchiveEntry file = base.InsertFile(filePath);
            DateTime lastMod = file.Date ?? File.GetLastWriteTime(filePath);
            file.ExtraInfo = getDateStr(lastMod);
            return file;
        }

        protected override void OrderFilesListInternal(List<ArchiveEntry> filesList)
        {
            // do nothing
        }

        public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
        {
            DateTime writeDate = DateTime.Now;
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            Int32 nrOfFiles = entries.Length;
            Byte[] buffer = new Byte[FileEntryLength];
            Encoding enc = Encoding.GetEncoding(437);
            Int32 curEntryStart = nrOfFiles * buffer.Length + 2;
            // Write amount of files in table
            ArrayUtils.WriteIntToByteArray(buffer, 0, 2, true, (UInt32)nrOfFiles);
            saveStream.Write(buffer, 0, 2);
            // Write files table
            for (Int32 i = 0; i < entries.Length; ++i)
            {
                ArchiveEntry entry = entries[i];
                Int32 fileLength = entry.Length;
                if (entry.PhysicalPath != null)
                {
                    FileInfo fi = new FileInfo(entry.PhysicalPath);
                    if (!fi.Exists)
                        throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                    fileLength = (Int32)fi.Length;
                }
                String curName = this.GetInternalFilename(entry.FileName);
                Int32 copySize = Math.Min(curName.Length, 12);
                Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                for (Int32 b = copySize; b < 12; b++)
                    buffer[b] = 0;
                DateTime dt = entry.Date ?? writeDate;
                UInt16 time = (UInt16)((dt.Second >> 1) | (dt.Minute << 5) | (dt.Hour << 11));
                UInt16 date = (UInt16)((dt.Day) | (dt.Month << 5) | ((dt.Year - 1980) << 9));
                ArrayUtils.WriteIntToByteArray(buffer, 0x0C, 2, true, (UInt64)time);
                ArrayUtils.WriteIntToByteArray(buffer, 0x0E, 2, true, (UInt64)date);
                ArrayUtils.WriteIntToByteArray(buffer, 0x10, 4, true, (UInt32)fileLength);
                ArrayUtils.WriteIntToByteArray(buffer, 0x14, 4, true, (UInt32)curEntryStart);
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