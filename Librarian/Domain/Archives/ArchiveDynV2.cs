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
        private static readonly Byte[] DynamixIDBytes2 = { 0x4F, 0x50, 0x4E, 0x3A };

        public override String ShortTypeName { get { return "Dynamix Archive v2"; } }
        public override String ShortTypeDescription { get { return "Dynamix Archive v2"; } }
        public override String[] FileExtensions { get { return new String[] { "000", "001", "002", "003", "004", "005", "006", "007", "008", "009" }; } }
        public override Boolean CanSave { get { return false; } }

        protected override void LoadArchiveInternal(Stream loadStream, String archivePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            Int32 curPos = (Int32)loadStream.Position;
            Int64 end = loadStream.Length;
            ReadMode readMode = ReadMode.ReadChunk;
            Int32 currentChunkStart = 0;
            Int32 currentChunkEnd = 0;
            Int32 currentChunkLength = 0;
            while (curPos < end)
            {
                switch (readMode)
                {
                    case ReadMode.ReadChunk:
                        Byte[] idBuffer = new Byte[DynamixIDBytes2.Length];
                        Int32 count = loadStream.Read(idBuffer, 0, idBuffer.Length);
                        if (count != idBuffer.Length || !DynamixIDBytes2.SequenceEqual(idBuffer))
                            throw new FileTypeLoadException("Not a Dynamix v2 archive.");
                        readMode = ReadMode.ReadChunkLength;
                        break;
                    case ReadMode.ReadChunkLength:
                        if (curPos + 4 >= end)
                            throw new FileTypeLoadException("Archive entry outside file bounds.");
                        Byte currentChunkLengthB1 = (Byte)loadStream.ReadByte();
                        Byte currentChunkLengthB2 = (Byte)loadStream.ReadByte();
                        Byte currentChunkLengthB3 = (Byte)loadStream.ReadByte();
                        Byte currentChunkLengthB4 = (Byte)loadStream.ReadByte();
                        //Boolean isArchive = (currentChunkLengthB4 & 0x80) != 0;
                        currentChunkLengthB4 = (Byte)(currentChunkLengthB4 & 0x7F);
                        currentChunkLength = currentChunkLengthB1 | (currentChunkLengthB2 << 0x08) | (currentChunkLengthB3 << 0x10) | (currentChunkLengthB4 << 0x18);
                        currentChunkStart = (Int32)loadStream.Position;
                        currentChunkEnd = currentChunkStart + currentChunkLength;
                        readMode = ReadMode.ReadCompression;
                        break;
                    case ReadMode.ReadCompression:
                        Byte[] currentChunk = new Byte[currentChunkLength];
                        if (loadStream.Read(currentChunk, 0, currentChunkLength) != currentChunkLength)
                            throw new FileTypeLoadException("Archive entry outside file bounds.");
                        currentChunk = DynamixCompression.DecodeChunk(currentChunk);
                        String curName = enc.GetString(currentChunk.TakeWhile(x => x != 0).ToArray());
                        ArchiveEntry fe = new ArchiveEntry(curName, archivePath, currentChunkStart, currentChunkLength);
                        this._filesList.Add(fe);
                        readMode = ReadMode.ReadChunk;
                        loadStream.Position = currentChunkEnd;
                        break;
                }
                curPos = (Int32)loadStream.Position;
            }
        }

        public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts the filename to the type supported internally.
        /// </summary>
        /// <param name="filePath">Original file path.</param>
        /// <returns></returns>
        public override String GetInternalFilename(String filePath)
        {
            String dirname = Path.GetDirectoryName(filePath);
            String[] folder = dirname == null ? new String[0] : dirname.Split('\\');
            String filename = base.GetInternalFilename(Path.GetFileName(filePath));
            for (Int32 i = 0; i < folder.Length; i++)
                folder[i] = base.GetInternalFilename(folder[i]);
            String fullfolder = String.Join("\\", folder);
            return Path.Combine(fullfolder, filename);
        }

        private enum ReadMode
        {
            ReadChunk,
            ReadChunkLength,
            ReadCompression,
            ReadFileLength,
            ReadFileName,
            ReadFile,
            SaveFiles,
        }
    }
}