using System.ComponentModel;

namespace Com.Scm.Upgrade.Dvo
{
    public class ScmDvo : INotifyPropertyChanged
    {
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
