using Com.Scm.Upgrade.Config;

namespace Com.Scm.Upgrade
{
    public class UpgradeCommand : UpgradeView
    {
        public const int MAJOR = 2;
        public const int MINOR = 1;
        public const int PATCH = 3;
        public const int BUILD = 4;

        public const string RELEASE_DATE = "2026-07-16";

        private UpgradeConfig _Config;
        private Upgrade _Upgrade;

        public void Run()
        {
            ShowHead();

            try
            {
                _Config = UpgradeConfig.Load();
                if (_Config == null)
                {
                    Log("[错误] 配置对象为空，结束升级任务");
                    Console.ReadKey();
                    return;
                }

                Log($"[信息] 应用名称：{_Config.Title}");
                Log($"[信息] 版本升级：{_Config.OldVersion} -> {_Config.NewVersion}");

                Log("[信息] 应用简介：");
                Log(_Config.AppInfo);

                Log("[信息] 升级事项：");
                Log(_Config.VerInfo);

                LogNewLine();

                Start();

                ShowFooter();
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine($"升级失败：{ex.Message}");
            }

            ShowFooter();

            if (!_Config.AutoClose)
            {
                Console.WriteLine("");
                Console.WriteLine("按任意键退出应用...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("");
                Console.WriteLine("升级程序即将退出...");
                Thread.Sleep(3000);
            }

            _Upgrade.Dispose();
        }

        private void Start()
        {
            _Upgrade = new Upgrade(this);

            if (!_Config.AutoStart)
            {
                Console.Write("是否要开始执行升级【y/n】：");
                var text = Console.ReadLine() ?? "";
                text = text.Trim();
                if (text.ToUpper() != "Y")
                {
                    return;
                }
            }

            _Upgrade.Start(_Config);
        }

        #region 工具方法
        /// <summary>
        /// 显示头部信息
        /// </summary>
        private void ShowHead()
        {
            Log("═══════════════════════════════════════════════");
            Log($"            Upgrade.Net v{MAJOR}.{MINOR}.{PATCH}.{BUILD}");
            Log("═══════════════════════════════════════════════");
            Log("");
        }

        /// <summary>
        /// 显示结束信息
        /// </summary>
        private void ShowFooter()
        {
            LogNewLine();
            Log("═══════════════════════════════════════════════");
            Log("            升级任务完成");
            Log("═══════════════════════════════════════════════");
        }
        #endregion

        #region 接口实现
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void LogNewLine()
        {
            Console.WriteLine("");
        }

        public void LogStep(int step, int count, string message)
        {
            Console.WriteLine($"[步骤{step}/{count}] " + message);
        }

        public void LogStepInfo(int step, string info, string message)
        {
            Console.WriteLine($"   [{info}] {message}");
        }

        public void LogStepWait(int step, int time, string message)
        {
            Console.WriteLine($"   [等待] {message}");
        }

        public void LogStepStatus(int step, StepStatus status, string message)
        {
            // 无需处理
            //Console.WriteLine($"   [消息] {message}");
        }

        public void LogStepProgress(int step, int progress, string message)
        {
            Console.Write($"\r   [进度] {message}");
        }

        public void ResetProgress()
        {
            // 无需处理
        }
        #endregion
    }
}
