using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibrarianTool.Domain;
using Nyerguds.Util;
using Nyerguds.Util.UI;

namespace LibrarianTool
{
    public partial class FrmLibTool : Form
    {
        private const String PROG_NAME = "Librarian";
        private const String PROG_AUTHOR = "Created by Nyerguds";

        protected String m_LastOpenedFolder;
        protected Archive m_LoadedArchive;

        public FrmLibTool()
        {
            this.InitializeComponent();
            this.Text = GetTitle(true);
        }

        public static String GetTitle(Boolean withAuthor)
        {
            String title = PROG_NAME + " " + GeneralUtils.ProgramVersion();
            if (withAuthor)
                title += " - " + PROG_AUTHOR;
            return title;
        }

        private void Frm_DragEnter(Object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Frm_DragDrop(Object sender, DragEventArgs e)
        {
            String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1)
                return;
            String path = files[0];
            this.m_LastOpenedFolder = Path.GetDirectoryName(path);
            Archive version = this.DetectArchive(path);
            if (version != null)
                this.LoadArchive(version);
        }


        private void Lv_DragEnter(Object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Lv_DragDrop(Object sender, DragEventArgs e)
        {
            String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0)
                return;
            if (files.Length == 1)
            {
                String path = files[0];
                Archive archive = this.DetectArchive(path);
                if (archive != null)
                {
                    this.LoadArchive(archive);
                    this.m_LastOpenedFolder = Path.GetDirectoryName(path);
                    return;
                }
            }
            this.AddFiles(files);
        }

        private void AddFiles(String[] files)
        {
            if(files.Length == 0)
                return;
            if (this.m_LoadedArchive == null)
                this.m_LoadedArchive = new ArchiveLibV1();
            foreach (String file in files)
                this.m_LoadedArchive.InsertFile(file);
            this.LoadArchive(this.m_LoadedArchive);
        }

