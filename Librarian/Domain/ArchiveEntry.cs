using System;
using System.IO;

namespace LibrarianTool.Domain
{
    public class ArchiveEntry
    {
        public String PhysicalPath { get; set; }
        public String FileName { get; set; }
        public String HashedFilename { get; set; }
        public HashType HashType { get; set; }
        public String ArchivePath { get; set; }
        public Int32 StartOffset { get; set; }
        public Int32 Length { get; set; }

        public ArchiveEntry ()
        {
            StartOffset = -1;
            Length = -1;
        }

        public ArchiveEntry(String physicalPath)
        {
            this.PhysicalPath = physicalPath;
            FileName = Path.GetFileName(physicalPath);
            StartOffset = -1;
            Length = -1;
        }

        public ArchiveEntry(String fileName, String archivePath, Int32 startOffset, Int32 endOffset)
        {
            FileName = fileName;
            ArchivePath = archivePath;
            StartOffset = startOffset;
            Length = endOffset;
        }

        public ArchiveEntry(String hashedFilename, HashType hashType, String archivePath, Int32 startOffset, Int32 endOffset)
        {
            HashedFilename = hashedFilename;
            HashType = hashType;
            ArchivePath = archivePath;
            StartOffset = startOffset;
            Length = endOffset;
        }

        public ArchiveEntry(String fileName, String hashedFilename, HashType hashType, String archivePath, Int32 startOffset, Int32 endOffset)
        {
            FileName = fileName;
            HashedFilename = hashedFilename;
            HashType = hashType;
            ArchivePath = archivePath;
            StartOffset = startOffset;
            Length = endOffset;
        }

        public override String ToString()
        {
            return PhysicalPath != null ? ("[" + Path.GetFileName(PhysicalPath) + "]") : (FileName ?? HashedFilename);
        }
    }
}