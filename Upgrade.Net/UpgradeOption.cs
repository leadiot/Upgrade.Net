namespace Com.Scm.Upgrade
{
    public enum UpgradeOption
    {
        None,
        /// <summary>
        /// 下载文件
        /// </summary>
        Download,
        /// <summary>
        /// 上传文件
        /// </summary>
        Upload,
        /// <summary>
        /// 执行命令
        /// </summary>
        Command,
        /// <summary>
        /// 启动应用
        /// </summary>
        Launch,
        /// <summary>
        /// 压缩
        /// </summary>
        Zip,
        /// <summary>
        /// 解压
        /// </summary>
        Unzip,
        /// <summary>
        /// 删除目录
        /// </summary>
        DeleteDir,
        /// <summary>
        /// 删除文件
        /// </summary>
        DeleteDoc,
        /// <summary>
        /// 移动目录
        /// </summary>
        MoveDir,
        /// <summary>
        /// 移动文件
        /// </summary>
        MoveDoc,
        /// <summary>
        /// 复制目录
        /// </summary>
        CopyDir,
        /// <summary>
        /// 复制文件
        /// </summary>
        CopyDoc,
        /// <summary>
        /// 创建目录
        /// </summary>
        CreateDir,
        /// <summary>
        /// 创建文件
        /// </summary>
        CreateDoc,
        /// <summary>
        /// 更名目录
        /// </summary>
        RenameDir,
        /// <summary>
        /// 更名文件
        /// </summary>
        RenameDoc
    }
}
