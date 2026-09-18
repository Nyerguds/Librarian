using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveGrx : Archive
    {
        public override String ShortTypeName { get { return "Genus Microprogramming Archive"; } }
        public override String ShortTypeDescription { get { return "GRX Archive"; } }
        public override String[] FileExtensions { get { return new String[] { "grx" }; } }

        const String GRX_BANNER = "Copyright (c) Genus Microprogramming, Inc. 1988-93";

        public override Boolean CanSave { get { return false; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(System.IO.Stream loadStream, string archivePath)
        {
            UInt32 end = (UInt32)loadStream.Length;
            Encoding enc = new ASCIIEncoding();
            if (end < 0x81)
                throw new FileTypeLoadException("Archive not long enough for header.");
            Byte[] buffer = new Byte[GRX_BANNER.Length];
            loadStream.Position = 2;
            loadStream.Read(buffer, 0, GRX_BANNER.Length);
            String header = enc.GetString(buffer);
            if (header != GRX_BANNER)
                throw new FileTypeLoadException("Header does not match.");
            // start on first file
            Int32 curPos = 0x81;
            const Int32 bufLen = 0x1A;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            Regex splitFilename = new Regex("(\\w+) *(\\.\\w+)");
            Int32 firstFileStart = -1;
            do
            {
                buffer = new Byte[bufLen];
                loadStream.Position = curPos;
                if (curPos + bufLen >= end)
                    throw new FileTypeLoadException("Archive not long enough for file header.");
                loadStream.Read(buffer, 0, bufLen);
                String curName = enc.GetString(buffer.Take(12).TakeWhile(x => x != 0).ToArray());
                Match m = splitFilename.Match(curName);
                if (!m.Success)
                    break;
                curName = m.Groups[1].Value + m.Groups[2].Value;
                Int32 address = (Int32) ArrayUtils.ReadIntFromByteArray(buffer, 0x0D, 4, true);
                Int32 length = (Int32) ArrayUtils.ReadIntFromByteArray(buffer, 0x11, 4, true);
                ArchiveEntry curEntry = new ArchiveEntry(curName, archivePath, address, length);
                curEntry.ExtraInfoBin = buffer;
                filesList.Add(curEntry);
                curPos += bufLen;
                firstFileStart = Math.Max(firstFileStart, address);
            } while (curPos < firstFileStart);
            return filesList;
        }

        public override bool SaveArchive(Archive archive, System.IO.Stream saveStream, string savePath)
        {
            throw new NotImplementedException();
        }
    }
}
