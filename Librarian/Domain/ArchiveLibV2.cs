using System;
using System.IO;
using System.Linq;

namespace LibrarianTool.Domain
{
    public class ArchiveLibV2 : ArchiveLibV1
    {
        // "LIC" + 0x1A
        private static readonly Byte[] IdBytesLic = { 0x4C, 0x49, 0x43, 0x1A };

        public override String ShortTypeName { get { return "Mythos LIB Archive v2"; } }
        public override String ShortTypeDescription { get { return "Mythos LIB v2"; } }

        public override Boolean LoadArchive(Stream loadStream, String archivePath)
        {
            this.FileName = archivePath;
            Int32 files = GetFilesCount(loadStream, IdBytesLic);
            if (files == -1)
                return false;
            loadStream.Position += 8 * (files + 1);
            return LoadArchive(loadStream, files, archivePath);
        }

        public override Boolean SaveArchive(Archive archive, Stream saveStream)
        {
            SaveHeader(archive, saveStream, IdBytesLic);
            saveStream.Position += 8 * (archive.FilesList.Count + 1);
            return SaveFiles(archive, saveStream);
        }
    }
}