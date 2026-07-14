using Com.Scm.Upgrade.Config;

namespace Com.Scm.Upgrade
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var config = UpgradeConfig.Load();
                if (config == null)
                {
                    Console.WriteLine("[错误] 配置文件 upgrade.json 不存在，结束升级任务");
                    Console.ReadKey();
                    return;
                }

                var upgrade = new Upgrade();
                upgrade.LogMessage += Console.WriteLine;
                upgrade.ProgressChanged += (percent, status) =>
                {
                    Console.Write($"\r{status}");
                };
                upgrade.StepStatusChanged += (stepNumber, title, message, success) =>
                {
                    var status = success ? "[完成]" : (message == "跳过" ? "[跳过]" : "[失败]");
                    Console.WriteLine($"   {status} {message}");
                };

                upgrade.Start(config);

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
                Console.WriteLine($"[错误] 更新失败：{ex.Message}");
                Console.ReadKey();
            }
        }
    }
}
