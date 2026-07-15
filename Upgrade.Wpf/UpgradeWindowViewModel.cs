using Com.Scm.Upgrade.Config;
using Com.Scm.Upgrade.Dvo;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Upgrade.Net;

namespace Com.Scm.Upgrade
{
    public class UpgradeWindowViewModel : ScmDvo, UpgradeView
    {
        public const int MAJOR = 2;
        public const int MINOR = 2;
        public const int PATCH = 3;
        public const int BUILD = 4;
        public const string RELEASE = "2026-07-15";

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

        public ICommand StartCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand LaterCommand { get; }
        public ICommand CloseWindowCommand { get; }

        public event Action<int> ScrollToStepRequested;

        private readonly UpgradeConfig _config;
        private readonly Upgrade _upgrade;
        private StreamWriter _writer;

        public UpgradeWindowViewModel(UpgradeConfig config)
        {
            _config = config;
            _upgrade = new Upgrade(this);

            StartCommand = new RelayCommand(ExecuteStart);
            CloseCommand = new RelayCommand(ExecuteClose);
            LaterCommand = new RelayCommand(ExecuteLater);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);

            Initialize();
        }

        public void OnWindowLoaded()
        {
            if (_config.AutoStart)
            {
                ExecuteStart(null);
            }
        }

        private void Initialize()
        {
            _writer = new StreamWriter("Upgrade.log") { AutoFlush = true };

            var appTitle = $"Upgrade.Wpf v{MAJOR}.{MINOR}.{PATCH}.{BUILD}";
            Title = string.IsNullOrEmpty(_config.Title) ? appTitle : _config.Title;
            Subtitle = $"{_config.OldVersion} → {_config.NewVersion}";
            AppInfo = _config.AppInfo ?? "应用简介为空";
            VerInfo = _config.VerInfo ?? "暂无版本升级说明";

            if (!string.IsNullOrWhiteSpace(_config.Icon) && File.Exists(_config.Icon))
            {
                Icon = new BitmapImage(new Uri(_config.Icon, UriKind.RelativeOrAbsolute));
            }
            else
            {
                Icon = new BitmapImage(new Uri("pack://application:,,,/Upgrade.Wpf;component/Resources/logo.ico", UriKind.Absolute));
            }

            if (string.IsNullOrEmpty(_config.InstallPath))
            {
                _config.InstallPath = AppDomain.CurrentDomain.BaseDirectory;
            }

            InitializeStepList();

            Notice = "初始化完成，点击开始升级";
        }

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
                    await _upgrade.StartAsync(_config);

                    if (!_config.AutoClose)
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

        private void ExecuteClose(object parameter)
        {
            Application.Current.Shutdown();
        }

        private void ExecuteLater(object parameter)
        {
            Application.Current.Shutdown();
        }

        private void ExecuteCloseWindow(object parameter)
        {
            Dispose();
            Application.Current.Shutdown();
        }

        private void InitializeStepList()
        {
            Steps.Clear();
            if (_config.Steps != null)
            {
                foreach (var step in _config.Steps)
                {
                    Steps.Add(new StepItemDvo
                    {
                        StepNumber = Steps.Count + 1,
                        Title = string.IsNullOrEmpty(step.Title) ? GetActionTitle(step.Option) : step.Title,
                        Status = StepStatus.Pending,
                        Message = "等待执行"
                    });
                }
            }
        }

        private string GetActionTitle(UpgradeOption option) => option switch
        {
            UpgradeOption.Download => "下载文件",
            UpgradeOption.Command => "执行命令",
            UpgradeOption.Launch => "执行程序",
            UpgradeOption.Zip => "压缩文件",
            UpgradeOption.Unzip => "解压文件",
            UpgradeOption.MoveDir => "移动目录",
            UpgradeOption.MoveDoc => "移动文件",
            UpgradeOption.CopyDir => "复制目录",
            UpgradeOption.CopyDoc => "复制文件",
            UpgradeOption.CreateDir => "创建目录",
            UpgradeOption.CreateDoc => "创建文件",
            UpgradeOption.DeleteDir => "删除目录",
            UpgradeOption.DeleteDoc => "删除文件",
            UpgradeOption.RenameDir => "重命名目录",
            UpgradeOption.RenameDoc => "重命名文件",
            _ => "未知操作"
        };

        private void LogToFile(string message)
        {
            _writer?.WriteLine(message);
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Close();
                _writer.Dispose();
                _writer = null;
            }
        }

        public void Log(string message)
        {
            Notice = message;
            LogToFile(message);
        }

        public void LogNewLine()
        {
        }

        public void LogStep(int step, int count, string message)
        {
            //Notice = $"[步骤{step}/{count}] " + message;
            Log($"[步骤{step}/{count}] " + message);
        }

        public void LogStepInfo(string info, string message)
        {
            //Notice = $"[{info}] {message}";
            LogToFile($"[{info}] {message}");
        }

        public void LogStepWait(int time, string message)
        {
            //Notice = message;
            LogToFile(message);
        }

        public void LogStepProgress(int progress, string message)
        {
            Percent = progress;
            Notice = message;
        }

        public void ResetProgress()
        {
            Percent = 0;
        }

        public void LogStepStatus(int stepNumber, StepStatus status, string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Steps.Count >= stepNumber)
                {
                    var step = Steps[stepNumber - 1];
                    step.Title = title;
                    step.Message = message;
                    step.Status = status;

                    if (status == StepStatus.Running)
                    {
                        ScrollToStepRequested?.Invoke(stepNumber - 1);
                    }
                }
            });
        }
    }
}
