using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveSwt : Archive
    {
        public override string ShortTypeName { get { return "SelectWare Technologies Archive"; } }
        public override string ShortTypeDescription { get { return "SelectWare Archive"; } }
        public override string[] FileExtensions { get { return new string[] { "swt" }; } }
        public override bool CanSave { get { return false; } }
        public override bool SupportsFolders { get { return true; } }

        const string SWT_BANNER = "SelectWare Technologies demo file";

        protected override List<ArchiveEntry> LoadArchiveInternal(System.IO.Stream loadStream, string archivePath)
        {
            uint end = (uint)loadStream.Length;
            Encoding enc = new ASCIIEncoding();
            int bannerLen = SWT_BANNER.Length;
            if (end < bannerLen)
                throw new FileTypeLoadException("Archive not long enough for header.");
            byte[] buffer = new byte[bannerLen];
            loadStream.Read(buffer, 0, bannerLen);
            byte[] header = enc.GetBytes(SWT_BANNER);
            for (int i = 0; i < bannerLen; ++i)
                if (header[i] != buffer[i])
                    throw new FileTypeLoadException("Header does not match.");            
            loadStream.Read(buffer, 0, 0x0B);
            // First 7 bytes should be [0A 1A 00 00 00 00 00]. Not going to check that though.
            // Next 4 bytes are unknown.
            // start on first file
            int curPos = 0x2C;
            const int bufLen = 46;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            List<string> currentPath = new List<string>();
            while (curPos < end)
            {
                buffer = new byte[bufLen];
                loadStream.Position = curPos;
                int address = curPos + bufLen;
                if (curPos + bufLen >= end)
                    throw new FileTypeLoadException("Archive not long enough for file header.");
                loadStream.Read(buffer, 0, bufLen);
                int parentDirLevel = (ushort)ArrayUtils.ReadIntFromByteArray(buffer, 0x00, 2, true);
                byte fileAttr = buffer[0x17];
                ushort dosTime = (ushort)ArrayUtils.ReadIntFromByteArray(buffer, 0x18, 2, true);
                ushort dosDate = (ushort)ArrayUtils.ReadIntFromByteArray(buffer, 0x1A, 2, true);
                bool isFolder = (fileAttr & 0x10) != 0;
                // lower this, so it's seen as "level of the containing parent folder" instead of seeing the folder as root entry of the next level.
                if (isFolder)
                    parentDirLevel--;
                DateTime dt;
                try
                {
                    dt = GeneralUtils.GetDosDateTime(dosTime, dosDate);
                }
                catch (ArgumentException argex)
                {
                    throw new FileTypeLoadException(argex.Message, argex);
                }
                int length = (int)ArrayUtils.ReadIntFromByteArray(buffer, 0x1C, 4, true);
                if (curPos + bufLen + length > end)
                    throw new FileTypeLoadException("File contains entry that exceeds file constraints.");
                string readName = enc.GetString(buffer.Skip(0x20).TakeWhile(x => x != 0).ToArray());
                int extraDirs = currentPath.Count - parentDirLevel;
                if (extraDirs > 0)
                    currentPath.RemoveRange(parentDirLevel, extraDirs);
                string curName = readName;
                if (currentPath.Count > 0)
                    curName = string.Join("\\", currentPath.ToArray()) + "\\" + curName;
                if (isFolder)
                    currentPath.Add(readName);
                ArchiveEntry curEntry = new ArchiveEntry(curName, archivePath, address, length);
                curEntry.ExtraInfoBin = buffer;
                curEntry.Date = dt;
                curEntry.IsFolder = isFolder;
                filesList.Add(curEntry);
                curPos += bufLen + length;
            }
            return filesList;
        }

        private string GetFileAttributes(byte fileAttr)
        {
            List<string> attributes = new List<string>();
            if ((fileAttr & 0x01) != 0)
                attributes.Add("Read-only");
            if ((fileAttr & 0x02) != 0)
                attributes.Add("Hidden");
            if ((fileAttr & 0x04) != 0)
                attributes.Add("System");
            if ((fileAttr & 0x08) != 0)
                attributes.Add("Volume label");
            if ((fileAttr & 0x10) != 0)
                attributes.Add("Folder");
            if ((fileAttr & 0x20) != 0)
                attributes.Add("Archived: " + ((fileAttr & 0x20) != 0 ? "no" : "yes"));
            return string.Join(", ", attributes.ToArray());
        }

        //protected override void OrderFilesListInternal(List<ArchiveEntry> filesList)
        //{
        //    // Do nothing.
        //    // May adapt this later; if I fill ExtraInfoBin with all info regarding files' folder IDs, index and folders' own IDs,
        //    // sorting may be possible that way.
        //}

        public override bool SaveArchive(Archive archive, System.IO.Stream saveStream, string savePath)
        {
            throw new NotImplementedException();
        }
    }
}
