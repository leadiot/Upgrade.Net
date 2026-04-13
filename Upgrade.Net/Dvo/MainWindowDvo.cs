using System.ComponentModel;

namespace Com.Scm.Upgrade.Dvo
{
    public class MainWindowDvo : INotifyPropertyChanged
    {
        private string info;
        public string Info
        {
            get
            {
                return info;
            }
            set
            {
                info = value;
                OnPropertyChanged(nameof(Info));
            }
        }

        private string status;
        public string Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        private double ratio;
        public double Ratio
        {
            get
            {
                return ratio;
            }
            set
            {
                ratio = value;
                OnPropertyChanged(nameof(Ratio));
            }
        }

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
