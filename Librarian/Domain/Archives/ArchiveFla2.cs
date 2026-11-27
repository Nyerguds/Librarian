using LibrarianTool.Domain.FileData;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace LibrarianTool.Domain.Archives
{
    /// <summary>
    /// Single-file archive used by Interactive Studios. Used in Glover (N64) and WarGames: Defcon 1 (PC).
    /// </summary>
    public class ArchiveFla2 : Archive
    {
        public override string ShortTypeName { get { return "FLA2 Archive"; } }
        public override string ShortTypeDescription { get { return "FLA2 Archive"; } }
        public override string[] FileExtensions { get { return new string[] { "fla" }; } }
        public override bool CanSave { get { return true; } }
        public override int MaximumEntries { get { return 1; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            long end = loadStream.Length;
            long curPos = loadStream.Position;
            byte[] fileContents = loadStream.ReadAllBytes();
            if (fileContents == null || fileContents.Length < 10)
                throw new FileTypeLoadException("Header too short to be this type.");
            uint magic = ArrayUtils.ReadUInt32FromByteArrayLe(fileContents, 0);
            // String "FLA2"
            if (magic != 0x32414C46)
                throw new FileTypeLoadException("Header identification does not match.");

            int need = ArrayUtils.ReadInt32FromByteArrayLe(fileContents, 4);

            string entryName = archivePath;
            if (entryName.Length > 4)
            {
                int entryExtIndex = entryName.Length - 4;
                string entryExt = entryName.Substring(entryExtIndex);
                if (".fla".Equals(entryExt, StringComparison.OrdinalIgnoreCase))
                {
                    entryName = Path.GetFileName(entryName.Substring(0, entryExtIndex));
                }
            }
            ArchiveEntry entry = new ArchiveEntry(entryName, archivePath, 8, need);
            int comprSize = fileContents.Length - entry.StartOffset;
            entry.ExtraInfo = "Compressed size: " + comprSize;
            byte[] decodedFile = Fla2Compression.Fla2Decode(fileContents, entry.StartOffset, comprSize, need, out int written, out int consumed);
            if (decodedFile == null)
            {
                throw new FileTypeLoadException("File contents could not be decompressed.");
            }
            ExtraInfo = "Compressed archive";
            List<ArchiveEntry> entries = new List<ArchiveEntry>() { entry };
            return entries;
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            int count = archive.FilesList.Count;
            if (count != 1)
            {
                throw new ArgumentException(
                    count == 0 ? "Cannot save an empty archive of this type." : "This archive only supports a single embedded file.",
                    nameof(archive));
            }
            ArchiveEntry entry = archive.FilesList[0];
            int fileLength = entry.Length;
            byte[] compressed;
            if (entry.PhysicalPath != null)
            {
                FileInfo fi = new FileInfo(entry.PhysicalPath);
                if (!fi.Exists)
                    throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                fileLength = (int)fi.Length;
                byte[] uncompressed;
                using (MemoryStream ms = new MemoryStream())
                using (FileStream fs = fi.OpenRead())
                {
                    CopyStream(fs, ms, fileLength);
                    ms.Flush();
                    uncompressed = ms.ToArray();
                }
                compressed = Fla2Compression.Fla2Encode(uncompressed);
            }
            else
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    // Just copy the original data. Don't recompress.
                    base.ExtractEntryContentsFromArchive(entry, ms);
                    ms.Flush();
                    compressed = ms.ToArray();
                }
            }
            if (compressed == null)
            {
                return false;
            }
            byte[] header = new byte[8];
            ArrayUtils.WriteUInt32ToByteArrayLe(header, 0, 0x32414C46);
            ArrayUtils.WriteInt32ToByteArrayLe(header, 4, fileLength);
            saveStream.Write(header, 0, header.Length);
            saveStream.Write(compressed, 0, compressed.Length);
            return true;
        }

        protected override void ExtractEntryContentsFromArchive(ArchiveEntry entry, Stream saveStream)
        {
            int start = entry.StartOffset;
            int length = entry.Length;
            using (FileStream fs = new FileStream(entry.ArchivePath, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(0, SeekOrigin.Begin);
                byte[] archive = fs.ReadAllBytes();
                int comprSize = archive.Length - start;
                byte[] decodedFile = Fla2Compression.Fla2Decode(archive, start, comprSize, length, out int written, out int consumed);
                using (MemoryStream ms = new MemoryStream(decodedFile))
                {
                    CopyStream(ms, saveStream, decodedFile.Length);
                }
            }
        }

        public override string GetInternalFilename(string filePath)
        {
            return filePath;
        }

        public override ArchiveEntry InsertFile(string filePath, int insertIndex)
        {
            ArchiveEntry ae = base.InsertFile(filePath, insertIndex);
            if (ae == null)
            {
                return null;
            }
            if (IsNew || this.FileName == null)
            {
                FileName = Path.GetFileName(filePath) + ".fla";
            }
            else
            {
                FileName = Path.Combine(Path.GetDirectoryName(FileName), Path.GetFileName(filePath) + ".fla");
            }
            return ae;
        }

        public override ArchiveEntry InsertFile(string filePath, string internalFilename, int insertIndex)
        {
            ArchiveEntry ae = base.InsertFile(filePath, internalFilename, insertIndex);
            if (ae == null)
            {
                return null;
            }
            if (IsNew || this.FileName == null)
            {
                FileName = Path.GetFileName(internalFilename) + ".fla";
            }
            else
            {
                FileName = Path.Combine(Path.GetDirectoryName(FileName), internalFilename + ".fla");
            }
            return ae;
        }

    }
}
