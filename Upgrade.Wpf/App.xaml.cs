using Com.Scm.Upgrade.Config;
using System.Windows;

namespace Com.Scm.Upgrade
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                UpgradeConfig settings = UpgradeConfig.Load();
                if (settings == null)
                {
                    settings = new UpgradeConfig();
                    settings.LoadDefault();
                }

                var mainWindow = new UpgradeWindow(settings);
                mainWindow.Show();
            }
            catch
            {
                Shutdown();
            }
        }
    }
}
