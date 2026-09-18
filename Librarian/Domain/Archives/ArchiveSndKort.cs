using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibrarianTool.Domain.Archives
{
	public class ArchiveSndKort : Archive
	{
        public override string ShortTypeName { get { return "KORT SND Archive"; } }
        public override string ShortTypeDescription { get { return "KORT SND"; } }
        public override string[] FileExtensions { get { return new string[] { "SND" }; } }
        public override bool CanSave { get { return true; } }

        protected const string BufferInfoFormat = "Buffer info: 0x{0:X8}";
        
		protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
		{
			loadStream.Position = 0;
			ExtraInfo = string.Empty;
            byte[] filesCount = new byte[2];
            int amount = loadStream.Read(filesCount, 0, 2);
            int nrOfFiles = (int)ArrayUtils.ReadIntFromByteArray(filesCount, 0, 2, true);
		    if (amount != 2)
		        throw new FileTypeLoadException("Too short to be a " + ShortTypeDescription + " archive.");
            byte[] buffer = new byte[25];
            long firstPos = 2 + nrOfFiles * 25;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
			while (loadStream.Position < firstPos)
			{
				amount = loadStream.Read(buffer, 0, 25);
			    if (amount < 25)
			        throw new FileTypeLoadException("Header too small! Not a " + ShortTypeDescription + " archive.");
                uint size = (uint)ArrayUtils.ReadIntFromByteArray(buffer, 0, 4, true);
                uint buff = (uint)ArrayUtils.ReadIntFromByteArray(buffer, 4, 4, true);
                byte[] buffBytes = new byte[4];
                Array.Copy(buffer, 4, buffBytes, 0, 4);
                uint offset = (uint)ArrayUtils.ReadIntFromByteArray(buffer, 8, 4, true);
			    if (offset + size > loadStream.Length)
			        throw new FileTypeLoadException("Header refers to data outside the file! Not a " + ShortTypeDescription + " archive.");
			    if (offset < firstPos)
			        throw new FileTypeLoadException("Header refers to data inside header! Not a " + ShortTypeDescription + " archive.");
                string filename = new string(buffer.Skip(12).TakeWhile(b => b != 0).Select(c => (char)(c <= 0x20 || c > 0x7F ? 0 : c)).ToArray());
			    if (filename.Contains('\0'))
			        throw new FileTypeLoadException("Non-ASCII filename characters found in header! Not a " + ShortTypeDescription + " archive.");
			    ArchiveEntry archiveEntry = new ArchiveEntry(filename, archivePath, (int)offset, (int)size);
				StringBuilder sbExtraInfo = new StringBuilder();
                sbExtraInfo.Append(string.Format(BufferInfoFormat, buff));
				IdentifyType(loadStream, offset, size, sbExtraInfo);
				archiveEntry.ExtraInfo = sbExtraInfo.ToString();
                archiveEntry.ExtraInfoBin = buffBytes;
				filesList.Add(archiveEntry);
			}
			ExtraInfo = "WARNING - The unknown 'Buffer' value will only be preserved when REPLACING files.";
		    return filesList;
		}

		protected void IdentifyType(Stream loadStream, uint offset, uint size, StringBuilder sbExtraInfo)
		{
            bool isVoc = false;
            long savedPos = loadStream.Position;
			if (size > 19u)
			{
                byte[] buff = new byte[19];
				loadStream.Position = offset;
				loadStream.Read(buff, 0, buff.Length);
				if (Encoding.ASCII.GetString(buff).Equals("Creative Voice File"))
				{
					sbExtraInfo.Append("\nType: Creative Voice File");
					isVoc = true;
				}
			}
			if (!isVoc && size > 8u)
			{
                byte[] buff = new byte[8];
				loadStream.Position = offset;
				loadStream.Read(buff, 0, buff.Length);
				if (Encoding.ASCII.GetString(buff, 0, 4).Equals("CTMF"))
				{
                    sbExtraInfo.Append("\nType: Creative Music Format");
				}
			}
			loadStream.Position = savedPos;
		}

        protected override ArchiveEntry InsertFileInternal(string filePath, string internalFilename, int foundIndex)
		{
            ArchiveEntry entry;
            byte[] extraInfoBin;
            StringBuilder sb = new StringBuilder();
            if (foundIndex == -1)
            {
                entry = new ArchiveEntry(filePath, internalFilename);
                extraInfoBin = new byte[4];
            }
            else
            {
                entry = new ArchiveEntry(filePath, internalFilename);
                extraInfoBin = _filesList[foundIndex].ExtraInfoBin;
            }
            if (extraInfoBin != null && extraInfoBin.Length >= 4)
            {
                uint buff = (uint) ArrayUtils.ReadIntFromByteArray(extraInfoBin, 0, 4, true);
                sb.Append(string.Format(BufferInfoFormat, buff));
            }
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
		    {
		        IdentifyType(fs, 0u, (uint) fs.Length, sb);
            }
            entry.ExtraInfo = sb.ToString();
            entry.ExtraInfoBin = extraInfoBin;
            if (foundIndex == -1)
                _filesList.Add(entry);
            else
		        _filesList[foundIndex] = entry;
            return entry;
		}

	    protected override void OrderFilesListInternal(List<ArchiveEntry> filesList)
		{
			List<ArchiveEntry> orderedList = filesList.OrderBy(x => x.FileName, new ExtensionSorter()).ToList();
			filesList.Clear();
			filesList.AddRange(orderedList);
		}

		public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
		{
			List<ArchiveEntry> filesList = archive.FilesList.ToList();
			OrderFilesListInternal(filesList);
            int nrOfFiles = filesList.Count;
            int firstFileOffset = 2 + 25 * nrOfFiles;
            int fileOffset = firstFileOffset;
			Encoding enc = Encoding.GetEncoding(437);
            byte[] nameBuffer = new byte[13];
			using (BinaryWriter bw = new BinaryWriter(new NonDisposingStream(saveStream)))
			{
				bw.Write((ushort)nrOfFiles);
				foreach (ArchiveEntry entry in filesList)
				{
                    int fileLength = entry.Length;
                    string curName = entry.FileName;
					if (entry.PhysicalPath != null)
					{
						FileInfo fi = new FileInfo(entry.PhysicalPath);
						if (!fi.Exists)
							throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
						fileLength = (int)fi.Length;
						curName = GetInternalFilename(entry.FileName);
					}
					bw.Write(fileLength);
                    uint buff = 0u;
                    byte[] extraInfoBin = entry.ExtraInfoBin;
                    if (extraInfoBin != null && extraInfoBin.Length >= 4)
                        buff = (uint)ArrayUtils.ReadIntFromByteArray(extraInfoBin, 0, 4, true);
					bw.Write(buff);
					bw.Write(fileOffset);
					fileOffset += fileLength;
                    int copySize = curName.Length;
					Array.Copy(enc.GetBytes(curName), 0, nameBuffer, 0, copySize);
					for (int b = copySize; b < 13; ++b)
					{
						nameBuffer[b] = 0;
					}
					bw.Write(nameBuffer, 0, nameBuffer.Length);
				}
			}
		    if (firstFileOffset != saveStream.Position)
		        throw new IndexOutOfRangeException("Programmer error: write start offset does not match end of index.");
		    foreach (ArchiveEntry entry in filesList)
				CopyEntryContentsToStream(entry, saveStream);
			return true;
		}
	}
}
