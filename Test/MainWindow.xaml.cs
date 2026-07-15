using Com.Scm.Upgrade.Config;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Upgrade.Net;

namespace Test
{
    public partial class MainWindow : Window, UpgradeView
    {
        private Com.Scm.Upgrade.Upgrade _upgrade;
        private Button _startButton;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 借助Upgrade.Wpf升级应用自身
        /// 说明：
        /// 执行此操作时，请将Upgrade.exe的相关执行程序放置到指定路径（如 .\Upgrade ）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpgradeSelft_Click(object sender, RoutedEventArgs e)
        {
            var baseDir = "Upgrade";
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }
            var file = Path.Combine(baseDir, "Upgrade.Wpf.exe");
            if (!File.Exists(file))
            {
                Log("Upgrade.Wpf.exe文件不存在");
                return;
            }

            var config = CreateUpgradeTask();
            config.Save(baseDir);

            try
            {
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = file
                };
                var process = System.Diagnostics.Process.Start(processStartInfo);
                if (process == null)
                {
                    Log("无法启动Upgrade.Wpf");
                    return;
                }

                this.Close();
            }
            catch (Exception exp)
            {
                Log(exp.Message);
            }
        }

        /// <summary>
        /// 使用Upgrade.Net升级其它应用
        /// 说明：
        /// 执行此操作时，无特定要求，但是无法对自身应用进行升级。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UpgradeOther_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                _startButton = button;
            }

            StatusLabel.Content = "升级中...";
            StatusLabel.Foreground = Brushes.Orange;
            LogTextBox.Clear();

            Log("========================================");
            Log("Upgrade.Net 使用示例");
            Log("========================================");

            try
            {
                var config = CreateUpgradeTask();

                _upgrade = new Com.Scm.Upgrade.Upgrade(this);
                await _upgrade.StartAsync(config);

                ShowStatus("升级完成", Color.FromRgb(56, 161, 105));
                if (_startButton != null)
                    _startButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ShowStatus("升级失败", Color.FromRgb(217, 74, 74));
                if (_startButton != null)
                    _startButton.IsEnabled = true;
            }
        }

        private void ShowStatus(string text, Color color)
        {
            Dispatcher.Invoke(() =>
            {
                StatusLabel.Content = text;
                StatusLabel.Foreground = new SolidColorBrush(color);
            });
        }

        /// <summary>
        /// 创建升级计划
        /// </summary>
        /// <returns></returns>
        private UpgradeConfig CreateUpgradeTask()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "UpgradeDemo");
            Directory.CreateDirectory(tempDir);

            return new UpgradeConfig
            {
                Title = "应用升级程序",
                OldVersion = "1.0.0",
                NewVersion = "2.0.0",
                ShowSteps = true,
                Steps = new List<StepConfig>
                {
                    StepConfig.NewCreateDirStep("创建临时目录", tempDir),
                    StepConfig.NewDownloadStep("下载更新包", "https://example.com/upgrade.zip", Path.Combine(tempDir, "upgrade.zip")),
                    StepConfig.NewZipStep("备份现有文件",Directory.GetCurrentDirectory(),Path.Combine(tempDir, "backup.zip")),
                    StepConfig.NewUnzipStep("解压更新包",Path.Combine(tempDir, "upgrade.zip"),Directory.GetCurrentDirectory(), true),
                    StepConfig.NewDeleteDocStep("清理临时文件",Path.Combine(tempDir, "upgrade.zip")),
                    StepConfig.NewDeleteDirStep("删除临时目录", tempDir),
                    StepConfig.NewLaunchStep("启动应用程序", "notepad.exe", "")
                }
            };
        }

        private void ResetLog_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
            ShowStatus("等待升级", Color.FromRgb(44, 62, 80));
        }

        #region 接口实现
        public void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText(message + "\r\n");
                LogTextBox.ScrollToEnd();
            });
        }

        public void LogNewLine()
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText("\r\n");
                LogTextBox.ScrollToEnd();
            });
        }

        public void LogStep(int step, int count, string message)
        {
            Log($"[步骤 {step}/{count}] {message}");
        }

        public void LogStepInfo(string info, string message)
        {
            Log($"  [{info}] {message}");
        }

        public void LogStepWait(int time, string message)
        {
            Log($"  [等待] {message}");
        }

        public void LogStepProgress(int progress, string message)
        {
            Dispatcher.Invoke(() =>
            {
                var lines = LogTextBox.Text.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (lines.Count > 0 && lines.Last().Contains("[进度]"))
                {
                    lines.RemoveAt(lines.Count - 1);
                }
                LogTextBox.Text = string.Join("\r\n", lines);
                LogTextBox.AppendText($"  [进度] {message}\r\n");
                LogTextBox.ScrollToEnd();
            });
        }

        public void LogStepStatus(int stepNumber, StepStatus status, string title, string message)
        {
            Log($"  [状态] {message}");
        }

        public void ResetProgress()
        {
        }
        #endregion
    }
}