using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveDynV1 : Archive
    {
        protected const int FileEntryLength = 0x11;

        public override string ShortTypeName { get { return "Dynamix Archive v1"; } }
        public override string ShortTypeDescription { get { return "Dynamix Archive v1"; } }
        public override string[] FileExtensions { get { return new string[] { "000", "001", "002", "003", "004", "005", "006", "007", "008", "009" }; } }
        public override bool CanSave { get { return true; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            long end = loadStream.Length;
            byte[] buffer = new byte[FileEntryLength];
            long curPos = loadStream.Position;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            while (curPos < end)
            {
                loadStream.Position = curPos;
                int loaded = loadStream.Read(buffer, 0, FileEntryLength);
                if (loaded < FileEntryLength)
                    throw new FileTypeLoadException("Encountered cut-off file entry.");
                byte[] curNameB = buffer.Take(13).TakeWhile(x => x != 0).ToArray();
                if (curNameB.Length == 0 || curNameB.Length > 12)
                    throw new FileTypeLoadException("Not a Dynamix v1 archive.");
                if (curNameB.Any(c => c < 0x20 || c >= 0x7F))
                    throw new FileTypeLoadException("Filename contains nonstandard characters.");
                string curName = enc.GetString(curNameB).Trim();
                int curEntryLength = (int)ArrayUtils.ReadIntFromByteArray(buffer, 0x0D, 4, true);
                if (curEntryLength < 0)
                    throw new FileTypeLoadException("Negative file length in archive entry.");
                curPos += FileEntryLength + curEntryLength;
                if (curPos < 0 || curPos > end)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");
                if (curName.Length == 0 && curEntryLength == 0)
                    continue;
                filesList.Add(new ArchiveEntry(curName, archivePath, (int)loadStream.Position, curEntryLength));
            }
            return filesList;
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            byte[] buffer = new byte[FileEntryLength];

            foreach (ArchiveEntry entry in archive.FilesList)
            {
                string filename = GetInternalFilename(entry.FileName);
                enc.GetBytes(filename, 0, Math.Min(filename.Length, 12), buffer, 0);
                for (int b = filename.Length; b <= 13; ++b)
                    buffer[b] = 0;
                int fileLength = entry.Length;
                if (entry.PhysicalPath != null)
                {
                    FileInfo fi = new FileInfo(entry.PhysicalPath);
                    if (!fi.Exists)
                        throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                    fileLength = (int)fi.Length;
                }
                ArrayUtils.WriteIntToByteArray(buffer, 0x0D, 4, true, (ulong)fileLength);
                saveStream.Write(buffer, 0, buffer.Length);
                CopyEntryContentsToStream(entry, saveStream);
            }
            return true;
        }
    }
}