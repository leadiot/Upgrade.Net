using Com.Scm.Upgrade.Dvo;

namespace Com.Scm.Upgrade
{
    public enum StepStatus
    {
        Pending,
        Running,
        Success,
        Failed,
        Skipped
    }

    public class StepItemDvo : ScmDvo
    {
        private int stepNumber;
        public int StepNumber
        {
            get { return stepNumber; }
            set { stepNumber = value; OnPropertyChanged(nameof(StepNumber)); }
        }

        private string title;
        public string Title
        {
            get { return title; }
            set { title = value; OnPropertyChanged(nameof(Title)); }
        }

        private string description;
        public string Description
        {
            get { return description; }
            set { description = value; OnPropertyChanged(nameof(Description)); }
        }

        private StepStatus status;
        public StepStatus Status
        {
            get { return status; }
            set { status = value; OnPropertyChanged(nameof(Status)); }
        }

        private string message;
        public string Message
        {
            get { return message; }
            set { message = value; OnPropertyChanged(nameof(Message)); }
        }

        public string StatusIcon
        {
            get
            {
                switch (Status)
                {
                    case StepStatus.Pending:
                        return "○";
                    case StepStatus.Running:
                        return "●";
                    case StepStatus.Success:
                        return "✓";
                    case StepStatus.Failed:
                        return "✕";
                    case StepStatus.Skipped:
                        return "→";
                    default:
                        return "○";
                }
            }
        }

        public string StatusColor
        {
            get
            {
                switch (Status)
                {
                    case StepStatus.Pending:
                        return "#7A8B9E";
                    case StepStatus.Running:
                        return "#2A6BC6";
                    case StepStatus.Success:
                        return "#38A169";
                    case StepStatus.Failed:
                        return "#D94A4A";
                    case StepStatus.Skipped:
                        return "#A0AEC0";
                    default:
                        return "#7A8B9E";
                }
            }
        }
    }
}
