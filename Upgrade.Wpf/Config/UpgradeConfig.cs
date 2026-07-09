using Com.Scm.Upgrade.Dto;
using System.IO;
using System.Text.Json;

namespace Com.Scm.Upgrade.Config
{
    public class UpgradeConfig
    {
        public const string CONFIG_FILE = "upgrade.json";

        public string Title { get; set; }

        public bool AutoClose { get; set; }

        public string InstallPath { get; set; }

        public InstallType InstallType { get; set; }

        public string InstallFile { get; set; }

        public OfflineConfig Offline { get; set; }

        public LaunchConfig Launch { get; set; }

        public BackupConfig Backup { get; set; }

        public ScmAppInfo AppInfo { get; set; } = new ScmAppInfo();

        public ScmVerInfo VerInfo { get; set; } = new ScmVerInfo();

        public List<string> IgnoreFiles { get; set; }

        public void LoadDefault()
        {
            Title = "Upgrade.Wpf更新";

            VerInfo.ver_info = "1.0.0";
            VerInfo.ver_date = "2024-01-01";
            VerInfo.remark = "这是版本更新说明！";
            VerInfo.url = "";
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

    public class LaunchConfig
    {
        public string File { get; set; }

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
