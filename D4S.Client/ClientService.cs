using System.Management;

namespace D4S.Client
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.ServiceProcess;
    using System.Text;
    using System.Threading.Tasks;
    using Utils;

    public partial class ClientService : ServiceBase
    {
        private static readonly string DominionsSaveData = @"Dominions4\savedgames";

        private EventLog eventLog;
        private string localSavePath;
        private string sharedSavePath;
        private FileSystemWatcher localWatcher;
        private FileSystemWatcher sharedWatcher;

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
                this.SetSharedFolder();

                // TODO: Offline changes
                // Part 1: check if there are turns in local not in shared
                // Part 2: check if there are results in shared not in local

                StartLocalFolderWatcher();
                StartSharedFolderWatcher();

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
            this.eventLog.WriteEntry("Stopping D4S.Client");

            this.StopLocalFolderWatcher();
            this.StopSharedFolderWatcher();

            this.eventLog.WriteEntry("D4S.Client stopped");
        }

        private void InitializeLogs()
        {
            this.eventLog = new EventLog();
            if (!EventLog.SourceExists("D4S.Client"))
            {
                EventLog.CreateEventSource("D4S.Client", "Information");
            }

            eventLog.Source = "D4S.Client";
            eventLog.Log = "Application";

            this.eventLog.WriteEntry("Starting D4S.Client");
        }

        private void SetLocalFolder()
        {
            this.localSavePath = this.sharedSavePath = ConfigurationManager.AppSettings["local_folder"];
            if (string.IsNullOrEmpty(this.localSavePath))
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem");
                ManagementObjectCollection collection = searcher.Get();
                string username = Path.GetFileName((string)collection.Cast<ManagementBaseObject>().First()["UserName"]);

                this.localSavePath = Path.Combine(@"c:\users", username, @"AppData\Roaming", DominionsSaveData);
            }

            if (!Directory.Exists(this.localSavePath))
            {
                throw new ApplicationException($"Error: dominions savedgames folder is not in {this.localSavePath}");
            }

            this.eventLog.WriteEntry($"Local data folder = {this.localSavePath}");
        }

        private void SetSharedFolder()
        {
            this.sharedSavePath = ConfigurationManager.AppSettings["shared_folder"];
            if (string.IsNullOrEmpty(this.sharedSavePath))
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem");
                ManagementObjectCollection collection = searcher.Get();
                string username = Path.GetFileName((string)collection.Cast<ManagementBaseObject>().First()["UserName"]);

                this.sharedSavePath = Path.Combine(@"c:\users", username, @"Dropbox\Dom4Games");
            }

            if (!Directory.Exists(this.sharedSavePath))
            {
                Directory.CreateDirectory(this.sharedSavePath);
            }

            this.eventLog.WriteEntry($"Shared data folder = {this.sharedSavePath}");
        }

        private void StartLocalFolderWatcher()
        {
            this.localWatcher = new FileSystemWatcher()
            {
                Path = this.localSavePath,
                Filter = "*.2h",
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size
            };

            this.localWatcher.Created += LocalWatcher_CreatedOrChanged;
            this.localWatcher.Changed += LocalWatcher_CreatedOrChanged;
            this.localWatcher.EnableRaisingEvents = true;

            this.eventLog.WriteEntry("Local folder watcher started");
        }

        private void StartSharedFolderWatcher()
        {
            this.sharedWatcher = new FileSystemWatcher()
            {
                Path = this.sharedSavePath,
                Filter = "*.trn",
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName
            };

            this.sharedWatcher.Created += SharedWatcher_Created;
            this.sharedWatcher.EnableRaisingEvents = true;

            this.eventLog.WriteEntry("Shared folder watcher started");
        }

        private void StopLocalFolderWatcher()
        {
            this.localWatcher.EnableRaisingEvents = false;
            this.localWatcher.Created -= LocalWatcher_CreatedOrChanged;
            this.localWatcher.Changed -= LocalWatcher_CreatedOrChanged;

            this.eventLog.WriteEntry("Local folder watched stopped");
        }

        private void StopSharedFolderWatcher()
        {
            this.sharedWatcher.EnableRaisingEvents = false;
            this.sharedWatcher.Created -= SharedWatcher_Created;

            this.eventLog.WriteEntry("Shared folder watched stopped");
        }

        private void LocalWatcher_CreatedOrChanged(object sender, FileSystemEventArgs e)
        {
            this.CopyFile(this.localSavePath, this.sharedSavePath, e.FullPath);
        }

        private void SharedWatcher_Created(object sender, FileSystemEventArgs e)
        {
            this.CopyFile(this.sharedSavePath, this.localSavePath, e.FullPath);
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