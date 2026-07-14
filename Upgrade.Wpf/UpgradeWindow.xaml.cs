using Com.Scm.Upgrade.Config;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

namespace Com.Scm.Upgrade
{
    public partial class UpgradeWindow : Window
    {
        public const int MAJOR = 1;
        public const int MINOR = 0;
        public const int PATCH = 2;
        public const int BUILD = 3;
        public const string RELEASE = "2026-07-14";

        private UpgradeWindowDvo _Dvo;
        private UpgradeConfig _AppConfig;
        private CancellationTokenSource _Token;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };

        private bool _Running;

        private StreamWriter _Writer;

        public UpgradeWindow()
        {
            InitializeComponent();
        }

        public void Init(UpgradeConfig appConfig)
        {
            _AppConfig = appConfig;

            _Dvo = new UpgradeWindowDvo();

            _Writer = new StreamWriter("Upgrade.log");

            var title = $"Upgrade.Wpf v{MAJOR}.{MINOR}.{PATCH}.{BUILD}";
            this.Title = title;
            this.TbTitle.Text = title;

            if (!string.IsNullOrWhiteSpace(_AppConfig.Icon) && File.Exists(_AppConfig.Icon))
            {
                _Dvo.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(_AppConfig.Icon, UriKind.RelativeOrAbsolute));
            }
            else
            {
                _Dvo.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Upgrade.Wpf;component/Resources/logo.ico", UriKind.Absolute));
            }

            _Dvo.Title = string.IsNullOrEmpty(_AppConfig.Title) ? title : appConfig.Title;
            _Dvo.Subtitle = appConfig.OldVersion + " → " + appConfig.NewVersion;

            _Dvo.AppInfo = appConfig.AppInfo ?? "应用简介为空";
            _Dvo.VerInfo = appConfig.VerInfo ?? "暂无版本更新说明";

            if (string.IsNullOrEmpty(_AppConfig.InstallPath))
            {
                _AppConfig.InstallPath = AppDomain.CurrentDomain.BaseDirectory;
            }

            Log("初始化完成，点击开始升级");

            this.DataContext = _Dvo;

