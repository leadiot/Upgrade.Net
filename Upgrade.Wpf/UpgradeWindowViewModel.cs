using Com.Scm.Upgrade.Config;
using Com.Scm.Upgrade.Dvo;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Com.Scm.Upgrade
{
    public class UpgradeWindowViewModel : ScmDvo, UpgradeView
    {
        public const int MAJOR = 2;
        public const int MINOR = 3;
        public const int PATCH = 5;
        public const int BUILD = 6;

        public const string RELEASE_DATE = "2026-07-16";

        #region 视图属性
        private ImageSource _icon;
        public ImageSource Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _subtitle;
        public string Subtitle
        {
            get => _subtitle;
            set => SetProperty(ref _subtitle, value);
        }

        private string _appInfo;
        public string AppInfo
        {
            get => _appInfo;
            set => SetProperty(ref _appInfo, value);
        }

        private Visibility _appVisibility = Visibility.Visible;
        public Visibility AppVisibility
        {
            get => _appVisibility;
            set => SetProperty(ref _appVisibility, value);
        }

        private string _verInfo;
        public string VerInfo
        {
            get => _verInfo;
            set => SetProperty(ref _verInfo, value);
        }

        private Visibility _verVisibility = Visibility.Visible;
        public Visibility VerVisibility
        {
            get => _verVisibility;
            set => SetProperty(ref _verVisibility, value);
        }

        private string _notice;
        public string Notice
        {
            get => _notice;
            set
            {
                SetProperty(ref _notice, value);
                OnPropertyChanged(nameof(NoticeColor));
            }
        }

        public string NoticeColor
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_notice))
                    return "#7A8B9E";
                if (_notice.Contains("失败") || _notice.Contains("错误"))
                    return "#D94A4A";
                if (_notice.Contains("完成"))
                    return "#38A169";
                return "#2A6BC6";
            }
        }

        private double _percent;
        public double Percent
        {
            get => _percent;
            set => SetProperty(ref _percent, value);
        }

        private ObservableCollection<StepItemDvo> _steps = new();
        public ObservableCollection<StepItemDvo> Steps
        {
            get => _steps;
            set => SetProperty(ref _steps, value);
        }

        private Visibility _stepsVisibility = Visibility.Visible;
        public Visibility StepsVisibility
        {
            get => _stepsVisibility;
            set => SetProperty(ref _stepsVisibility, value);
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        private bool _startEnabled = true;
        public bool StartEnabled
        {
            get => _startEnabled;
            set => SetProperty(ref _startEnabled, value);
        }

        private Visibility _startVisibility = Visibility.Visible;
        public Visibility StartVisibility
        {
            get => _startVisibility;
            set => SetProperty(ref _startVisibility, value);
        }

        private bool _laterEnabled = true;
        public bool LaterEnabled
        {
            get => _laterEnabled;
            set => SetProperty(ref _laterEnabled, value);
        }

        private Visibility _laterVisibility = Visibility.Visible;
        public Visibility LaterVisibility
        {
            get => _laterVisibility;
            set => SetProperty(ref _laterVisibility, value);
        }

        private bool _closeEnabled;
        public bool CloseEnabled
        {
            get => _closeEnabled;
            set => SetProperty(ref _closeEnabled, value);
        }

        private Visibility _closeVisibility = Visibility.Collapsed;
        public Visibility CloseVisibility
        {
            get => _closeVisibility;
            set => SetProperty(ref _closeVisibility, value);
        }

        private string _startButtonText = "立即升级";
        public string StartButtonText
        {
            get => _startButtonText;
            set => SetProperty(ref _startButtonText, value);
        }
        #endregion

        #region 视图事件
        public ICommand StartCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand LaterCommand { get; }
        public ICommand CloseWindowCommand { get; }

        public event Action<int> ScrollToStepRequested;
        #endregion

        private readonly UpgradeConfig _Config;
        private readonly Upgrade _Upgrade;

        public UpgradeWindowViewModel(UpgradeConfig config)
        {
            _Config = config;
            _Upgrade = new Upgrade(this);

            StartCommand = new RelayCommand(ExecuteStart);
            CloseCommand = new RelayCommand(ExecuteClose);
            LaterCommand = new RelayCommand(ExecuteLater);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);

            Initialize();
        }

        #region 初始化
        private void Initialize()
        {
            var appTitle = $"Upgrade.Wpf v{MAJOR}.{MINOR}.{PATCH}.{BUILD}";
            Title = string.IsNullOrEmpty(_Config.Title) ? appTitle : _Config.Title;
            Subtitle = $"{_Config.OldVersion} → {_Config.NewVersion}";
            AppInfo = _Config.AppInfo ?? "应用简介为空";
            VerInfo = _Config.VerInfo ?? "暂无版本升级说明";

            if (!string.IsNullOrWhiteSpace(_Config.Icon) && File.Exists(_Config.Icon))
            {
                Icon = new BitmapImage(new Uri(_Config.Icon, UriKind.RelativeOrAbsolute));
            }
            else
            {
                Icon = new BitmapImage(new Uri("pack://application:,,,/Upgrade.Wpf;component/Resources/logo.ico", UriKind.Absolute));
            }

            InitializeStepList();

            Notice = "初始化完成，点击开始升级";
        }

        private void InitializeStepList()
        {
            Steps.Clear();
            if (_Config.Steps == null)
            {
                return;
            }

            foreach (var step in _Config.Steps)
            {
                Steps.Add(new StepItemDvo
                {
                    StepNumber = Steps.Count + 1,
                    Title = string.IsNullOrEmpty(step.Title) ? UpgradeAction.GetActionTitle(step.Option) : step.Title,
                    Status = StepStatus.Pending,
                    Message = "等待执行"
                });
            }
        }
        #endregion

        #region 交互事件
        /// <summary>
        /// 窗口加载完成事件
        /// </summary>
        public void OnWindowLoaded()
        {
            if (_Config.AutoStart)
            {
                ExecuteStart(null);
            }
        }

        /// <summary>
        /// 开始升级按钮事件
        /// </summary>
        /// <param name="parameter"></param>
        private void ExecuteStart(object parameter)
        {
            if (IsRunning) return;

            IsRunning = true;
            StartEnabled = false;
            LaterEnabled = false;
            CloseEnabled = false;
            LaterVisibility = Visibility.Collapsed;
            StartButtonText = "升级中...";

            Notice = "开始升级流程...";

            Task.Run(async () =>
            {
                try
                {
                    await _Upgrade.StartAsync(_Config);

                    if (!_Config.AutoClose)
                    {
                        Notice = "升级完成！";
                        CloseEnabled = true;
                        StartVisibility = Visibility.Collapsed;
                        CloseVisibility = Visibility.Visible;
                    }
                    else
                    {
                        var step = 3;
                        while (step > 0)
                        {
                            Notice = $"升级完成，{step}秒后自动关闭...";
                            step -= 1;
                            await Task.Delay(1000);
                        }
                        Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
                    }
                }
                catch (OperationCanceledException)
                {
                    //Notice = "用户已取消升级";
                    Log("用户已取消升级");
                    LaterVisibility = Visibility.Collapsed;
                    StartVisibility = Visibility.Collapsed;
                    CloseEnabled = true;
                    CloseVisibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    //Notice = $"升级失败：{ex.Message}";
                    Log($"升级失败：{ex.Message}");
                    LaterVisibility = Visibility.Collapsed;
                    StartVisibility = Visibility.Collapsed;
                    CloseEnabled = true;
                    CloseVisibility = Visibility.Visible;
                }
                finally
                {
                    IsRunning = false;
                }
            });
        }

        /// <summary>
        /// 关闭窗口按钮事件
        /// </summary>
        /// <param name="parameter"></param>
        private void ExecuteClose(object parameter)
        {
            Dispose();
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 稍后提醒按钮事件
        /// </summary>
        /// <param name="parameter"></param>
        private void ExecuteLater(object parameter)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 关闭窗口窗体事件
        /// </summary>
        /// <param name="parameter"></param>
        private void ExecuteCloseWindow(object parameter)
        {
            Dispose();
            Application.Current.Shutdown();
        }
        #endregion

        #region 工具方法

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _Upgrade.Dispose();
        }
        #endregion

        #region 接口实现
        public void Log(string message)
        {
            Notice = message;
        }

        public void LogNewLine()
        {
            // 无需处理
        }

        public void LogStep(int stepNumber, int count, string message)
        {
            //Notice = $"[步骤{step}/{count}] " + message;
            Log($"[步骤{stepNumber}/{count}] " + message);
        }

        public void LogStepInfo(int step, string info, string message)
        {
            // 无需处理
            //Notice = $"[{info}] {message}";
        }

        public void LogStepWait(int step, int time, string message)
        {
            // 无需处理
            //Notice = message;
        }

        public void LogStepProgress(int step, int progress, string message)
        {
            Percent = progress;
            Notice = message;
        }

        public void LogStepStatus(int stepNumber, StepStatus status, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Steps.Count >= stepNumber)
                {
                    var step = Steps[stepNumber - 1];
                    //step.Title = title;
                    step.Message = message;
                    step.Status = status;

                    if (status == StepStatus.Running)
                    {
                        ScrollToStepRequested?.Invoke(stepNumber - 1);
                    }
                }
            });
        }

        public void ResetProgress()
        {
            Percent = 0;
        }
        #endregion
    }
}
