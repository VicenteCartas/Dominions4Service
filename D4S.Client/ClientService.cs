namespace D4S.Client
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Diagnostics;
    using System.ServiceProcess;
    using Utils;

    public partial class ClientService : ServiceBase
    {
        private static readonly string LocalDataFolderKey = "local_folder";
        private static readonly string HostDataFolderKey = "host_folder";

        private static readonly string DominionsSaveData = @"Dominions4\savedgames";

        private EventLog eventLog;
        private string localSaveFolder;
        private string hostSaveFolder;
        private FileSystemWatcher localWatcher;
        private FileSystemWatcher hostWatcher;

        public ClientService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                this.InitializeLogs();
                this.SetLocalFolder();
                this.SetHostFolder();

                // TODO: Offline changes
                // Part 1: check if there are turns in local not in shared
                // Part 2: check if there are results in shared not in local

                StartLocalFolderWatcher();
                StartHostFolderWatcher();

                this.eventLog.WriteEntry("D4S.Client started");
            }
            catch (Exception ex)
            {
                this.eventLog?.WriteEntry($"D4S.Client exception: {ex.Message} - {ex.StackTrace}", EventLogEntryType.Error);
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                this.eventLog.WriteEntry("Stopping D4S.Client");

                this.StopLocalFolderWatcher();
                this.StopHostFolderWatcher();

                this.eventLog.WriteEntry("D4S.Client stopped");
            }
            catch (Exception ex)
            {
                this.eventLog?.WriteEntry($"D4S.Client exception: {ex.Message} - {ex.StackTrace}", EventLogEntryType.Error);
                throw;
            }
        }

        private void InitializeLogs()
        {
            this.eventLog = new EventLog();
            if (!EventLog.SourceExists("D4S.Client"))
            {
                EventLog.CreateEventSource("D4S.Client", "Application");
            }

            eventLog.Source = "D4S.Client";
            eventLog.Log = "Application";

            this.eventLog.WriteEntry("Starting D4S.Client");
        }

        private void SetLocalFolder()
        {
            this.localSaveFolder = ConfigurationManager.AppSettings[LocalDataFolderKey];
            if (string.IsNullOrEmpty(this.localSaveFolder))
            {
                string username = WindowsUtils.GetFirstUserName();
                this.localSaveFolder = Path.Combine(@"c:\users", username, @"AppData\Roaming", DominionsSaveData);
            }

            if (!Directory.Exists(this.localSaveFolder))
            {
                throw new ApplicationException($"Error: dominions local savedgames folder could not be found in {this.localSaveFolder}");
            }

            this.eventLog.WriteEntry($"Local data folder = {this.localSaveFolder}");
        }

        private void SetHostFolder()
        {
            this.hostSaveFolder = ConfigurationManager.AppSettings[HostDataFolderKey];
            if (string.IsNullOrEmpty(this.hostSaveFolder))
            {
                string username = WindowsUtils.GetFirstUserName();
                this.hostSaveFolder = Path.Combine(@"c:\users", username, @"Dropbox\Dom4Games");
            }

            if (!Directory.Exists(this.hostSaveFolder))
            {
                Directory.CreateDirectory(this.hostSaveFolder);
            }

            this.eventLog.WriteEntry($"Host data folder = {this.hostSaveFolder}");
        }

        private void StartLocalFolderWatcher()
        {
            this.localWatcher = new FileSystemWatcher()
            {
                Path = this.localSaveFolder,
                Filter = "*.2h",
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size
            };

            this.localWatcher.Created += LocalWatcher_CreatedOrChanged;
            this.localWatcher.Changed += LocalWatcher_CreatedOrChanged;
            this.localWatcher.EnableRaisingEvents = true;

            this.eventLog.WriteEntry("Local folder watcher started");
        }

        private void StartHostFolderWatcher()
        {
            this.hostWatcher = new FileSystemWatcher()
            {
                Path = this.hostSaveFolder,
                Filter = "*.trn",
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName
            };

            this.hostWatcher.Created += HostWatcherCreated;
            this.hostWatcher.EnableRaisingEvents = true;

            this.eventLog.WriteEntry("Host folder watcher started");
        }

        private void StopLocalFolderWatcher()
        {
            this.localWatcher.EnableRaisingEvents = false;
            this.localWatcher.Created -= LocalWatcher_CreatedOrChanged;
            this.localWatcher.Changed -= LocalWatcher_CreatedOrChanged;

            this.eventLog.WriteEntry("Local folder watched stopped");
        }

        private void StopHostFolderWatcher()
        {
            this.hostWatcher.EnableRaisingEvents = false;
            this.hostWatcher.Created -= HostWatcherCreated;

            this.eventLog.WriteEntry("Host folder watched stopped");
        }

        private void LocalWatcher_CreatedOrChanged(object sender, FileSystemEventArgs e)
        {
            this.CopyFile(this.localSaveFolder, this.hostSaveFolder, e.FullPath);
        }

        private void HostWatcherCreated(object sender, FileSystemEventArgs e)
        {
            if (Dom4Utils.IsPlayerTurn(this.localSaveFolder, e.FullPath))
            {
                this.CopyFile(this.hostSaveFolder, this.localSaveFolder, e.FullPath);
            }
        }

        private void CopyFile(string sourceDir, string targetDir, string fileFullPath)
        {
            string relativePath = PathUtils.GetRelativePath(sourceDir, Path.GetDirectoryName(fileFullPath));
            string targetPath = Path.Combine(targetDir, relativePath, Path.GetFileName(fileFullPath));

            string targetDirectory = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Copy(fileFullPath, targetPath, true);

            this.eventLog.WriteEntry($"File watcher: file created/changed in {fileFullPath} and copied to {targetPath}");
        }
    }
}