using Com.Scm.Upgrade.Dto;
using System.Text.Json;

namespace Com.Scm.Upgrade.Config
{
    public class UpgradeConfig
    {
        /// <summary>
        /// 配置文件
        /// </summary>
        public const string CONFIG_FILE = "upgrade.json";

        /// <summary>
        /// 应用标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 是否自动关闭原程序
        /// </summary>
        public bool AutoClose { get; set; }

        /// <summary>
        /// 安装路径
        /// </summary>
        public string InstallPath { get; set; }

        /// <summary>
        /// 安装来源
        /// </summary>
        public InstallType InstallType { get; set; }

        /// <summary>
        /// 安装zip包路径
        /// </summary>
        public string InstallFile { get; set; }

        /// <summary>
        /// 离线文件路径
        /// </summary>
        public OfflineConfig Offline { get; set; }

        /// <summary>
        /// 重启参数
        /// </summary>
        public LaunchConfig Launch { get; set; }

        /// <summary>
        /// 自动备份路径
        /// </summary>
        public BackupConfig Backup { get; set; }

        /// <summary>
        /// 应用信息
        /// </summary>
        public ScmAppInfo AppInfo { get; set; } = new ScmAppInfo();

        /// <summary>
        /// 版本信息
        /// </summary>
        public ScmVerInfo VerInfo { get; set; } = new ScmVerInfo();

        /// <summary>
        /// 忽略文件列表
        /// </summary>
        public List<string> IgnoreFiles { get; set; }

        public void LoadDefault()
        {
            Title = "Upgrade.Net更新";

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
