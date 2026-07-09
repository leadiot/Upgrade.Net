namespace Com.Scm.Upgrade
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var _ = new Upgrade().StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"升级任务失败: {ex.Message}");
                Console.WriteLine($"错误详情: {ex.ToString()}");
            }
        }
    }
}
