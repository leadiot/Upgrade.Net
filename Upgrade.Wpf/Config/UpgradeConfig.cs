using System.IO;
using System.Text.Json;

namespace Com.Scm.Upgrade.Config
{
    public class UpgradeConfig
    {
        public const string CONFIG_FILE = "upgrade.json";

        /// <summary>
        /// 应用图标，用于升级程序显示
        /// 默认：无
        /// 选项：可选
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// 升级程序显示标题
        /// 默认：Upgrade.Wpf更新
        /// 选项：可选
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 升级程序是否自动执行更新
        /// 默认：false
        /// 选项：可选
        /// </summary>
        public bool AutoStart { get; set; }

        /// <summary>
        /// 升级程序是否在升级完成后自动退出
        /// 默认：false
        /// 选项：可选
        /// </summary>
        public bool AutoClose { get; set; }

        /// <summary>
        /// 待升级程序安装路径
        /// 默认：当前所在目录
        /// 选项：可选
        /// </summary>
        public string InstallPath { get; set; }

        /// <summary>
        /// 待升级程序安装来源类型
        /// 默认：Auto
        /// 选项：必需
        /// </summary>
        public InstallType InstallType { get; set; }

        /// <summary>
        /// 待升级程序安装zip文件路径
        /// 默认：空
        /// 选项：可选（当InstallType为FromZip时必需）
        /// </summary>
        public string InstallFile { get; set; }

        /// <summary>
        /// 升级程序安装zip文件下载地址
        /// 默认：空
        /// 选项：可选（当InstallType为FromUrl时必需）
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// 离线文件路径（用于Web应用自动识别以停止服务）
        /// 默认：空（仅IIS待应用时可以使用此配置）
        /// 选项：可选
        /// </summary>
        public OfflineConfig Offline { get; set; }

        /// <summary>
        /// 升级执行前自动备份路径配置
        /// 默认：null
        /// 选项：可选
        /// </summary>
        public BackupConfig Backup { get; set; }

        /// <summary>
        /// 升级完成后重启程序的配置
        /// 默认：null
        /// 选项：可选
        /// </summary>
        public LaunchConfig Launch { get; set; }

        /// <summary>
        /// 升级过程中需要忽略的文件列表（如配置文件、数据库等）
        /// 默认：null
        /// 选项：可选
        /// </summary>
        public List<string> IgnoreFiles { get; set; }

        /// <summary>
        /// 应用信息配置，用于应用信息的展示
        /// 默认：null
        /// 选项：可选
        /// </summary>
        public string AppInfo { get; set; }

        /// <summary>
        /// 版本信息配置，用于版本信息的展示
        /// 默认：null
        /// 选项：可选
        /// </summary>
        public string VerInfo { get; set; }

        public string OldVersion { get; set; }
        public string NewVersion { get; set; }

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

    public class LaunchConfig
    {
        /// <summary>
        /// 启动命令，支持格式：
        /// - 简单命令: "MyApp.exe"
        /// - 带参数: "dotnet MyApp.dll"
        /// - 完整路径: "C:\\Program Files\\MyApp.exe"
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// 额外参数，追加到命令后面
        /// </summary>
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
