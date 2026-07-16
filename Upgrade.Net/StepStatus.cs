namespace Com.Scm.Upgrade
{
    public enum StepStatus
    {
        /// <summary>
        /// 待执行
        /// </summary>
        Pending,
        /// <summary>
        /// 执行中
        /// </summary>
        Running,
        /// <summary>
        /// 成功
        /// </summary>
        Success,
        /// <summary>
        /// 失败
        /// </summary>
        Failed,
        /// <summary>
        /// 跳过
        /// </summary>
        Skipped
    }
}
