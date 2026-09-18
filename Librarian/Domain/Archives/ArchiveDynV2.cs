using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.GameData.Dynamix;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveDynV2 : Archive
    {
        // "OPN:"
        private static readonly byte[] DynamixIDBytes2 = { 0x4F, 0x50, 0x4E, 0x3A };
        private static Encoding Enc = Encoding.GetEncoding(437);

        public override string ShortTypeName { get { return "Dynamix Archive v2"; } }
        public override string ShortTypeDescription { get { return "Dynamix Archive v2"; } }
        public override string[] FileExtensions { get { return new string[] { "000", "001", "002", "003", "004", "005", "006", "007", "008", "009" }; } }
        public override bool CanSave { get { return false; } }
        public override bool SupportsFolders { get { return true; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            long end = loadStream.Length;
            //Boolean isArchive = false;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            while (loadStream.Position < end)
            {
                byte[] fileContents;
                ArchiveEntry fe = ReadFileFromStream(loadStream, archivePath, -1, out fileContents);
                filesList.Add(fe);
            }
            return filesList;
        }

        private ArchiveEntry ReadFileFromStream(Stream loadStream, string archivePath, int currentChunkLength, out byte[] uncompressedData)
        {
            if (currentChunkLength == -1)
            {
                // Read chunk header
                byte[] idBuffer = new byte[DynamixIDBytes2.Length];
                int count = loadStream.Read(idBuffer, 0, idBuffer.Length);
                if (count != idBuffer.Length || !DynamixIDBytes2.SequenceEqual(idBuffer))
                    throw new FileTypeLoadException("Not a Dynamix v2 archive.");
                // Read chunk length
                byte[] lenBuffer = new byte[4];
                if (loadStream.Read(lenBuffer, 0, 4) != 4)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");
                lenBuffer[3] &= 0x7F; // Remove archive bit
                currentChunkLength = (int) ArrayUtils.ReadIntFromByteArray(lenBuffer, 0, 4, true);
            }
            int currentChunkStart = (int)loadStream.Position;
            //Int32 currentChunkEnd = currentChunkStart + currentChunkLength;
            //Read compression:
            byte[] currentChunk = new byte[currentChunkLength];
            if (loadStream.Read(currentChunk, 0, currentChunkLength) != currentChunkLength)
                throw new FileTypeLoadException("Archive entry outside file bounds.");
            byte compression = currentChunk.Length == 0 ? (byte)0 : currentChunk[0];
            uncompressedData = DynamixCompression.DecodeChunk(currentChunk);
            string curName = Enc.GetString(uncompressedData.TakeWhile(x => x != 0).ToArray());
            int curNameLen = curName.Length + 1;
            ArchiveEntry fe = new ArchiveEntry(curName, archivePath, currentChunkStart, currentChunkLength);
            string compressionStr;
            switch (compression)
            {
                case 0: compressionStr = "Uncompressed"; break;
                case 1: compressionStr = "RLE"; break;
                case 2: compressionStr = "LZW"; break;
                case 3: compressionStr = "LZSS"; break;
                default: compressionStr = "Unknown"; break;
            }
            fe.ExtraInfo = "Compression: " + compressionStr + "\nUncompressed size: " + (uncompressedData.Length - curNameLen);
            //loadStream.Position = currentChunkEnd;
            return fe;
        }


        /// <summary>Extracts the requested file from the _filesList list.</summary>
        /// <param name="entry">Name of the file to extract.</param>
        /// <param name="savePath">Path to save the file to.</param>
        /// <returns></returns>
        public override bool ExtractFile(ArchiveEntry entry, string savePath)
        {
            if (entry == null)
                return false;
            string folder = Path.GetDirectoryName(savePath);
            if (entry.IsFolder)
            {
                Directory.CreateDirectory(savePath);
            }
            else
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                using (FileStream fs = new FileStream(savePath, FileMode.Create))
                    ExtractContents(entry, fs);
            }
            return true;
        }

        private void ExtractContents(ArchiveEntry entry, Stream saveStream)
        {
            string readFile;
            if (entry.PhysicalPath != null)
            {
                // Copy actual file
                readFile = entry.PhysicalPath;
                FileInfo fi = new FileInfo(entry.PhysicalPath);
                using (FileStream fs = new FileStream(readFile, FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    CopyStream(fs, saveStream, fi.Length);
                }
            }
            else
            {
                // Uncompress from archive.
                readFile = entry.ArchivePath;
                using (FileStream fs = new FileStream(readFile, FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(entry.StartOffset, SeekOrigin.Begin);
                    byte[] uncompressedData;
                    ArchiveEntry fe = ReadFileFromStream(fs, string.Empty, entry.Length, out uncompressedData);
                    int uncStart = fe.FileName.Length + 1;
                    int uncLength = uncompressedData.Length - uncStart;
                    // Skip file name
                    using (MemoryStream ms = new MemoryStream(uncompressedData, uncStart, uncLength))
                        CopyStream(ms, saveStream, uncLength);
                }
            }
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            throw new NotImplementedException();
        }

    }
}