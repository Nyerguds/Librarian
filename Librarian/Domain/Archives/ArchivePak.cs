using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchivePakV1 : ArchivePak
    {
        protected override PakVersion PakVer { get { return PakVersion.PakVersion1;} }
    }

    public class ArchivePakV2 : ArchivePak
    {
        protected override PakVersion PakVer { get { return PakVersion.PakVersion2; } }
    }

    public class ArchivePakV3 : ArchivePak
    {
        protected override PakVersion PakVer { get { return PakVersion.PakVersion3; } }
    }

    public abstract class ArchivePak : Archive
    {
        public override string ShortTypeName { get { return "Westwood PAK Archive v" + (int)PakVer; } }
        public override string ShortTypeDescription { get { return "Westwood PAK v" + (int)PakVer; } }
        public override string[] FileExtensions { get { return new string[] { "PAK" }; } }
        public override bool CanSave { get { return true; } }
        protected abstract PakVersion PakVer { get; }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            uint end = (uint)loadStream.Length;
            Encoding enc = new ASCIIEncoding();
            if (end < 4)
                throw new FileTypeLoadException("Archive not long enough for a single entry.");
            byte[] addressBuffer = new byte[4];
            loadStream.Position = 0;
            ExtraInfo = string.Empty;
            ArchiveEntry curEntry = null;
            uint minOffs = end;
            // Need at least the address plus one byte for a 0-terminated name.
            bool foundEndAddress = false;
            bool foundEndAddressEntryV3 = false;
            bool foundNullAddress = false;
            bool foundNullName = false;
            uint address = 0;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            while (loadStream.Position < minOffs)
            {
                if (loadStream.Read(addressBuffer, 0, 4) < 4)
                    throw new FileTypeLoadException("Archive not long enough to read file offset.");
                address = (uint)ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 4, true);
                if (address == 0)
                {
                    foundNullAddress = true;
                    if (curEntry != null && curEntry.Length == -1 && PakVer == PakVersion.PakVersion2)
                        curEntry.Length = (int)end - curEntry.StartOffset;
                }
                if (address == end && PakVer == PakVersion.PakVersion1)
                {
                    foundEndAddress = true;
                    if (curEntry != null && curEntry.Length == -1)
                        curEntry.Length = (int)end - curEntry.StartOffset;
                }
                if (PakVer == PakVersion.PakVersion3 && foundNullName && foundEndAddressEntryV3 && foundNullAddress)
                    break;
                if (PakVer == PakVersion.PakVersion2 && foundNullAddress)
                    break;
                if (PakVer == PakVersion.PakVersion1 && foundEndAddress)
                    break;
                if (loadStream.Position == minOffs)
                    break;
                if (address > end)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");

                if (curEntry != null && curEntry.Length == -1)
                    curEntry.Length = (int)address - curEntry.StartOffset;

                byte[] nameBuf = new byte[13];
                int curNamePos;
                for (curNamePos = 0; curNamePos < nameBuf.Length; ++curNamePos)
                {
                    int curByte = loadStream.ReadByte();
                    if (curByte == -1)
                        throw new FileTypeLoadException("Archive not long enough to read file name.");
                    if (curByte == 0)
                        break;
                    if (curByte < 0x20)
                        throw new FileTypeLoadException("Illegal values in file name.");
                    nameBuf[curNamePos] = (byte) curByte;
                }
                if (curNamePos == 13)
                    throw new FileTypeLoadException("Bad file name.");
                string curName = enc.GetString(nameBuf.TakeWhile(x => x != 0).ToArray());
                if (curName.Length == 0)
                {
                    foundNullName = true;
                    if ((curEntry == null || (curEntry.Length == -1 && address > curEntry.StartOffset)) && address != 0 && address <= end && PakVer == PakVersion.PakVersion3)
                    {
                        foundEndAddressEntryV3 = true;
                        if (curEntry != null)
                            curEntry.Length = (int)address - curEntry.StartOffset;
                    }
                }
                if (!foundEndAddress && !foundNullAddress && !foundNullName)
                {
                    curEntry = new ArchiveEntry(curName, archivePath, (int) address, -1);
                    minOffs = Math.Min(minOffs, address);
                    filesList.Add(curEntry);
                }
            }

            if (PakVer == PakVersion.PakVersion3 && (!foundNullName || !foundEndAddressEntryV3 || !foundNullAddress))
                throw new FileTypeLoadException("This is not a v3 PAK file.");
            if (PakVer == PakVersion.PakVersion2 && (foundNullName || foundEndAddress || !foundNullAddress))
                throw new FileTypeLoadException("This is not a v2 PAK file.");
            if (PakVer == PakVersion.PakVersion1 && (foundNullName || foundNullAddress))
                throw new FileTypeLoadException("This is not a v1 PAK file.");
            if (PakVer == PakVersion.PakVersion1 && !foundEndAddress && loadStream.Position == minOffs && curEntry != null && curEntry.Length == -1)
            {
                // Seems to be a problem in some v1 pak files where the last entry is gibberish.
                curEntry.Length = (int)end - curEntry.StartOffset;
                ExtraInfo = "File has corrupted end offset: " + address.ToString("X8");
            }
            // All cases should be handled.
            if (curEntry != null && curEntry.Length == -1)
                throw new FileTypeLoadException("This is not a PAK file.");
            // Not gonna allow this. Too much chance on empty edge cases.
            if (filesList.Count == 0)
                throw new FileTypeLoadException("Not entries in PAK file.");
            return filesList;
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            // Filename lengths + version-dependent padding
            int firstFileOffset = entries.Sum(en => en.FileName.Length) + entries.Length * 5;
            switch (PakVer)
            {
                case PakVersion.PakVersion1:
                case PakVersion.PakVersion2:
                    // v1: added dword with end
                    // v2: added dword with zero
                    firstFileOffset += 4;
                    break;
                case PakVersion.PakVersion3:
                    // v3: added dword with end, added byte for empty filename, added dword with zero.
                    firstFileOffset += 9;
                    break;
            }
            int fileOffset = firstFileOffset;
            Encoding enc = Encoding.GetEncoding(437);

            byte[] buffer = new byte[13];
            using (BinaryWriter bw = new BinaryWriter(new NonDisposingStream(saveStream)))
            {
                foreach (ArchiveEntry entry in entries)
                {
                    int fileLength = entry.Length;
                    string curName = entry.FileName;
                    if (entry.PhysicalPath != null)
                    {
                        // To be 100% sure the index is OK, this is updated at the moment of writing.
                        FileInfo fi = new FileInfo(entry.PhysicalPath);
                        if (!fi.Exists)
                            throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                        fileLength = (int) fi.Length;
                        // not really necessary since the input method takes care of it, but, just to be sure.
                        curName = GetInternalFilename(entry.FileName);
                    }
                    bw.Write(fileOffset);
                    fileOffset += fileLength;
                    int copySize = curName.Length;
                    Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                    for (int b = copySize; b < 13; ++b)
                        buffer[b] = 0;
                    bw.Write(buffer, 0, copySize + 1);
                }
                switch (PakVer)
                {
                    case PakVersion.PakVersion1:
                        bw.Write(fileOffset);
                        break;
                    case PakVersion.PakVersion2:
                        bw.Write((int)0);
                        break;
                    case PakVersion.PakVersion3:
                        bw.Write(fileOffset);
                        bw.Write(0);
                        bw.Write((byte)0);
                        break;
                }
            }
            if (firstFileOffset != saveStream.Position)
                throw new IndexOutOfRangeException("Programmer error: write start offset does not match end of index.");
            foreach (ArchiveEntry entry in entries)
                CopyEntryContentsToStream(entry, saveStream);
            return true;
        }

        protected enum PakVersion
        {
            PakVersion1 = 1,
            PakVersion2 = 2,
            PakVersion3 = 3,
        }

    }
}