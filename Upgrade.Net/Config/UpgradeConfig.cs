using System.Text.Json;

namespace Com.Scm.Upgrade.Config
{
    public class UpgradeConfig
    {
        public const string CONFIG_FILE = "upgrade.json";

        public string Icon { get; set; }

        public string Title { get; set; }

        public bool AutoStart { get; set; }

        public bool AutoClose { get; set; }

        public bool ShowSteps { get; set; }

        public string InstallPath { get; set; }

        public InstallType InstallType { get; set; }

        public string InstallFile { get; set; }

        public string DownloadUrl { get; set; }

        public OfflineConfig Offline { get; set; }

        public BackupConfig Backup { get; set; }

        public LaunchConfig Launch { get; set; }

        public List<string> IgnoreFiles { get; set; }

        public string AppInfo { get; set; }

        public string VerInfo { get; set; }

        public string OldVersion { get; set; }

        public string NewVersion { get; set; }

        public List<StepConfig> Steps { get; set; }

        public void LoadDefault()
        {
            Title = "Upgrade.Wpf更新";
            VerInfo = "这是版本更新说明！";
        }

        public static UpgradeConfig Load()
        {
            var file = Path.Combine(AppContext.BaseDirectory, CONFIG_FILE);
            if (File.Exists(file))
            {
                var json = File.ReadAllText(file);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<UpgradeConfig>(json, options);
            }

            return null;
        }

        public void Save(string baseDir)
        {
            var file = Path.Combine(AppContext.BaseDirectory, CONFIG_FILE);
            var json = JsonSerializer.Serialize(this);
            File.WriteAllText(file, json);
        }
    }

    public class StepConfig
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public UpgradeOption Option { get; set; }

        public int WaitTime { get; set; }

        public string Source { get; set; }

        public string Destination { get; set; }

        public string Path { get; set; }

        public string Url { get; set; }

        public string File { get; set; }

        public string Command { get; set; }

        public string Args { get; set; }

        public string OldName { get; set; }

        public string NewName { get; set; }

        public bool Overwrite { get; set; } = true;

        public UpgradeAction Action { get; set; }
    }

    public class UpgradePlan
    {
        public string Name { get; set; }

        public List<StepConfig> Steps { get; set; }
    }

    public class UpgradeResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }
    }

    public class UpgradeReport
    {
        public UpgradeResult Result { get; set; }

        public List<StepConfig> Steps { get; set; }
    }

    public enum UpgradeOption
    {
        None,
        Download,
        Command,
        Zip,
        Unzip,
        MoveDir,
        MoveDoc,
        CopyDir,
        CopyDoc,
        CreateDir,
        CreateDoc,
        DeleteDir,
        DeleteDoc,
        RenameDir,
        RenameDoc
    }

    public class UpgradeAction
    {
        public UpgradeOption Option { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public Func<StepConfig, UpgradeResult> Execute { get; set; }
    }

    public class LaunchConfig
    {
        public string Command { get; set; }

        public string Args { get; set; }
    }

    public class BackupConfig
    {
        public string Path { get; set; }
    }

    public class OfflineConfig
    {
        public string File { get; set; }

        public int Time { get; set; }
    }

    public enum InstallType
    {
        Auto,
        FromZip,
        FromUrl
    }
}
