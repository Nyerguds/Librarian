using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{
    /// <summary>
    /// Static class for the main static functions. the type can be derived from the out param, so this makes it unnecessary to put it in the function call.
    /// </summary>
    public static class FileDialogGenerator
    {

        /// <summary>
        /// generates a file open dialog with automatically generated types list. Returns the chosen filename, or null if user cancelled.
        /// The output parameter "selectedItem" will contain a (blank) object of the chosen type, or null if "all files" or "all supported types" was selected.
        /// </summary>
        /// <typeparam name="T">The basic type of which subtypes populate the typesList. Needs to inherit from FileTypeBroadcaster.</typeparam>
        /// <param name="owner">Owner window for the dialog.</param>
        /// <param name="title">Title of the dialog.</param>
        /// <param name="typesList">List of class types that inherit from T.</param>
        /// <param name="currentPath">Path to open. Can contain a filename, but only the path is used.</param>
        /// <param name="generaltypedesc">General description of the type, to be used in "All supported ???". Defaults to "files" if left blank.</param>
        /// <param name="generaltypeExt">Specific extension to always be supported. Can be left blank for none.</param>
        /// <param name="orderList">True to order the list of entries by their description.</param>
        /// <param name="selectedItem">Returns a (blank) object of the chosen type, or null if "all files" or "all supported types" was selected. Can be used for loading in the file's data.</param>
        /// <returns>The chosen filename, or null if the user cancelled.</returns>
        public static string ShowOpenFileFialog<T>(IWin32Window owner, string title, Type[] typesList, string currentPath, string generaltypedesc, string generaltypeExt, bool orderList, out T selectedItem) where T : IFileTypeBroadcaster
        {
            selectedItem = default(T);
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            FileDialogItem<T>[] items = typesList.Select(x => new FileDialogItem<T>(x)).ToArray();
            if (orderList)
                items = items.OrderBy(item => item.Description).ToArray();
            T[] correspondingObjects;
            if (title != null)
                ofd.Title = title;
            ofd.Filter = GetFileFilterForOpen<T>(items, generaltypedesc, generaltypeExt, out correspondingObjects);
            //ofd.FilterIndex = 1; // "all supported files". One-based for some fucked up reason.
            //"Westwood font files (*.fnt)|*.fnt|All Files (*.*)|*.*";
            ofd.InitialDirectory = string.IsNullOrEmpty(currentPath) ? Path.GetFullPath(".") : Path.GetDirectoryName(currentPath);
            //ofd.FilterIndex
            DialogResult res = ofd.ShowDialog(owner);
            if (res != DialogResult.OK)
                return null;
            selectedItem = correspondingObjects[ofd.FilterIndex - 1];
            return ofd.FileName;
        }

        /// <summary>
        /// Generates a file save dialog with automatically generated types list. Returns the chosen filename, or null if user cancelled.
        /// The output parameter "selectedItem" will contain a (blank) object of the chosen type that can be used to determine how to save the data.
        /// </summary>
        /// <typeparam name="T">The basic type of which subtypes populate the typesList. Needs to inherit from FileTypeBroadcaster.</typeparam>
        /// <param name="owner">Owner window for the dialog.</param>
        /// <param name="selectType">Type to select in the dropdown as default.</param>
        /// <param name="typesList">List of class types that inherit from T.</param>
        /// <param name="defaultSaveType">Default type to save as if the type was not found in the types list.</param>
        /// <param name="skipOtherExtensions">True to only use the first extension for each item in the list.</param>
        /// <param name="joinExtensions">True to combine all extensions into one filter.</param>
        /// <param name="currentPath">Path and filename to set as default in the save dialog.</param>
        /// <param name="selectedItem">Returns a (blank) object of the chosen type, or null if "all files" or "all supported types" was selected. Can be used for loading in the file's data.</param>
        /// <returns>The chosen filename, or null if the user cancelled.</returns>
        public static string ShowSaveFileFialog<T>(IWin32Window owner, Type selectType, Type[] typesList, Type defaultSaveType, bool skipOtherExtensions, bool joinExtensions, string currentPath, out T selectedItem) where T : IFileTypeBroadcaster
        {
            selectedItem = default(T);
            SaveFileDialog sfd = new SaveFileDialog();
            List<FileDialogItem<T>> items = new List<FileDialogItem<T>>();
            foreach (Type type in typesList)
            {
                FileDialogItem<T> fdi = new FileDialogItem<T>(type);
                if (fdi.ItemObject.CanSave)
                    items.Add(fdi);
            }
            int filterIndex = 0;
            bool typeFound = false;
            if (selectType != null)
            {
                for (filterIndex = 0; filterIndex < items.Count; ++filterIndex)
                {
                    if (selectType != items[filterIndex].ItemType)
                        continue;
                    typeFound = true;
                    break;
                }
            }
            if (!typeFound && defaultSaveType != null)
            {
                for (filterIndex = 0; filterIndex < items.Count; ++filterIndex)
                {
                    if (defaultSaveType != items[filterIndex].ItemType)
                        continue;
                    typeFound = true;
                    break;
                }
            }
            if (typeFound)
                filterIndex++;
            else
            {
                // detect by loaded file extension
                T specificType = FindMoreSpecificItem<T>(typesList, currentPath, selectType, out filterIndex);
                if (specificType != null && !specificType.Equals(default(T)))
                {
                    typeFound = true;
                    filterIndex++;
                }
            }
            if (!typeFound)
                filterIndex = 1;
            T[] correspondingObjects;
            sfd.Filter = GetFileFilterForSave(items.ToArray(), skipOtherExtensions, joinExtensions, out correspondingObjects);
            sfd.FilterIndex = filterIndex;
            //sfd.Filter = "Westwood font file (*.fnt)|*.fnt";
            sfd.InitialDirectory = string.IsNullOrEmpty(currentPath) ? Path.GetFullPath(".") : Path.GetDirectoryName(currentPath);
            if (!string.IsNullOrEmpty(currentPath))
            {
                string fn = Path.GetFileName(currentPath);
                string ext = Path.GetExtension(currentPath).TrimStart('.');
                T selectedType = correspondingObjects[filterIndex - 1];
                if (selectedType != null && !selectedType.Equals(default(T)) && selectedType.FileExtensions.Length > 0)
                {
                    // makes sure the extension's case matches the one in the filter, so the dialog doesn't add an additional one.
                    int extIndex = Array.FindIndex(selectedType.FileExtensions, x => x.Equals(ext, StringComparison.OrdinalIgnoreCase));
                    ext = selectedType.FileExtensions[extIndex == -1 ? 0 : extIndex];
                    fn = Path.GetFileNameWithoutExtension(currentPath) + "." + ext;
                }
                sfd.FileName = fn;
            }
            DialogResult res = sfd.ShowDialog(owner);
            if (res != DialogResult.OK)
                return null;
            selectedItem = correspondingObjects[sfd.FilterIndex - 1];
            return sfd.FileName;
        }

        private static T FindMoreSpecificItem<T>(Type[] moreSpecificTypesList, string currentPath, Type currentType, out int indexInList) where T : IFileTypeBroadcaster
        {
            indexInList = 0;
            if (currentPath == null)
                return default(T);
            FileDialogItem<T>[] items = moreSpecificTypesList.Select(x => new FileDialogItem<T>(x)).ToArray();
            string ext = Path.GetExtension(currentPath).TrimStart('.');
            T[] specificTypes = IdentifyByExtension<T>(moreSpecificTypesList, currentPath);
            T specificType = default(T);
            bool typeFound = false;
            if (specificTypes.Length > 0)
            {
                foreach (T obj in specificTypes)
                {
                    if ((currentType != null && !currentType.IsInstanceOfType(obj)) || !obj.FileExtensions.Contains(ext, StringComparer.InvariantCultureIgnoreCase))
                        continue;
                    specificType = obj;
                    break;
                }
                if (specificType != null && !specificType.Equals(default(T)))
                {
                    for (indexInList = 0; indexInList < items.Length; ++indexInList)
                    {
                        if (specificType.GetType() != items[indexInList].ItemType)
                            continue;
                        typeFound = true;
                        break;
                    }
                }
            }
            if (!typeFound)
                indexInList = 0;
            return specificType;
        }

        private static string GetFileFilterForSave<T>(FileDialogItem<T>[] fileDialogItems, bool skipOtherExtensions, bool joinExtensions, out T[] correspondingObjects) where T : IFileTypeBroadcaster
        {
            List<string> types = new List<string>();
            List<T> objects = new List<T>();
            foreach (FileDialogItem<T> itemType in fileDialogItems)
            {
                string[] extensions = itemType.Extensions;
                string[] filters = itemType.Filters;
                string[] descriptions = itemType.DescriptionsForExtensions;
                if (!skipOtherExtensions && joinExtensions)
                {
                    List<string> curTypes = new List<string>();
                    foreach (string filter in itemType.Filters.Distinct())
                        curTypes.Add(filter);
                    types.Add(string.Format("{0} ({1})|{1}", itemType.Description, string.Join(";", curTypes.ToArray())));
                    objects.Add(itemType.ItemObject);
                    continue;
                }
                int extLength = extensions.Length;
                for (int i = 0; i < extLength; ++i)
                {
                    string descr = skipOtherExtensions ? itemType.Description : descriptions[i];
                    types.Add(string.Format("{0} ({1})|{1}", descr, filters[i]));
                    T obj = itemType.ItemObject;
                    objects.Add(obj);
                    if (skipOtherExtensions)
                        break;
                }
            }
            correspondingObjects = objects.ToArray();
            return string.Join("|", types.ToArray());
        }

        private static string GetFileFilterForOpen<T>(FileDialogItem<T>[] fileDialogItems, string generaltypedesc, string generaltypeExt, out T[] correspondingObjects) where T : IFileTypeBroadcaster
        {
            // don't add a "all supported types" entry if there is only one supported type.
            bool singleItem = fileDialogItems.Length == 1;
            List<string> types = new List<string>();
            List<T> objects = new List<T>();
            HashSet<string> allTypes = singleItem ? null : new HashSet<string>();
            if (!singleItem)
            {
                types.Add(string.Empty); // to be replaced later
                objects.Add(default(T));
            }
            foreach (FileDialogItem<T> itemType in fileDialogItems)
            {
                HashSet<string> curTypes = new HashSet<string>();
                foreach (string filter in itemType.Filters.Distinct())
                {
                    curTypes.Add(filter);
                    if (!singleItem)
                        allTypes.Add(filter);
                }
                types.Add(string.Format("{0} ({1})|{1}", itemType.Description, string.Join(";", curTypes.ToArray())));
                objects.Add(itemType.ItemObject);
            }
            if (string.IsNullOrEmpty(generaltypedesc))
                generaltypedesc = "files";
            if (!singleItem)
            {
                if (!string.IsNullOrEmpty(generaltypeExt))
                    allTypes.Add("*." + generaltypeExt);
                string allTypesStr = string.Join(";", allTypes.ToArray());
                types[0] = "All supported " + generaltypedesc + " (" + allTypesStr + ")|" + allTypesStr;
            }
            types.Add("All files (*.*)|*.*");
            objects.Add(default(T));
            correspondingObjects = objects.ToArray();
            return string.Join("|", types.ToArray());
        }

        public static T[] IdentifyByExtension<T>(T[] typesList, string receivedPath) where T : IFileTypeBroadcaster
        {
            FileDialogItem<T>[] items = typesList.Select(x => new FileDialogItem<T>(x)).ToArray();
            return IdentifyByExtension(items, receivedPath);
        }

        public static T[] IdentifyByExtension<T>(Type[] typesList, string receivedPath) where T : IFileTypeBroadcaster
        {
            FileDialogItem<T>[] items = typesList.Select(x => new FileDialogItem<T>(x)).ToArray();
            return IdentifyByExtension(items, receivedPath);
        }

        public static T[] IdentifyByExtension<T>(FileDialogItem<T>[] items, string receivedPath) where T : IFileTypeBroadcaster
        {
            List<T> possibleMatches = new List<T>();
            string ext = (Path.GetExtension(receivedPath) ?? string.Empty).TrimStart('.');
            // prefer those on which it is the primary type
            // Try only the single-extension types
            foreach (FileDialogItem<T> item in items)
                if (item.Extensions.Length == 1 && item.Extensions[0].Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                    possibleMatches.Add(item.ItemObject);
            // Try primary extension of each joint type
            foreach (FileDialogItem<T> item in items)
                if (item.Extensions.Length > 1 && item.Extensions[0].Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                    possibleMatches.Add(item.ItemObject);
            // final fallback: sub-types of joint type
            foreach (FileDialogItem<T> item in items)
                if (item.Extensions.Length > 1 && item.Extensions.Skip(1).Contains(ext, StringComparer.InvariantCultureIgnoreCase))
                    possibleMatches.Add(item.ItemObject);
            return possibleMatches.ToArray();
        }

        public static T[] GetItemsList<T>(Type[] typesList) where T : IFileTypeBroadcaster
        {
            int typesListLength = typesList.Length;
            T[] items = new T[typesListLength];
            for (int i = 0; i < typesListLength; ++i)
                items[i] = (T)Activator.CreateInstance(typesList[i]);
            return items;
        }

    }

    public class FileDialogItem<T> where T : IFileTypeBroadcaster
    {
        public string[] Extensions { get; private set; }
        public string[] DescriptionsForExtensions { get; private set; }
        public string[] Filters { get { return Extensions.Select(x => "*." + x).ToArray(); } }
        public string Description { get; private set; }
        public string FullDescription
        {
            get { return string.Format("{0} (*.{1})", Description, Extensions); }
        }

        /// <summary>Returns a newly created instance of this type.</summary>
        public T ItemObject { get { return itemObjectSet ? itemObject : (T)Activator.CreateInstance(ItemType); } }

        private T itemObject;
        private bool itemObjectSet;

        public Type ItemType { get; private set; }

        public FileDialogItem(Type itemtype)
        {
            if (!itemtype.IsSubclassOf(typeof(T)))
                throw new ArgumentException("Entries in list must all be " + typeof(T).Name + " classes!", "itemtype");
            ItemType = itemtype;
            T item = ItemObject;
            if (item.FileExtensions.Length != item.DescriptionsForExtensions.Length)
                throw new ArgumentException("Entry " + ItemObject.GetType().Name + " does not have equal amount of extensions and descriptions!", "itemtype");
            Description = item.ShortTypeDescription;
            Extensions = item.FileExtensions;
            DescriptionsForExtensions = item.DescriptionsForExtensions;
        }

        public FileDialogItem(T item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            ItemType = item.GetType();
            itemObject = item;
            itemObjectSet = true;
            if (item.FileExtensions.Length != item.DescriptionsForExtensions.Length)
                throw new ArgumentException("Entry " + ItemObject.GetType().Name + " does not have equal amount of extensions and descriptions!", "item");
            Description = item.ShortTypeDescription;
            Extensions = item.FileExtensions;
            DescriptionsForExtensions = item.DescriptionsForExtensions;
        }

        public override string ToString()
        {
            return ItemObject.ShortTypeDescription;
        }
    }
}
