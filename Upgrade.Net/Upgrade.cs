using Com.Scm.Upgrade.Config;
using System.IO.Compression;

namespace Com.Scm.Upgrade
{
    public class Upgrade
    {
        public const int MAJOR = 1;
        public const int MINOR = 0;
        public const int PATCH = 0;
        public const int BUILD = 1;

        public void Start()
        {
            Log("═══════════════════════════════════════════════");
            Log($"            应用升级程序 v{MAJOR}.{MINOR}.{PATCH}.{BUILD}");
            Log("═══════════════════════════════════════════════");
            Log("");

            Log("[步骤1/8] 读取配置文件...");
            var config = UpgradeConfig.Load();
            if (config == null)
            {
                Log("   [错误] 配置文件 upgrade.json 不存在，结束升级任务");
                ExitApplication();
                return;
            }
            Log("   [成功] 配置文件读取成功");

            Log("[步骤2/8] 验证配置信息...");
            if (!ValidateConfig(config))
            {
                ExitApplication();
                return;
            }
            Log("   [成功] 配置信息验证通过");
            Log($"   ├─ 当前版本： {config.OldVersion}");
            Log($"   ├─ 目标版本： {config.NewVersion}");
            Log($"   ├─ 下载地址： {config.DownloadUrl}");
            Log($"   ├─ 安装路径： {config.InstallPath}");
            if (config.Backup != null)
            {
                Log($"   ├─ 备份路径： {config.Backup.Path}");
            }
            Log($"   └─ 安装模式： {config.InstallType}");

            Log("[步骤3/8] 准备安装目录...");
            if (Directory.Exists(config.InstallPath))
            {
                Log("   [信息] 安装目录已存在");
            }
            else
            {
                Directory.CreateDirectory(config.InstallPath);
                Log("   [成功] 安装目录创建成功");
            }

            Log("[步骤4/8] 获取安装文件...");
            string zipFile = null;
            bool isDownloaded = false;

            if (config.InstallType == InstallType.FromZip)
            {
                zipFile = config.InstallFile;
                if (!File.Exists(zipFile))
                {
                    Log($"   [错误] 指定的本地安装文件不存在: {zipFile}，结束升级任务");
                    ExitApplication();
                    return;
                }
                Log($"   [信息] 使用本地文件: {zipFile}");
            }
            else if (config.InstallType == InstallType.FromUrl)
            {
                if (string.IsNullOrWhiteSpace(config.DownloadUrl))
                {
                    Log($"   [错误] 远程下载地址为空，结束升级任务");
                    ExitApplication();
                    return;
                }
                Log("   [信息] 从远程下载安装文件...");
                zipFile = DownloadFile(config.DownloadUrl);
                isDownloaded = true;
                Log($"   [成功] 文件下载完成");
            }
            else
            {
                zipFile = config.InstallFile;
                if (File.Exists(zipFile))
                {
                    Log($"   [信息] 使用本地文件: {zipFile}");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(config.DownloadUrl))
                    {
                        Log($"   [错误] 远程下载地址为空，结束升级任务");
                        ExitApplication();
                        return;
                    }
                    Log($"   [信息] 本地文件不存在，转为远程下载...");
                    zipFile = DownloadFile(config.DownloadUrl);
                    isDownloaded = true;
                    Log($"   [成功] 文件下载完成");
                }
            }

            var file = CopyOffline(config.InstallPath, config.Offline);

            Log("[步骤5/8] 备份现有文件...");
            var backupResult = BackupFiles(config.InstallPath, config.Backup);
            if (backupResult.Success)
            {
                Log($"   [成功] 备份完成: {backupResult.BackupPath}");
                if (backupResult.SkippedCount > 0)
                    Log($"   [信息] 已备份 {backupResult.ProcessedCount} 个文件，跳过 {backupResult.SkippedCount} 个锁定文件");
            }
            else
            {
                Log($"   [警告] 备份失败或未启用，继续升级");
            }

            Log("[步骤6/8] 解压文件到安装目录...");
            var extractResult = ExtractFiles(config.InstallPath, zipFile, config.IgnoreFiles);
            Log($"   [成功] 文件解压完成");
            if (extractResult.SkippedCount > 0)
            {
                Log($"   [信息] 已解压 {extractResult.ProcessedCount} 个文件，跳过 {extractResult.SkippedCount} 个忽略文件");
                foreach (var skippedFile in extractResult.SkippedFiles)
                {
                    Log($"         └─ {skippedFile}");
                }
            }