        private Archive DetectArchive(String path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open))
                    return Archive.LoadArchive(path, fs);
            }
            catch { return null; }
        }

        private void LoadArchive(Archive archive)
        {
            this.m_LoadedArchive = archive;
            this.lblFileNameVal.Text = archive == null ? "No file loaded" : Path.GetFileName(archive.FileName);
            this.lblArchiveTypeVal.Text = archive == null ? "-" : archive.ShortTypeName;
            this.lblFilesVal.Text = archive == null ? "-" : archive.FilesList.Count.ToString();
            this.lbFilesList.Items.Clear();
            if (archive != null)
                foreach (ArchiveEntry entry in archive.FilesList)
                    this.lbFilesList.Items.Add(entry);
        }

        private void lbFilesList_SelectedIndexChanged(Object sender, EventArgs e)
        {
            Int32 selected = this.lbFilesList.SelectedIndices.Count;

            if (selected > 1)
                this.lblSelectedFileVal.Text = "Multiple selected";
            if (selected == 0)
                this.lblSelectedFileVal.Text = "Nothing selected";
            if (selected > 1 || selected == 0)
            {
                this.lblLocationVal.Text = "-";
                this.lblStartOffsetVal.Text = "-";
                this.lblFileSizeVal.Text = "-";
                return;
            }
            ArchiveEntry entry = this.lbFilesList.SelectedItem as ArchiveEntry;
            if (entry == null)
                return;
            this.lblSelectedFileVal.Text = entry.FileName;
            Boolean isInserted = entry.PhysicalPath != null;
            this.lblLocationVal.Text = isInserted ? "On disk" :  "In archive";
            this.lblStartOffsetVal.Text = isInserted ? "-" : entry.StartOffset.ToString();
            this.lblFileSizeVal.Text = isInserted ? "-" : entry.Length.ToString();
        }

        protected override Boolean ProcessCmdKey(ref Message msg, Keys keyData)
        {
            ListBox list = this.ActiveControl as ListBox;
            if (list == null || keyData != (Keys.Control | Keys.A))
                return base.ProcessCmdKey(ref msg, keyData);
            list.BeginUpdate();
            list.Select();
            SendKeys.Send("{Home}");
            SendKeys.Send("{End}");
            SendKeys.Send("+{Home}");
            list.EndUpdate();
            if (list.SelectedItems.Count == 0)
                for (Int32 i = 0; i < list.Items.Count; i++)
                    list.SetSelected(i, true);
            return true;
        }

        private void tsmiOpen_Click(Object sender, EventArgs e)
        {
            //if (this.AbortForChangesAskSave(QUESTION_SAVEFILE_OPENNEW))
            //    return;
            Archive selectedItem;
            String filename = FileDialogGenerator.ShowOpenFileFialog(this, Archive.SupportedTypes, this.m_LastOpenedFolder, "archives", "lib", out selectedItem);
            if (filename == null)
                return;
            if (selectedItem != null && selectedItem.LoadArchive(filename))
            {
                this.LoadArchive(selectedItem);
                return;
            }
            Archive archive;
            using (FileStream fs = new FileStream(filename, FileMode.Open))
                archive = Archive.LoadArchive(filename, fs);
            if (archive != null)
                this.LoadArchive(archive);
        }

        private void tsmiSave_Click(Object sender, EventArgs e)
        {
            this.Save();
        }

        private void tsmiSaveAs_Click(Object sender, EventArgs e)
        {
            this.SaveAs();
        }

        private void tsmiReload_Click(object sender, EventArgs e)
        {
            if (this.m_LoadedArchive == null)
                return;
            if (this.m_LoadedArchive.FileName == null)
                return;
            String filename = this.m_LoadedArchive.FileName;

            using (FileStream fs = new FileStream(filename, FileMode.Open))
                this.m_LoadedArchive.LoadArchive(fs, filename);
            this.LoadArchive(this.m_LoadedArchive);
        }

        private void tsmiExit_Click(Object sender, EventArgs e)
        {
            this.Close();
        }

        private void tsmiInsert_Click(Object sender, EventArgs e)
        {
            OpenFileDialog sfd = new OpenFileDialog();
            sfd.InitialDirectory = m_LastOpenedFolder;
            if (sfd.ShowDialog(this) == DialogResult.OK)
                AddFiles(new String[] {sfd.FileName});
        }

        private void tsmiExtract_Click(Object sender, EventArgs e)
        {
            if (m_LoadedArchive == null || lbFilesList.SelectedItems.Count == 0)
                return;
            if (lbFilesList.SelectedItems.Count == 1)
            {
                ArchiveEntry entry = this.lbFilesList.SelectedItem as ArchiveEntry;
                if (entry == null)
                    return;
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = entry.FileName;
                sfd.InitialDirectory = m_LastOpenedFolder;
                if (sfd.ShowDialog(this) == DialogResult.OK)
                    m_LoadedArchive.ExtractFile(entry.FileName, sfd.FileName);
            }
            else
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                //fbd.RootFolder = Environment.SpecialFolder.MyComputer;
                if (m_LastOpenedFolder == null)
                    m_LastOpenedFolder = Environment.GetFolderPath((Environment.SpecialFolder.MyComputer));
                fbd.SelectedPath = m_LastOpenedFolder;
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    String path = fbd.SelectedPath;
                    String[] filenames = this.lbFilesList.SelectedItems.Cast<ArchiveEntry>().Select(en => en.FileName).ToArray();
                    foreach (String filename in filenames)
                        m_LoadedArchive.ExtractFile(filename, Path.Combine(path, filename));
                }
            }
        }

        private void tsmiDelete_Click(Object sender, EventArgs e)
        {
            if (m_LoadedArchive == null || lbFilesList.SelectedItems.Count == 0)
                return;
            foreach (ArchiveEntry entry in lbFilesList.SelectedItems)
                this.m_LoadedArchive.FilesList.Remove(entry);
            this.LoadArchive(this.m_LoadedArchive);
        }

        private void Save()
        {
            if (this.m_LoadedArchive == null || this.m_LoadedArchive.FileName == null)
                this.SaveAs();
            else
                this.SaveArchive(this.m_LoadedArchive, this.m_LoadedArchive.FileName);
        }

        private void SaveAs()
        {
            if (this.m_LoadedArchive == null)
                return;
            Archive selectedItem;
            String suggestedfilename = this.m_LoadedArchive.FileName ?? "archive.lib" + (this.m_LoadedArchive.FileExtensions.FirstOrDefault() ?? "lib");
            String filename = FileDialogGenerator.ShowSaveFileFialog(this, this.m_LoadedArchive.GetType(), Archive.SupportedTypes, typeof(ArchiveLibV1), true, suggestedfilename, out selectedItem);
            if (filename == null || selectedItem == null)
                return;
            this.SaveArchive(selectedItem, filename);
        }

        private void SaveArchive(Archive archiveType, String filename)
        {
            archiveType.SaveArchive(this.m_LoadedArchive, filename);
            if (filename == this.m_LoadedArchive.FileName)
            {
                Archive archive;
                using (FileStream fs = new FileStream(filename, FileMode.Open))
                    archive = Archive.LoadArchive(filename, fs);
                this.LoadArchive(archive);
            }
        }

    }
}
