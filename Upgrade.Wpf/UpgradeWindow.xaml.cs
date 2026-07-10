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
        public const int PATCH = 0;
        public const int BUILD = 1;

        private UpgradeWindowDvo _Dvo;
        private UpgradeConfig _AppConfig;
        private CancellationTokenSource _Token;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };

        private bool _Paused;
        private bool _Running;

        public UpgradeWindow()
        {
            InitializeComponent();
        }

        public void Init(UpgradeConfig appConfig)
        {
            _AppConfig = appConfig;

            _Dvo = new UpgradeWindowDvo();

            var title = $"Upgrade.Wpf 升级 v{MAJOR}.{MINOR}.{PATCH}.{BUILD}";
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
            _Dvo.VerInfo = appConfig.VerInfo ?? "版本信息为空！";

            if (string.IsNullOrEmpty(_AppConfig.InstallPath))
            {
                _AppConfig.InstallPath = AppDomain.CurrentDomain.BaseDirectory;
            }

            Log("准备更新...");

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
            var executePath = Path.Combine(_AppConfig.InstallPath, _AppConfig.Launch.File);
            if (!File.Exists(executePath))
            {
                MessageBox.Show(this, $"执行文件 {_AppConfig.Launch.File} 不存在！");
                return;
            }
            Execute(_AppConfig.InstallPath, executePath, _AppConfig.Launch.Args);
        }
        #endregion

        #region 公共方法
        private void Log(string message)
        {
            _Dvo.Notice = message;
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

        private void Execute(string path, string file, string args)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args ?? string.Empty,
                    WorkingDirectory = path,
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                Process.Start(processStartInfo);
            }
            catch (Exception exp)
            {
                MessageBox.Show(this, exp.Message);
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
                PrepareInstallDirectory();

                var zipFile = await GetInstallFile();
                if (zipFile == null)
                {
                    return;
                }

                bool isDownloaded = !string.Equals(zipFile, _AppConfig.InstallFile);

                string offlineFile = await CopyOfflineFile();

                await BackupFiles();

                await ExtractFiles(zipFile);

                await CleanupFiles(offlineFile, zipFile, isDownloaded);

                await LaunchApplication();

                Log("升级完成！");

                if (!_AppConfig.AutoClose)
                {
                    BtStart.Visibility = Visibility.Collapsed;
                    BtLaunch.Visibility = Visibility.Visible;
                    return;
                }

                Thread.Sleep(3000);
                this.Close();
            }
            catch (OperationCanceledException)
            {
                Log("已取消");
                return;
            }
            catch (Exception ex)
            {
                Log($"更新失败：{ex.Message}");
                BtLater.Visibility = Visibility.Visible;
                BtStart.Visibility = Visibility.Visible;
                BtLaunch.Visibility = Visibility.Collapsed;
                return;
            }
        }

        private void PrepareInstallDirectory()
        {
            Log("[步骤3/8] 准备安装目录...");
            if (!Directory.Exists(_AppConfig.InstallPath))
            {
                Directory.CreateDirectory(_AppConfig.InstallPath);
            }
            Log("[步骤3/8] 安装目录准备完成");
        }

        private async Task<string> GetInstallFile()
        {
            Log("[步骤4/8] 获取安装文件...");

            if (_AppConfig.InstallType == InstallType.FromZip)
            {
                if (!File.Exists(_AppConfig.InstallFile))
                {
                    Log($"[步骤4/8] 错误：指定的本地文件不存在: {_AppConfig.InstallFile}");
                    return null;
                }
                Log($"[步骤4/8] 使用本地文件: {_AppConfig.InstallFile}");
                return _AppConfig.InstallFile;
            }
            else if (_AppConfig.InstallType == InstallType.FromUrl)
            {
                Log("[步骤4/8] 从远程服务器下载...");
                return await DownloadFileAsync(_AppConfig.DownloadUrl);
            }
            else
            {
                if (File.Exists(_AppConfig.InstallFile))
                {
                    Log($"[步骤4/8] 使用本地文件: {_AppConfig.InstallFile}");
                    return _AppConfig.InstallFile;
                }
                else
                {
                    Log("[步骤4/8] 本地文件不存在，转为远程下载...");
                    return await DownloadFileAsync(_AppConfig.DownloadUrl);
                }
            }
        }

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
                        while (_Paused)
                        {
                            await Task.Delay(100, _Token.Token);
                        }

                        await fileStream.WriteAsync(buffer, 0, bytesRead, _Token.Token);
                        downloadedBytes += bytesRead;

                        if (totalBytes > 0)
                        {
                            var progress = (downloadedBytes * 100.0) / totalBytes;
                            _Dvo.Percent = Math.Min(progress, 100);
                            Log($"[步骤4/8] 下载中：{FormatFileSize(downloadedBytes)} / {FormatFileSize(totalBytes)} ({progress:0.00}%)");
                        }
                        else
                        {
                            Log($"[步骤4/8] 下载中：{FormatFileSize(downloadedBytes)}");
                        }
                    }
                }
            }

            _Dvo.Percent = 100;
            Log("[步骤4/8] 文件下载完成");
            return tempFilePath;
        }

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

            Log("[步骤4/8] 复制离线文件...");
            var name = Path.GetFileName(file);
            var dstFile = Path.Combine(_AppConfig.InstallPath, name);
            Log("[步骤4/8] 离线文件复制完成");

            await Task.Run(() =>
            {
                File.Copy(file, dstFile, true);

                var seconds = _AppConfig.Offline.Time;
                if (seconds > 0)
                {
                    for (int i = seconds; i > 0; i--)
                    {
                        Log($"[步骤4/8] 升级程序将在 {i} 秒后执行...");
                        Thread.Sleep(1000);
                    }
                }
            });

            Log("[步骤4/8] 开始执行升级任务");
            return dstFile;
        }

        private async Task BackupFiles()
        {
            _Dvo.Percent = 0;
            Log("[步骤5/8] 备份现有文件...");

            if (_AppConfig.Backup == null || string.IsNullOrEmpty(_AppConfig.Backup.Path))
            {
                Log("[步骤5/8] 未配置备份，跳过");
                return;
            }

            if (!Directory.Exists(_AppConfig.InstallPath))
            {
                Log("[步骤5/8] 安装目录不存在，跳过备份");
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
                        Log("[步骤5/8] 安装目录为空，跳过备份");
                        return;
                    }

                    Log($"[步骤5/8] 准备备份 {totalFiles} 个文件...");

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
                            Log($"[步骤5/8] 备份中：{processed}/{totalFiles} ({progress:0.00}%)");
                        }
                    }

                    Log($"[步骤5/8] 备份完成: {backupFilePath}");
                }
                catch (Exception ex)
                {
                    Log($"[步骤5/8] 备份失败: {ex.Message}");
                }
            });
        }

        private async Task ExtractFiles(string zipPath)
        {
            _Dvo.Percent = 0;
            Log("[步骤6/8] 解压文件...");

            await Task.Run(() =>
            {
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    var entries = archive.Entries.ToList();
                    var totalEntries = entries.Count;
                    var processedEntries = 0;

                    if (totalEntries == 0)
                    {
                        Log("[步骤6/8] 压缩包为空");
                        return;
                    }

                    Log($"[步骤6/8] 准备解压 {totalEntries} 个文件...");

                    foreach (var entry in entries)
                    {
                        if (entry.FullName.EndsWith("/"))
                        {
                            var dirPath = Path.Combine(_AppConfig.InstallPath, entry.FullName);
                            Directory.CreateDirectory(dirPath);
                            processedEntries++;
                            continue;
                        }

                        var destPath = Path.Combine(_AppConfig.InstallPath, entry.FullName);
                        var relativePath = entry.FullName;

                        bool shouldIgnore = false;
                        if (_AppConfig.IgnoreFiles != null && _AppConfig.IgnoreFiles.Any(ignore =>
                            relativePath.Contains(ignore, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (File.Exists(destPath))
                            {
                                shouldIgnore = true;
                            }
                        }

                        if (!shouldIgnore)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                            entry.ExtractToFile(destPath, true);
                        }

                        processedEntries++;
                        var progress = (processedEntries * 100.0) / totalEntries;
                        _Dvo.Percent = Math.Min(progress, 100);
                        Log($"[步骤6/8] 解压中：{processedEntries}/{totalEntries} ({progress:0.00}%)");
                    }
                }
            });

            Log("[步骤6/8] 文件解压完成");
        }

        private async Task DeleteOfflineFile(string file)
        {
            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                await Task.Run(() =>
                {
                    try
                    {
                        File.Delete(file);
                        Log($"[步骤6/8] 删除离线文件: {file}");
                    }
                    catch { }
                });
            }
        }

        private async Task CleanupFiles(string offlineFile, string zipFile, bool isDownloaded)
        {
            Log("[步骤7/8] 清理临时文件...");

            if (File.Exists(offlineFile))
            {
                await Task.Run(() => File.Delete(offlineFile));
                Log("[步骤7/8] 离线文件已清理");
            }

            if (isDownloaded && File.Exists(zipFile))
            {
                await Task.Run(() => File.Delete(zipFile));
                Log("[步骤7/8] 下载文件已清理");
            }
            else
            {
                Log("[步骤7/8] 使用本地文件，保留原文件");
            }
        }

        private async Task LaunchApplication()
        {
            Log("[步骤8/8] 启动应用程序...");

            if (_AppConfig.Launch == null || string.IsNullOrEmpty(_AppConfig.Launch.File))
            {
                Log("[步骤8/8] 未配置启动程序，跳过");
                return;
            }

            var executePath = Path.Combine(_AppConfig.InstallPath, _AppConfig.Launch.File);

            if (!File.Exists(executePath))
            {
                Log($"[步骤8/8] 警告：执行文件不存在: {executePath}");
                return;
            }

            Log($"[步骤8/8] 启动程序: {_AppConfig.Launch.File}");
            await Task.Run(() => Execute(_AppConfig.InstallPath, executePath, _AppConfig.Launch.Args));
        }
        #endregion
    }
}