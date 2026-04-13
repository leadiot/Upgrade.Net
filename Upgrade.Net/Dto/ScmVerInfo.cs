namespace Com.Scm.Upgrade.Dto
{
    public class ScmVerInfo
    {
        public string ver { get; set; }

        public string date { get; set; }

        public string build { get; set; }

        public string ver_min { get; set; }

        public string ver_max { get; set; }

        public bool alpha { get; set; }

        public bool beta { get; set; }

        public bool forced { get; set; }

        public bool current { get; set; }

        public string url { get; set; }

        public string remark { get; set; }
    }
}
