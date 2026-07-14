using Com.Scm.Upgrade.Config;
using System.IO;
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
        private Upgrade _Upgrade;
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

            _Upgrade = new Upgrade();
            _Upgrade.LogMessage += OnLogMessage;
            _Upgrade.ProgressChanged += OnProgressChanged;
            _Upgrade.StepStatusChanged += OnStepStatusChanged;

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
        private void BtClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
        private void Start()
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

            Task.Run(() =>
            {
                try
                {
                    Dispatcher.Invoke(() => InitializeStepList(_AppConfig));

                    Log("开始升级流程...");

                    _Upgrade.Start();

                    Dispatcher.Invoke(() =>
                    {
                        if (!_AppConfig.AutoClose)
                        {
                            Log("升级完成！");
                            BtStart.Visibility = Visibility.Collapsed;
                            BtClose.Visibility = Visibility.Visible;
                            return;
                        }

                        Log("升级完成，3秒后升级程序自动关闭...");
                        Task.Delay(3000).ContinueWith(t => Dispatcher.Invoke(() => this.Close()));
                    });
                }
                catch (OperationCanceledException)
                {
                    Log("用户已取消升级");
                }
                catch (Exception ex)
                {
                    Log($"更新失败：{ex.Message}");
                    Dispatcher.Invoke(() =>
                    {
                        BtLater.Visibility = Visibility.Visible;
                        BtStart.Visibility = Visibility.Visible;
                        BtClose.Visibility = Visibility.Collapsed;
                    });
                }
                finally
                {
                    _Running = false;
                }
            });
        }

        private void OnLogMessage(string message)
        {
            Dispatcher.Invoke(() => Log(message));
        }

        private void OnProgressChanged(int percent, string status)
        {
            Dispatcher.Invoke(() =>
            {
                _Dvo.Percent = percent;
                Log(status);
            });
        }

        private void OnStepStatusChanged(int stepNumber, string title, string message, bool success)
        {
            Dispatcher.Invoke(() =>
            {
                if (_Dvo.Steps.Count >= stepNumber)
                {
                    var stepItem = _Dvo.Steps[stepNumber - 1];
                    stepItem.Title = title;
                    stepItem.Message = message;
                    stepItem.Status = message == "跳过" ? StepStatus.Skipped :
                                       message == "执行中" ? StepStatus.Running :
                                       success ? StepStatus.Success : StepStatus.Failed;
                }
            });
        }

        private void InitializeStepList(UpgradeConfig config)
        {
            _Dvo.Steps.Clear();

            if (config.Steps != null && config.Steps.Count > 0)
            {
                foreach (var step in config.Steps)
                {
                    _Dvo.Steps.Add(new StepItemDvo
                    {
                        StepNumber = _Dvo.Steps.Count + 1,
                        Title = string.IsNullOrEmpty(step.Title) ? GetActionTitle(step.Option) : step.Title,
                        Status = StepStatus.Pending,
                        Message = "等待执行"
                    });
                }
            }
        }

        private string GetActionTitle(UpgradeOption option)
        {
            var actions = new Dictionary<UpgradeOption, string>
            {
                { UpgradeOption.Download, "下载文件" },
                { UpgradeOption.Command, "执行命令" },
                { UpgradeOption.Zip, "压缩文件" },
                { UpgradeOption.Unzip, "解压文件" },
                { UpgradeOption.MoveDir, "移动目录" },
                { UpgradeOption.MoveDoc, "移动文件" },
                { UpgradeOption.CopyDir, "复制目录" },
                { UpgradeOption.CopyDoc, "复制文件" },
                { UpgradeOption.CreateDir, "创建目录" },
                { UpgradeOption.CreateDoc, "创建文件" },
                { UpgradeOption.DeleteDir, "删除目录" },
                { UpgradeOption.DeleteDoc, "删除文件" },
                { UpgradeOption.RenameDir, "重命名目录" },
                { UpgradeOption.RenameDoc, "重命名文件" }
            };

            return actions.TryGetValue(option, out var title) ? title : "未知操作";
        }
        #endregion
    }
}