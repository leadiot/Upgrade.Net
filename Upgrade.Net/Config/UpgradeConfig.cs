using System.Text.Json;
using System.Text.Json.Serialization;

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
        /// 目标版本，用于升级界面展示
        /// 默认：null，
        /// 选项：可选
        /// </summary>
        public string NewVersion { get; set; }

        /// <summary>
        /// 现有版本，用于升级界面展示
        /// 默认：null，
        /// 选项：可选
        /// </summary>
        public string OldVersion { get; set; }

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
        /// 是否展示升级步骤列表
        /// 默认：false
        /// 选项：可选
        /// </summary>
        public bool ShowSteps { get; set; }

        /// <summary>
        /// 是否记录日志到文件
        /// 默认：false
        /// 选项：可选
        /// </summary>
        public bool LogToFile { get; set; }

        /// <summary>
        /// 升级步骤，系统按此步骤执行升级过程
        /// 默认：null
        /// 选项：必选
        /// </summary>
        public List<StepConfig> Steps { get; set; }

        public void LoadDefault()
        {
            Title = "Upgrade.Net 升级";
            AppInfo = "这是应用简介";
            VerInfo = "这是升级事项！";
            OldVersion = "1.0.0";
            NewVersion = "2.0.0";

            Steps = new List<StepConfig>();
        }

        public static UpgradeConfig Load()
        {
            var file = Path.Combine(AppContext.BaseDirectory, CONFIG_FILE);
            if (!File.Exists(file))
            {
                return null;
            }

            var json = File.ReadAllText(file);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return JsonSerializer.Deserialize<UpgradeConfig>(json, options);
        }

        public void Save(string path)
        {
            var file = Path.Combine(path, CONFIG_FILE);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new JsonStringEnumConverter());
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(file, json);
        }
    }
}
