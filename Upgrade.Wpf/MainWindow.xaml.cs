using Com.Scm.Upgrade.Config;
using Com.Scm.Upgrade.Dvo;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;

namespace Com.Scm.Upgrade
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowDvo _Dvo;

        private UpgradeConfig _AppConfig;

        /// <summary>
        /// HttpClient实例（静态单例避免重复创建）
        /// </summary>
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30) // 设置超时时间
        };

        /// <summary>
        /// 取消令牌源
        /// </summary>
        private CancellationTokenSource _Token;
        /// <summary>
        /// 是否暂停
        /// </summary>
        private bool _Paused;
        /// <summary>
        /// 已下载字节数
        /// </summary>
        private long _DownloadedBytes;
        /// <summary>
        /// 文件总字节数
        /// </summary>
        private long _TotalBytes;

        /// <summary>
        /// 压缩包内总文件/文件夹数
        /// </summary>
        private int _TotalEntries;
        /// <summary>
        /// 已处理的文件/文件夹数
        /// </summary>
        private int _ProcessedEntries;

        public MainWindow()
        {
            InitializeComponent();
        }

        public async void Init(UpgradeConfig appConfig)
        {
            _AppConfig = appConfig;

            _Dvo = new MainWindowDvo();

            if (!string.IsNullOrEmpty(_AppConfig.Title))
            {
                this.Title = _AppConfig.Title;
            }

            if (appConfig.AppInfo != null)
            {
                //_Dvo.AppName = appConfig.AppInfo.name;
                _Dvo.Content = appConfig.AppInfo.content;
            }
            if (appConfig.VerInfo == null)
            {
                _Dvo.Info = "版本信息为空！";
                return;
            }
            _Dvo.Info = appConfig.VerInfo.remark;

            if (string.IsNullOrEmpty(_AppConfig.InstallPath))
            {
                _AppConfig.InstallPath = AppDomain.CurrentDomain.BaseDirectory;
            }

            //TbVersion.Text = $"版本号: {verInfo.Version}  发布日期: {verInfo.ReleaseDate}";
            _Dvo.Enabled = true;
            _Dvo.Status = "准备下载...";

            this.DataContext = _Dvo;

            Start();
        }

        private void BtStart_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void BtPause_Click(object sender, RoutedEventArgs e)
        {
            _Paused = true;
            _Dvo.Status = "下载已暂停";
            BtStart.IsEnabled = true;
            BtPause.IsEnabled = false;
            BtStart.Content = "继续下载";
        }

        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            _Token?.Cancel();
            _Dvo.Status = "正在取消下载...";
            BtStart.IsEnabled = true;
            BtPause.IsEnabled = false;
            BtCancel.IsEnabled = false;
        }

        private async void Start()
        {
            // 如果是暂停后继续
            if (_Paused)
            {
                _Paused = false;
                BtStart.IsEnabled = false;
                BtPause.IsEnabled = true;
                BtCancel.IsEnabled = true;
                return;
            }

            if (string.IsNullOrEmpty(_AppConfig.VerInfo?.url))
            {
                BtStart.IsEnabled = false;
                BtPause.IsEnabled = false;
                BtCancel.IsEnabled = false;
                _Dvo.Status = "下载地址为空，无法更新！";
                return;
            }

            // 重置状态
            _Token = new CancellationTokenSource();
            _DownloadedBytes = 0;
            _TotalBytes = 0;
            PbInfo.Value = 0;
            _Dvo.Status = "准备下载...";

            // 禁用/启用按钮
            BtStart.IsEnabled = false;
            BtPause.IsEnabled = true;
            BtCancel.IsEnabled = true;

            try
            {
                var file = Path.Combine(AppContext.BaseDirectory, ".Temp");
                if (!Directory.Exists(file))
                {
                    Directory.CreateDirectory(file);
                }

                file = Path.Combine(file, DateTime.Now.ToFileTime() + ".zip");
                var result = await DownloadFileAsync(_AppConfig.VerInfo.url, file, _Token.Token);
                if (!result)
                {
                    return;
                }

                result = await UnzipFileAsync(file, _AppConfig.InstallPath, _Token.Token);
                if (!result)
                {
                    return;
                }

                Restart();

                if (!_AppConfig.AutoClose)
                {
                    BtStart.IsEnabled = false;
                    BtPause.IsEnabled = false;
                    BtCancel.IsEnabled = true;
                    return;
                }

                Close();
            }
            catch (OperationCanceledException)
            {
                _Dvo.Status = "下载已取消";
            }
            catch (Exception ex)
            {
                _Dvo.Status = $"更新失败：{ex.Message}";
                MessageBox.Show($"更新出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 恢复按钮状态
                BtStart.IsEnabled = true;
                BtPause.IsEnabled = false;
                BtCancel.IsEnabled = false;
                _Paused = false;
            }
        }

        /// <summary>
        /// 文件下载
        /// </summary>
        /// <param name="url"></param>
        /// <param name="savePath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> DownloadFileAsync(string url, string savePath, CancellationToken cancellationToken)
        {
            // 检查文件是否已存在，若存在则删除（也可实现断点续传，此处简化）
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode(); // 确保响应成功

                // 获取文件总大小
                _TotalBytes = response.Content.Headers.ContentLength ?? -1;
                if (_TotalBytes == -1)
                {
                    _Dvo.Status = "无法获取文件大小";
                }

                // 读取响应流并写入文件
                using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
                using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    double progress;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        // 检查是否暂停
                        while (_Paused)
                        {
                            await Task.Delay(100, cancellationToken); // 暂停时循环等待
                        }

                        // 写入文件
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);

                        // 更新已下载字节数
                        _DownloadedBytes += bytesRead;

                        // 计算进度
                        if (_TotalBytes > 0)
                        {
                            progress = (_DownloadedBytes * 100.0) / _TotalBytes;
                            _Dvo.Ratio = Math.Min(progress, 100);

                            // 更新进度文本（格式化大小）
                            _Dvo.Status = $"已下载：{FormatFileSize(_DownloadedBytes)} / {FormatFileSize(_TotalBytes)} ({progress:0.00}%)";
                        }
                        else
                        {
                            _Dvo.Status = $"已下载：{FormatFileSize(_DownloadedBytes)} (未知总大小)";
                        }
                    }
                }
            }

            _Dvo.Status = "文件下载完成！";
            return true;
        }

        /// <summary>
        /// 格式化文件大小（字节转KB/MB/GB）
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:0.00} KB";
            else if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024):0.00} MB";
            else
                return $"{bytes / (1024.0 * 1024 * 1024):0.00} GB";
        }

        /// <summary>
        /// 文件解压
        /// </summary>
        /// <param name="zipFilePath">ZIP文件路径</param>
        /// <param name="unzipPath">解压目标路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task<bool> UnzipFileAsync(string zipFilePath, string unzipPath, CancellationToken cancellationToken)
        {
            // 创建解压目录（不存在则创建）
            if (!Directory.Exists(unzipPath))
            {
                Directory.CreateDirectory(unzipPath);
            }

            // 异步解压（避免UI卡顿）
            using (var archive = await ZipFile.OpenReadAsync(zipFilePath))
            {
                // 获取压缩包内总条目数
                _TotalEntries = archive.Entries.Count;
                if (_TotalEntries == 0)
                {
                    _Dvo.Status = "压缩包为空！";
                    return false;
                }

                // 遍历所有条目并解压
                foreach (var entry in archive.Entries)
                {
                    // 检查是否取消
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        // 计算条目完整路径
                        var entryPath = Path.Combine(unzipPath, entry.FullName);

                        // 如果是目录，创建目录
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            if (!Directory.Exists(entryPath))
                            {
                                Directory.CreateDirectory(entryPath);
                            }
                        }
                        else
                        {
                            // 如果是文件，确保目录存在后解压
                            var entryDir = Path.GetDirectoryName(entryPath);
                            if (!Directory.Exists(entryDir))
                            {
                                Directory.CreateDirectory(entryDir);
                            }

                            // 解压文件（覆盖已存在的文件）
                            await entry.ExtractToFileAsync(entryPath, true);
                        }

                        // 更新进度
                        _ProcessedEntries++;
                        var progress = (double)_ProcessedEntries / _TotalEntries * 100;

                        // 更新UI（必须通过Dispatcher切换到主线程）
                        _Dvo.Ratio = Math.Min(progress, 100);
                        _Dvo.Status = $"正在解压：{entry.FullName} ({_ProcessedEntries}/{_TotalEntries}) ({progress:0.00}%)";
                    }
                    catch (Exception ex)
                    {
                        _Dvo.Status = $"解压文件 {entry.FullName} 失败：{ex.Message}";
                        // 单个文件失败不终止整体解压，继续处理下一个
                        continue;
                    }
                }
            }

            _Dvo.Status = "文件解压完成！";
            return true;
        }

        private void Restart()
        {
            if (!_AppConfig.AutoStart)
            {
                _Dvo.Status = "应用更新成功，请尝试自行启动！";
                return;
            }

            _Dvo.Status = "正在重启应用程序...";
            var path = _AppConfig.InstallPath;
            Directory.SetCurrentDirectory(path);
            if (!Path.IsPathRooted(_AppConfig.ExecuteFile))
            {
                path = Path.Combine(_AppConfig.InstallPath, _AppConfig.ExecuteFile);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                Arguments = _AppConfig.ExecuteArgs,
                UseShellExecute = true
            });

            _Dvo.Status = "应用更新成功！";
        }

        // 窗口关闭时取消下载
        protected override void OnClosed(EventArgs e)
        {
            _Token?.Cancel();
            _httpClient.Dispose();
            base.OnClosed(e);
        }
    }
}