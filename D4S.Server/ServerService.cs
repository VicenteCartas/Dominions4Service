using Utils;

namespace D4S.Host
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.ServiceProcess;
    using System.Timers;

    public partial class ServerService : ServiceBase
    {
        private static readonly string LocalDataFolderKey = "local_folder";
        private static readonly string HostDataFolderKey = "host_folder";
        private static readonly string DominionsFolderKey = "dominions_folder";

        private static readonly string DominionsSaveData = @"Dominions4\savedgames";
        private static readonly string DominionsExecutable = "Dominions4.exe";

        private EventLog eventLog;
        private string localSaveFolder;
        private string hostSaveFolder;
        private string dominionsFolder;
        private List<GameInformation> games;
        private Timer hostTimer;

        public ServerService()
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
                this.SetDominionsFolder();
                this.ReadGamesInformation();
                this.StartHostTimer();

                this.eventLog.WriteEntry("D4S.Host started");
            }
            catch (Exception ex)
            {
                this.eventLog?.WriteEntry($"D4S.Host exception: {ex.Message} - {ex.StackTrace}", EventLogEntryType.Error);
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                this.eventLog.WriteEntry("Stopping D4S.Host");

                this.StopHostTimer();

                this.eventLog.WriteEntry("D4S.Host stopped");
            }
            catch (Exception ex)
            {
                this.eventLog?.WriteEntry($"D4S.Host exception: {ex.Message} - {ex.StackTrace}", EventLogEntryType.Error);
                throw;
            }
        }

        private void InitializeLogs()
        {
            this.eventLog = new EventLog();
            if (!EventLog.SourceExists("D4S.Host"))
            {
                EventLog.CreateEventSource("D4S.Host", "Application");
            }

            eventLog.Source = "D4S.Host";
            eventLog.Log = "Application";

            this.eventLog.WriteEntry("Starting D4S.Host");
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
                throw new ApplicationException(
                    $"Error: dominions local savedgames folder could not be found in {this.localSaveFolder}");
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

        private void SetDominionsFolder()
        {
            this.dominionsFolder = ConfigurationManager.AppSettings[DominionsFolderKey];
            if (string.IsNullOrEmpty(this.hostSaveFolder))
            {
                this.dominionsFolder = @"C:\Program Files (x86)\Steam\steamapps\common\Dominions4";
            }

            if (!Directory.Exists(this.dominionsFolder))
            {
                throw new ApplicationException(
                    $"Couldn't find dominions installation folder. Configure appconfig key \"dominions_folder\" ");
            }

            this.eventLog.WriteEntry($"Dominions data folder = {this.dominionsFolder}");
        }

        private void ReadGamesInformation()
        {
            this.games = new List<GameInformation>();

            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (!key.Equals(DominionsFolderKey, StringComparison.InvariantCultureIgnoreCase))
                {
                    this.games.Add(new GameInformation(key, ConfigurationManager.AppSettings[key]));
                }
            }
        }

        private void StartHostTimer()
        {
            this.hostTimer = new Timer(1800000);
            this.hostTimer.Enabled = true;
            this.hostTimer.Elapsed += HostTimer_Elapsed;
            this.hostTimer.Start();
        }

        private void StopHostTimer()
        {
            this.hostTimer.Enabled = false;
            this.hostTimer.Elapsed -= HostTimer_Elapsed;
            this.hostTimer.Stop();
        }

        private void HostTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var game in this.games)
            {
                if (this.NeedsHost(game))
                {
                    this.RunHost(game.Name);
                }
            }
        }

        private bool NeedsHost(GameInformation game)
        {
            int hostHour = game.Schedule[DateTime.UtcNow.DayOfWeek].Hour;
            if (DateTime.UtcNow.Hour >= hostHour)
            {
                return true;
            }

            return false;
        }

        private void RunHost(string gameName)
        {
            this.CopyPlayerTurns(gameName);
            this.RunDominionsProcess(gameName);
            this.CopyTurnResults(gameName);
        }

        private void CopyPlayerTurns(string gameName)
        {
            string[] files = Directory.GetFiles(Path.Combine(this.hostSaveFolder, gameName), "*.2h");
            foreach (var file in files)
            {
                this.CopyFile(this.hostSaveFolder, this.localSaveFolder, file);
            }
        }

        private void RunDominionsProcess(string gameName)
        {
            Process firstProc = new Process();
            firstProc.StartInfo.FileName = Path.Combine(dominionsFolder, DominionsExecutable);
            firstProc.StartInfo.Arguments = $"-g {gameName}";
            firstProc.Start();
            firstProc.WaitForExit();
        }

        private void CopyTurnResults(string gameName)
        {
            string[] files = Directory.GetFiles(Path.Combine(this.localSaveFolder, gameName), "*.trn");
            foreach (var file in files)
            {
                this.CopyFile(this.localSaveFolder, this.hostSaveFolder, file);
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

            this.eventLog.WriteEntry($"File copied from {fileFullPath} to {targetPath}");
        }
    }
}