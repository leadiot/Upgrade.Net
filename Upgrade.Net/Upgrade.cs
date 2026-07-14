using Com.Scm.Upgrade.Config;
using System.IO.Compression;

namespace Com.Scm.Upgrade
{
    public class Upgrade
    {
        public const int MAJOR = 1;
        public const int MINOR = 1;
        public const int PATCH = 3;
        public const int BUILD = 3;
        public const string RELEASE = "2026-07-14";

        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };

        public void Start()
        {
            Log("═══════════════════════════════════════════════");
            Log($"            Upgrade.Net v{MAJOR}.{MINOR}.{PATCH}.{BUILD}");
            Log("═══════════════════════════════════════════════");
            Log("");

            try
            {
                Log("[步骤1/7] 准备安装目录...");
                var config = UpgradeConfig.Load();
                if (config == null)
                {
                    Log("   [错误] 配置文件 upgrade.json 不存在，结束升级任务");
                    ExitApplication();
                    return;
                }

                if (string.IsNullOrEmpty(config.InstallPath))
                {
                    config.InstallPath = AppDomain.CurrentDomain.BaseDirectory;
                }

                if (!Directory.Exists(config.InstallPath))
                {
                    Directory.CreateDirectory(config.InstallPath);
                    Log($"   [创建] 安装目录：{config.InstallPath}");
                }
                Log("   [完成] 安装目录准备完成");

                Log("[步骤2/7] 获取安装文件...");
                string zipFile = null;
                bool isDownloaded = false;

                if (config.InstallType == InstallType.FromZip)
                {
                    zipFile = config.InstallFile;
                    if (!File.Exists(zipFile))
                    {
                        Log($"   [错误] 指定的本地安装文件不存在：{zipFile}，结束升级任务");
                        ExitApplication();
                        return;
                    }
                    Log($"   [使用] 本地压缩包：{Path.GetFileName(zipFile)}");
                }
                else if (config.InstallType == InstallType.FromUrl)
                {
                    if (string.IsNullOrWhiteSpace(config.DownloadUrl))
                    {
                        Log($"   [错误] 远程下载地址为空，结束升级任务");
                        ExitApplication();
                        return;
                    }
                    Log("   [下载] 从远程服务器下载更新包...");
                    zipFile = DownloadFile(config.DownloadUrl);
                    isDownloaded = true;
                    Log("   [完成] 文件下载完成");
                }
                else
                {
                    zipFile = config.InstallFile;
                    if (File.Exists(zipFile))
                    {
                        Log($"   [使用] 本地压缩包：{Path.GetFileName(zipFile)}");
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(config.DownloadUrl))
                        {
                            Log($"   [错误] 远程下载地址为空，结束升级任务");
                            ExitApplication();
                            return;
                        }
                        Log("   [切换] 本地文件不存在，转为远程下载...");
                        zipFile = DownloadFile(config.DownloadUrl);
                        isDownloaded = true;
                        Log("   [完成] 文件下载完成");
                    }
                }

                string offlineFile = CopyOffline(config.InstallPath, config.Offline);

                Log("[步骤4/7] 备份现有文件...");
                var backupResult = BackupFiles(config.InstallPath, config.Backup);
                if (backupResult.Success)
                {
                    Log($"   [完成] 备份完成：{Path.GetFileName(backupResult.BackupPath)}");
                    if (backupResult.SkippedCount > 0)
                        Log($"   [信息] 已备份 {backupResult.ProcessedCount} 个文件，跳过 {backupResult.SkippedCount} 个锁定文件");
                }
                else
                {
                    Log($"   [跳过] 备份未启用或失败，继续升级");
                }

                Log("[步骤5/7] 解压文件到安装目录...");
                var extractResult = ExtractFiles(config.InstallPath, zipFile, config.IgnoreFiles);
                Log($"   [完成] 文件解压完成");
                if (extractResult.SkippedCount > 0)
                {
                    Log($"   [信息] 已解压 {extractResult.ProcessedCount} 个文件，忽略 {extractResult.SkippedCount} 个文件");
                    foreach (var skippedFile in extractResult.SkippedFiles)
                    {
                        Log($"         +-- {skippedFile}");
                    }
                }

                DeleteOffline(offlineFile);

                Log("[步骤6/7] 清理临时文件...");
                if (isDownloaded)
                {
                    CleanupFile(zipFile);
                    Log("   [完成] 下载文件已清理");
                }
                else
                {
                    Log("   [跳过] 使用本地文件，保留原文件");
                }

                Log("[步骤7/7] 启动应用程序...");
                LaunchApplication(config.InstallPath, config.Launch);

                Log("");
                Log("═══════════════════════════════════════════════");
                Log("            升级任务完成");
                Log("═══════════════════════════════════════════════");

                var summaryLines = new List<string>();
                if (!string.IsNullOrEmpty(config.NewVersion))
                    summaryLines.Add($"目标版本：{config.NewVersion}");
                summaryLines.Add($"安装路径：{config.InstallPath}");
                if (backupResult.Success)
                    summaryLines.Add($"备份文件：{Path.GetFileName(backupResult.BackupPath)}");
                summaryLines.Add($"处理文件：{extractResult.ProcessedCount} 个");
                if (extractResult.SkippedCount > 0)
                    summaryLines.Add($"忽略文件：{extractResult.SkippedCount} 个");
                if (config.Launch != null && !string.IsNullOrEmpty(config.Launch.Command))
                    summaryLines.Add($"启动命令：{config.Launch.Command}");

                for (int i = 0; i < summaryLines.Count; i++)
                {
                    var prefix = i == summaryLines.Count - 1 ? "   +-- " : "   |-- ";
                    Log(prefix + summaryLines[i]);
                }

                Log("═══════════════════════════════════════════════");

                CloseApplication(config.AutoClose);
            }
            catch (Exception ex)
            {
                Log($"");
                Log($"[错误] 更新失败：{ex.Message}");
                ExitApplication();
            }
        }

        private string DownloadFile(string url)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");

            var response = _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
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
                        Console.Write($"\r   [下载] 进度：{progress}% ({FormatFileSize(totalRead)} / {FormatFileSize(totalBytes)})");
                    }
                    else
                    {
                        Console.Write($"\r   [下载] 进度：{FormatFileSize(totalRead)}");
                    }
                }
                Console.WriteLine();
            }
            return tempFilePath;
        }

        private string FormatFileSize(long size)
        {
            var units = new string[] { "B", "KB", "MB", "GB", "TB", "PB" };
            int i = 0;
            while (size > 1024)
            {
                size = size >> 10;
                i++;
            }
            return size + units[i];
        }

        private ExtractResult ExtractFiles(string installPath, string zipPath, List<string> ignoreFiles = null)
        {
            var result = new ExtractResult { ProcessedCount = 0, SkippedCount = 0, SkippedFiles = new List<string>() };

            var fileName = Path.GetFileNameWithoutExtension(zipPath);

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                var entries = archive.Entries.ToList();
                var totalEntries = entries.Count;

                if (totalEntries == 0)
                {
                    Log("   [警告] 压缩包为空，跳过解压步骤");
                    return result;
                }

                Log($"   [解压] 准备解压 {totalEntries} 个文件...");

                foreach (var entry in entries)
                {
                    var path = TrimStart(entry.FullName, fileName);
                    if (path.EndsWith("/"))
                    {
                        var dirPath = Path.Combine(installPath, path);
                        Directory.CreateDirectory(dirPath);
                        result.ProcessedCount++;
                        continue;
                    }

                    var docPath = Path.Combine(installPath, path);

                    bool shouldIgnore = false;
                    if (ignoreFiles != null && ignoreFiles.Any(ignore => docPath.Contains(ignore, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (File.Exists(docPath))
                        {
                            result.SkippedCount++;
                            result.SkippedFiles.Add(docPath);
                            shouldIgnore = true;
                        }
                    }

                    if (!shouldIgnore)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(docPath));
                        entry.ExtractToFile(docPath, true);
                    }

                    result.ProcessedCount++;

                    var progress = (int)((result.ProcessedCount * 100) / totalEntries);
                    Console.Write($"\r   [解压] 进度：{result.ProcessedCount}/{totalEntries} ({progress}%){(result.SkippedCount > 0 ? $"，忽略 {result.SkippedCount} 个" : "")}");
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

            if (config == null || string.IsNullOrEmpty(config.Path))
            {
                Log("   [跳过] 未配置备份路径，跳过备份");
                return result;
            }

            if (!Directory.Exists(installPath))
            {
                Log("   [跳过] 安装目录不存在，跳过备份");
                return result;
            }

            try
            {
                Directory.CreateDirectory(config.Path);
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var backupFileName = $"backup_{timestamp}.zip";
                var backupFilePath = Path.Combine(config.Path, backupFileName);

                var allFiles = Directory.GetFiles(installPath, "*", SearchOption.AllDirectories);
                var totalFiles = allFiles.Length;

                if (totalFiles == 0)
                {
                    Log("   [跳过] 安装目录为空，跳过备份");
                    return result;
                }

                Log($"   [备份] 准备备份 {totalFiles} 个文件...");

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
                        Console.Write($"\r   [备份] 进度：{result.ProcessedCount + result.SkippedCount}/{totalFiles} ({progress}%){(result.SkippedCount > 0 ? $"，跳过 {result.SkippedCount} 个" : "")}");
                    }
                }

                Console.WriteLine();
                result.Success = true;
                result.BackupPath = backupFilePath;
            }
            catch (Exception ex)
            {
                Log($"   [错误] 备份失败：{ex.Message}");
            }

            return result;
        }

        private void LaunchApplication(string installPath, LaunchConfig config)
        {
            if (config == null)
            {
                Log("   [跳过] 未配置启动程序，跳过启动步骤");
                return;
            }

            if (string.IsNullOrWhiteSpace(config.Command))
            {
                Log("   [跳过] 未配置启动命令，跳过启动步骤");
                return;
            }

            Log($"   [执行] 命令：{config.Command} {config.Args ?? ""}");

            try
            {
                var parts = ParseCommand(config.Command);
                var exePath = parts.Item1;
                var exeArgs = parts.Item2;

                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"{exeArgs} {config.Args ?? string.Empty}".Trim(),
                    WorkingDirectory = installPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = System.Diagnostics.Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        Log($"   [信息] 进程已启动，PID：{process.Id}");

                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        var exited = process.WaitForExit(5000);

                        if (!string.IsNullOrEmpty(output))
                        {
                            Log($"   [输出] 命令输出：{output.Trim().Substring(0, Math.Min(200, output.Length))}");
                        }
                        if (!string.IsNullOrEmpty(error))
                        {
                            Log($"   [警告] 命令错误：{error.Trim().Substring(0, Math.Min(200, error.Length))}");
                        }

                        if (exited)
                        {
                            if (process.ExitCode == 0)
                            {
                                Log($"   [完成] 命令执行成功，退出码：{process.ExitCode}");
                            }
                            else
                            {
                                Log($"   [错误] 命令执行失败，退出码：{process.ExitCode}");
                            }
                        }
                        else
                        {
                            Log($"   [信息] 命令进程在 5 秒内未退出，视为后台运行");
                        }
                    }
                    else
                    {
                        Log($"   [错误] 进程启动失败");
                    }
                }
            }
            catch (Exception ex)
            {
                Log("   [错误] 命令执行异常：" + ex.Message);
            }
        }

        private Tuple<string, string> ParseCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return Tuple.Create(string.Empty, string.Empty);
            }

            command = command.Trim();

            if (command.StartsWith("\""))
            {
                var endQuote = command.IndexOf("\"", 1);
                if (endQuote > 0)
                {
                    var exe = command.Substring(1, endQuote - 1);
                    var args = command.Substring(endQuote + 1).Trim();
                    return Tuple.Create(exe, args);
                }
            }

            var spaceIndex = command.IndexOf(' ');
            if (spaceIndex > 0)
            {
                var exe = command.Substring(0, spaceIndex);
                var args = command.Substring(spaceIndex + 1);
                return Tuple.Create(exe, args);
            }

            return Tuple.Create(command, string.Empty);
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
                return null;
            }

            Log("[步骤3/7] 复制离线升级文件...");
            var name = Path.GetFileName(file);
            var dstFile = Path.Combine(installPath, name);
            File.Copy(file, dstFile, true);

            Log("   [完成] 离线文件复制完成");

            var seconds = config.Time;
            if (seconds > 0)
            {
                Log($"   [等待] 等待 {seconds} 秒后执行升级...");
                for (int i = seconds; i > 0; i--)
                {
                    Log($"   [等待] 倒计时：{i} 秒");
                    Thread.Sleep(1000);
                }
            }
            Log("   [开始] 开始执行升级任务");

            return dstFile;
        }

        private void DeleteOffline(string file)
        {
            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                    Log($"   [完成] 删除离线文件：{Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    Log($"   [警告] 删除离线文件失败：{ex.Message}");
                }
            }
        }

        private void CloseApplication(bool autoClose)
        {
            if (autoClose)
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

        private string TrimStart(string path, string start)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            if (path.StartsWith(start, StringComparison.OrdinalIgnoreCase))
            {
                return path.Substring(start.Length);
            }
            return path;
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