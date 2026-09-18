using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibrarianTool.Domain.Archives;
using Nyerguds.Util;
using Nyerguds.Util.UI;

namespace LibrarianTool.Domain
{
    public abstract class Archive : FileTypeBroadcaster
    {
        public abstract String ShortTypeName { get; }
        public abstract String ShortTypeDescription { get; }
        public abstract String[] FileExtensions { get; }
        public virtual String[] DescriptionsForExtensions { get { return Enumerable.Repeat(this.ShortTypeDescription, this.FileExtensions.Length).ToArray(); } }
        public virtual String FileExtension { get; set; }
        /// <summary>Supported types can always be loaded, but this indicates if save functionality to this type is also available.</summary>
        public virtual Boolean CanSave { get { return true; } }
        
        protected List<ArchiveEntry> _filesList = new List<ArchiveEntry>();
        public List<ArchiveEntry> FilesList { get { return this._filesList; } }
        public String FileName { get; protected set; }
        public virtual String ExtraInfo { get; protected set; }
        
        /// <summary>Reads the file, and fills in the _filesList list;</summary>
        /// <param name="loadPath">Path to load the file from.</param>
        /// <returns>True if loading succeeded.</returns>
        public void LoadArchive(String loadPath)
        {
            this.FileName = loadPath;
            using (FileStream fs = new FileStream(loadPath, FileMode.Open))
                this.LoadArchiveInternal(fs, loadPath);
        }

        /// <summary>Reads the stream, and fills in the _filesList list;</summary>
        /// <param name="loadStream">Stream to load the file from.</param>
        /// <param name="archivePath">Path of the loaded archive.</param>
        /// <returns>True if loading succeeded.</returns>
        public void LoadArchive(Stream loadStream, String archivePath)
        {
            this.FileName = archivePath;
            this.LoadArchiveInternal(loadStream, archivePath);
        }

        /// <summary>Reads the bytes, and fills in the _filesList list;</summary>
        /// <param name="loadData">Data to load the file from.</param>
        /// <param name="archivePath">Path of the loaded archive.</param>
        /// <returns>True if loading succeeded.</returns>
        public void LoadArchive(Byte[] loadData, String archivePath)
        {
            this.FileName = archivePath;
            using (MemoryStream ms = new MemoryStream(loadData))
                this.LoadArchiveInternal(ms, archivePath);
        }

        /// <summary>Reads the stream, and fills in the _filesList list;</summary>
        /// <param name="loadStream">Stream to load the file from.</param>
        /// <param name="archivePath">Path of the loaded archive.</param>
        /// <returns>True if loading succeeded.</returns>
        protected abstract void LoadArchiveInternal(Stream loadStream, String archivePath);

        /// <summary>Saves the given archive as this specific type</summary>
        /// <param name="archive">Archive to save as this type.</param>
        /// <param name="saveStream">Stream to save the archive to.</param>
        /// <param name="savePath">Path that the file will be saved to. Sometimes needed for saving accompanying files.</param>
        /// <returns></returns>
        public abstract Boolean SaveArchive(Archive archive, Stream saveStream, String savePath);

        /// <summary>Extracts the requested file from the _filesList list.</summary>
        /// <param name="filename">Name of the file to extract.</param>
        /// <param name="savePath">Path to save the file to.</param>
        /// <returns></returns>
        public Boolean ExtractFile(String filename, String savePath)
        {
            Int32 index;
            return this.ExtractFile(this.FindFile(filename, out index), savePath);
        }

        /// <summary>Extracts the requested file from the _filesList list.</summary>
        /// <param name="entry">Name of the file to extract.</param>
        /// <param name="savePath">Path to save the file to.</param>
        /// <returns></returns>
        public virtual Boolean ExtractFile(ArchiveEntry entry, String savePath)
        {
            if (entry == null)
                return false;
            using (FileStream fs = new FileStream(savePath, FileMode.Create))
                CopyEntryContentsToStream(entry, fs);
            return true;
        }

        /// <summary>
        /// Find file entry in archive. Override this for things like hash lookups.
        /// </summary>
        /// <param name="filePath">path of a file of which to look up whether a file with that name exists in the archive.</param>
        /// <param name="index">Index at which this file was found.</param>
        /// <returns>the found entry</returns>
        public virtual ArchiveEntry FindFile(String filePath, out Int32 index)
        {
            String filename = this.GetInternalFilename(filePath);
            for (Int32 i = 0; i < this._filesList.Count; i++)
            {
                ArchiveEntry current = this._filesList[i];
                if (current.FileName.Equals(filename, StringComparison.InvariantCultureIgnoreCase))
                {
                    index = i;
                    return current;
                }
            }
            index = -1;
            return null;
        }
        
