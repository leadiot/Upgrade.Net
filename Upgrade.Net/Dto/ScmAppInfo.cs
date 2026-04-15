namespace Com.Scm.Upgrade.Dto
{
    public class ScmAppInfo
    {
        public int types { get; set; }

        /// <summary>
        /// 应用代码
        /// </summary>
        public string code { get; set; }

        /// <summary>
        /// 应用名称
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 应用介绍
        /// </summary>
        public string content { get; set; }
    }
}