            if (_AppConfig.AutoStart)
            {
                Start();
            }
        }

        #region 事件处理
        /// <summary>
        /// 拖拽逻辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && this.WindowState != WindowState.Maximized)
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 稍后提醒
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtLater_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 开始更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtStart_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        /// <summary>
        /// 扫描文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (_AppConfig.Launch == null || string.IsNullOrWhiteSpace(_AppConfig.Launch.Command))
            {
                MessageBox.Show(this, "未配置启动命令！");
                return;
            }
            var result = ExecuteCommand(_AppConfig.InstallPath, _AppConfig.Launch.Command, _AppConfig.Launch.Args);
            if (!result)
            {
                MessageBox.Show(this, "启动程序失败！");
            }
        }
        #endregion

        #region 公共方法
        private void Log(string message)
        {
            _Dvo.Notice = message;
            LogFiles(message);
        }

        private void LogFiles(string message)
        {
            if (_Writer != null)
            {
                _Writer.WriteLine(message);
            }
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

        private bool ExecuteCommand(string path, string command, string args)
        {
            try
            {
                var parts = ParseCommand(command);
                var exePath = parts.Item1;
                var exeArgs = parts.Item2;

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"{exeArgs} {args ?? string.Empty}".Trim(),
                    WorkingDirectory = path,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        Log($"🔹 命令进程已启动，PID：{process.Id}");

                        var outputReader = process.StandardOutput.ReadToEndAsync();
                        var errorReader = process.StandardError.ReadToEndAsync();
                        var timeoutMs = 5000;
                        var exited = process.WaitForExit(timeoutMs);

                        var output = outputReader.Result;
                        var error = errorReader.Result;

                        if (!string.IsNullOrEmpty(output))
                        {
                            Log($"📋 命令输出：{output.Trim().Substring(0, Math.Min(200, output.Length))}");
                        }
                        if (!string.IsNullOrEmpty(error))
                        {
                            Log($"⚠️ 命令错误：{error.Trim().Substring(0, Math.Min(200, error.Length))}");
                        }

                        if (exited)
                        {
                            if (process.ExitCode == 0)
                            {
                                Log($"✅ 命令执行成功，退出码：{process.ExitCode}");
                                return true;
                            }
                            else
                            {
                                Log($"❌ 命令执行失败，退出码：{process.ExitCode}");
                                return false;
                            }
                        }
                        else
                        {
                            Log($"🔄 命令进程在 {timeoutMs / 1000} 秒内未退出，视为后台运行");
                            return true;
                        }
                    }
                    Log($"❌ 命令进程启动失败");
                    return false;
                }
            }
            catch (Exception exp)
            {
                Log($"❌ 命令执行异常：{exp.Message}");
                return false;
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_Token != null && !_Token.IsCancellationRequested)
            {
                _Token.Cancel();
            }
            if (_Writer != null)
            {
                _Writer.Flush();
                _Writer.Close();
                _Writer.Dispose();
                _Writer = null;
            }
        }
        #endregion

        #region 核心逻辑
        private async void Start()
        {
            if (_Running)
            {
                return;
            }

            _Running = true;

            BtStart.IsEnabled = false;
            BtStart.Content = "更新中...";
            BtLater.Visibility = Visibility.Collapsed;

            _Token = new CancellationTokenSource();
            _Dvo.Percent = 0;

            try
            {
                Log("开始升级流程，共7个步骤...");

                // 1. 准备安装目录
                PrepareInstallDirectory();

                // 2. 获取安装文件
                var zipFile = await GetInstallFile();
                if (zipFile == null)
                {
                    return;
                }

                bool isDownloaded = !string.Equals(zipFile, _AppConfig.InstallFile);

                // 3. 复制离线升级文件
                string offlineFile = await CopyOfflineFile();

                // 4. 备份现有文件
                await BackupFiles();

                // 5. 解压文件
                await ExtractFiles(zipFile);

                // 6. 清理临时文件
                await CleanupFiles(offlineFile, zipFile, isDownloaded);

                // 7. 启动应用程序
                await LaunchApplication();

                if (!_AppConfig.AutoClose)
                {
                    Log("🎉 升级完成！");
                    BtStart.Visibility = Visibility.Collapsed;
                    BtLaunch.Visibility = Visibility.Visible;
                    return;
                }

                Log("🎉 升级完成，3秒后升级程序自动关闭...");
                await Task.Delay(3000);
                this.Close();
            }
            catch (OperationCanceledException)
            {
                Log("🔹 用户已取消升级");
                return;
            }
            catch (Exception ex)
            {
                Log($"❌ 更新失败：{ex.Message}");
                BtLater.Visibility = Visibility.Visible;
                BtStart.Visibility = Visibility.Visible;
                BtLaunch.Visibility = Visibility.Collapsed;
                return;
            }
            finally
            {
                _Running = false;
            }
        }

        /// <summary>
        /// 准备安装目录，如果不存在则创建
        /// </summary>
        private void PrepareInstallDirectory()
        {
            Log("[步骤1/7] 准备安装目录...");
            if (!Directory.Exists(_AppConfig.InstallPath))
            {
                Log($"[步骤1/7] 创建安装目录：{_AppConfig.InstallPath}");
                Directory.CreateDirectory(_AppConfig.InstallPath);
            }
            Log("[步骤1/7] 安装目录准备完成");
        }

        /// <summary>
        /// 获取安装文件，如果是远程下载则返回临时文件路径，如果是本地文件则返回原路径
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetInstallFile()
        {
            Log("[步骤2/7] 获取安装文件...");

            if (_AppConfig.InstallType == InstallType.FromZip)
            {
                if (!File.Exists(_AppConfig.InstallFile))
                {
                    Log($"❌ [步骤2/7] 错误：指定的本地文件不存在：{_AppConfig.InstallFile}");
                    return null;
                }
                Log($"[步骤2/7] 使用本地压缩包：{Path.GetFileName(_AppConfig.InstallFile)}");
                return _AppConfig.InstallFile;
            }
            else if (_AppConfig.InstallType == InstallType.FromUrl)
            {
                Log("[步骤2/7] 从远程服务器下载更新包...");
                return await DownloadFileAsync(_AppConfig.DownloadUrl);
            }
            else
            {
                if (File.Exists(_AppConfig.InstallFile))
                {
                    Log($"[步骤2/7] 使用本地压缩包：{Path.GetFileName(_AppConfig.InstallFile)}");
                    return _AppConfig.InstallFile;
                }
                else
                {
                    Log("[步骤2/7] 本地文件不存在，转为远程下载...");
                    return await DownloadFileAsync(_AppConfig.DownloadUrl);
                }
            }
        }

        /// <summary>
        /// 下载文件并返回临时文件路径
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task<string> DownloadFileAsync(string url)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");

            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, _Token.Token))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                var downloadedBytes = 0;

                using (var stream = await response.Content.ReadAsStreamAsync(_Token.Token))
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    var buffer = new byte[81920];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _Token.Token)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, _Token.Token);
                        downloadedBytes += bytesRead;

                        if (totalBytes > 0)
                        {
                            var progress = (downloadedBytes * 100.0) / totalBytes;
                            _Dvo.Percent = Math.Min(progress, 100);
                            Log($"[步骤2/7] 📥 下载中：{FormatFileSize(downloadedBytes)} / {FormatFileSize(totalBytes)} ({progress:0.0}%)");
                        }
                        else
                        {
                            Log($"[步骤2/7] 📥 下载中：{FormatFileSize(downloadedBytes)}");
                        }
                    }
                }
            }

            _Dvo.Percent = 100;
            Log("[步骤2/7] ✅ 文件下载完成");
            return tempFilePath;
        }

        /// <summary>
        /// 复制离线升级文件到安装目录，如果配置了等待时间则等待指定秒数后继续
        /// </summary>
        /// <returns></returns>
        private async Task<string> CopyOfflineFile()
        {
            if (_AppConfig.Offline == null)
            {
                return null;
            }

            var file = _AppConfig.Offline.File;
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                return null;
            }

            Log("[步骤3/7] 📦 复制离线升级文件...");
            var name = Path.GetFileName(file);
            var dstFile = Path.Combine(_AppConfig.InstallPath, name);

            await Task.Run(() =>
            {
                File.Copy(file, dstFile, true);
                Log("[步骤3/7] ✅ 离线文件复制完成");

                var seconds = _AppConfig.Offline.Time;
                if (seconds > 0)
                {
                    Log($"[步骤3/7] ⏳ 等待 {seconds} 秒后执行升级...");
                    for (int i = seconds; i > 0; i--)
                    {
                        Log($"[步骤3/7] ⏳ 倒计时：{i} 秒");
                        Thread.Sleep(1000);
                    }
                }
            });

            Log("[步骤3/7] 🚀 开始执行升级任务");
            return dstFile;
        }

        private async Task BackupFiles()
        {
            _Dvo.Percent = 0;
            Log("[步骤4/7] 📋 备份现有文件...");

            if (_AppConfig.Backup == null || string.IsNullOrEmpty(_AppConfig.Backup.Path))
            {
                Log("[步骤4/7] ⏭️ 未配置备份路径，跳过备份");
                return;
            }

            if (!Directory.Exists(_AppConfig.InstallPath))
            {
                Log("[步骤4/7] ⏭️ 安装目录不存在，跳过备份");
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    Directory.CreateDirectory(_AppConfig.Backup.Path);
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var backupFileName = $"backup_{timestamp}.zip";
                    var backupFilePath = Path.Combine(_AppConfig.Backup.Path, backupFileName);

                    var allFiles = Directory.GetFiles(_AppConfig.InstallPath, "*", SearchOption.AllDirectories);
                    var totalFiles = allFiles.Length;

                    if (totalFiles == 0)
                    {
                        Log("[步骤4/7] ⏭️ 安装目录为空，跳过备份");
                        return;
                    }

                    Log($"[步骤4/7] 📦 准备备份 {totalFiles} 个文件...");

                    using (var zipStream = File.Create(backupFilePath))
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                    {
                        int processed = 0;
                        int skipped = 0;

                        foreach (var filePath in allFiles)
                        {
                            var relativePath = Path.GetRelativePath(_AppConfig.InstallPath, filePath);

                            try
                            {
                                archive.CreateEntryFromFile(filePath, relativePath, CompressionLevel.Optimal);
                                processed++;
                            }
                            catch (IOException)
                            {
                                skipped++;
                            }

                            var progress = ((processed + skipped) * 100.0) / totalFiles;
                            _Dvo.Percent = Math.Min(progress, 100);
                            Log($"[步骤4/7] 🔄 备份中：{processed}/{totalFiles} ({progress:0.0}%){(skipped > 0 ? $"，跳过 {skipped} 个" : "")}");
                        }
                    }

                    Log($"[步骤4/7] ✅ 备份完成：{Path.GetFileName(backupFilePath)}");
                }
                catch (Exception ex)
                {
                    Log($"❌ [步骤4/7] 备份失败：{ex.Message}");
                }
            });
        }

        private async Task ExtractFiles(string zipPath)
        {
            _Dvo.Percent = 0;
            Log("[步骤5/7] 📂 解压文件...");

            await Task.Run(() =>
            {
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    var entries = archive.Entries.ToList();
                    var totalEntries = entries.Count;
                    var processedEntries = 0;
                    var ignoredCount = 0;

                    if (totalEntries == 0)
                    {
                        Log("[步骤5/7] ⚠️ 压缩包为空");
                        return;
                    }

                    Log($"[步骤5/7] 📦 准备解压 {totalEntries} 个文件...");
                    LogFiles("解压目标目录：" + _AppConfig.InstallPath);

                    foreach (var entry in entries)
                    {
                        var path = Path.Combine(_AppConfig.InstallPath, entry.FullName);
                        if (path.EndsWith("/"))
                        {
                            Directory.CreateDirectory(path);
                            processedEntries++;
                            continue;
                        }

                        LogFiles("解压文件：" + path);

                        bool shouldIgnore = false;
                        if (_AppConfig.IgnoreFiles != null && _AppConfig.IgnoreFiles.Any(ignore =>
                            path.Contains(ignore, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (File.Exists(path))
                            {
                                shouldIgnore = true;
                                ignoredCount++;
                            }
                        }

                        if (!shouldIgnore)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                            entry.ExtractToFile(path, true);
                        }

                        processedEntries++;
                        var progress = (processedEntries * 100.0) / totalEntries;
                        _Dvo.Percent = Math.Min(progress, 100);
                        Log($"[步骤5/7] 🔄 解压中：{processedEntries}/{totalEntries} ({progress:0.0}%){(ignoredCount > 0 ? $"，忽略 {ignoredCount} 个" : "")}");
                    }
                }
            });

            Log("[步骤5/7] ✅ 文件解压完成");
        }

        private async Task CleanupFiles(string offlineFile, string zipFile, bool isDownloaded)
        {
            Log("[步骤6/7] 🧹 清理临时文件...");

            if (File.Exists(offlineFile))
            {
                await Task.Run(() => File.Delete(offlineFile));
                Log("[步骤6/7] ✅ 离线文件已清理");
            }

            if (isDownloaded && File.Exists(zipFile))
            {
                await Task.Run(() => File.Delete(zipFile));
                Log("[步骤6/7] ✅ 下载文件已清理");
            }
            else
            {
                Log("[步骤6/7] ⏭️ 使用本地文件，保留原文件");
            }
        }

        /// <summary>
        /// 启动应用程序，如果配置了启动命令则执行，否则跳过
        /// </summary>
        /// <returns></returns>
        private async Task LaunchApplication()
        {
            Log("[步骤7/7] 🚀 启动应用程序...");

            if (_AppConfig.Launch == null)
            {
                Log("[步骤7/7] ⏭️ 未配置启动程序，跳过");
                return;
            }

            if (string.IsNullOrWhiteSpace(_AppConfig.Launch.Command))
            {
                Log("[步骤7/7] ⏭️ 未配置启动命令，跳过");
                return;
            }

            Log($"[步骤7/7] 🔛 执行命令：{_AppConfig.Launch.Command} {(_AppConfig.Launch.Args ?? "")}");
            await Task.Run(() => ExecuteCommand(_AppConfig.InstallPath, _AppConfig.Launch.Command, _AppConfig.Launch.Args));

            Log("[步骤7/7] ✅ 命令执行完成");
        }
        #endregion
    }
}