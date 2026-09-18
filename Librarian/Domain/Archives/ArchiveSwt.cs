using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveSwt : Archive
    {
        public override String ShortTypeName { get { return "SelectWare Technologies Archive"; } }
        public override String ShortTypeDescription { get { return "SelectWare Archive"; } }
        public override String[] FileExtensions { get { return new String[] { "swt" }; } }
        public override Boolean CanSave { get { return false; } }

        const String SWT_BANNER = "SelectWare Technologies demo file";

        protected override List<ArchiveEntry> LoadArchiveInternal(System.IO.Stream loadStream, string archivePath)
        {
            UInt32 end = (UInt32)loadStream.Length;
            Encoding enc = new ASCIIEncoding();
            if (end < SWT_BANNER.Length)
                throw new FileTypeLoadException("Archive not long enough for header.");
            Byte[] buffer = new Byte[SWT_BANNER.Length];
            loadStream.Read(buffer, 0, SWT_BANNER.Length);
            String header = enc.GetString(buffer);
            if (header != SWT_BANNER)
                throw new FileTypeLoadException("Header does not match.");
            // start on first file
            Int32 curPos = 0x2C;
            const Int32 bufLen = 46;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            while (curPos < end)
            {
                buffer = new Byte[bufLen];
                loadStream.Position = curPos;
                Int32 address = curPos + bufLen;
                if (curPos + bufLen >= end)
                    throw new FileTypeLoadException("Archive not long enough for file header.");
                loadStream.Read(buffer, 0, bufLen);
                Int32 length = (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0x1C, 4, true);
                String curName = enc.GetString(buffer.Skip(0x20).TakeWhile(x => x != 0).ToArray());
                ArchiveEntry curEntry = new ArchiveEntry(curName, archivePath, address, length);
                curEntry.ExtraInfoBin = buffer;
                filesList.Add(curEntry);
                curPos += bufLen + length;
            }
            return filesList;
        }

        public override bool SaveArchive(Archive archive, System.IO.Stream saveStream, string savePath)
        {
            throw new NotImplementedException();
        }
    }
}
