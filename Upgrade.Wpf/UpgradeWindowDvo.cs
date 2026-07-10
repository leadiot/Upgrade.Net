using Com.Scm.Upgrade.Dvo;

namespace Com.Scm.Upgrade
{
    public class UpgradeWindowDvo : ScmDvo
    {
        /// <summary>
        /// 主标题
        /// </summary>
        private string title;
        public string Title { get { return title; } set { title = value; OnPropertyChanged(nameof(Title)); } }

        /// <summary>
        /// 子标题
        /// </summary>
        private string subtitle;
        public string Subtitle { get { return subtitle; } set { subtitle = value; OnPropertyChanged(nameof(Subtitle)); } }

        /// <summary>
        /// 应用信息
        /// </summary>
        private string appInfo;
        public string AppInfo { get { return appInfo; } set { appInfo = value; OnPropertyChanged(nameof(AppInfo)); } }

        /// <summary>
        /// 版本信息
        /// </summary>
        private string verInfo;
        public string VerInfo { get { return verInfo; } set { verInfo = value; OnPropertyChanged(nameof(VerInfo)); } }

        /// <summary>
        /// 消息提示
        /// </summary>
        private string notice;
        public string Notice { get { return notice; } set { notice = value; OnPropertyChanged(nameof(Notice)); } }

        /// <summary>
        /// 进度百分比，范围0-100
        /// </summary>
        private double percent;
        public double Percent { get { return percent; } set { percent = value; OnPropertyChanged(nameof(Percent)); } }

    }
}
