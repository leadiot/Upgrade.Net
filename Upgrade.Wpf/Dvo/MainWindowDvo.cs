using System.ComponentModel;

namespace Com.Scm.Upgrade.Dvo
{
    public class MainWindowDvo : INotifyPropertyChanged
    {
        private string content;
        public string Content { get { return content; } set { content = value; OnPropertyChanged(nameof(Content)); } }

        private string info;
        public string Info { get { return info; } set { info = value; OnPropertyChanged(nameof(Info)); } }

        private string status;
        public string Status { get { return status; } set { status = value; OnPropertyChanged(nameof(Status)); } }

        private double ratio;
        public double Percent { get { return ratio; } set { ratio = value; OnPropertyChanged(nameof(Percent)); } }


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
