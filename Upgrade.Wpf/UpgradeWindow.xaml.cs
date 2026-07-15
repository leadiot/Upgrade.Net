using Com.Scm.Upgrade.Config;
using System.Windows;
using System.Windows.Input;

namespace Com.Scm.Upgrade
{
    public partial class UpgradeWindow : Window
    {
        private UpgradeWindowViewModel _viewModel;

        public UpgradeWindow(UpgradeConfig config)
        {
            InitializeComponent();
            _viewModel = new UpgradeWindowViewModel(config);
            DataContext = _viewModel;

            var title = $"Upgrade.Wpf v{UpgradeWindowViewModel.MAJOR}.{UpgradeWindowViewModel.MINOR}.{UpgradeWindowViewModel.PATCH}.{UpgradeWindowViewModel.BUILD}";
            this.Title = title;
            TbTitle.Text = title;

            _viewModel.ScrollToStepRequested += ScrollToStep;
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel?.OnWindowLoaded();
        }

        private void ScrollToStep(int stepIndex)
        {
            if (StepItemsControl.Items.Count == 0 || stepIndex < 0 || stepIndex >= StepItemsControl.Items.Count)
                return;

            var container = StepItemsControl.ItemContainerGenerator.ContainerFromIndex(stepIndex) as FrameworkElement;
            container?.BringIntoView();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && WindowState != WindowState.Maximized)
            {
                DragMove();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel?.Dispose();
        }
    }
}
