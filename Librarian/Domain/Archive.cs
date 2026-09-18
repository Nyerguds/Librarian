using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nyerguds.Util;

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
        public List<ArchiveEntry> FilesList { get { return _filesList; } }
        public String FileName { get; protected set; }

        public abstract Boolean LoadArchive(Stream loadStream, String archivePath);
        public abstract Boolean SaveArchive(Archive archive, Stream saveStream);
        public abstract Boolean ExtractFile(String filename, String savePath);

        public virtual void InsertFile(String filePath)
        {
            String filename = Path.GetFileName(filePath);
            Boolean replaced = false;
            for (Int32 i = 0; i < FilesList.Count; i++)
            {
                if (FilesList[i].FileName.Equals(filename, StringComparison.InvariantCultureIgnoreCase))
                {
                    FilesList[i] = new ArchiveEntry(filePath);
                    replaced = true;
                }
            }
            if (!replaced)
                FilesList.Add(new ArchiveEntry(filePath));
            this._filesList = this.FilesList.OrderBy(x => x.FileName).ToList();
        }
        
        public virtual void RemoveFiles(String[] filenames)
        {
            if (filenames == null || filenames.Length == 0)
                return;
            List<ArchiveEntry> toRemove = new List<ArchiveEntry>();
            foreach (ArchiveEntry entry in _filesList)
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
                _filesList.Remove(entry);
            this._filesList = this.FilesList.OrderBy(x => x.FileName).ToList();
        }

        public Boolean LoadArchive(String loadPath)
        {
            this.FileName = loadPath;
            using (FileStream fs = new FileStream(loadPath, FileMode.Open))
                return this.LoadArchive(fs, loadPath);
        }

        public Boolean SaveArchive(Archive archive, String savePath)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Cannot be done straight to the FileStream since unmodified entries may be read from the original file.
                if (!this.SaveArchive(archive, ms))
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
                if (!this.SaveArchive(archive, ms))
                    return null;
                return ms.ToArray();
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
        /// Attempts to load the given data as one of the known archive types.
        /// </summary>
        ///<param name="path">Path the file was loaded from.</param>
        /// <param name="fileData">File data</param>
        /// <returns>An instance of the detected archive, or null if not found.</returns>
        public static Archive LoadArchive(String path, Stream fileData)
        {
            Type archType = typeof(Archive);
            foreach (Type t in SupportedTypes)
                if (!t.IsSubclassOf(archType))
                    throw new Exception("Entries in autoDetectTypes list must all be Archive classes!");
            List<Type> notAttemptedTypes = new List<Type>();
            Boolean extFail;
            foreach (Type type in SupportedTypes)
            {
                Archive arch = TryLoad(type, fileData, true, path, out extFail);
                if (arch != null)
                    return arch;
                if (extFail)
                    notAttemptedTypes.Add(type);
            }
            foreach (Type type in notAttemptedTypes)
            {
                Archive arch = TryLoad(type, fileData, false, path, out extFail);
                if (arch != null)
                    return arch;
            }
            return null;
        }

        private static Archive TryLoad(Type type, Stream fileData, Boolean byExtension, String path, out Boolean extFail)
        {
            Archive archInstance = null;
            try { archInstance = (Archive)Activator.CreateInstance(type); }
            catch { /* Ignore; programmer error. */ }
            extFail = false;
            if (archInstance == null)
                return null;
            try
            {
                if (byExtension)
                {
                    foreach (String ext in archInstance.FileExtensions)
                    {
                        if (!path.EndsWith("." + ext.TrimStart('.'), StringComparison.InvariantCultureIgnoreCase))
                        {
                            extFail = true;
                            return null;
                        }
                    }
                }
                if (archInstance.LoadArchive(fileData, path))
                    return archInstance;
            }
            catch
            {
                return null;
            }
            return null;
        }

        public static Type[] SupportedTypes =
        {
            typeof(ArchiveLibV1),
            typeof(ArchiveLibV2),
        };
    }
}