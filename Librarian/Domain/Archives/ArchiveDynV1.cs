using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveDynV1 : Archive
    {
        protected const int FILE_ENTRY_LENGTH = 0x11;
        protected const int FILENAME_LEN = 13;
        protected const int HASH_FILE_HEADER_LEN = 6;
        protected const int HASH_ENTRY_HEADER_LEN = FILENAME_LEN + 2;

        public override string ShortTypeName { get { return "Dynamix Archive v1"; } }
        public override string ShortTypeDescription { get { return "Dynamix Archive v1"; } }
        public override string[] FileExtensions { get { return new string[] { "000", "001", "002", "003", "004", "005", "006", "007", "008", "009" }; } }
        public override bool CanSave { get { return true; } }
        //public override bool IsOrderSensitive { get { return true; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            long end = loadStream.Length;
            byte[] buffer = new byte[FILE_ENTRY_LENGTH];
            long curPos = loadStream.Position;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            while (curPos < end)
            {
                loadStream.Position = curPos;
                int indexPos = (int)curPos;
                int loaded = loadStream.Read(buffer, 0, FILE_ENTRY_LENGTH);
                if (loaded < FILE_ENTRY_LENGTH)
                    throw new FileTypeLoadException("Encountered cut-off file entry.");
                byte[] curNameB = buffer.Take(FILENAME_LEN).TakeWhile(x => x != 0).ToArray();
                if (curNameB.Length == 0 || curNameB.Length > 12)
                    throw new FileTypeLoadException("Not a Dynamix v1 archive.");
                if (curNameB.Any(c => c < 0x20 || c >= 0x7F))
                    throw new FileTypeLoadException("Filename contains nonstandard characters.");
                string curName = enc.GetString(curNameB).Trim();
                int curEntryLength = (int)ArrayUtils.ReadIntFromByteArray(buffer, 0x0D, 4, true);
                if (curEntryLength == -1)
                    curEntryLength = 0;
                if (curEntryLength < 0)
                    throw new FileTypeLoadException("Negative file length in archive entry.");
                curPos += FILE_ENTRY_LENGTH + curEntryLength;
                if (curPos < 0 || curPos > end)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");
                if (curName.Length == 0 && curEntryLength == 0)
                    continue;
                ArchiveEntry file = new ArchiveEntry(curName, archivePath, (int)loadStream.Position, curEntryLength);
                file.IndexOffset = indexPos;
                filesList.Add(file);
            }
            byte[][] hashTables = FindHashTablesFile(archivePath, out string hashFile, out int[] hashIndex, archivePath);
            if (hashTables != null && hashIndex.Length > 0 && hashIndex[0] != 0)
            {
                ReadHashesFromTable(filesList, hashTables[hashIndex[0]], end);
                ExtraInfo = "File hashes read from " + hashFile;
            }
            return filesList;
        }

        private byte[][] FindHashTablesFile(string archivePath, out string hashFile, out int[] foundIndices, params string[] filesToFind)
        {
            string[] pathsToFind = new string[filesToFind == null ? 0 : filesToFind.Length];
            if (filesToFind != null)
            {
                for (int i = 0; i < filesToFind.Length; i++)
                {
                    pathsToFind[i] = GetInternalFilename(Path.GetFileName(filesToFind[i]));
                }
            }
            foundIndices = new int[pathsToFind.Length];
            hashFile = null;
            string archiveDir = Path.GetDirectoryName(archivePath);
            string archiveBare = Path.GetFileNameWithoutExtension(archivePath);
            string archiveFull = Path.GetFullPath(archivePath);
            string[] sideFiles = Directory.GetFiles(archiveDir, archiveBare + ".*");
            byte[][] hashTables = null;
            for (int i = 0; i < sideFiles.Length; ++i)
            {
                string curFile = Path.GetFullPath(sideFiles[i]);
                if (Regex.IsMatch(Path.GetExtension(curFile), "\\.\\d{3}")
                    || string.Equals(archiveFull, curFile, StringComparison.OrdinalIgnoreCase))
                    continue;
                // Read hash map from .map
                hashTables = ReadHashTables(curFile, pathsToFind, out foundIndices);
                if (hashTables != null)
                {
                    hashFile = Path.GetFileName(curFile);
                    break;
                }
            }
            return hashTables;
        }

        private byte[][] ReadHashTables(string mapFile, string[] filesToLocate, out int[] locatedIndex)
        {
            bool[] duplicate = new bool[filesToLocate.Length];
            HashSet<string> unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < filesToLocate.Length; i++)
            {
                if (unique.Contains(filesToLocate[i]))
                    duplicate[i] = true;
                else
                    unique.Add(filesToLocate[i]);
            }

            locatedIndex = new int[filesToLocate == null ? 0 : filesToLocate.Length];
            Encoding enc = Encoding.GetEncoding(437);
            byte[] map = File.ReadAllBytes(mapFile);
            if (map.Length < HASH_FILE_HEADER_LEN + HASH_ENTRY_HEADER_LEN)
                return null;
            //int unknown1 = map[0];
            //int unknown2 = map[1];
            //int unknown3 = map[2];
            int identifier = map[3];
            if (identifier != 7)
                return null;
            int entries = ArrayUtils.ReadUInt16FromByteArrayLe(map, 4);
            byte[][] hashTables = new byte[entries + 1][];
            hashTables[0] = new byte[HASH_FILE_HEADER_LEN];
            Array.Copy(map, hashTables[0], HASH_FILE_HEADER_LEN);
            int ptr = 6;
            byte[] entryHeader = new byte[HASH_ENTRY_HEADER_LEN];
            for (int i = 1; i <= entries; ++i)
            {
                if (ptr + HASH_ENTRY_HEADER_LEN > map.Length)
                    return null;
                Array.Copy(map, ptr, entryHeader, 0, HASH_ENTRY_HEADER_LEN);
                byte[] curNameB = entryHeader.Take(FILENAME_LEN).TakeWhile(x => x != 0).ToArray();
                if (curNameB.Length == 0 || curNameB.Length > 12)
                    return null;
                if (curNameB.Any(c => c < 0x20 || c >= 0x7F))
                    return null;
                string curName = enc.GetString(curNameB).Trim();
                int entryLen = ArrayUtils.ReadUInt16FromByteArrayLe(entryHeader, FILENAME_LEN);
                int entryContentLength = HASH_ENTRY_HEADER_LEN + entryLen * 8;
                // Not enough space for this file entry.
                if (map.Length < ptr + entryContentLength)
                    return null;
                hashTables[i] = new byte[entryContentLength];
                Array.Copy(map, ptr, hashTables[i], 0, hashTables[i].Length);
                ptr += entryContentLength;
                if (filesToLocate != null)
                {
                    for (int j = 0; j < filesToLocate.Length; j++)
                    {
                        if (locatedIndex[j] == 0 && !duplicate[j] && string.Equals(curName, filesToLocate[j]))
                        {
                            locatedIndex[j] = i;
                        }
                    }
                }
            }
            return hashTables;
        }

        private void ReadHashesFromTable(List<ArchiveEntry> filesList, byte[] hashTable, long archiveSize)
        {
            int dataLen = hashTable.Length;
            int entryLen = ArrayUtils.ReadUInt16FromByteArrayLe(hashTable, FILENAME_LEN);
            int ptr = HASH_ENTRY_HEADER_LEN;
            Dictionary<int, ArchiveEntry> lookup = filesList.ToDictionary(ae => ae.IndexOffset);
            for (int i = 0; i < entryLen; ++i)
            {
                if (dataLen < ptr + 8)
                    return;
                uint hash = ArrayUtils.ReadUInt32FromByteArrayLe(hashTable, ptr);
                int offset = ArrayUtils.ReadInt32FromByteArrayLe(hashTable, ptr + 4);
                if (offset > archiveSize)
                    return;
                if (lookup.TryGetValue(offset, out ArchiveEntry entry))
                {
                    entry.HashedFilename = hash;
                    entry.HashType = HashType.Dyn1;
                }
                else
                {
                    return;
                }
                ptr += 8;
            }
        }

        /*/
        public override List<ArchiveEntry> OrderFilesList(List<ArchiveEntry> filesList, bool copy)
        {
            // Do nothing.
            return filesList;
        }
        //*/

        public override ArchiveEntry InsertFile(string filePath, int insertIndex)
        {
            // TODO put global hash lookup in RMF file in here? That would require
            // opening and reading ALL archives to get their filenames though.
            return base.InsertFile(filePath, insertIndex);
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            byte[] fileEntryHeader = new byte[FILE_ENTRY_LENGTH];
            byte[][] hashTables = FindHashTablesFile(savePath, out string hashFile, out int[] hashIndices, savePath, archive.FileName);
            if (hashTables == null)
            {
                // Create a new one based on the old one, in which the old archive filename will be replaced.
                hashTables = FindHashTablesFile(archive.FileName, out hashFile, out hashIndices, archive.FileName);
                if (hashTables != null)
                {
                    hashFile = Path.GetFileNameWithoutExtension(savePath) + Path.GetExtension(hashFile);
                }
            }
            int fileCount = archive.FilesList.Count;
            int[] writeOffsets = hashTables != null ? new int[fileCount] : null;
            int pos = 0;
            for (int i = 0; i < fileCount; ++i)
            {
                ArchiveEntry entry = archive.FilesList[i];
                string filename = GetInternalFilename(entry.FileName);
                enc.GetBytes(filename, 0, Math.Min(filename.Length, 12), fileEntryHeader, 0);
                for (int b = filename.Length; b <= FILENAME_LEN; ++b)
                    fileEntryHeader[b] = 0;
                int fileLength = entry.Length;
                if (entry.PhysicalPath != null)
                {
                    FileInfo fi = new FileInfo(entry.PhysicalPath);
                    if (!fi.Exists)
                        throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                    fileLength = (int)fi.Length;
                }
                if (writeOffsets != null)
                {
                    writeOffsets[i] = pos;
                    pos += FILE_ENTRY_LENGTH + fileLength;
                }
                // Apparently this format writes empty files with length FFFFFFFF.
                if (fileLength == 0)
                {
                    fileLength = -1;
                }
                ArrayUtils.WriteIntToByteArray(fileEntryHeader, 0x0D, 4, true, (ulong)fileLength);
                saveStream.Write(fileEntryHeader, 0, fileEntryHeader.Length);
                CopyEntryContentsToStream(entry, saveStream);
            }
            if (hashTables != null)
            {
                int replaceIndex = 0;
                // If new filename to write was found, replace that. But if not, but the original archive filename was referenced, replace that.
                if (hashIndices[0] != 0)
                    replaceIndex = hashIndices[0];
                else if (hashIndices[1] != 0)
                    replaceIndex = hashIndices[1];
                // Write hash table.
                byte[] hashTable = CreateHashTable(archive.FilesList, writeOffsets, savePath);
                if (replaceIndex == 0)
                {
                    byte[][] hashTables2 = new byte[hashTables.Length + 1][];
                    for (int i = 0; i < hashTables.Length; ++i)
                    {
                        hashTables2[i] = hashTables[i];
                    }
                    hashTables2[hashTables.Length] = hashTable;
                    ArrayUtils.WriteUInt16ToByteArrayBe(hashTables2[0], 4, (ushort)hashTables2.Length);
                }
                hashTables[replaceIndex] = hashTable;
                string writeHash = Path.Combine(Path.GetDirectoryName(savePath), hashFile);
                using (FileStream fs = new FileStream(writeHash, FileMode.Create))
                {
                    for (int i = 0; i < hashTables.Length; i++)
                    {
                        fs.Write(hashTables[i], 0, hashTables[i].Length);
                    }
                }
            }
            return true;
        }

        private byte[] CreateHashTable(List<ArchiveEntry> filesList, int[] writeOffsets, string savePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            int filesCount = filesList.Count;
            byte[] hashTable = new byte[HASH_ENTRY_HEADER_LEN + filesCount * 8];
            string filename = GetInternalFilename(Path.GetFileName(savePath));
            enc.GetBytes(filename, 0, Math.Min(filename.Length, 12), hashTable, 0);
            for (int b = filename.Length; b <= FILENAME_LEN; ++b)
                hashTable[b] = 0;
            ArrayUtils.WriteUInt16ToByteArrayLe(hashTable, FILENAME_LEN, (ushort)filesCount);
            int ptr = FILENAME_LEN + 2;
            for (int i = 0; i < filesCount; ++i)
            {
                ArrayUtils.WriteUInt32ToByteArrayLe(hashTable, ptr, filesList[i].HashedFilename.GetValueOrDefault(0xFFFFFF));
                ArrayUtils.WriteInt32ToByteArrayLe(hashTable, ptr + 4, writeOffsets[i]);
                ptr += 8;
            }
            return hashTable;
        }
    }
}