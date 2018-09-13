﻿using DoomLauncher.Forms;
using DoomLauncher.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DoomLauncher
{
    public partial class MainForm
    {
        private bool VerifyDatabase()
        {
            bool check = false;
            try
            {
                check = InitFileCheck("DoomLauncher.sqlite", "DoomLauncher_.sqlite", false);

                if (!check)
                {
                    MessageBox.Show(this, "Initialization failure. Could not find DoomLauncher database",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Util.DisplayUnexpectedException(this, ex);
            }

            return check;
        }

        private bool VerifyGameFilesDirectory()
        {
            bool check = false;
            try
            {
                InitGameFiles();
                check = InitFileCheck("GameFiles", "GameFiles_", true);

                if (!check)
                {
                    //MessageBox.Show(this, "Initialization failure. Could not find DoomLauncher GameFiles directory. Please update your settings to continue.",
                    //    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    DialogResult result;
                    bool success = false;

                    do
                    {
                        success = ShowSettings(true, out result);
                    } while (result != DialogResult.Cancel && !success);

                    check = success;
                }
            }
            catch (Exception ex)
            {
                Util.DisplayUnexpectedException(this, ex);
            }

            return check;
        }

        private void InitGameFiles()
        {
            string basePath = "GameFiles";

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Demos"));
                //Directory.CreateDirectory(Path.Combine(basePath, "GameWads"));
                Directory.CreateDirectory(Path.Combine(basePath, "SaveGames"));
                Directory.CreateDirectory(Path.Combine(basePath, "Screenshots"));
                Directory.CreateDirectory(Path.Combine(basePath, "Temp"));
            }
        }

        private bool InitFileCheck(string initFile, string file, bool directory)
        {
            string dataSource = Path.Combine(Directory.GetCurrentDirectory(), initFile);
            string initDataSource = Path.Combine(Directory.GetCurrentDirectory(), file);

            if (directory)
            {
                DirectoryInfo diInit = new DirectoryInfo(initDataSource);
                DirectoryInfo diSource = new DirectoryInfo(dataSource);

                if (diSource.Exists)
                {
                    if (diInit.Exists)
                        diInit.Delete(true);
                }
                else
                {
                    if (diInit.Exists)
                        diInit.MoveTo(dataSource);
                    else
                        return false;
                }
            }
            else
            {
                FileInfo fiInit = new FileInfo(initDataSource);

                if (File.Exists(dataSource))
                {
                    if (fiInit.Exists)
                        fiInit.Delete();
                }
                else
                {
                    if (fiInit.Exists)
                        fiInit.MoveTo(dataSource);
                    else
                        return false;
                }
            }
 
            return true;
        }

        private void BackupDatabase(string dataSource)
        {
            FileInfo fi = new FileInfo(dataSource);

            if (fi.Exists)
            {
                Directory.CreateDirectory("Backup");
                string backupName = GetBackupFileName(fi);

                FileInfo fiBackup = new FileInfo(backupName);
                if (!fiBackup.Exists)
                    fi.CopyTo(backupName);

                CleanupBackupDirectory();
            }
        }

        private void CleanupBackupDirectory()
        {
            string[] files = Directory.GetFiles("Backup", "*.sqlite");
            List<FileInfo> filesInfo = new List<FileInfo>();
            Array.ForEach(files, x => filesInfo.Add(new FileInfo(x)));
            List<FileInfo> filesInfoOrdered = filesInfo.OrderBy(x => x.CreationTime).ToList();

            while(filesInfoOrdered.Count > 10)
            {
                filesInfoOrdered.First().Delete();
                filesInfoOrdered.RemoveAt(0);
            }
        }

        private string GetBackupFileName(FileInfo fi)
        {
            string end = DateTime.Now.ToString("yyyy_MM_dd") + fi.Extension;
            return Path.Combine(fi.DirectoryName, "Backup", fi.Name.Replace(fi.Extension, end));
        }

        private void SetupTabs()
        {
            List<ITabView> tabViews = new List<ITabView>();
            ColumnConfig[] colConfig = GetColumnConfig();

            TagMapLookup = new TagMapLookup(DataSourceAdapter);

            ColumnField[] columnTextFields = DefaultColumnTextFields;

            OptionsTabViewCtrl tabViewRecent = new OptionsTabViewCtrl(s_recentKey, s_recentKey, DataSourceAdapter, DefaultGameFileSelectFields, TagMapLookup);
            tabViewRecent.SetColumnConfig(columnTextFields, colConfig);
            tabViewRecent.GameFileViewControl.SetContextMenuStrip(mnuLocal);
            tabViewRecent.GameFileViewControl.AllowDrop = true;
            tabViewRecent.Options = new GameFileGetOptions();
            tabViewRecent.Options.Limit = 10;
            tabViewRecent.Options.OrderBy = OrderType.Desc;
            tabViewRecent.Options.OrderField = GameFileFieldType.Downloaded;
 
            SetGameFileViewEvents(tabViewRecent.GameFileViewControl, true);

            tabViews.Add(tabViewRecent);

            LocalTabViewCtrl tabViewLocal = new LocalTabViewCtrl(s_localKey, s_localKey, DataSourceAdapter, DefaultGameFileSelectFields, TagMapLookup);
            tabViewLocal.SetColumnConfig(columnTextFields, colConfig);
            tabViewLocal.GameFileViewControl.SetContextMenuStrip(mnuLocal);
            tabViewLocal.GameFileViewControl.AllowDrop = true;
            SetGameFileViewEvents(tabViewLocal.GameFileViewControl, true);

            tabViews.Add(tabViewLocal);

            columnTextFields = new ColumnField[]
            {
                new ColumnField("FileName", "File"),
                new ColumnField("Title", "Title"),
                new ColumnField("LastPlayed", "Last Played")
            };

            IWadTabViewCtrl tabViewIwads = new IWadTabViewCtrl(s_iwadKey, s_iwadKey, DataSourceAdapter, DefaultGameFileSelectFields, TagMapLookup);
            tabViewIwads.SetColumnConfig(columnTextFields, colConfig);
            tabViewIwads.GameFileViewControl.SetContextMenuStrip(mnuLocal);
            tabViewIwads.GameFileViewControl.AllowDrop = true;
            SetGameFileViewEvents(tabViewIwads.GameFileViewControl, true);

            tabViews.Add(tabViewIwads);

            columnTextFields = new ColumnField[]
            {
                new ColumnField("Title", "Title"),
                new ColumnField("Author", "Author"),
                new ColumnField("Description", "Description"),
                new ColumnField("Rating", "Rating"),
            };

            IdGamesDataSourceAdapter = new IdGamesDataAdapater(AppConfiguration.IdGamesUrl, AppConfiguration.ApiPage, AppConfiguration.MirrorUrl);
            IdGamesTabViewCtrl tabViewIdGames = new IdGamesTabViewCtrl(s_idGamesKey, s_idGamesKey, IdGamesDataSourceAdapter, DefaultGameFileSelectFields);
            tabViewIdGames.SetColumnConfig(columnTextFields, colConfig);
            tabViewIdGames.GameFileViewControl.SetContextMenuStrip(mnuIdGames);
            SetGameFileViewEvents(tabViewIdGames.GameFileViewControl, false);

            tabViews.Add(tabViewIdGames);

            tabViews.AddRange(CreateTagTabs(DefaultColumnTextFields, colConfig));

            m_tabHandler = new TabHandler(tabControl);
            m_tabHandler.SetTabs(tabViews);
        }

        private List<ITabView> CreateTagTabs(ColumnField[] columnTextFields, ColumnConfig[] colConfig)
        {
            List<ITabView> ret = new List<ITabView>();           
            IEnumerable<ITagData> tags = DataSourceAdapter.GetTags().Where(x => x.HasTab).OrderBy(x => x.Name);
            Tags = tags.ToArray();

            foreach(ITagData tag in tags)
                ret.Add(CreateTagTab(columnTextFields, colConfig, tag.Name, tag, false));

            return ret;
        }

        private TagTabView CreateTagTab(ColumnField[] columnTextFields, ColumnConfig[] colConfig, string name, ITagData tag, bool isNew)
        {
            //use the local tab configuration for new tabs
            if (isNew)
            {
                colConfig = colConfig.Where(x => x.Parent == "Local").ToArray();
                Array.ForEach(colConfig, x => x.Parent = tag.Name);
            }

            TagTabView tabView = new TagTabView(tag.TagID, name, DataSourceAdapter, DefaultGameFileSelectFields, tag);
            tabView.SetColumnConfig(columnTextFields, colConfig);
            tabView.GameFileViewControl.SetColumnFormat("ReleaseDate", CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
            tabView.GameFileViewControl.SetColumnFormat("Downloaded", CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
            tabView.GameFileViewControl.SetColumnFormat("LastPlayed", CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
            tabView.GameFileViewControl.SetContextMenuStrip(mnuLocal);
            tabView.GameFileViewControl.AllowDrop = true;
            SetGameFileViewEvents(tabView.GameFileViewControl, true);

            return tabView;
        }

        private void RebuildUtilityToolStrip()
        {
            var utilities = DataSourceAdapter.GetUtilities();
            ToolStripMenuItem utilityToolStrip = mnuLocal.Items.Cast<ToolStripItem>().FirstOrDefault(x => x.Text == "Utility") as ToolStripMenuItem;

            while (utilityToolStrip.DropDownItems.Count > 2)
                utilityToolStrip.DropDownItems.RemoveAt(utilityToolStrip.DropDownItems.Count - 1);

            foreach (var utility in utilities)
                utilityToolStrip.DropDownItems.Add(utility.Name, null, utilityToolStripItem_Click);
        }

        private void RebuildTagToolStrip()
        {
            IEnumerable<ITagData> tags = DataSourceAdapter.GetTags().OrderBy(x => x.Name);
            Tags = tags.ToArray();

            ToolStripMenuItem tagToolStrip = mnuLocal.Items.Cast<ToolStripItem>().FirstOrDefault(x => x.Text == "Tag") as ToolStripMenuItem;
            ToolStripMenuItem removeTagToolStrip = mnuLocal.Items.Cast<ToolStripItem>().FirstOrDefault(x => x.Text == "Remove Tag") as ToolStripMenuItem;

            if (tagToolStrip != null)
            {
                BuildTagToolStrip(tagToolStrip, tags, tagToolStripItem_Click);
                BuildTagToolStrip(removeTagToolStrip, tags, removeTagToolStripItem_Click);
            }
        }

        private void BuildTagToolStrip(ToolStripMenuItem tagToolStrip, IEnumerable<ITagData> tags, EventHandler handler)
        {
            while (tagToolStrip.DropDownItems.Count > 2)
            {
                tagToolStrip.DropDownItems.RemoveAt(tagToolStrip.DropDownItems.Count - 1);
            }

            foreach (ITagData tag in tags)
            {
                tagToolStrip.DropDownItems.Add(tag.Name, null, handler);
            }
        }

        private void SetGameFileViewEvents(GameFileViewControl ctrl, bool dragDrop)
        {
            ctrl.ToolTipTextNeeded += ctrlView_ToolTipTextNeeded;
            ctrl.RowDoubleClicked += ctrlView_RowDoubleClicked;
            ctrl.SelectionChange += ctrlView_SelectionChange;
            ctrl.GridKeyPress += ctrlView_GridKeyPress;

            if (dragDrop)
            {
                ctrl.DragDrop += ctrlView_DragDrop;
                ctrl.DragEnter += ctrlView_DragEnter;
                ctrl.GridKeyDown += ctrlView_GridKeyDown;
            }
        }

        private void ctrlView_GridKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                HandleDelete();
        }

        private ProgressBarForm m_progressBarUpdate;

        private async void Initialize()
        {
            string dataSource = Path.Combine(Directory.GetCurrentDirectory(), DbDataSourceAdapter.GetDatabaseFileName());
            DataAccess access = new DataAccess(new SqliteDatabaseAdapter(), DbDataSourceAdapter.CreateConnectionString(dataSource));

            m_versionHandler = new VersionHandler(access, DataSourceAdapter, AppConfiguration);

            if (m_versionHandler.UpdateRequired())
            {
                m_versionHandler.UpdateProgress += handler_UpdateProgress;

                m_progressBarUpdate = CreateProgressBar("Updating...", ProgressBarStyle.Continuous);
                ProgressBarStart(m_progressBarUpdate);

                await Task.Run(() => ExecuteVersionUpdate());

                ProgressBarEnd(m_progressBarUpdate);
            }

            try
            {
                //Only set location and window state if the location is valid, either way we always set Width, Height, and splitter values
                if (ValidatePosition(AppConfiguration))
                {
                    StartPosition = FormStartPosition.Manual;
                    Location = new Point(AppConfiguration.AppX, AppConfiguration.AppY);

                    WindowState = AppConfiguration.WindowState;
                }

                Width = AppConfiguration.AppWidth;
                Height = AppConfiguration.AppHeight;

                splitTopBottom.SplitterDistance = AppConfiguration.SplitTopBottom;
                splitLeftRight.SplitterDistance = AppConfiguration.SplitLeftRight;
            }
            catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show(this, string.Format("The directory specified in your settings was incorrect: '{0}'", ex.Message),
                    "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tblMain.Enabled = false;
                return;
            }

            if (AppConfiguration.CleanTemp)
                CleanTempDirectory();

            DirectoryDataSourceAdapter = new DirectoryDataSourceAdapter(AppConfiguration.GameFileDirectory, AppConfiguration.DateParseFormats);
            SetupTabs();
            RebuildTagToolStrip();
            RebuildUtilityToolStrip();

            m_downloadView = new DownloadView();
            m_downloadView.UserPlay += DownloadView_UserPlay;
            m_downloadHandler = new DownloadHandler(AppConfiguration.TempDirectory, m_downloadView);

            ctrlAssociationView.Initialize(DataSourceAdapter, AppConfiguration);
            ctrlAssociationView.FileDeleted += ctrlAssociationView_FileDeleted;
            ctrlAssociationView.FileOrderChanged += ctrlAssociationView_FileOrderChanged;
            ctrlAssociationView.RequestScreenshots += CtrlAssociationView_RequestScreenshots;

            m_splash.Close();

            await CheckFirstInit();
            UpdateLocal();

            SetupSearchFilters();
            HandleTabSelectionChange();
        }

        private async Task CheckFirstInit()
        {
            if (!DataSourceAdapter.GetSourcePorts().Any()) //If no source ports setup then it's the first time setup, display welcome/setup info
            {
                DisplayWelcome();
                HandleEditSourcePorts(true);
            }

            if (!DataSourceAdapter.GetIWads().Any()) //If no iwads then prompt to add iwads
            { 
                await HandleAddIWads();
                this.Invoke((MethodInvoker)delegate { tabControl.SelectedIndex = 2; }); //the user has only added iwads on setup, so set the tab to iwads on first launch so there is something to see
                DisplayInitSettings(); //give user the change set default port, iwad, skill
            }
        }

        private void DisplayInitSettings()
        {
            SettingsForm settings = new SettingsForm(DataSourceAdapter, AppConfiguration);
            settings.SetToLaunchSettingsTab();
            settings.StartPosition = FormStartPosition.CenterParent;
            settings.ShowDialog();
        }

        private void DisplayWelcome()
        {
            Welcome welcome = new Welcome();
            welcome.StartPosition = FormStartPosition.CenterParent;
            welcome.ShowDialog();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (m_launchFile != null)
            {
                string addFile = m_launchFile;

                IGameFile launchFile = DataSourceAdapter.GetGameFile(m_launchFile);
                m_launchFile = null;

                if (launchFile == null && File.Exists(addFile))
                    HandleAddGameFiles(AddFileType.GameFile, new string[] { addFile });
                else
                    HandlePlay(new IGameFile[] { launchFile });
            }
        }

        private static bool ValidatePosition(AppConfiguration config)
        {
            if (config.WindowState == FormWindowState.Maximized)
            {
                //Maximized goes outside the bounds to hide the border, bring the rectangle in a more than safe amount to check if the monitor is still there
                //Windows 7 can be -4, later version are -6, could change based on DPI
                int offs = 32; 
                Rectangle formRectangle = new Rectangle(config.AppX + offs, config.AppY + offs, config.AppWidth - offs*2, config.AppHeight - offs*2);
                return Screen.AllScreens.Any(x => x.WorkingArea.Contains(formRectangle));
            }
            else if (config.WindowState != FormWindowState.Minimized)
            {
                Point formPt = new Point(config.AppX, config.AppY);
                return Screen.AllScreens.Any(x => x.WorkingArea.Contains(formPt));
            }

            return true;
        }

        private void ExecuteVersionUpdate()
        {
            m_versionHandler.HandleVersionUpdate();
        }

        void handler_UpdateProgress(object sender, EventArgs e)
        {
            if (InvokeRequired)
                Invoke(new Action(UpdateVersionProgress));
            else
                UpdateVersionProgress();
        }

        void UpdateVersionProgress()
        {
            m_progressBarUpdate.Value = m_versionHandler.ProgressPercent;
        }

        private void SetupSearchFilters()
        {
            chkAutoSearch.Checked = (bool)AppConfiguration.GetTypedConfigValue(ConfigType.AutoSearch, typeof(bool));

            ctrlSearch.SearchTextChanged += ctrlSearch_SearchTextChanged;
            Util.SetDefaultSearchFields(ctrlSearch);
        }

        private void CreateSendToLink()
        {
            //http://stackoverflow.com/questions/234231/creating-application-shortcut-in-a-directory
            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); //Windows Script Host Shell Object
            dynamic shell = Activator.CreateInstance(t);
            try
            {
                string sendToPath = Environment.ExpandEnvironmentVariables(@"%AppData%\Microsoft\Windows\SendTo");
                var lnk = shell.CreateShortcut(Path.Combine(sendToPath, "DoomLauncher.lnk"));
                try
                {
                    lnk.TargetPath = Path.Combine(Directory.GetCurrentDirectory(), "DoomLauncher.exe");
                    lnk.IconLocation = string.Format(Path.Combine(Directory.GetCurrentDirectory(), "DoomLauncher.ico"));
                    lnk.Save();
                }
                finally
                {
                    Marshal.FinalReleaseComObject(lnk);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }

        void ctrlSearch_SearchTextChanged(object sender, EventArgs e)
        {
            if (chkAutoSearch.Checked && GetCurrentTabView() != null &&
                GetCurrentTabView().GetType() != typeof(IdGamesTabViewCtrl))
            {
                HandleSearch();
            }
        }

        private ColumnField[] DefaultColumnTextFields
        {
            get
            {
                 return new ColumnField[]
                 {
                    new ColumnField("FileName", "File"),
                    new ColumnField("Title", "Title"),
                    new ColumnField("Author", "Author"),
                    new ColumnField("ReleaseDate", "Release Date"),
                    new ColumnField("MapCount", "Maps"),
                    new ColumnField("Comments", "Comments"),
                    new ColumnField("Rating", "Rating"),
                    new ColumnField("Downloaded", "Downloaded"),
                    new ColumnField("LastPlayed", "Last Played")
                 };
            }
        }

        private GameFileFieldType[] DefaultGameFileSelectFields
        {
            get
            {
                return new GameFileFieldType[]
                {
                    GameFileFieldType.GameFileID,
                    GameFileFieldType.Filename,
                    GameFileFieldType.Author,
                    GameFileFieldType.Title,
                    GameFileFieldType.Description,
                    GameFileFieldType.Downloaded,
                    GameFileFieldType.LastPlayed,
                    GameFileFieldType.ReleaseDate,
                    GameFileFieldType.Comments,
                    GameFileFieldType.Rating,
                    GameFileFieldType.MapCount,
                    GameFileFieldType.MinutesPlayed,
                    GameFileFieldType.IWadID
                };
            }
        }

        private ITagMapLookup TagMapLookup { get; set; }
    }
}
