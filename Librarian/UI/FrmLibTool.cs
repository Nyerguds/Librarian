using LibrarianTool.Domain;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace LibrarianTool
{
    public partial class FrmLibTool : Form
    {
        public delegate void InvokeDelegateReload(Archive newFile, bool asNew, bool resetZoom);
        public delegate DialogResult InvokeDelegateMessageBox(string message, MessageBoxButtons buttons, MessageBoxIcon icon);
        public delegate DialogResult InvokeDelegateMessageBoxDef(string message, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defButtons);
        public delegate void InvokeDelegateTwoArgs(object arg1, object arg2);
        public delegate void InvokeDelegateSingleArg(object value);
        public delegate void InvokeDelegateEnableControls(bool enabled, string processingLabel);

        private const string PROG_NAME = "Librarian";
        private const string PROG_AUTHOR = "Created by Nyerguds";

        protected readonly string m_ProgFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        protected string m_LastOpenedFolder;
        protected Archive m_LoadedArchive;
        protected List<ArchiveEntry> m_FilesListOrigState;
        protected string argFile;

        public FrmLibTool(string[] args)
            :this()
        {
            if (args.Length > 0 && File.Exists(args[0]))
                argFile = args[0];
        }

        public FrmLibTool()
        {
            InitializeComponent();
            AddNewTypes();
            m_LastOpenedFolder = m_ProgFolder;
            Text = GetTitle(true, true);
        }

        public string GetTitle(bool withAuthor, bool withLoadedArchive)
        {
            StringBuilder title = GetTitleBuilder(withAuthor);
            if (withLoadedArchive && m_LoadedArchive != null)
            {
                title.Append(" - ");
                if (m_LoadedArchive.FileName == null)
                    title.Append("New archive");
                else
                    title.Append("\"").Append(Path.GetFileName(m_LoadedArchive.FileName)).Append("\"");
                if (IsArchiveModified())
                    title.Append(" *");
                title.Append(" (").Append(m_LoadedArchive.ShortTypeDescription).Append(")");
            }
            return title.ToString();
        }

        public static string GetTitle(bool withAuthor)
        {
            return GetTitleBuilder(withAuthor).ToString();
        }

        public static StringBuilder GetTitleBuilder(bool withAuthor)
        {
            StringBuilder title = new StringBuilder(PROG_NAME);
            title.Append(" ").Append(GeneralUtils.ProgramVersion());
            if (withAuthor)
                title.Append(" - ").Append(PROG_AUTHOR);
            return title;
        }

        private void AddNewTypes()
        {
            foreach (Type type in Archive.SupportedTypes)
            {
                Archive archInstance = null;
                try { archInstance = (Archive)Activator.CreateInstance(type); }
                catch { /* Ignore; programmer error. */ }
                if (archInstance == null || !archInstance.CanSave)
                    continue;
                ToolStripMenuItem archtypeMenu = new ToolStripMenuItem();
                archtypeMenu.Text = archInstance.ShortTypeDescription;
                archtypeMenu.Tag = type;
                archtypeMenu.Click += NewFileClick;
                tsmiFileNew.DropDownItems.Add(archtypeMenu);
            }
        }

        private void NewFileClick(object sender, EventArgs e)
        {
            ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
            Type type;
            if (tsmi == null || (type = tsmi.Tag as Type) == null)
                return;
            Archive archInstance;
            try { archInstance = (Archive)Activator.CreateInstance(type); }
            catch { return; }
            LoadArchive(archInstance, true);
        }


        private void FrmLibTool_Shown(object sender, EventArgs e)
        {
            if (argFile != null)
                DetectArchive(argFile, true);
            else
                LoadArchive(null, false);
        }

        private void Frm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Frm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1)
                return;
            string path = files[0];
            m_LastOpenedFolder = Path.GetDirectoryName(path);
            Archive arch = DetectArchive(path, true);
            if (arch != null)
                LoadArchive(arch, true);
        }

        private void Lv_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Lv_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0)
                return;
            if (m_LoadedArchive == null)
            {
                const string message = "No archive has been opened.\n\n" +
                                       "To make a new archive, use the \"New archive\" function in the menu.\n\n" +
                                       "To open an archive, drop it into the area outside the files list.";
                Invoke(new InvokeDelegateMessageBox(ShowMessageBox), message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
                AddFiles(files);
        }

        private void AddFiles(string[] files)
        {
            if (files.Length == 0)
                return;
            if (m_LoadedArchive == null)
                return;
            List<string> filesList = new List<string>();
            List<string> filesListCustomNames = new List<string>();
            foreach (string file in files)
            {
                bool isDirectory;
                try
                {
                    isDirectory = (File.GetAttributes(file) & FileAttributes.Directory) == FileAttributes.Directory;
                }
                catch (Exception ex)
                {
                    string err = String.Format("Error accessing file.\n\nPath:\n{0}\n\nError:\n{1}.", file, ex.Message);
                    MessageBox.Show(err, GetTitle(false, false));
                    return;
                }
                if (isDirectory)
                {
                    if (!m_LoadedArchive.SupportsFolders)
                    {
                        MessageBox.Show("This archive type does not support folders.", GetTitle(false, false));
                        return;
                    }
                    string containingFolder = Path.GetDirectoryName(file);
                    AddFilesRecursive(file, containingFolder, filesList, filesListCustomNames);
                }
                else
                {
                    filesList.Add(file);
                    filesListCustomNames.Add(null);
                }
            }
            string[] adaptedFiles = new string[filesList.Count];
            for (int i = 0; i < filesList.Count; ++i)
            {
                if (filesListCustomNames[i] == null)
                    m_LoadedArchive.InsertFile(filesList[i]);
                else
                    m_LoadedArchive.InsertFile(filesList[i], filesListCustomNames[i]);
            }
            int firstIndex = m_LoadedArchive.FilesList.Count;
            ArchiveEntry firstEntry = null;
            for (int i = 0; i < filesList.Count; ++i)
            {
                ArchiveEntry entry = m_LoadedArchive.FindFile(filesListCustomNames[i] ?? filesList[i], out int index);
                if (index < firstIndex)
                {
                    firstIndex = index;
                    firstEntry = entry;
                }
            }
            LoadArchive(m_LoadedArchive, false, firstEntry?.FileName, firstEntry?.HashedFilename);
        }

        private void AddFilesRecursive(string file, string basePath, List<string> filesList, List<string> filesListCustomNames)
        {
            string fullBasePath = Path.GetFullPath(basePath);
            string fullFilePath = Path.GetFullPath(file);
            int basePathLen = fullBasePath.Length + 1;
            filesList.Add(file);
            string filePathRel = fullFilePath.Substring(basePathLen);
            filesListCustomNames.Add(filePathRel.Contains(Path.PathSeparator) ? filePathRel : null);
            if ((File.GetAttributes(file) & FileAttributes.Directory) == 0)
                return;
            string[] files = Directory.GetFiles(file);
            filesList.AddRange(files);
            for (int i = 0; i < files.Length; ++i)
                files[i] = files[i].Substring(basePathLen);
            filesListCustomNames.AddRange(files);
            string[] subDirs = Directory.GetDirectories(file);
            foreach (string subDir in subDirs)
                AddFilesRecursive(subDir, basePath, filesList, filesListCustomNames);
        }

        private Archive DetectArchive(string path, bool showErrors)
        {
            return DetectArchive(path, null, showErrors);
        }

        private Archive DetectArchive(string path, Archive[] specificOpenTypes, bool showErrors)
        {
            try
            {
                Archive archive;
                List<FileTypeLoadException> loadErrors;
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    archive = Archive.LoadArchiveAutodetect(fs, path, specificOpenTypes, specificOpenTypes != null, out loadErrors);
                    if (archive != null)
                        return archive;
                    if (loadErrors != null && loadErrors.Count > 0 && showErrors)
                    {
                        string errors = string.Join("\n", loadErrors.Select(er => er.AttemptedLoadedType + ": " + er.Message).ToArray());
                        string filename = path == null ? string.Empty : (" of \"" + Path.GetFileName(path) + "\"");
                        string message = "File type of " + filename + " could not be identified. Errors returned by all attempts:\n\n" + errors;
                        Invoke(new InvokeDelegateMessageBox(ShowMessageBox), message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception e)
            {
                if (showErrors)
                    Invoke(new InvokeDelegateMessageBox(ShowMessageBox), e.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return null;
        }

        private void LoadArchive(Archive archive, bool refreshState, string selectFile = null, uint? selectFileHash = null)
        {
            m_LoadedArchive = archive;
            bool loaded = archive != null;
            m_LastOpenedFolder = loaded ? Path.GetDirectoryName(archive.FileName) ?? m_ProgFolder : m_ProgFolder;
            if (refreshState)
                m_FilesListOrigState = loaded ? archive.FilesList.ToList() : null;
            lblFileNameVal.Text = loaded ? Path.GetFileName(archive.FileName) : "No file loaded";
            lblArchiveTypeVal.Text = loaded ? archive.ShortTypeName : "-";
            lblFilesVal.Text = loaded ? archive.FilesList.Count.ToString() : "-";
            lblExtraInfoVal.Text = loaded && archive.ExtraInfo != null ? archive.ExtraInfo : "-";
            int scrollIndex = lbFilesList.TopIndex;
            lbFilesList.BeginUpdate();
            lbFilesList.Items.Clear();
            tsmiFileSave.Enabled = loaded && archive.CanSave;
            tsmiFileSaveAs.Enabled = loaded;
            tsmiFileReload.Enabled = loaded;
            tsmiFileClose.Enabled = loaded;
            tsmiArchiveInsert.Enabled = loaded;
            tsmiArchiveInsertAs.Enabled = loaded;
            tsmiArchiveDelete.Enabled = false;
            tsmiArchiveExtract.Enabled = false;
            int toSelect = 0;
            bool doSearch = selectFile != null || selectFileHash.HasValue;
            // This is immediately put on true if there is nothing to search.
            bool searchComplete = !doSearch;
            if (loaded)
            {
                foreach (ArchiveEntry entry in archive.FilesList)
                {
                    lbFilesList.Items.Add(entry);
                    if (searchComplete)
                        continue;
                    if (entry.FileName == selectFile || (selectFileHash.HasValue && selectFileHash.Value == entry.HashedFilename))
                    {
                        searchComplete = true;
                    }
                    else
                    {
                        toSelect++;
                    }
                }
            }
            lbFilesList.EndUpdate();
            if (loaded)
            {
                lbFilesList.TopIndex = Math.Min(scrollIndex, lbFilesList.Items.Count - 1);
                // Only execute if a search was done, and it found something.
                if (doSearch && searchComplete)
                {
                    lbFilesList.SelectedIndex  = toSelect;
                }
            }
            RefreshSidebarFileInfo();
            Text = GetTitle(true, true);
        }

        private bool IsArchiveModified()
        {
            if (m_LoadedArchive == null)
                return false;
            List<ArchiveEntry> curState = m_LoadedArchive.FilesList.OrderBy(x => x.FileName).ToList();
            List<ArchiveEntry> origState = m_FilesListOrigState.OrderBy(x => x.FileName).ToList();
            if (curState.Count != origState.Count)
                return true;
            for (int i = 0; i < curState.Count; ++i)
                if (!curState[i].Equals(origState[i]))
                    return true;
            return false;
        }

        private void lbFilesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshSidebarFileInfo();
        }

        private void RefreshSidebarFileInfo()
        {
            int selected = lbFilesList.SelectedIndices.Count;
            tsmiArchiveExtract.Enabled = selected > 0;
            tsmiArchiveDelete.Enabled = selected > 0;
            if (selected > 1)
                lblSelectedFileVal.Text = "Multiple selected (" + selected + ")";
            if (selected == 0)
                lblSelectedFileVal.Text = "Nothing selected";
            if (selected > 1 || selected == 0)
            {
                lblLocationVal.Text = "-";
                lblArchiveNameVal.Text = "-";
                lblStartOffsetVal.Text = "-";
                lblFileSizeVal.Text = "-";
                lblDateStampVal.Text = "-";
                lblIsDirectoryVal.Text = "-";
                lblEntryExtraInfoVal.Text = "-";
                return;
            }
            ArchiveEntry entry = lbFilesList.SelectedItem as ArchiveEntry;
            if (entry == null)
                return;
            lblSelectedFileVal.Text = entry.FileName;
            bool isInserted = entry.PhysicalPath != null;
            string lengthStr;
            bool accessible = true;
            if (isInserted)
            {
                try
                {
                    if (entry.IsFolder && new DirectoryInfo(entry.PhysicalPath).Exists)
                        lengthStr = "0";
                    else
                        lengthStr = new FileInfo(entry.PhysicalPath).Length.ToString();
                }
                catch
                {
                    lengthStr = "?";
                    accessible = false;
                }
            }
            else
                lengthStr = entry.Length.ToString();
            lblLocationVal.Text = isInserted ? entry.PhysicalPath : "In archive";
            lblArchiveNameVal.Text = Path.GetFileName(entry.ArchivePath);
            lblStartOffsetVal.Text = isInserted ? (accessible ? "0" : "?") : entry.StartOffset.ToString();
            lblFileSizeVal.Text = lengthStr;
            lblDateStampVal.Text = entry.Date.HasValue ? entry.Date.Value.ToString("yyyy-MM-dd, HH:mm:ss") : "-";
            lblIsDirectoryVal.Text = entry.IsFolder ? "Yes" : "No";
            lblEntryExtraInfoVal.Text = entry.ExtraInfo;
            if(!accessible)
                DeleteFileFromArchive(entry.FileName + " appears to be missing! Remove entry from the list?", true);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            ListBox list = ActiveControl as ListBox;
            if (list == null || keyData != (Keys.Control | Keys.A))
                return base.ProcessCmdKey(ref msg, keyData);
            list.BeginUpdate();
            list.Select();
            SendKeys.Send("{Home}");
            SendKeys.Send("{End}");
            SendKeys.Send("+{Home}");
            list.EndUpdate();
            if (list.SelectedItems.Count == 0)
                for (int i = 0; i < list.Items.Count; ++i)
                    list.SetSelected(i, true);
            return true;
        }

        private void tsmiFileOpen_Click(object sender, EventArgs e)
        {
            //if (this.AbortForChangesAskSave(QUESTION_SAVEFILE_OPENNEW))
            //    return;
            Archive selectedItem;
            string filename = FileDialogGenerator.ShowOpenFileFialog(this, GetTitle(false), Archive.SupportedTypes, m_LastOpenedFolder, "archives", null, true, out selectedItem);
            if (filename == null)
                return;
            
            Archive[] preferredType = selectedItem == null ? null : new Archive[] {selectedItem};
            Archive archive = DetectArchive(filename, preferredType, true);
            if (archive != null)
                LoadArchive(archive, true);
        }

        private void tsmiFileSave_Click(object sender, EventArgs e)
        {
            SaveArchive();
        }

        private void tsmiFileSaveAs_Click(object sender, EventArgs e)
        {
            SaveArchiveAs(false);
        }

        private void tsmiFileReload_Click(object sender, EventArgs e)
        {
            if (m_LoadedArchive == null)
                return;
            if (m_LoadedArchive.FileName == null)
                return;
            string filename = m_LoadedArchive.FileName;

            using (FileStream fs = new FileStream(filename, FileMode.Open))
                m_LoadedArchive.LoadArchive(fs, filename);
            LoadArchive(m_LoadedArchive, true);
        }

        private void tsmiFileClose_Click(object sender, EventArgs e)
        {
            LoadArchive(null, true);
        }

        private void tsmiFileExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void tsmiArchiveInsert_Click(object sender, EventArgs e)
        {
            OpenFileDialog sfd = new OpenFileDialog();
            sfd.InitialDirectory = m_LastOpenedFolder;
            if (sfd.ShowDialog(this) == DialogResult.OK)
                AddFiles(new string[] {sfd.FileName});
        }

        private void tsmiArchiveInsertAs_Click(object sender, EventArgs e)
        {
            if (m_LoadedArchive == null)
                return;
            OpenFileDialog sfd = new OpenFileDialog();
            sfd.InitialDirectory = m_LastOpenedFolder;
            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;
            string internalName = m_LoadedArchive.GetInternalFilename(Path.GetFileName(sfd.FileName));
            bool nameOk = false;
            string newName = null;
            while (!nameOk)
            {
                newName = InputBox.Show("Filename in archive:", "Give filename", Path.GetFileName(internalName));
                // user pressed cancel
                if (newName == null) return;
                if (string.IsNullOrEmpty(newName))
                {
                    MessageBox.Show("File name cannot be empty.", GetTitle(false, false));
                    continue;
                }
                if (!m_LoadedArchive.SupportsFolders && (newName.Contains('\\') || newName.Contains('/')))
                {
                    MessageBox.Show("This archive type does not support folders.", GetTitle(false, false));
                    continue;
                }
                nameOk = true;
            }
            m_LoadedArchive.InsertFile(sfd.FileName, newName);
            newName = m_LoadedArchive.GetInternalFilename(newName);
            LoadArchive(m_LoadedArchive, false, newName);
        }

        private void tsmiArchiveExtract_Click(object sender, EventArgs e)
        {
            if (m_LoadedArchive == null || lbFilesList.SelectedItems.Count == 0)
                return;
            if (lbFilesList.SelectedItems.Count == 1)
            {
                ArchiveEntry entry = lbFilesList.SelectedItem as ArchiveEntry;
                if (entry == null)
                    return;
                SaveFileDialog sfd = new SaveFileDialog();
                string filename = entry.FileName;
                int folderSep = filename.LastIndexOf('\\');
                if (folderSep != -1)
                    filename = filename.Substring(folderSep + 1);
                sfd.FileName = filename;
                sfd.InitialDirectory = m_LastOpenedFolder;
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    m_LastOpenedFolder = Path.GetDirectoryName(sfd.FileName);
                    m_LoadedArchive.ExtractFile(entry.FileName, sfd.FileName);
                }
            }
            else
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.SelectedPath = m_LastOpenedFolder;
                fbd.ShowNewFolderButton = true;
                DialogResult res = FolderBrowserLauncher.ShowFolderBrowser(fbd, true, this);
                if (res == DialogResult.OK)
                {
                    string path = fbd.SelectedPath;
                    m_LastOpenedFolder = fbd.SelectedPath;
                    string[] filenames = lbFilesList.SelectedItems.Cast<ArchiveEntry>().Select(en => en.FileName).ToArray();
                    foreach (string filename in filenames)
                        m_LoadedArchive.ExtractFile(filename, Path.Combine(path, filename));
                }
            }
        }

        private void tsmiArchiveDelete_Click(object sender, EventArgs e)
        {
            if (m_LoadedArchive == null || lbFilesList.SelectedItems.Count == 0)
                return;
            string question = "Remove ";
            if (lbFilesList.SelectedItems.Count == 1)
                question += "\"" + ((ArchiveEntry) lbFilesList.SelectedItem).FileName + "\"?";
            else
                question += lbFilesList.SelectedItems.Count + " items?";
            DeleteFileFromArchive(question, false);
        }
        
        private void DeleteFileFromArchive(string question, bool useYesNo)
        {
            if (m_LoadedArchive == null || lbFilesList.SelectedItems.Count == 0)
                return;
            DialogResult dr = (DialogResult)Invoke(new InvokeDelegateMessageBox(ShowMessageBox),
                question, (useYesNo ? MessageBoxButtons.YesNo : MessageBoxButtons.OKCancel), MessageBoxIcon.Information);
            if ((useYesNo ? DialogResult.Yes : DialogResult.OK) != dr)
                return;
            foreach (ArchiveEntry entry in lbFilesList.SelectedItems)
                m_LoadedArchive.FilesList.Remove(entry);
            LoadArchive(m_LoadedArchive, false);
        }

        private void SaveArchive()
        {
            if (m_LoadedArchive == null)
                return;
            if (m_LoadedArchive.FileName == null)
                SaveArchiveAs(true);
            else
                SaveArchive(m_LoadedArchive, m_LoadedArchive.FileName, true);
        }

        private void SaveArchiveAs(bool reload)
        {
            if (m_LoadedArchive == null)
                return;
            Archive selectedItem;
            string suggestedfilename = m_LoadedArchive.FileName ?? Path.Combine(m_LastOpenedFolder, "archive." + (m_LoadedArchive.FileExtensions.FirstOrDefault() ?? "lib").ToLowerInvariant());
            string filename = FileDialogGenerator.ShowSaveFileFialog(this, m_LoadedArchive.GetType(), Archive.SupportedSaveTypes, m_LoadedArchive.GetType(), false, true, suggestedfilename, out selectedItem);
            if (filename == null || selectedItem == null)
                return;
            if (!reload && filename == m_LoadedArchive.FileName)
                reload = true;
            SaveArchive(selectedItem, filename, reload);
        }

        private void SaveArchive(Archive archiveType, string filename, bool reload)
        {
            if (!archiveType.CanSave)
            {
                Invoke(new InvokeDelegateMessageBox(ShowMessageBox), "Saving is not supported for this format. Sorry!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!archiveType.SupportsFolders)
            {
                foreach (ArchiveEntry entry in m_LoadedArchive.FilesList)
                {
                    if (entry.IsFolder || entry.FileName.Contains("\\"))
                    {
                        Invoke(new InvokeDelegateMessageBox(ShowMessageBox), "Cannot save as this archive type; it does not support subfolders.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }
            try
            {
                FileInfo fi = new FileInfo(filename);
                if (fi.Exists && fi.IsReadOnly)
                {
                    Invoke(new InvokeDelegateMessageBox(ShowMessageBox), "Cannot save to this file; it is read-only.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (Exception)
            {
                Invoke(new InvokeDelegateMessageBox(ShowMessageBox), "Could not access the file path.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                archiveType.SaveArchive(m_LoadedArchive, filename);
            }
            catch (NotImplementedException)
            {
                Invoke(new InvokeDelegateMessageBox(ShowMessageBox), "Saving is not supported for this format. Sorry!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (ArgumentException e)
            {
                // No stack trace; just show the message.
                Invoke(new InvokeDelegateMessageBox(ShowMessageBox), e.Message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception e)
            {
                Invoke(new InvokeDelegateMessageBox(ShowMessageBox), e.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (reload)
            {
                ArchiveEntry toReselect = lbFilesList.SelectedItem as ArchiveEntry;
                using (FileStream fs = new FileStream(filename, FileMode.Open))
                    archiveType.LoadArchive(fs, filename);
                LoadArchive(archiveType, true, toReselect?.FileName, toReselect?.HashedFilename);
            }
        }

        private DialogResult ShowMessageBox(string message, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return ShowMessageBox(message, buttons, icon, MessageBoxDefaultButton.Button1);
        }

        private DialogResult ShowMessageBox(string message, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defButtons)
        {
            if (message == null)
                return buttons == MessageBoxButtons.YesNo ? DialogResult.No : (buttons == MessageBoxButtons.OK ? DialogResult.OK : DialogResult.Cancel);
            return MessageBox.Show(this, message, GetTitle(false), buttons, icon, defButtons);
        }
        
        private void lbFilesList_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            ContextMenu cm = new ContextMenu();
            MenuItem cmInsert = new MenuItem(tsmiArchiveInsert.Text, tsmiArchiveInsert_Click);
            MenuItem cmInsertAs = new MenuItem(tsmiArchiveInsertAs.Text, tsmiArchiveInsertAs_Click);
            MenuItem cmExtract = new MenuItem(tsmiArchiveExtract.Text, tsmiArchiveExtract_Click);
            MenuItem cmDelete = new MenuItem(tsmiArchiveDelete.Text, tsmiArchiveDelete_Click);

            bool loaded = m_LoadedArchive != null;
            bool selected = lbFilesList.SelectedIndices.Count > 0;
            cmInsert.Enabled = loaded;
            cmInsertAs.Enabled = loaded;
            cmDelete.Enabled = selected;
            cmExtract.Enabled = selected;
            cm.MenuItems.Add(cmInsert);
            cm.MenuItems.Add(cmInsertAs);
            cm.MenuItems.Add(cmDelete);
            cm.MenuItems.Add(cmExtract);
            cm.Show(lbFilesList, e.Location);
        }

    }
}
