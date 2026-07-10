using Com.Scm.Upgrade.Dto;
using System.IO;
using System.Text.Json;

namespace Com.Scm.Upgrade.Config
{
    public class UpgradeConfig
    {
        public const string CONFIG_FILE = "upgrade.json";

        /// <summary>
        /// 升级程序显示标题
        /// 默认：Upgrade.Wpf更新
        /// 选项：可选
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 升级程序是否在升级完成后自动退出
        /// 默认：false
        /// 选项：可选
        /// </summary>
        public bool AutoClose { get; set; }

        /// <summary>
        /// 待升级程序安装路径
        /// 默认：空
        /// 选项：必需
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
        public ScmAppInfo AppInfo { get; set; } = new ScmAppInfo();

        /// <summary>
        /// 版本信息配置，用于版本信息的展示
        /// 默认：null
        /// 选项：可选
        /// </summary>
        public ScmVerInfo VerInfo { get; set; } = new ScmVerInfo();

        public void LoadDefault()
        {
            Title = "Upgrade.Net更新";

            VerInfo.ver_info = "1.0.0";
            VerInfo.ver_date = "2024-01-01";
            VerInfo.remark = "这是版本更新说明！";
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
        /// 执行文件
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// 执行参数
        /// </summary>
        public string Args { get; set; }
    }

    public class BackupConfig
    {
        /// <summary>
        /// 备份文件路径
        /// </summary>
        public string Path { get; set; }
    }

    public class OfflineConfig
    {
        /// <summary>
        /// 离线展示文件
        /// </summary>
        public string File { get; set; }
        /// <summary>
        /// 服务离线时间，单位秒
        /// </summary>
        public int Time { get; set; }
    }

    public enum InstallType
    {
        /// <summary>
        /// 自动
        /// </summary>
        Auto,
        /// <summary>
        /// 从zip文件安装
        /// </summary>
        FromZip,
        /// <summary>
        /// 从url下载安装
        /// </summary>
        FromUrl
    }
}
