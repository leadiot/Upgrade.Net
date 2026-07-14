using Com.Scm.Upgrade.Config;

namespace Com.Scm.Upgrade
{
    public class Upgrade
    {
        private UpgradeConfig _Config;
        private List<UpgradeAction> _Actions = new List<UpgradeAction>();

        public void Init(UpgradeConfig config)
        {
            foreach (var step in config.Steps)
            {
                _Actions.Add(ParseSteps(step));
            }
        }

        public UpgradeAction ParseSteps(StepConfig config)
        {
            return null;
        }

        private void Log(string message)
        {
        }
    }

    public class UpgradeAction
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public long WaitTime { get; set; }

        public UpgradeAction() { }

        public void Execute()
        {
        }
    }

    public class CopyFileAction : UpgradeAction
    {
        public string Source { get; set; }
        public string Destination { get; set; }

        public bool Overwrite { get; set; }
    }

    public class MoveFileAction : UpgradeAction
    {
        public string Source { get; set; }
        public string Destination { get; set; }

        public bool Overwrite { get; set; }
    }

    public class DeleteFileAction : UpgradeAction
    {
        public string Path { get; set; }
    }

    public class RenameFileAction : UpgradeAction
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public bool Overwrite { get; set; }
    }

    public class CreateDirectoryAction : UpgradeAction
    {
        public string Path { get; set; }
    }

    public class DeleteDirectoryAction : UpgradeAction
    {
        public string Path { get; set; }
        public bool Recursive { get; set; }
    }

    public class RenameDirectoryAction : UpgradeAction
    {
        public string Source { get; set; }
        public string Destination { get; set; }
    }

    public class ZipPackageAction : UpgradeAction
    {
        public string FilePath { get; set; }
        public string ExtractPath { get; set; }
    }

    public class UnzipPackageAction : UpgradeAction
    {
        public string FilePath { get; set; }
        public string ExtractPath { get; set; }
    }
}
