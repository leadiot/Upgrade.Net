using System.Windows;

namespace Com.Scm.Upgrade
{
    /// <summary>
    /// 启动窗口交互逻辑
    /// </summary>
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        public bool IsIndeterminate
        {
            get
            {
                return progressBar.IsIndeterminate;
            }
            set
            {
                progressBar.IsIndeterminate = value;
            }
        }

        /// <summary>
        /// 对外提供进度更新方法（需在 UI 线程调用）
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="status"></param>
        public void UpdateProgress(int progress, string status)
        {
            Dispatcher.Invoke(() => // 确保 UI 线程更新
            {
                progressBar.Value = progress;
                statusText.Text = status;
            });
        }

        /// <summary>
        /// 显示异常
        /// </summary>
        /// <param name="exp"></param>
        public void ShowError(Exception exp)
        {
            PlLogo.Visibility = Visibility.Collapsed;
            PlInfo.Visibility = Visibility.Visible;

            TbInfo.Text = exp.Message + Environment.NewLine + exp.StackTrace;
        }

        private void BtExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(1);
        }
    }
}
