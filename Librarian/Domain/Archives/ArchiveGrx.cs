using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveGrx : Archive
    {
        public override string ShortTypeName { get { return "Genus Microprogramming Archive"; } }
        public override string ShortTypeDescription { get { return "GRX Archive"; } }
        public override string[] FileExtensions { get { return new string[] { "grx" }; } }
        public override bool CanSave { get { return false; } }

        const string GRX_BANNER = "Copyright (c) Genus Microprogramming, Inc. 1988-93";
        const string GRX_BANNER_REGEX = "Copyright \\(c\\) Genus Microprogramming, Inc. \\d\\d\\d\\d-\\d\\d";

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            uint end = (uint)loadStream.Length;
            Encoding enc = new ASCIIEncoding();
            if (end < 0x81)
                throw new FileTypeLoadException("Archive not long enough for header.");
            byte[] buffer = new byte[GRX_BANNER.Length];
            loadStream.Position = 2;
            loadStream.Read(buffer, 0, GRX_BANNER.Length);
            string header = enc.GetString(buffer);
            if (!Regex.IsMatch(header, GRX_BANNER_REGEX))
                throw new FileTypeLoadException("Header does not match.");
            // start on first file
            int curPos = 0x80;
            const int bufLen = 0x1A;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            Regex splitFilename = new Regex("(\\w+) *(\\.\\w+)");
            int firstFileStart = -1;
            do
            {
                buffer = new byte[bufLen];
                loadStream.Position = curPos;
                if (curPos + bufLen >= end)
                    throw new FileTypeLoadException("Archive not long enough for file header.");
                loadStream.Read(buffer, 0, bufLen);
                byte[] nameBuffer = new byte[12];
                Array.Copy(buffer, 1, nameBuffer, 0, 12);
                string curName = enc.GetString(nameBuffer.TakeWhile(x => x != 0).ToArray());
                Match m = splitFilename.Match(curName);
                if (!m.Success)
                    break;
                curName = m.Groups[1].Value + m.Groups[2].Value;
                int address = (int) ArrayUtils.ReadIntFromByteArray(buffer, 0x0E, 4, true);
                int length = (int) ArrayUtils.ReadIntFromByteArray(buffer, 0x12, 4, true);
                ushort dosDate = (ushort)ArrayUtils.ReadIntFromByteArray(buffer, 0x16, 2, true);
                ushort dosTime = (ushort)ArrayUtils.ReadIntFromByteArray(buffer, 0x18, 2, true);
                DateTime dt;
                try
                {
                    dt = GeneralUtils.GetDosDateTime(dosTime, dosDate);
                }
                catch (ArgumentException argex)
                {
                    throw new FileTypeLoadException(argex.Message, argex);
                }
                string extraInfo = GeneralUtils.GetDateString(dt);
                ArchiveEntry curEntry = new ArchiveEntry(curName, archivePath, address, length, extraInfo);
                curEntry.ExtraInfoBin = buffer;
                curEntry.Date = dt;
                filesList.Add(curEntry);
                curPos += bufLen;
                firstFileStart = Math.Max(firstFileStart, address);
            } while (curPos < firstFileStart);
            return filesList;
        }

        /// <summary>Inserts a file into the archive. This can be overridden to add filtering on the input.</summary>
        /// <param name="filePath">Path of the file to load.</param>
        public override ArchiveEntry InsertFile(string filePath)
        {
            ArchiveEntry file = base.InsertFile(filePath);
            DateTime lastMod = file.Date ?? File.GetLastWriteTime(filePath);
            file.ExtraInfo = GeneralUtils.GetDateString(lastMod);
            return file;
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            throw new NotImplementedException();
        }
    }
}
