using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Com.Scm.Upgrade
{
    public partial class UpdateWindow : Window
    {
        private DispatcherTimer _progressTimer;
        private double _currentProgress = 0;

        public UpdateWindow()
        {
            InitializeComponent();

            // 监听窗口状态变化，防止最大化时拖拽异常
            this.StateChanged += (s, e) =>
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    // 最大化时禁止拖拽标题栏
                    this.MouseDown -= TitleBar_MouseLeftButtonDown;
                }
                else
                {
                    this.MouseDown += TitleBar_MouseLeftButtonDown;
                }
            };
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
        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            BtnUpdate.IsEnabled = false;
            BtnUpdate.Content = "更新中...";
            BtnLater.Visibility = Visibility.Collapsed;
            ProgressArea.Visibility = Visibility.Visible;

            _currentProgress = 0;
            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _progressTimer.Tick += (s, args) =>
            {
                _currentProgress += new Random().Next(3, 15);
                if (_currentProgress > 100) _currentProgress = 100;

                UpdateProgress.Value = _currentProgress;
                ProgressText.Text = $"正在下载并应用更新... {(int)_currentProgress}%";

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
            ProgressText.Text = "正在验证文件完整性...";

            var delayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
            delayTimer.Tick += (s, args) =>
            {
                delayTimer.Stop();

                ProgressArea.Visibility = Visibility.Collapsed;
                SuccessStatus.Visibility = Visibility.Visible;

                BtnUpdate.Content = "重启应用";
                BtnUpdate.IsEnabled = true;
                BtnUpdate.Background = FindResource("SuccessGreen") as System.Windows.Media.SolidColorBrush;

                BtnUpdate.Click -= BtnUpdate_Click;
                BtnUpdate.Click += (sender, e) =>
                {
                    MessageBox.Show("🚀 模拟重启：应用将关闭并加载新版本！", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    Application.Current.Shutdown();
                };
            };
            delayTimer.Start();
        }

        // 5. 稍后提醒
        private void BtnLater_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}