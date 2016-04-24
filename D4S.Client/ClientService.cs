using Utils;

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
            this.InitializeLogs();
            this.SetLocalFolder();
            this.SetSharedFolder();
            
            // Offline changes

            // Part 1: check if there are turns in local not in shared

            // Part 2: check if there are results in shared not in local

            // Activate watcher in local folder
            StartLocalFolderWatcher();

            // Activate watcher in shared folder
            StartSharedFolderWatcher();

            this.eventLog.WriteEntry("D4S.Client started");
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
            eventLog.Log = "Information";

            this.eventLog.WriteEntry("Starting D4S.Client");
        }

        private void SetLocalFolder()
        {
            this.localSavePath = Path.Combine(Environment.GetEnvironmentVariable("APPDATA"), DominionsSaveData);
            this.eventLog.WriteEntry($"Local data folder = {this.localSavePath}");
        }

        private void SetSharedFolder()
        {
            this.sharedSavePath = ConfigurationManager.AppSettings["shared_folder"];
            this.eventLog.WriteEntry($"Shared data folder = {this.sharedSavePath}");
        }

        private void StartLocalFolderWatcher()
        {
            this.localWatcher = new FileSystemWatcher()
            {
                Path = this.localSavePath,
                Filter = "*.2h",
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName
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
            string relativePath = PathUtils.GetRelativePath(this.localSavePath, Path.GetDirectoryName(e.FullPath));
            string targetPath = Path.Combine(this.sharedSavePath, relativePath, e.Name);
            File.Copy(e.FullPath, targetPath, true);

            this.eventLog.WriteEntry($"Local watcher: file created/changed in {e.Name} and copied to {targetPath}");
        }

        private void SharedWatcher_Created(object sender, FileSystemEventArgs e)
        {
            string relativePath = PathUtils.GetRelativePath(this.sharedSavePath, Path.GetDirectoryName(e.FullPath));
            string targetPath = Path.Combine(this.localSavePath, relativePath, e.Name);
            File.Copy(e.FullPath, targetPath, true);

            this.eventLog.WriteEntry($"Shared watcher: file created in {e.Name} and copied to {targetPath}");
        }
    }
}