        /// <summary>Inserts a file into the archive. This can be overridden to add filtering on the input.</summary>
        /// <param name="filePath">Path of the file to load.</param>
		public virtual void InsertFile(String filePath)
		{
			String internalFilename = this.GetInternalFilename(Path.GetFileName(filePath));
			Int32 foundIndex;
			this.FindFile(filePath, out foundIndex);
			this.InsertFileInternal(filePath, internalFilename, foundIndex);
			this.OrderFilesListInternal(this._filesList);
		}

        /// <summary>Inserts a file into the archive. This can be overridden to add filtering on the input.</summary>
        /// <param name="filePath">Path of the file to load.</param>
        /// <param name="internalFilename">Filename as it is stored in the archive.</param>
		public virtual void InsertFile(String filePath, String internalFilename)
		{

			internalFilename = this.GetInternalFilename(internalFilename);
			Int32 foundIndex;
			this.FindFile(internalFilename, out foundIndex);
			this.InsertFileInternal(filePath, internalFilename, foundIndex);
			this.OrderFilesListInternal(this._filesList);
		}

		protected virtual void InsertFileInternal(String filePath, String internalFilename, Int32 foundIndex)
		{
		    if (foundIndex == -1)
		        this._filesList.Add(new ArchiveEntry(filePath, internalFilename));
		    else
		        this._filesList[foundIndex] = new ArchiveEntry(filePath, internalFilename, this._filesList[foundIndex].ExtraInfo);
		}

        protected virtual void OrderFilesListInternal(List<ArchiveEntry> filesList)
		{
            List<ArchiveEntry> orderedList = this.FilesList.OrderBy(x => x.FileName).ToList();
			filesList.Clear();
			filesList.AddRange(orderedList);
		}

        /// <summary>
        /// Converts the filename to the type supported internally. By default, this strips
        /// out all non-ascii characters, converts to uppercase, and limits the length to 8.3.
        /// Override if needed.
        /// </summary>
        /// <param name="filePath">Original file path.</param>
        /// <returns></returns>
        public virtual String GetInternalFilename(String filePath)
        {
            String filename = Path.GetFileNameWithoutExtension(filePath) ?? String.Empty;
            String extension = Path.GetExtension(filePath) ?? String.Empty;
            if (filename.Length > 8)
                filename = filename.Substring(0, 8);
            if (extension.Length > 4)
                extension = extension.Substring(0, 4);
            return new String((filename + extension).ToUpperInvariant().Replace(' ','_').Where(x => x > 0x20 && x < 0x7F).ToArray());
        }

        public virtual void RemoveFiles(String[] filenames)
        {
            if (filenames == null || filenames.Length == 0)
                return;
            List<ArchiveEntry> toRemove = new List<ArchiveEntry>();
            foreach (ArchiveEntry entry in this._filesList)
            {
                Boolean inList = false;
                foreach (String filename in filenames)
                {
                    if (!entry.FileName.Equals(filename, StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    inList = true;
                    break;
                }
                if (!inList)
                    continue;
                toRemove.Add(entry);
                break;
            }
            foreach (ArchiveEntry entry in toRemove)
                this._filesList.Remove(entry);
            this._filesList = this.FilesList.OrderBy(x => x.FileName).ToList();
        }

        public Boolean SaveArchive(Archive archive, String savePath)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Cannot be done straight to the FileStream since unmodified entries may be read from the original file.
                if (!this.SaveArchive(archive, ms, savePath))
                    return false;
                ms.Position = 0;
                using (FileStream fs = new FileStream(savePath, FileMode.Create))
                    CopyStream(ms, fs, ms.Length);
            }
            return true;
        }
        
