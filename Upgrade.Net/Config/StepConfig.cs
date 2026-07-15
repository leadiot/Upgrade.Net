namespace Com.Scm.Upgrade.Config
{
    public class StepConfig
    {
        #region 公共属性
        /// <summary>
        /// 步骤标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 步骤说明
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public UpgradeOption Option { get; set; }

        /// <summary>
        /// （操作完成后）等待时间
        /// </summary>
        public int WaitTime { get; set; }

        /// <summary>
        /// 错误时是否继续
        /// </summary>
        public bool ContinueOnError { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 重试等待时间（单位毫秒）
        /// </summary>
        public int RetryDelay { get; set; } = 1000;
        #endregion

        #region 扩展属性
        /// <summary>
        /// 来源文件或路径
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 目的文件或路径
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// 目标文件
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// 目标路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 下载链接
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 执行命令
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// 命令参数
        /// </summary>
        public string Args { get; set; }

        /// <summary>
        /// 原名称
        /// </summary>
        public string OldName { get; set; }

        /// <summary>
        /// 新名称
        /// </summary>
        public string NewName { get; set; }

        /// <summary>
        /// 是否覆盖目标
        /// </summary>
        public bool Overwrite { get; set; } = true;
        #endregion

        /// <summary>
        /// 下载步骤
        /// </summary>
        /// <param name="url">下载链接</param>
        /// <param name="file">保存路径</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewDownloadStep(string url, string file)
        {
            return new StepConfig
            {
                Option = UpgradeOption.Download,
                Url = url,
                File = file
            };
        }

        /// <summary>
        /// 命令步骤
        /// </summary>
        /// <param name="command">要执行的命令</param>
        /// <param name="args">命令参数</param>
        /// <param name="path">工作目录（可选，默认为当前目录）</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewCommandStep(string command, string args, string path = null)
        {
            return new StepConfig
            {
                Option = UpgradeOption.Command,
                Command = command,
                Args = args,
                Path = path,
            };
        }

        /// <summary>
        /// 压缩步骤
        /// </summary>
        /// <param name="source">源文件或目录路径</param>
        /// <param name="destination">目标压缩文件路径</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewZipStep(string source, string destination)
        {
            return new StepConfig
            {
                Option = UpgradeOption.Zip,
                Source = source,
                Destination = destination
            };
        }

        /// <summary>
        /// 解压步骤
        /// </summary>
        /// <param name="source">源压缩文件路径</param>
        /// <param name="destination">解压目标目录</param>
        /// <param name="overwrite">是否覆盖已存在的文件（默认true）</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewUnzipStep(string source, string destination, bool overwrite = true)
        {
            return new StepConfig
            {
                Option = UpgradeOption.Unzip,
                Source = source,
                Destination = destination,
                Overwrite = overwrite
            };
        }

        /// <summary>
        /// 移动目录步骤
        /// </summary>
        /// <param name="source">源目录路径</param>
        /// <param name="destination">目标目录路径</param>
        /// <param name="overwrite">是否覆盖已存在的文件（默认true）</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewMoveDirStep(string source, string destination, bool overwrite = true)
        {
            return new StepConfig
            {
                Option = UpgradeOption.MoveDir,
                Source = source,
                Destination = destination,
                Overwrite = overwrite
            };
        }

        /// <summary>
        /// 移动文件步骤
        /// </summary>
        /// <param name="source">源文件路径</param>
        /// <param name="destination">目标文件路径</param>
        /// <param name="overwrite">是否覆盖已存在的文件（默认true）</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewMoveDocStep(string source, string destination, bool overwrite = true)
        {
            return new StepConfig
            {
                Option = UpgradeOption.MoveDoc,
                Source = source,
                Destination = destination,
                Overwrite = overwrite
            };
        }

        /// <summary>
        /// 复制目录步骤
        /// </summary>
        /// <param name="source">源目录路径</param>
        /// <param name="destination">目标目录路径</param>
        /// <param name="overwrite">是否覆盖已存在的文件（默认true）</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewCopyDirStep(string source, string destination, bool overwrite = true)
        {
            return new StepConfig
            {
                Option = UpgradeOption.CopyDir,
                Source = source,
                Destination = destination,
                Overwrite = overwrite
            };
        }

        /// <summary>
        /// 复制文件步骤
        /// </summary>
        /// <param name="source">源文件路径</param>
        /// <param name="destination">目标文件路径</param>
        /// <param name="overwrite">是否覆盖已存在的文件（默认true）</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewCopyDocStep(string source, string destination, bool overwrite = true)
        {
            return new StepConfig
            {
                Option = UpgradeOption.CopyDoc,
                Source = source,
                Destination = destination,
                Overwrite = overwrite
            };
        }

        /// <summary>
        /// 创建目录步骤
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewCreateDirStep(string path)
        {
            return new StepConfig
            {
                Option = UpgradeOption.CreateDir,
                Path = path
            };
        }

        /// <summary>
        /// 创建文件步骤
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="overwrite">文件已存在时是否覆盖（默认true）</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewCreateDocStep(string path, bool overwrite = true)
        {
            return new StepConfig
            {
                Option = UpgradeOption.CreateDoc,
                Path = path,
                Overwrite = overwrite
            };
        }

        /// <summary>
        /// 删除目录步骤
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewDeleteDirStep(string path)
        {
            return new StepConfig
            {
                Option = UpgradeOption.DeleteDir,
                Path = path
            };
        }

        /// <summary>
        /// 删除文件步骤
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewDeleteDocStep(string path)
        {
            return new StepConfig
            {
                Option = UpgradeOption.DeleteDoc,
                Path = path
            };
        }

        /// <summary>
        /// 重命名目录步骤
        /// </summary>
        /// <param name="oldName">原目录路径</param>
        /// <param name="newName">新目录路径</param>
        /// <param name="overwrite">新目录已存在时是否覆盖（默认true）</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewRenameDirStep(string oldName, string newName, bool overwrite = true)
        {
            return new StepConfig
            {
                Option = UpgradeOption.RenameDir,
                OldName = oldName,
                NewName = newName,
                Overwrite = overwrite
            };
        }

        /// <summary>
        /// 重命名文件步骤
        /// </summary>
        /// <param name="oldName">原文件路径</param>
        /// <param name="newName">新文件路径</param>
        /// <param name="overwrite">新文件已存在时是否覆盖（默认true）</param>
        /// <returns>步骤配置</returns>
        public static StepConfig NewRenameDocStep(string oldName, string newName, bool overwrite = true)
        {
            return new StepConfig
            {
                Option = UpgradeOption.RenameDoc,
                OldName = oldName,
                NewName = newName,
                Overwrite = overwrite
            };
        }
    }
}