            DeleteOffline(file);

            Log("[步骤7/8] 清理临时文件...");
            if (isDownloaded)
            {
                CleanupFile(zipFile);
                Log("   [成功] 下载文件已清理");
            }
            else
            {
                Log("   [信息] 使用本地文件，保留原文件");
            }

            Log("[步骤8/8] 启动应用程序...");
            LaunchFile(config.InstallPath, config.Launch);

            Log("");
            Log("═══════════════════════════════════════════════");
            Log("            升级任务完成");
            Log("═══════════════════════════════════════════════");

            var summaryLines = new List<string>();
            summaryLines.Add($"目标版本: {config.NewVersion}");
            summaryLines.Add($"安装路径: {config.InstallPath}");
            if (backupResult.Success)
                summaryLines.Add($"备份文件: {backupResult.BackupPath}");
            summaryLines.Add($"处理文件: {extractResult.ProcessedCount} 个");
            if (extractResult.SkippedCount > 0)
                summaryLines.Add($"跳过文件: {extractResult.SkippedCount} 个");
            if (config.Launch != null && !string.IsNullOrEmpty(config.Launch.File))
                summaryLines.Add($"启动程序: {config.Launch.File}");

            for (int i = 0; i < summaryLines.Count; i++)
            {
                var prefix = i == summaryLines.Count - 1 ? "   └─ " : "   ├─ ";
                Log(prefix + summaryLines[i]);
            }

            Log("═══════════════════════════════════════════════");

            CloseApplication(config.AutoClose);
        }

        private bool ValidateConfig(UpgradeConfig config)
        {
            if (string.IsNullOrEmpty(config.DownloadUrl))
            {
                Log("   [错误] 下载地址为空，结束升级任务");
                return false;
            }
            if (string.IsNullOrEmpty(config.InstallPath))
            {
                Log("   [错误] 安装路径为空，结束升级任务");
                return false;
            }
            return true;
        }

