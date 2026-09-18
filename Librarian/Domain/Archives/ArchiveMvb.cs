using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveMvb : Archive
    {
        public override string ShortTypeName { get { return "PC Spiel MVB Archive"; } }
        public override string ShortTypeDescription { get { return "MVB Archive"; } }
        public override string[] FileExtensions { get { return new string[] { "mvb" }; } }
        public override bool CanSave { get { return false; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            loadStream.Position = 0;
            List<ArchiveEntry> entries = new List<ArchiveEntry>();
            int fileNum = 0;
            using (BinaryReader br = new BinaryReader(new NonDisposingStream(loadStream)))
            {
                int unkn1 = br.ReadInt32();
                int indexFileOffs = br.ReadInt32(); // also files archive index?
                int unkn2 = br.ReadInt32();
                int length = br.ReadInt32(); // full length
                long curFile = loadStream.Position;

                if (loadStream.Length != length)
                    throw new FileTypeLoadException("Archive size does not match.");
                if (indexFileOffs + 9 > length)
                    throw new FileTypeLoadException("index file header not inside file bounds.");
                loadStream.Position = indexFileOffs;
                int indexFullLength = br.ReadInt32();
                int indexFileLength = br.ReadInt32();
                if (indexFileLength + 9 != indexFullLength)
                    throw new FileTypeLoadException("index file header not correct.");
                byte indexFlags = br.ReadByte();
                if (indexFileOffs + indexFullLength > length)
                    throw new FileTypeLoadException("index file not inside file bounds.");
                byte[] indexFile = br.ReadBytes(indexFileLength);
                Dictionary<int, string> filenames = ScanIndexFile(indexFile, indexFileOffs, length);
                loadStream.Position = curFile;
                while (curFile < length)
                {
                    loadStream.Position = curFile;
                    int fullLength = br.ReadInt32();
                    int fileLength = br.ReadInt32();
                    if (fullLength == 0 && fileLength < length)
                    {
                        // Empty entry. Not sure what those are....
                        curFile = fileLength;
                        continue;
                    }
                    if (fileLength + 9 > fullLength)
                        throw new FileTypeLoadException("File header not correct.");
                    byte flags = br.ReadByte();
                    if (curFile + fullLength > length)
                        throw new FileTypeLoadException("index file not inside file bounds.");
                    if (fileLength > 0)
                    {
                        string filename;
                        if (!filenames.TryGetValue((int) curFile, out filename))
                        {
                            if (curFile == indexFileOffs)
                                filename = string.Format("{0:000000}.idx", fileNum);
                            else
                            filename = string.Format("{0:000000}.dat", fileNum);
                        }
                        ArchiveEntry ae = new ArchiveEntry(filename, archivePath, (int) (curFile + 9), fileLength);
                        if (fullLength - fileLength > 9)
                            ae.ExtraInfo = "Extra data in chunk: " + (fullLength - fileLength - 9) + " bytes";
                        if (curFile == indexFileOffs)
                            ae.ExtraInfo += (string.IsNullOrEmpty(ae.ExtraInfo) ? string.Empty : "\n") + "Filenames table";

                        entries.Add(ae);
                    }
                    curFile += fullLength;
                    fileNum++;
                }
            }
            return entries;
        }

        private Dictionary<int, string> ScanIndexFile(byte[] indexFile, int indexFilePos, int fullLength)
        {
            int indexFileEndPos = indexFilePos + indexFile.Length;
            Dictionary<int, string> filenames = new Dictionary<int, string>();
            byte[] fileChars = Encoding.ASCII.GetBytes("!#$&()+,-.0123456789@ABCDEFGHIJKLMNOPQRSTUVwXYZ[]^_`abcdefghijklmnopqrstuvwxyz{}~");
            bool[] allowed = new bool[256];
            for (int i = 0; i < fileChars.Length; ++i)
                allowed[fileChars[i]] = true;
            int bufLen = 13;
            byte[] buffer = new byte[13];
            int end = indexFile.Length;
            int curPos = 0;
            while (curPos < end)
            {
                if (!allowed[indexFile[curPos++]])
                    continue;
                int strStart = curPos - 1;
                while (allowed[indexFile[curPos]] && indexFile[curPos] != 0 && curPos < end)
                    curPos++;
                if (indexFile[curPos] != 0)
                    continue;
                int nameLen = curPos - strStart;
                if (nameLen >= bufLen)
                    continue;
                Array.Copy(indexFile, strStart, buffer, 0, nameLen);
                Array.Clear(buffer, nameLen, bufLen - nameLen);
                string filename = Encoding.ASCII.GetString(buffer, 0, nameLen);
                int indexOfDot = filename.IndexOf(".", StringComparison.OrdinalIgnoreCase);
                if (indexOfDot == 0 || indexOfDot == -1)
                    continue;
                if (filename.Length > indexOfDot + 1 && filename.IndexOf(".", indexOfDot + 1, StringComparison.OrdinalIgnoreCase) != -1)
                    continue;
                // Skip past 00
                curPos++;
                if (curPos + 4 >= end)
                    break;
                int address = (indexFile[curPos] | (indexFile[curPos + 1] << 8) | (indexFile[curPos + 2] << 16) | (indexFile[curPos + 3] << 24));
                if (address > fullLength || (address > indexFilePos && address < indexFileEndPos))
                    continue;
                if (filenames.ContainsKey(address))
                {
                    string match = filenames[address];
                    if (filename == match)
                        continue;
                    if (match.EndsWith(filename))
                        continue;
                    if (filename.EndsWith(match))
                    {
                        filenames[address] = filename;
                        continue;
                    }
                    throw new FileTypeLoadException("Conflict in index file!");
                }
                filenames.Add(address, filename);
            }
            return filenames;
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            throw new NotImplementedException();
        }

    }
}