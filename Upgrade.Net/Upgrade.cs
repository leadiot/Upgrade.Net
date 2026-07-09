using Com.Scm.Upgrade.Config;
using System.IO.Compression;

namespace Com.Scm.Upgrade
{
    public class Upgrade
    {
        public async Task StartAsync()
        {
            Log("=== 开始升级任务 ===");

            Log("1. 读取配置文件...");
            var config = UpgradeConfig.Load();
            if (config == null)
            {
                Log("   配置文件不存在，结束升级任务");
                return;
            }
            Log("   配置文件读取成功");

            Log("2. 验证配置信息...");
            if (!ValidateConfig(config))
            {
                return;
            }
            Log("   配置信息验证通过");
            Log($"   版本: {config.VerInfo.ver_info}");
            Log($"   下载地址: {config.VerInfo.url}");
            Log($"   安装路径: {config.InstallPath}");

            Log("3. 创建安装目录...");
            Directory.CreateDirectory(config.InstallPath);
            Log("   安装目录创建成功");

            Log("4. 下载更新文件...");
            var tempFilePath = await DownloadFileAsync(config.VerInfo.url);
            Log($"   文件下载成功: {tempFilePath}");

            Log("5. 备份现有文件...");
            BackupFiles(config.InstallPath, config.BackupPath, config.AutoBackup);

            try
            {
                Log("6. 解压文件到安装目录...");
                ExtractFiles(tempFilePath, config.InstallPath);
                Log("   文件解压成功");
            }
            finally
            {
                Log("7. 清理临时文件...");
                CleanupTempFile(tempFilePath);
                Log("   临时文件清理成功");
            }

            Log("8. 启动执行文件...");
            LaunchExecuteFile(config.InstallPath, config.ExecuteFile, config.ExecuteArgs);

            Log("=== 升级任务完成 ===");
        }

        private bool ValidateConfig(UpgradeConfig config)
        {
            if (string.IsNullOrEmpty(config.VerInfo?.url))
            {
                Log("   下载地址为空，结束升级任务");
                return false;
            }
            if (string.IsNullOrEmpty(config.InstallPath))
            {
                Log("   安装路径为空，结束升级任务");
                return false;
            }
            return true;
        }

        private async Task<string> DownloadFileAsync(string url)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
            using (var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) })
            {
                var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(tempFilePath))
                {
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        totalRead += bytesRead;
                        if (totalBytes > 0)
                        {
                            var progress = (int)((totalRead * 100) / totalBytes);
                            Console.Write($"\r   下载进度: {progress}% ({totalRead:N0} / {totalBytes:N0} bytes)");
                        }
                        else
                        {
                            Console.Write($"\r   下载进度: {totalRead:N0} bytes");
                        }
                    }
                    Log("");
                }
            }
            return tempFilePath;
        }

        private void ExtractFiles(string zipPath, string installPath)
        {
            ZipFile.ExtractToDirectory(zipPath, installPath, true);
        }

        private void CleanupTempFile(string tempFilePath)
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }

        private void BackupFiles(string installPath, string backupPath, bool autoBackup)
        {
            if (!autoBackup)
            {
                Log("   未启用自动备份，跳过备份步骤");
                return;
            }

            if (string.IsNullOrEmpty(backupPath))
            {
                Log("   备份路径为空，跳过备份步骤");
                return;
            }

            if (!Directory.Exists(installPath))
            {
                Log("   安装目录不存在，跳过备份步骤");
                return;
            }

            try
            {
                Directory.CreateDirectory(backupPath);
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var backupFileName = $"backup_{timestamp}.zip";
                var backupFilePath = Path.Combine(backupPath, backupFileName);

                ZipFile.CreateFromDirectory(installPath, backupFilePath, CompressionLevel.Optimal, false);
                Log($"   备份成功: {backupFilePath}");
            }
            catch (Exception ex)
            {
                Log($"   备份失败: {ex.Message}");
            }
        }

        private void LaunchExecuteFile(string installPath, string executeFile, string executeArgs)
        {
            if (string.IsNullOrEmpty(executeFile))
            {
                Log("   未配置执行文件，跳过启动步骤");
                return;
            }

            var executePath = Path.Combine(installPath, executeFile);
            if (File.Exists(executePath))
            {
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = executePath,
                    Arguments = executeArgs ?? string.Empty,
                    WorkingDirectory = installPath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(processStartInfo);
                Log($"   启动成功: {executePath}");
            }
            else
            {
                Log($"   警告: 执行文件不存在: {executePath}");
            }
        }

        private void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
