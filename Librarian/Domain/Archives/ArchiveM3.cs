using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    /// <summary>
    /// Interactive Girls Club .m3 / .slb archive format. 
    /// Very simple archive without file names. To handle internal order correctly, just name files accordingly.
    /// </summary>
    public class ArchiveM3 : Archive
    {
        protected const int FileEntryLength = 0x11;

        public override string ShortTypeName { get { return "Interactive Girls Archive"; } }
        public override string ShortTypeDescription { get { return "Interactive Girls Archive"; } }
        public override string[] FileExtensions { get { return new string[] { "m3", "slb" }; } }
        public override bool CanSave { get { return true; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            loadStream.Position = 0;
            long streamLength = loadStream.Length;
            byte[] addressBuffer = new byte[4];
            if (loadStream.Read(addressBuffer, 0, 4) < 4)
                throw new FileTypeLoadException("Archive not long enough to read file offset!");
            if (ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 4, true) != 0)
                throw new FileTypeLoadException("Not an IGC Archive!");
            int readOffs = 4;
            int minOffs = int.MaxValue;
            int indexOffs = 0;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            do
            {
                int prevIndexOffs = indexOffs;
                if (loadStream.Read(addressBuffer, 0, 4) < 4)
                    throw new FileTypeLoadException("Archive not long enough to read file offset!");
                indexOffs = (int)ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 4, true);
                minOffs = Math.Min(minOffs, indexOffs);
                if (indexOffs > streamLength || prevIndexOffs >= indexOffs)
                    throw new FileTypeLoadException("Not an IGC archive!");
                if (prevIndexOffs != 0)
                {
                    bool isImage = prevIndexOffs + 0x1B <= indexOffs;
                    bool isScript = prevIndexOffs + 2 <= indexOffs;
                    if (isImage || isScript)
                    {
                        // Check for image                        
                        loadStream.Position = prevIndexOffs;
                        ushort script;
                        if (!isImage)
                        {
                            loadStream.Read(addressBuffer, 0, 2);
                            script = (ushort)ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 2, true);
                        }
                        else
                        {
                            loadStream.Read(addressBuffer, 0, 4);
                            uint magic01 = (uint) ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 4, true);
                            script = (ushort)ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 2, true);
                            loadStream.Position = prevIndexOffs + 0x12;
                            loadStream.Read(addressBuffer, 0, 4);
                            uint magic02 = (uint) ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 4, true);
                            isImage = magic01 == 0x01325847 && magic02 == 0x58465053;
                        }
                        isScript = !isImage && script == 0x7E7C;
                        loadStream.Position = readOffs;
                    }
                    string filename = filesList.Count.ToString("00000000") + "." + (isImage ? "gx2" : (isScript? "txt" : "dat"));
                    filesList.Add(new ArchiveEntry(filename, archivePath, prevIndexOffs, indexOffs - prevIndexOffs));
                }
                readOffs += 4;
                loadStream.Position = readOffs;
            } while (readOffs < minOffs && indexOffs < streamLength);
            if (readOffs != minOffs || indexOffs != streamLength)
                throw new FileTypeLoadException("Not an IGC archive!");
            return filesList;
        }

        public override string GetInternalFilename(string filePath)
        {
            return Path.GetFileName(filePath);
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            int firstFileOffset = (entries.Length + 2) * 4;
            int fileOffset = firstFileOffset;
            using (BinaryWriter bw = new BinaryWriter(new NonDisposingStream(saveStream)))
            {
                bw.Write((uint)0);
                foreach (ArchiveEntry entry in entries)
                {
                    int fileLength = entry.Length;
                    if (entry.PhysicalPath != null)
                    {
                        // To be 100% sure the index is OK, this is updated at the moment of writing.
                        FileInfo fi = new FileInfo(entry.PhysicalPath);
                        if (!fi.Exists)
                            throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                        fileLength = (int)fi.Length;
                    }
                    bw.Write(fileOffset);
                    fileOffset += fileLength;
                }
                bw.Write(fileOffset);
            }
            if (firstFileOffset != saveStream.Position)
                throw new IndexOutOfRangeException("Programmer error: write start offset does not match end of index.");
            foreach (ArchiveEntry entry in entries)
                CopyEntryContentsToStream(entry, saveStream);
            return true;
        }
    }
}