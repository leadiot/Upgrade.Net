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
        /// 是否展示升级步骤列表
        /// 默认：false
        /// 选项：可选
        /// </summary>
        public bool ShowSteps { get; set; }

        /// <summary>
        /// 待升级程序安装路径
        /// 默认：当前所在目录
        /// 选项：可选
        /// </summary>
        public string InstallPath { get; set; }

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

        /// <summary>
        /// 现有版本，用于升级界面展示
        /// 默认：null，
        /// 选项：可选
        /// </summary>
        public string OldVersion { get; set; }

        /// <summary>
        /// 目标版本，用于升级界面展示
        /// 默认：null，
        /// 选项：可选
        /// </summary>
        public string NewVersion { get; set; }

        /// <summary>
        /// 升级步骤，系统按此步骤执行升级过程
        /// 默认：null
        /// 选项：必选
        /// </summary>
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
        /// <summary>
        /// 步骤标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 步骤说明
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public UpgradeOption Option { get; set; }

        /// <summary>
        /// （操作完成后）等待时间
        /// </summary>
        public int WaitTime { get; set; }

        /// <summary>
        /// 错误时是否继续
        /// </summary>
        public bool ContinueOnError { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 重试等待时间（单位毫秒）
        /// </summary>
        public int RetryDelay { get; set; } = 1000;

        /// <summary>
        /// 来源文件或路径
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 目的文件或路径
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// 目标文件
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// 目标路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 下载链接
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 执行命令
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// 命令参数
        /// </summary>
        public string Args { get; set; }

        /// <summary>
        /// 原名称
        /// </summary>
        public string OldName { get; set; }

        /// <summary>
        /// 新名称
        /// </summary>
        public string NewName { get; set; }

        /// <summary>
        /// 是否覆盖目标
        /// </summary>
        public bool Overwrite { get; set; } = true;
    }

    public class UpgradeResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }
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
}
