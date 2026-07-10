using System.ComponentModel;

namespace Com.Scm.Upgrade.Dvo
{
    public class MainWindowDvo : INotifyPropertyChanged
    {
        private string title;
        public string Title { get { return title; } set { title = value; OnPropertyChanged(nameof(Title)); } }

        private string version;
        public string Version { get { return version; } set { version = value; OnPropertyChanged(nameof(Version)); } }

        private string appInfo;
        public string AppInfo { get { return appInfo; } set { appInfo = value; OnPropertyChanged(nameof(AppInfo)); } }

        private string verInfo;
        public string VerInfo { get { return verInfo; } set { verInfo = value; OnPropertyChanged(nameof(VerInfo)); } }

        private string status;
        public string Status { get { return status; } set { status = value; OnPropertyChanged(nameof(Status)); } }

        private double ratio;
        public double Ratio { get { return ratio; } set { ratio = value; OnPropertyChanged(nameof(Ratio)); } }

        private bool startEnabled = true;
        public bool StartEnabled { get { return startEnabled; } set { startEnabled = value; OnPropertyChanged(nameof(StartEnabled)); } }

        private bool pauseEnabled;
        public bool PauseEnabled { get { return pauseEnabled; } set { pauseEnabled = value; OnPropertyChanged(nameof(PauseEnabled)); } }

        private bool cancelEnabled;
        public bool CancelEnabled { get { return cancelEnabled; } set { cancelEnabled = value; OnPropertyChanged(nameof(CancelEnabled)); } }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void SetProperty(ref object property, object value)
        {
            if (property != value)
            {
                property = value;
                OnPropertyChanged(property.GetType().Name);
            }
        }
    }
}
