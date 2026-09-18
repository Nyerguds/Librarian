using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveRenpy : Archive
    {
        // "LIB" + 0x1A
        private static readonly byte[] IdBytesLib = Encoding.ASCII.GetBytes("RPA-3.0 ");
        private static readonly byte[] SeparatorBytesLib = Encoding.ASCII.GetBytes("Made with Ren'Py.");
        private static readonly byte[] Rpc2IdBytes = Encoding.ASCII.GetBytes("RENPY RPC2");

        public override string ShortTypeName { get { return "Ren'Py Archive"; } }
        public override string ShortTypeDescription { get { return "Ren'Py Archive"; } }
        public override string[] FileExtensions { get { return new string[] { "rpa" }; } }
        public override bool CanSave { get { return false; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            loadStream.Position = 0;
            if (loadStream.Length < IdBytesLib.Length)
                throw new FileTypeLoadException("Too short to be a Ren'Py archive.");
            byte[] testArray = new byte[IdBytesLib.Length];
            loadStream.Read(testArray, 0, testArray.Length);
            if (!testArray.SequenceEqual(IdBytesLib))
                throw new FileTypeLoadException("Not a Ren'Py archive.");
            int sepLen = SeparatorBytesLib.Length;
            string baseName = Path.GetFileNameWithoutExtension(archivePath);
            bool separatorFound = loadStream.JumpToNextMatch(SeparatorBytesLib, SeparatorBytesLib.Length);
            if (!separatorFound)
                throw new FileTypeLoadException("Not a Ren'Py archive.");
            long startIndex = loadStream.Position + sepLen;
            int fileNamecounter = 0;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            do
            {
                loadStream.Position = startIndex; // skip the separator
                bool isRpc2 = loadStream.MatchAtCurrentPos(Rpc2IdBytes);
                string filename = baseName + fileNamecounter.ToString("0000");
                ++fileNamecounter;
                string extension = isRpc2 ? "rpyc" : MimeTypeDetector.GetMimeType(loadStream)[0];
                separatorFound = loadStream.JumpToNextMatch(SeparatorBytesLib, 0x80);
                long endIndex = separatorFound ? loadStream.Position : loadStream.Length;
                int entryLength = (int)(endIndex - startIndex);

                ArchiveEntry archiveEntry = new ArchiveEntry(filename + "." + extension, archivePath, (int)startIndex, entryLength);
                filesList.Add(archiveEntry);
                startIndex = endIndex + sepLen;
            }
            while (separatorFound);
            return filesList;
        }

        public override string GetInternalFilename(string filePath)
        {
            return Path.GetFileName(filePath);
        }
        
        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            throw new NotImplementedException();
        }
    }
}