        public Byte[] SaveArchive(Archive archive)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                if (!this.SaveArchive(archive, ms, null))
                    return null;
                return ms.ToArray();
            }
        }
        
        protected static void CopyEntryContentsToStream(ArchiveEntry entry, Stream saveStream)
        {
            String readFile;
            Int32 start;
            Int32 length;
            if (entry.PhysicalPath != null)
            {
                readFile = entry.PhysicalPath;
                start = 0;
                FileInfo fi = new FileInfo(entry.PhysicalPath);
                length = (Int32)fi.Length;
            }
            else
            {
                readFile = entry.ArchivePath;
                start = entry.StartOffset;
                length = entry.Length;
            }
            using (FileStream fs = new FileStream(readFile, FileMode.Open))
            {
                fs.Seek(start, SeekOrigin.Begin);
                CopyStream(fs, saveStream, length);
            }
        }

        public static void CopyStream(Stream input, Stream output, Int64 length)
        {
            Int64 remainder = length;
            Byte[] buffer = new Byte[Math.Min(length, 0x8000)];
            Int32 read;
            while (remainder > 0 && (read = input.Read(buffer, 0, (Int32)Math.Min(remainder, buffer.Length))) > 0)
            {
                output.Write(buffer, 0, read);
                remainder -= read;
            }
        }
        
        /// <summary>
        /// Autodetects the file type from the given list, and if that fails, from the full autodetect list.
        /// </summary>
        /// <param name="fileStream">File dat to load file from.</param>
        /// <param name="path">File path, used for extension filtering and file initialisation. Not for reading as bytes; fileData is used for that.</param>
        /// <param name="preferredTypes">List of the most likely types it can be.</param>
        /// <param name="loadErrors">Returned list of occurred errors during autodetect.</param>
        /// <param name="onlyGivenTypes">True if only the possibleTypes list is processed to autodetect the type.</param>
        /// <returns>The detected type, or null if detection failed.</returns>
        public static Archive LoadArchiveAutodetect(Stream fileStream, String path, Archive[] preferredTypes, Boolean onlyGivenTypes, out List<FileTypeLoadException> loadErrors)
        {
            loadErrors = new List<FileTypeLoadException>();
            // See which extensions match, and try those first.
            if (preferredTypes == null)
                preferredTypes = FileDialogGenerator.IdentifyByExtension<Archive>(AutoDetectTypes, path);
            else if (onlyGivenTypes)
            {
                // Try extension-filtering first, then the rest.
                Archive[] preferredTypesExt = FileDialogGenerator.IdentifyByExtension(preferredTypes, path);
                foreach (Archive typeObj in preferredTypesExt)
                {
                    try
                    {
                        fileStream.Position = 0;
                        typeObj.LoadArchive(fileStream, path);
                        return typeObj;
                    }
                    catch (FileTypeLoadException e)
                    {
                        e.AttemptedLoadedType = typeObj.ShortTypeName;
                        loadErrors.Add(e);
                    }
                    preferredTypes = preferredTypes.Where(tp => preferredTypesExt.All(tpe => tpe.GetType() != tp.GetType())).ToArray();
                }
            }
            foreach (Archive typeObj in preferredTypes)
            {
                try
                {
                    fileStream.Position = 0;
                    typeObj.LoadArchive(fileStream, path);
                    return typeObj;
                }
                catch (FileTypeLoadException e)
                {
                    e.AttemptedLoadedType = typeObj.ShortTypeName;
                    loadErrors.Add(e);
                }
            }
            if (onlyGivenTypes)
                return null;
            foreach (Type type in AutoDetectTypes)
            {
                // Skip entries on the already-tried list.
                if (preferredTypes.Any(x => x.GetType() == type))
                    continue;
                Archive objInstance = null;
                try { objInstance = (Archive)Activator.CreateInstance(type); }
                catch { /* Ignore; programmer error. */ }
                if (objInstance == null)
                    continue;
                try
                {
                    objInstance.LoadArchive(fileStream, path);
                    return objInstance;
                }
                catch (FileTypeLoadException e)
                {
                    // objInstance should not be disposed here since it never succeeded in initializing,
                    // and should not contain any loaded images at that point.
                    e.AttemptedLoadedType = objInstance.ShortTypeName;
                    loadErrors.Add(e);
                    //objInstance.Dispose();
                }
            }
            return null;
        }

        public static Type[] SupportedSaveTypes
        {
            get
            {
                List<Type> saveTypes = new List<Type>();
                foreach (Type type in SupportedTypes)
                {
                    Archive objInstance = null;
                    try
                    {
                        objInstance = (Archive) Activator.CreateInstance(type);
                    }
                    catch
                    {
                        /* Ignore; programmer error. */
                    }
                    if (objInstance == null)
                        continue;
                    if (objInstance.CanSave)
                        saveTypes.Add(type);
                }
                return saveTypes.ToArray();
            }
        }

        public static Type[] SupportedTypes =
        {
            typeof(ArchiveLibV1),
            typeof(ArchiveLibV2),
            typeof(ArchivePakV1),
            typeof(ArchivePakV2),
            typeof(ArchivePakV3),
            typeof(ArchiveDynV1),
            typeof(ArchiveDynV2),
            typeof(ArchiveRenpy),
            typeof(ArchiveM3),
			typeof(ArchiveSndKort),
        };

        public static Type[] AutoDetectTypes =
        {
            typeof(ArchiveRenpy),
            typeof(ArchiveLibV1),
            typeof(ArchiveLibV2),
            typeof(ArchiveM3),
            typeof(ArchivePakV3),
            typeof(ArchivePakV2),
            typeof(ArchivePakV1),
            typeof(ArchiveDynV1),
            typeof(ArchiveDynV2),
			typeof(ArchiveSndKort),
        };

    }
}