using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveLadyGl : Archive
    {
        const int FileNameLength = 0x0D;
        const int FileEntryLength = FileNameLength + 8;

        public override string ShortTypeName { get { return "LadyLove GL/GLT Archive"; } }
        public override string ShortTypeDescription { get { return "LadyLove GL/GLT Archive"; } }
        public override string[] FileExtensions { get { return new string[] {"glt"}; } }
        public override bool CanSave { get { return true; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            if (archivePath == null)
                throw new FileTypeLoadException("Need path to identify this type.");
            string basePath = Path.GetDirectoryName(archivePath);
            string baseName = Path.Combine(basePath, Path.GetFileNameWithoutExtension(archivePath));
            string ext = Path.GetExtension(archivePath);
            string curStreamName = archivePath;
            string secondStreamName;
            bool secondStreamIsContent;
            if (".GLT".Equals(ext, StringComparison.InvariantCultureIgnoreCase))
            {
                secondStreamIsContent = true;
                secondStreamName = baseName + ".GL";
            }
            else if (".GL".Equals(ext, StringComparison.InvariantCultureIgnoreCase))
            {
                secondStreamIsContent = false;
                secondStreamName = baseName + ".GLT";
            }
            else
            {
                if (File.Exists(baseName + ".GL"))
                {
                    secondStreamIsContent = true;
                    secondStreamName = baseName + ".GL";
                }
                else if (File.Exists(baseName + ".GLT"))
                {
                    secondStreamIsContent = false;
                    secondStreamName = baseName + ".GLT";
                }
                else
                    throw new FileTypeLoadException("Cannot find accompanying file.");
            }
            if (!File.Exists(secondStreamName))
                throw new FileTypeLoadException("Cannot find accompanying file.");
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            using (FileStream secondStream = File.OpenRead(secondStreamName))
            {
                Stream tableData = secondStreamIsContent ? loadStream : secondStream;
                Stream archiveData = secondStreamIsContent ? secondStream : loadStream;
                string archiveDataName = secondStreamIsContent ? secondStreamName : curStreamName;
                FileName = secondStreamIsContent ? curStreamName : secondStreamName;
                ExtraInfo = "Data archive: " + Path.GetFileName(archiveDataName);

                int tableLength = (int) tableData.Length;
                int dataLength = (int) archiveData.Length;
                if (tableLength % FileEntryLength != 0)
                    throw new FileTypeLoadException("Table data does not exact amount of entries.");
                int frameNr = 0;
                List<int[]> contentOverlapCheck = new List<int[]>();
                while (true)
                {
                    byte[] nameBuf = new byte[FileEntryLength];
                    int readAmount = tableData.Read(nameBuf, 0, FileEntryLength);
                    if (readAmount < FileEntryLength)
                        break;
                    string fileName = new string(nameBuf.TakeWhile(b => b != 0).Select(c => (char) (c <= 0x20 || c > 0x7F ? 0 : c)).ToArray());
                    if (fileName.Contains('\0'))
                        throw new FileTypeLoadException("Non-ascii characters in internal filename.");
                    string[] nameSplit = fileName.Split('.');
                    int actualNameLen = fileName.Length;
                    if (actualNameLen == 0 || actualNameLen > 12 || nameSplit[0].Length > 8 || nameSplit.Length > 2 || (nameSplit.Length == 2 && nameSplit[1].Length > 3))
                        throw new FileTypeLoadException("Internal filename does not match DOS 8.3 format.");
                    int fileOffset = (int) ArrayUtils.ReadIntFromByteArray(nameBuf, FileNameLength, 4, true);
                    int fileLength = (int) ArrayUtils.ReadIntFromByteArray(nameBuf, FileNameLength + 4, 4, true);
                    if (fileOffset < 0 || fileLength < 0)
                        throw new FileTypeLoadException("Bad data in table.");
                    int fileEnd = fileOffset + fileLength;

                    for (int i = 0; i < frameNr; ++i)
                    {
                        int[] prevFrameLen = contentOverlapCheck[i];
                        int prevStart = prevFrameLen[0];
                        int prevEnd = prevFrameLen[1];
                        if ((fileOffset >= prevStart && fileOffset < prevEnd) || (fileEnd >= prevStart && fileEnd < prevEnd))
                            throw new FileTypeLoadException("Overlapping files in table.");
                    }
                    contentOverlapCheck.Add(new int[] {fileOffset, fileEnd});
                    if (dataLength < fileEnd)
                        throw new FileTypeLoadException("Internal file does not fit in archive.");
                    filesList.Add(new ArchiveEntry(fileName, archiveDataName, fileOffset, fileLength));
                    frameNr++;
                }
            }
            return filesList;
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            if (savePath == null)
                throw new ArgumentException("This type needs a filename since it writes its data to an accompanying file.");
            if ( ".GL".Equals(Path.GetExtension(savePath)))
                throw new ArgumentException("Suggested name cannot have extension \".gl\"; it is reserved for the data file.");
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            int nrOfEntries = entries.Length;
            string dataPath = Path.Combine(Path.GetDirectoryName(savePath), Path.GetFileNameWithoutExtension(savePath) + ".gl");

            byte[] buffer = new byte[FileEntryLength];
            using (FileStream dataSaveStream = File.OpenWrite(dataPath))
            {
                for (int i = 0; i < nrOfEntries; ++i)
                {
                    ArchiveEntry entry = entries[i];
                    if (entry.FileName.Any(c => c <= 0x20 || c > 0x7F))
                        throw new ArgumentException("Filenames must be pure ASCII.");
                    string fileName = entry.FileName;
                    string[] nameSplit = fileName.Split('.');
                    int actualNameLen = fileName.Length;
                    if (actualNameLen == 0 || actualNameLen > 12 || nameSplit[0].Length > 8 || nameSplit.Length > 2 || (nameSplit.Length == 2 && nameSplit[1].Length > 3))
                        throw new FileTypeLoadException("Filenames must match DOS 8.3 format.");
                    byte[] nameBytes = Encoding.ASCII.GetBytes(entry.FileName);
                    Array.Clear(buffer, 0, FileNameLength);
                    Array.Copy(nameBytes, buffer, nameBytes.Length);
                    ArrayUtils.WriteIntToByteArray(buffer, FileNameLength, 4, true, (uint)dataSaveStream.Position);
                    int fileLength = entry.Length;
                    if (entry.PhysicalPath != null)
                    {
                        FileInfo fi = new FileInfo(entry.PhysicalPath);
                        if (!fi.Exists)
                            throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                        fileLength = (int)fi.Length;
                    }
                    ArrayUtils.WriteIntToByteArray(buffer, FileNameLength + 4, 4, true, (uint)fileLength);
                    saveStream.Write(buffer, 0, FileEntryLength);
                    CopyEntryContentsToStream(entry, dataSaveStream);
                }
            }
            return true;
        }
    }
}