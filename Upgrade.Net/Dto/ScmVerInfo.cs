namespace Com.Scm.Upgrade.Dto
{
    public class ScmVerInfo
    {
        /// <summary>
        /// 发行版本
        /// </summary>
        public string ver_info { get; set; }

        /// <summary>
        /// 发行日期
        /// </summary>
        public string ver_date { get; set; }

        /// <summary>
        /// 发行代号
        /// </summary>
        public string ver_code { get; set; }

        /// <summary>
        /// 最小版本
        /// </summary>
        public string ver_min { get; set; }

        /// <summary>
        /// 最大版本
        /// </summary>
        public string ver_max { get; set; }

        /// <summary>
        /// 主要版本
        /// </summary>
        public int major { get; set; }

        /// <summary>
        /// 次要版本
        /// </summary>
        public int minor { get; set; }

        /// <summary>
        /// 修订版本
        /// </summary>
        public int patch { get; set; }

        /// <summary>
        /// 构建版本
        /// </summary>
        public int build { get; set; }

        /// <summary>
        /// 研发阶段
        /// </summary>
        public int phase { get; set; }

        /// <summary>
        /// 强制更新
        /// </summary>
        public bool forced { get; set; }

        /// <summary>
        /// 当前版本
        /// </summary>
        public bool current { get; set; }

        /// <summary>
        /// 下载路径
        /// </summary>
        public string url { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        public int size { get; set; }

        /// <summary>
        /// 更新事项
        /// </summary>
        public string remark { get; set; }

        public bool IsNewer(int build)
        {
            return this.build > build;
        }
    }
}
