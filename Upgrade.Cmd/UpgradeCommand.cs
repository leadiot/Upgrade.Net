using Com.Scm.Upgrade.Config;
using Upgrade.Net;

namespace Com.Scm.Upgrade
{
    public class UpgradeCommand : UpgradeView
    {
        public const int MAJOR = 1;
        public const int MINOR = 1;
        public const int PATCH = 3;
        public const int BUILD = 3;
        public const string RELEASE = "2026-07-14";

        public void Run()
        {
            try
            {
                ShowHead();

                var config = UpgradeConfig.Load();
                if (config == null)
                {
                    Console.WriteLine("[错误] 配置文件 upgrade.json 不存在，结束升级任务");
                    Console.ReadKey();
                    return;
                }

                if (config == null)
                {
                    Log("[错误] 配置对象为空，结束升级任务");
                    return;
                }

                if (!string.IsNullOrEmpty(config.Title))
                {
                    Log($"[信息] 应用名称：{config.Title}");
                }

                if (!string.IsNullOrEmpty(config.OldVersion) && !string.IsNullOrEmpty(config.NewVersion))
                {
                    Log($"[信息] 版本升级：{config.OldVersion} -> {config.NewVersion}");
                }

                if (!string.IsNullOrEmpty(config.VerInfo))
                {
                    Log("[信息] 更新说明：");
                    Log(config.VerInfo);
                }

                Log("");

                var upgrade = new Upgrade(this);

                if (!config.AutoStart)
                {
                    Console.Write("是否要开始执行升级【y/n】：");
                    var text = Console.ReadLine() ?? "";
                    text = text.Trim();
                    if (text.ToUpper() != "Y")
                    {
                        return;
                    }
                }

                upgrade.Start(config);

                ShowFooter();

                if (!config.AutoClose)
                {
                    Console.WriteLine("");
                    Console.WriteLine("[信息] 升级程序已结束，按任意键退出...");
                    Console.ReadKey();
                }
                else
                {
                    Console.WriteLine("");
                    Console.WriteLine("[信息] 升级程序即将退出...");
                    Thread.Sleep(3000);
                }
            }
            catch (Exception ex)
            {
                ShowFooter();
                Console.WriteLine($"[错误] 更新失败：{ex.Message}");
                Console.ReadKey();
            }
        }

        private void ShowHead()
        {
            Log("═══════════════════════════════════════════════");
            Log($"            Upgrade.Net v{MAJOR}.{MINOR}.{PATCH}.{BUILD}");
            Log("═══════════════════════════════════════════════");
            Log("");
        }

        private void ShowFooter()
        {
            Log("");
            Log("═══════════════════════════════════════════════");
            Log("            升级任务完成");
            Log("═══════════════════════════════════════════════");
        }

        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void LogStep(int step, int count, string message)
        {
            Console.WriteLine($"   [消息] {message}");
        }

        public void LogStepInfo(string info, string message)
        {
            Console.WriteLine($"   [{info}] {message}");
        }

        public void LogStepWait(int time, string message)
        {
            Console.WriteLine($"   [等待] {message}");
        }

        public void LogStepProgress(int progress, string message)
        {
            Console.Write($"\r   [下载] {message}");
        }

        public void LogStepStatus(int stepNumber, StepStatus status, string title, string message)
        {
            Console.WriteLine($"   [消息] {message}");
        }

        public void ResetProgress()
        {
        }
    }
}
