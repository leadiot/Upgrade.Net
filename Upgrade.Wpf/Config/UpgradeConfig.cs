using Com.Scm.Upgrade.Dto;
using System.IO;
using System.Text.Json;

namespace Com.Scm.Upgrade.Config
{
    public class UpgradeConfig
    {
        /// <summary>
        /// 配置文件
        /// </summary>
        public const string CONFIG_FILE = "Upgrade.Net.json";

        /// <summary>
        /// 应用标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 安装路径
        /// </summary>
        public string InstallPath { get; set; }

        /// <summary>
        /// 是否重启
        /// </summary>
        public bool AutoStart { get; set; }

        /// <summary>
        /// 是否自动关闭原程序
        /// </summary>
        public bool AutoClose { get; set; }

        /// <summary>
        /// 重启文件
        /// </summary>
        public string ExecuteFile { get; set; }

        /// <summary>
        /// 重启参数
        /// </summary>
        public string ExecuteArgs { get; set; }

        /// <summary>
        /// 应用信息
        /// </summary>
        public ScmAppInfo AppInfo { get; set; } = new ScmAppInfo();

        /// <summary>
        /// 版本信息
        /// </summary>
        public ScmVerInfo VerInfo { get; set; } = new ScmVerInfo();

        public void LoadDefault()
        {
            Title = "Nas.Net更新";

            VerInfo.ver_info = "1.0.0";
            VerInfo.ver_date = "2024-01-01";
            VerInfo.remark = "这是版本更新说明！";
            VerInfo.url = "";

            AutoStart = true;
            ExecuteFile = "";
            ExecuteArgs = null;
        }

        public static UpgradeConfig Load()
        {
            var file = Path.Combine(AppContext.BaseDirectory, CONFIG_FILE);
            if (File.Exists(file))
            {
                var json = File.ReadAllText(file);
                return JsonSerializer.Deserialize<UpgradeConfig>(json);
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
}
