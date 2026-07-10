using Com.Scm.Upgrade.Config;
using Com.Scm.Upgrade.Dvo;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Com.Scm.Upgrade
{
    public partial class UpdateWindow : Window
    {
        public const int MAJOR = 1;
        public const int MINOR = 0;
        public const int PATCH = 0;
        public const int BUILD = 1;

        private MainWindowDvo _Dvo;
        private UpgradeConfig _AppConfig;
        private CancellationTokenSource _Token;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };

        private bool _Paused;
        private long _DownloadedBytes;
        private long _TotalBytes;
        private int _TotalEntries;
        private int _ProcessedEntries;

        private DispatcherTimer _progressTimer;
        private double _currentProgress = 0;

        public UpdateWindow()
        {
            InitializeComponent();

            var title = $"升级程序 v{MAJOR}.{MINOR}.{PATCH}.{BUILD}";
            this.Title = title;
            this.TbTitle.Text = title;
        }

        public void Init(UpgradeConfig appConfig)
        {
            _AppConfig = appConfig;

            _Dvo = new MainWindowDvo();

            if (!string.IsNullOrEmpty(_AppConfig.Title))
            {
                this.Title = _AppConfig.Title;
            }

            _Dvo.Title = appConfig.Title;
            _Dvo.Version = appConfig.OldVersion + " → " + appConfig.NewVersion;

            if (appConfig.AppInfo != null)
            {
                _Dvo.AppInfo = appConfig.AppInfo.content;
            }
            if (appConfig.VerInfo == null)
            {
                _Dvo.VerInfo = "版本信息为空！";
                return;
            }
            _Dvo.VerInfo = appConfig.VerInfo.remark;

            if (string.IsNullOrEmpty(_AppConfig.InstallPath))
            {
                _AppConfig.InstallPath = AppDomain.CurrentDomain.BaseDirectory;
            }

            _Dvo.StartEnabled = true;
            _Dvo.PauseEnabled = false;
            _Dvo.CancelEnabled = false;
            Log("准备更新...");

            this.DataContext = _Dvo;

            Start();
        }

        // 1. 标题栏拖拽逻辑
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && this.WindowState != WindowState.Maximized)
            {
                this.DragMove();
            }
        }

        // 2. 右上角关闭按钮
        private void BtnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // 3. 立即更新逻辑
        private void BtStart_Click(object sender, RoutedEventArgs e)
        {
            BtStart.IsEnabled = false;
            BtStart.Content = "更新中...";
            BtLater.Visibility = Visibility.Collapsed;
            ProgressArea.Visibility = Visibility.Visible;

            _currentProgress = 0;
            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _progressTimer.Tick += (s, args) =>
            {
                _currentProgress += new Random().Next(3, 15);
                if (_currentProgress > 100) _currentProgress = 100;

                PbInfo.Value = _currentProgress;
                TbInfo.Text = $"正在下载并应用更新... {(int)_currentProgress}%";

                if (_currentProgress >= 100)
                {
                    _progressTimer.Stop();
                    FinishUpdate();
                }
            };
            _progressTimer.Start();
        }

        // 4. 更新完成逻辑
        private void FinishUpdate()
        {
            TbInfo.Text = "正在验证文件完整性...";

            var delayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
            delayTimer.Tick += (s, args) =>
            {
                delayTimer.Stop();

                ProgressArea.Visibility = Visibility.Collapsed;
                SuccessStatus.Visibility = Visibility.Visible;

                BtStart.Content = "重启应用";
                BtStart.IsEnabled = true;
                BtStart.Background = FindResource("SuccessGreen") as System.Windows.Media.SolidColorBrush;

                BtStart.Click -= BtStart_Click;
                BtStart.Click += (sender, e) =>
                {
                    MessageBox.Show("🚀 模拟重启：应用将关闭并加载新版本！", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    Application.Current.Shutdown();
                };
            };
            delayTimer.Start();
        }

        // 5. 稍后提醒
        private void BtLater_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #region 公共方法
        private void Log(string message)
        {
            _Dvo.Status = message;
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
        #endregion

        #region 核心逻辑
        private async void Start()
        {
            if (_Paused)
            {
                _Paused = false;
                _Dvo.StartEnabled = false;
                _Dvo.PauseEnabled = true;
                _Dvo.CancelEnabled = true;
                return;
            }

            if (!ValidateConfig(_AppConfig))
            {
                return;
            }

            _Token = new CancellationTokenSource();
            _Dvo.Ratio = 0;

            _Dvo.StartEnabled = false;
            _Dvo.PauseEnabled = true;
            _Dvo.CancelEnabled = true;

            try
            {
                await PrepareInstallDirectory();

                var zipFile = await GetInstallFile();
                if (zipFile == null)
                {
                    return;
                }

                bool isDownloaded = !string.Equals(zipFile, _AppConfig.InstallFile);

                string offlineFile = await CopyOfflineFile();

                await BackupFiles();

                await ExtractFiles(zipFile);

                await DeleteOfflineFile(offlineFile);

                await CleanupTempFile(zipFile, isDownloaded);

                await LaunchApplication();

                Log("升级完成！");

                if (_AppConfig.AutoClose)
                {
                    await Task.Delay(1000);
                    Close();
                }
            }
            catch (OperationCanceledException)
            {
                Log("已取消");
            }
            catch (Exception ex)
            {
                Log($"更新失败：{ex.Message}");
                MessageBox.Show($"更新出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _Dvo.StartEnabled = true;
                _Dvo.PauseEnabled = false;
                _Dvo.CancelEnabled = false;
                _Paused = false;
                //BtStart.Content = "开始";
            }
        }

        private bool ValidateConfig(UpgradeConfig config)
        {
            if (string.IsNullOrEmpty(config.VerInfo?.url))
            {
                Log("下载地址为空，无法更新！");
                return false;
            }
            if (string.IsNullOrEmpty(config.InstallPath))
            {
                Log("安装路径为空，无法更新！");
                return false;
            }
            return true;
        }

        private async Task PrepareInstallDirectory()
        {
            Log("[步骤3/8] 准备安装目录...");
            await Task.Run(() =>
            {
                if (!Directory.Exists(_AppConfig.InstallPath))
                {
                    Directory.CreateDirectory(_AppConfig.InstallPath);
                }
            });
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
                return await DownloadFileAsync(_AppConfig.VerInfo.url);
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
                    return await DownloadFileAsync(_AppConfig.VerInfo.url);
                }
            }
        }

        private async Task<string> DownloadFileAsync(string url)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");

            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, _Token.Token))
            {
                response.EnsureSuccessStatusCode();

                _TotalBytes = response.Content.Headers.ContentLength ?? -1;
                _DownloadedBytes = 0;

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
                        _DownloadedBytes += bytesRead;

                        if (_TotalBytes > 0)
                        {
                            var progress = (_DownloadedBytes * 100.0) / _TotalBytes;
                            _Dvo.Ratio = Math.Min(progress, 100);
                            Log($"[步骤4/8] 下载中：{FormatFileSize(_DownloadedBytes)} / {FormatFileSize(_TotalBytes)} ({progress:0.00}%)");
                        }
                        else
                        {
                            Log($"[步骤4/8] 下载中：{FormatFileSize(_DownloadedBytes)}");
                        }
                    }
                }
            }

            _Dvo.Ratio = 100;
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
            _Dvo.Ratio = 0;
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
                            _Dvo.Ratio = Math.Min(progress, 100);
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
            _Dvo.Ratio = 0;
            Log("[步骤6/8] 解压文件...");

            await Task.Run(() =>
            {
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    var entries = archive.Entries.ToList();
                    _TotalEntries = entries.Count;
                    _ProcessedEntries = 0;

                    if (_TotalEntries == 0)
                    {
                        Log("[步骤6/8] 压缩包为空");
                        return;
                    }

                    Log($"[步骤6/8] 准备解压 {_TotalEntries} 个文件...");

                    foreach (var entry in entries)
                    {
                        if (entry.FullName.EndsWith("/"))
                        {
                            var dirPath = Path.Combine(_AppConfig.InstallPath, entry.FullName);
                            Directory.CreateDirectory(dirPath);
                            _ProcessedEntries++;
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

                        _ProcessedEntries++;
                        var progress = (_ProcessedEntries * 100.0) / _TotalEntries;
                        _Dvo.Ratio = Math.Min(progress, 100);
                        Log($"[步骤6/8] 解压中：{_ProcessedEntries}/{_TotalEntries} ({progress:0.00}%)");
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

        private async Task CleanupTempFile(string zipFile, bool isDownloaded)
        {
            Log("[步骤7/8] 清理临时文件...");

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

            if (File.Exists(executePath))
            {
                await Task.Run(() =>
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = executePath,
                        Arguments = _AppConfig.Launch.Args ?? string.Empty,
                        WorkingDirectory = _AppConfig.InstallPath,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };
                    Process.Start(processStartInfo);
                });

                Log($"[步骤8/8] 启动程序: {_AppConfig.Launch.File}");
            }
            else
            {
                Log($"[步骤8/8] 警告：执行文件不存在: {executePath}");
            }
        }
        #endregion
    }
}