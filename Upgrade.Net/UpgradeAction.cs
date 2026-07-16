using Com.Scm.Upgrade.Config;

namespace Com.Scm.Upgrade
{
    public class UpgradeAction
    {
        public UpgradeOption Option { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public Func<StepConfig, int, UpgradeResult> Execute { get; set; }

        public static string GetActionTitle(UpgradeOption option) => option switch
        {
            UpgradeOption.Download => "下载文件",
            UpgradeOption.Upload => "上传文件",
            UpgradeOption.Command => "执行命令",
            UpgradeOption.Launch => "执行程序",
            UpgradeOption.Zip => "压缩文件",
            UpgradeOption.Unzip => "解压文件",
            UpgradeOption.MoveDir => "移动目录",
            UpgradeOption.MoveDoc => "移动文件",
            UpgradeOption.CopyDir => "复制目录",
            UpgradeOption.CopyDoc => "复制文件",
            UpgradeOption.CreateDir => "创建目录",
            UpgradeOption.CreateDoc => "创建文件",
            UpgradeOption.DeleteDir => "删除目录",
            UpgradeOption.DeleteDoc => "删除文件",
            UpgradeOption.RenameDir => "更名目录",
            UpgradeOption.RenameDoc => "更名文件",
            _ => "未知操作"
        };
    }
}