        private string DownloadFile(string url)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
            using (var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) })
            {
                var response = httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                using (var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                using (var fileStream = File.Create(tempFilePath))
                {
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int bytesRead;
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
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
                    Console.WriteLine();
                }
            }
            return tempFilePath;
        }

        private ExtractResult ExtractFiles(string installPath, string zipPath, List<string> ignoreFiles = null)
        {
            var result = new ExtractResult { ProcessedCount = 0, SkippedCount = 0, SkippedFiles = new List<string>() };

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                var entries = archive.Entries.ToList();
                var totalEntries = entries.Count;

                if (totalEntries == 0)
                {
                    Log("   [警告] 压缩包为空，跳过解压步骤");
                    return result;
                }

                Log($"   [信息] 准备解压 {totalEntries} 个文件...");

                foreach (var entry in entries)
                {
                    if (entry.FullName.EndsWith("/"))
                    {
                        var dirPath = Path.Combine(installPath, entry.FullName);
                        Directory.CreateDirectory(dirPath);
                        result.ProcessedCount++;
                        continue;
                    }

                    var destPath = Path.Combine(installPath, entry.FullName);
                    var relativePath = entry.FullName;

                    bool shouldIgnore = false;
                    if (ignoreFiles != null && ignoreFiles.Any(ignore => relativePath.Contains(ignore, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (File.Exists(destPath))
                        {
                            result.SkippedCount++;
                            result.SkippedFiles.Add(relativePath);
                            shouldIgnore = true;
                        }
                    }

                    if (!shouldIgnore)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                        entry.ExtractToFile(destPath, true);
                        result.ProcessedCount++;
                    }
                    else
                    {
                        result.ProcessedCount++;
                    }

                    var progress = (int)((result.ProcessedCount * 100) / totalEntries);
                    Console.Write($"\r   解压进度: {result.ProcessedCount}/{totalEntries} ({progress}%)");
                }
            }

            Console.WriteLine();
            return result;
        }

        private void CleanupFile(string tempFilePath)
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }

        private BackupResult BackupFiles(string installPath, BackupConfig config)
        {
            var result = new BackupResult { Success = false, ProcessedCount = 0, SkippedCount = 0 };

            if (config == null)
            {
                Log("   [信息] 未启用自动备份，跳过备份步骤");
                return result;
            }

            var backupPath = config.Path;
            if (string.IsNullOrEmpty(backupPath))
            {
                Log("   [信息] 备份路径为空，跳过备份步骤");
                return result;
            }

            if (!Directory.Exists(installPath))
            {
                Log("   [信息] 安装目录不存在，跳过备份步骤");
                return result;
            }

            try
            {
                Directory.CreateDirectory(backupPath);
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var backupFileName = $"backup_{timestamp}.zip";
                var backupFilePath = Path.Combine(backupPath, backupFileName);

                var allFiles = Directory.GetFiles(installPath, "*", SearchOption.AllDirectories);
                var totalFiles = allFiles.Length;

                if (totalFiles == 0)
                {
                    Log("   [信息] 安装目录为空，跳过备份步骤");
                    return result;
                }

                Log($"   [信息] 准备备份 {totalFiles} 个文件...");

                using (var zipStream = File.Create(backupFilePath))
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    foreach (var filePath in allFiles)
                    {
                        var relativePath = Path.GetRelativePath(installPath, filePath);

                        try
                        {
                            archive.CreateEntryFromFile(filePath, relativePath, CompressionLevel.Optimal);
                            result.ProcessedCount++;
                        }
                        catch (IOException)
                        {
                            result.SkippedCount++;
                        }

                        var progress = (int)((result.ProcessedCount + result.SkippedCount) * 100 / totalFiles);
                        Console.Write($"\r   备份进度: {result.ProcessedCount + result.SkippedCount}/{totalFiles} ({progress}%)");
                    }
                }

                Console.WriteLine();
                result.Success = true;
                result.BackupPath = backupFilePath;
            }
            catch (Exception ex)
            {
                Log($"   [错误] 备份失败: {ex.Message}");
            }

            return result;
        }

        private void LaunchFile(string installPath, LaunchConfig config)
        {
            if (config == null)
            {
                Log("   [信息] 未配置启动程序，跳过启动步骤");
                return;
            }

            if (string.IsNullOrEmpty(config.File))
            {
                Log("   [信息] 未配置执行文件，跳过启动步骤");
                return;
            }

            var executePath = Path.Combine(installPath, config.File);
            if (File.Exists(executePath))
            {
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = executePath,
                    Arguments = config.Args ?? string.Empty,
                    WorkingDirectory = installPath,
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                System.Diagnostics.Process.Start(processStartInfo);
                Log($"   [成功] 启动程序: {config.File}");
            }
            else
            {
                Log($"   [警告] 执行文件不存在: {executePath}");
            }
        }

        private string CopyOffline(string installPath, OfflineConfig config)
        {
            if (config == null)
            {
                return null;
            }

            var file = config.File;
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                Log("   [信息] 离线文件不存在，跳过离线文件复制步骤");
                return null;
            }

            var name = Path.GetFileName(file);
            var dstFile = Path.Combine(installPath, name);
            File.Copy(file, dstFile, true);

            Log($"   [成功] 离线文件复制完成: {dstFile}");

            var seconds = config.Time;
            if (seconds > 0)
            {
                for (int i = seconds; i > 0; i--)
                {
                    Log($"   [信息] 升级任务将在 {i} 秒后执行...");
                    Thread.Sleep(1000);
                }
            }
            Log("");
            Log("   [信息] 开始执行升级任务");

            return dstFile;
        }

        private void DeleteOffline(string file)
        {
            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                    Log($"   [成功] 删除离线文件: {file}");
                }
                catch (Exception ex)
                {
                    Log($"   [警告] 删除离线文件失败: {ex.Message}");
                }
            }
        }

        private void CloseApplication(bool close)
        {
            if (close)
            {
                Log("");
                Log("   [信息] 升级程序即将退出...");
                return;
            }

            ExitApplication();
        }

        private void ExitApplication()
        {
            Log("");
            Log("[信息] 升级程序已结束，按任意键退出...");
            Console.ReadKey();
        }

        private void Log(string message)
        {
            Console.WriteLine(message);
        }

        private class ExtractResult
        {
            public int ProcessedCount { get; set; }
            public int SkippedCount { get; set; }
            public List<string> SkippedFiles { get; set; } = new List<string>();
        }

        private class BackupResult
        {
            public bool Success { get; set; }
            public string BackupPath { get; set; }
            public int ProcessedCount { get; set; }
            public int SkippedCount { get; set; }
        }
    }
}
